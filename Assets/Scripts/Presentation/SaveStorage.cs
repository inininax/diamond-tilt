using System;
using System.IO;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using UnityEngine;

namespace DiamondTilt.Presentation
{
    public static class SaveStorage
    {
        private const string FileName = "save.json";

        public static SaveData LoadOrDefault(byte[] key)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, FileName);
                if (!File.Exists(path)) return FreshSave();

                var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
                if (envelope?.Payload == null || envelope.Tag == null) return FreshSave();
                if (!SaveIntegrity.Verify(envelope.Payload, envelope.Tag, key)) return FreshSave();

                var data = JsonUtility.FromJson<SaveData>(envelope.Payload);
                if (data == null || !SaveClamp.MigrateToCurrent(data)) return FreshSave();
                if (WalletReconciliation.Apply(data, key) == ReconcileStatus.InvalidChain) return FreshSave();

                SaveClamp.Clamp(data.Match);
                SaveClamp.Clamp(data);
                return data;
            }
            catch (Exception)
            {
                return FreshSave();
            }
        }

        public static void Store(GameServices services, byte[] key)
        {
            try
            {
                var data = new SaveData();
                services.WriteBackTo(data);

                string payload = JsonUtility.ToJson(data);
                var envelope = new Envelope { Payload = payload, Tag = SaveIntegrity.Tag(payload, key) };
                string path = Path.Combine(Application.persistentDataPath, FileName);
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(envelope));
                if (File.Exists(path)) File.Delete(path);
                File.Move(path + ".tmp", path);
            }
            catch (Exception)
            {
            }
        }

        private static SaveData FreshSave()
        {
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            SaveClamp.Clamp(save.Match);
            SaveClamp.Clamp(save);
            return save;
        }

        [Serializable]
        private sealed class Envelope
        {
            public string Payload;
            public string Tag;
        }
    }
}
