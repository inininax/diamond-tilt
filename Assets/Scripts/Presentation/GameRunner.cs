using System;
using System.IO;
using DiamondTilt.Core;
using UnityEngine;

namespace DiamondTilt.Presentation
{
    public sealed class GameRunner : MonoBehaviour
    {
        private GameServices _services;

        public GameServices Services => _services;

        private void Awake()
        {
            Application.targetFrameRate = 60;

            var clock = new UnityClock();
            byte[] key = SaveIntegrity.DeriveKey(DeviceSeed.Value);
            SaveData save = SaveStorage.LoadOrDefault(key);
            _services = new GameServices(save, key, clock);
            _services.OnSaveRequested += () => SaveStorage.Store(_services, key);

            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) RequestSave();
        }

        private void OnApplicationQuit()
        {
            RequestSave();
        }

        private void RequestSave()
        {
            _services?.RequestManualSave();
        }
    }

    public sealed class UnityClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    internal static class DeviceSeed
    {
        private const string PrefKey = "dt.device.seed";

        internal static uint Value
        {
            get
            {
                int stored = PlayerPrefs.GetInt(PrefKey, -1);
                if (stored >= 0) return unchecked((uint)stored);

                uint fresh = (uint)Environment.TickCount;
                if (fresh == 0) fresh = 1;
                PlayerPrefs.SetInt(PrefKey, unchecked((int)fresh));
                PlayerPrefs.Save();
                return fresh;
            }
        }
    }
}
