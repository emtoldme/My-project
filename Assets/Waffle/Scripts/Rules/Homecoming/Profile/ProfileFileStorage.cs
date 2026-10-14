using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Waffle.Homecoming.Profile
{
    /// <summary>
    /// File-based implementation of IProfileStoragePort.
    /// Provides atomic write via temporary file, replacement of main file,
    /// backup file management, corruption fallback, and version guard.
    /// Pure C# (System.IO) with no UnityEngine dependencies.
    /// </summary>
    public sealed class ProfileFileStorage : IProfileStoragePort
    {
        private readonly string mainFilePath;
        private readonly string tempFilePath;
        private readonly string backupFilePath;
        private readonly string corruptedFilePath;

        public ProfileFileStorage(string directoryPath, string fileName = "homecoming_profile.json")
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be empty", nameof(directoryPath));

            mainFilePath = Path.Combine(directoryPath, fileName);
            tempFilePath = Path.Combine(directoryPath, fileName + ".tmp");
            backupFilePath = Path.Combine(directoryPath, fileName + ".bak");
            corruptedFilePath = Path.Combine(directoryPath, fileName + ".corrupted");
        }

        public ProfileLoadResult Load()
        {
            CleanUpLeftoverTempFile();

            if (!File.Exists(mainFilePath))
            {
                if (File.Exists(backupFilePath))
                {
                    // Main is missing but backup exists -> recover from backup
                    return TryLoadFromBackup("Main save file missing, recovered from backup.");
                }
                return ProfileLoadResult.NewProfile();
            }

            // Check file size
            var fi = new FileInfo(mainFilePath);
            if (fi.Length > ProfileConfig.MaxFileSizeBytes)
            {
                PreserveCorruptedFile(mainFilePath);
                return TryLoadFromBackup("Main file exceeded maximum size limit. Recovered from backup.");
            }

            try
            {
                string json = File.ReadAllText(mainFilePath, Encoding.UTF8);
                // Validate schema version before full parse
                int version = PeekSchemaVersion(json);
                if (version > ProfileConfig.CurrentSchemaVersion)
                {
                    return new ProfileLoadResult(ProfileLoadStatus.VersionTooNewReadOnly, json,
                        "Save file version (" + version + ") is newer than supported version (" + ProfileConfig.CurrentSchemaVersion + "). Running in read-only mode.");
                }

                // Verify parsability and validity
                ProfileData.FromJson(json);
                return new ProfileLoadResult(ProfileLoadStatus.Success, json, "Profile loaded successfully.");
            }
            catch (Exception ex)
            {
                // Main file is corrupted!
                PreserveCorruptedFile(mainFilePath);
                return TryLoadFromBackup("Main profile file was corrupted (" + ex.Message + "). Recovered from backup.");
            }
        }

        private ProfileLoadResult TryLoadFromBackup(string reason)
        {
            if (!File.Exists(backupFilePath))
            {
                return new ProfileLoadResult(ProfileLoadStatus.CorruptedAllPreserved, null,
                    reason + " However, backup file is also missing or was never created. Fresh profile started, corrupted file preserved.",
                    corruptedFilePath);
            }

            try
            {
                var fi = new FileInfo(backupFilePath);
                if (fi.Length > ProfileConfig.MaxFileSizeBytes)
                {
                    PreserveCorruptedFile(backupFilePath);
                    return new ProfileLoadResult(ProfileLoadStatus.CorruptedAllPreserved, null,
                        "Backup file also exceeded size limit. Fresh profile started.", corruptedFilePath);
                }

                string backupJson = File.ReadAllText(backupFilePath, Encoding.UTF8);
                int version = PeekSchemaVersion(backupJson);
                if (version > ProfileConfig.CurrentSchemaVersion)
                {
                    return new ProfileLoadResult(ProfileLoadStatus.VersionTooNewReadOnly, backupJson,
                        "Backup file version is newer than supported version.");
                }

                ProfileData.FromJson(backupJson);

                // Restore main file from valid backup
                try
                {
                    File.Copy(backupFilePath, mainFilePath, true);
                }
                catch { /* Ignore non-fatal copy failure */ }

                return new ProfileLoadResult(ProfileLoadStatus.RecoveredFromBackup, backupJson, reason);
            }
            catch (Exception ex)
            {
                PreserveCorruptedFile(backupFilePath);
                return new ProfileLoadResult(ProfileLoadStatus.CorruptedAllPreserved, null,
                    "Both main save and backup file are corrupted (" + ex.Message + "). Fresh profile started, corrupted files preserved.",
                    corruptedFilePath);
            }
        }

        public ProfileSaveResult SaveAtomic(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
                return ProfileSaveResult.Fail("Payload is empty");

            try
            {
                string dir = Path.GetDirectoryName(mainFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Step 1: Write completely to temporary file
                CleanUpLeftoverTempFile();
                File.WriteAllText(tempFilePath, jsonPayload, Encoding.UTF8);

                // Step 2: Flush and verify temporary file exists and is not empty
                var tempFi = new FileInfo(tempFilePath);
                if (!tempFi.Exists || tempFi.Length == 0)
                {
                    return ProfileSaveResult.Fail("Temporary file write failed or file is empty.");
                }

                // Step 3: Backup existing main file if present
                if (File.Exists(mainFilePath))
                {
                    try
                    {
                        File.Copy(mainFilePath, backupFilePath, true);
                    }
                    catch (Exception)
                    {
                        // Backup warning, proceed with caution or log
                    }
                }

                // Step 4: Atomic file replacement
                // On Windows, File.Replace handles atomic swap; if destination doesn't exist, File.Move.
                if (File.Exists(mainFilePath))
                {
                    try
                    {
                        File.Replace(tempFilePath, mainFilePath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(mainFilePath);
                        File.Move(tempFilePath, mainFilePath);
                    }
                    catch (IOException)
                    {
                        // Fallback for Windows file lock / replace quirks
                        File.Delete(mainFilePath);
                        File.Move(tempFilePath, mainFilePath);
                    }
                }
                else
                {
                    File.Move(tempFilePath, mainFilePath);
                }

                CleanUpLeftoverTempFile();
                return ProfileSaveResult.Ok();
            }
            catch (Exception ex)
            {
                CleanUpLeftoverTempFile();
                return ProfileSaveResult.Fail("Save failed: " + ex.Message);
            }
        }

        private void CleanUpLeftoverTempFile()
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch { /* Best effort */ }
        }

        private void PreserveCorruptedFile(string sourcePath)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    string target = corruptedFilePath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    File.Copy(sourcePath, target, true);
                }
            }
            catch { /* Best effort */ }
        }

        private static int PeekSchemaVersion(string json)
        {
            // Fast peek for "schemaVersion": N
            int idx = json.IndexOf("\"schemaVersion\"", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int colon = json.IndexOf(':', idx);
                if (colon >= 0)
                {
                    int start = colon + 1;
                    while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
                    int end = start;
                    while (end < json.Length && char.IsDigit(json[end])) end++;
                    if (end > start)
                    {
                        return int.Parse(json.Substring(start, end - start));
                    }
                }
            }
            return 1;
        }
    }
}
