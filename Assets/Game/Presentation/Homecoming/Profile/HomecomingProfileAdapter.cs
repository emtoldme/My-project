using System.IO;
using UnityEngine;
using Waffle.Homecoming.Profile;

namespace Reverb.Homecoming.Profile
{
    /// <summary>
    /// Presentation-side lifecycle bridge and file storage adapter.
    /// Injects Application.persistentDataPath into the pure C# ProfileFileStorage.
    /// Pure presentation adapter; does NOT start a second GameLoop or simulate combat.
    /// </summary>
    public sealed class HomecomingProfileAdapter : MonoBehaviour
    {
        [SerializeField] private string profileFileName = "homecoming_profile.json";

        public HomecomingProfile ProfileInstance { get; private set; }

        private void Awake()
        {
            InitializeProfile();
        }

        public void InitializeProfile()
        {
            if (ProfileInstance != null) return;

            string storageDir = Application.persistentDataPath;
            var storage = new ProfileFileStorage(storageDir, profileFileName);
            ProfileInstance = new HomecomingProfile(storage);
        }

        public string GetProfileStorageDirectory()
        {
            return Application.persistentDataPath;
        }

        public ProfileLoadStatus GetLoadStatus()
        {
            return ProfileInstance != null ? ProfileInstance.LoadStatus : ProfileLoadStatus.CreatedNew;
        }

        public string GetStatusMessage()
        {
            return ProfileInstance != null ? ProfileInstance.StatusMessage : string.Empty;
        }
    }
}
