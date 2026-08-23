using System;
using System.Text.Json;
using DiamondTilt.Core;

namespace DiamondTilt.Tests
{
    public sealed class SaveEnvelope
    {
        public string Payload { get; set; }
        public string Tag { get; set; }
    }

    public static class SaveJsonAdapter
    {
        public static string SerializeEnvelope(SaveData data, byte[] key)
        {
            string payload = JsonSerializer.Serialize(data);
            return JsonSerializer.Serialize(new SaveEnvelope { Payload = payload, Tag = SaveIntegrity.Tag(payload, key) });
        }

        public static bool TryLoad(string json, byte[] key, out SaveData loaded)
        {
            loaded = null;
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var envelope = JsonSerializer.Deserialize<SaveEnvelope>(json);
                if (envelope?.Payload == null || envelope.Tag == null) return false;
                if (!SaveIntegrity.Verify(envelope.Payload, envelope.Tag, key)) return false;

                var data = JsonSerializer.Deserialize<SaveData>(envelope.Payload);
                if (data == null || data.Match == null) return false;
                if (!SaveClamp.IsSupportedSchema(data.SchemaVersion)) return false;

                SaveClamp.Clamp(data.Match);
                loaded = data;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
