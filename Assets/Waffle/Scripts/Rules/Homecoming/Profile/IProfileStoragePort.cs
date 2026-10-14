using System;
using System.Collections.Generic;

namespace Waffle.Homecoming.Profile
{
    /// <summary>
    /// Storage port abstraction for loading and atomic saving of profile payloads.
    /// Pure C# interface with zero Unity dependencies, fully testable in memory.
    /// </summary>
    public interface IProfileStoragePort
    {
        ProfileLoadResult Load();
        ProfileSaveResult SaveAtomic(string jsonPayload);
    }

    public enum ProfileLoadStatus
    {
        Success,
        CreatedNew,
        RecoveredFromBackup,
        CorruptedAllPreserved,
        VersionTooNewReadOnly
    }

    public sealed class ProfileLoadResult
    {
        public ProfileLoadStatus Status { get; }
        public string JsonPayload { get; }
        public string StatusMessage { get; }
        public string FailedFilePath { get; }

        public ProfileLoadResult(ProfileLoadStatus status, string jsonPayload, string message = "", string failedFilePath = null)
        {
            Status = status;
            JsonPayload = jsonPayload;
            StatusMessage = message;
            FailedFilePath = failedFilePath;
        }

        public static ProfileLoadResult NewProfile()
            => new ProfileLoadResult(ProfileLoadStatus.CreatedNew, null, "No save file found; created a fresh profile.");
    }

    public sealed class ProfileSaveResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public ProfileSaveResult(bool success, string errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static ProfileSaveResult Ok() => new ProfileSaveResult(true);
        public static ProfileSaveResult Fail(string error) => new ProfileSaveResult(false, error);
    }
}
