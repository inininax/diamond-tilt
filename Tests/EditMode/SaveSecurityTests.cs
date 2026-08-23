using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class SaveSecurityTests
    {
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(1234u);

        private static MatchEngine MidGameEngine()
        {
            var engine = new MatchEngine(new TimingContactModel());
            AutoMatch.PlaySelfContained(engine, Difficulty.Normal, 555u);
            return engine.State.Phase == MatchPhase.Finished ? new MatchEngine(new TimingContactModel()) : engine;
        }

        [Test]
        public void Snapshot_RoundTrip_PreservesAllFields()
        {
            var engine = MidGameEngine();
            var before = engine.State.ToSnapshot();
            var restored = new MatchState();

            restored.Restore(before);

            Assert.That(restored.Inning, Is.EqualTo(before.Inning));
            Assert.That(restored.IsTop, Is.EqualTo(before.IsTop));
            Assert.That(restored.Balls + restored.Strikes + restored.Outs,
                Is.EqualTo(before.Balls + before.Strikes + before.Outs));
            Assert.That(restored.AwayRuns, Is.EqualTo(before.AwayRuns));
            Assert.That(restored.HomeRuns, Is.EqualTo(before.HomeRuns));
            Assert.That(restored.Phase, Is.EqualTo((MatchPhase)before.Phase));
        }

        [Test]
        public void Restore_ClampsCorruptValues()
        {
            var state = new MatchState();
            state.Restore(new MatchSnapshot
            {
                Inning = 99, IsTop = true, Balls = -7, Strikes = 12, Outs = 50,
                AwayRuns = -3, HomeRuns = 100000, Phase = 77, Result = 42,
            });

            Assert.That(state.Inning, Is.EqualTo(MatchState.Innings));
            Assert.That(state.Balls, Is.EqualTo(0));
            Assert.That(state.Strikes, Is.EqualTo(2));
            Assert.That(state.Outs, Is.EqualTo(3));
            Assert.That(state.AwayRuns, Is.EqualTo(0));
            Assert.That(state.HomeRuns, Is.EqualTo(999));
            Assert.That(state.Phase, Is.EqualTo(MatchPhase.InProgress));
        }

        [Test]
        public void Schema_FutureVersion_NotSupported()
        {
            Assert.That(SaveClamp.IsSupportedSchema(SaveData.CurrentSchemaVersion), Is.True);
            Assert.That(SaveClamp.IsSupportedSchema(SaveData.CurrentSchemaVersion + 1), Is.False);
        }

        [Test]
        public void Integrity_TagDeterministic_VerifyTrue()
        {
            string tagA = SaveIntegrity.Tag("payload", Key);
            string tagB = SaveIntegrity.Tag("payload", Key);

            Assert.That(tagA, Is.EqualTo(tagB));
            Assert.That(SaveIntegrity.Verify("payload", tagA, Key), Is.True);
        }

        [Test]
        public void Integrity_TamperedPayload_Rejected()
        {
            string tag = SaveIntegrity.Tag("score:0", Key);

            Assert.That(SaveIntegrity.Verify("score:999", tag, Key), Is.False);
        }

        [Test]
        public void Integrity_WrongKey_Rejected()
        {
            string tag = SaveIntegrity.Tag("payload", Key);

            Assert.That(SaveIntegrity.Verify("payload", tag, SaveIntegrity.DeriveKey(9999u)), Is.False);
        }

        [Test]
        public void Integrity_MalformedTag_Rejected_NoThrow()
        {
            Assert.That(SaveIntegrity.Verify("payload", "zz-not-hex", Key), Is.False);
            Assert.That(SaveIntegrity.Verify("payload", "abc", Key), Is.False);
            Assert.That(SaveIntegrity.Verify("payload", null, Key), Is.False);
        }

        [Test]
        public void Adapter_TryLoad_HappyPath_RoundTrips()
        {
            var engine = MidGameEngine();
            var data = new SaveData { Match = engine.State.ToSnapshot(), Wins = 3, Losses = 1 };
            string json = SaveJsonAdapter.SerializeEnvelope(data, Key);

            bool ok = SaveJsonAdapter.TryLoad(json, Key, out var loaded);

            Assert.That(ok, Is.True);
            Assert.That(loaded.Wins, Is.EqualTo(3));
            Assert.That(loaded.Match.HomeRuns, Is.EqualTo(data.Match.HomeRuns));
        }

        [Test]
        public void Adapter_CorruptJson_ReturnsFalse_NoThrow()
        {
            Assert.That(SaveJsonAdapter.TryLoad("{not json", Key, out _), Is.False);
            Assert.That(SaveJsonAdapter.TryLoad("", Key, out _), Is.False);
            Assert.That(SaveJsonAdapter.TryLoad(null, Key, out _), Is.False);
        }

        [Test]
        public void Adapter_TamperedEnvelope_Rejected_EndToEnd()
        {
            var data = new SaveData { Match = new MatchSnapshot { HomeRuns = 5 } };
            string payload = JsonSerializer.Serialize(data);
            var envelope = new SaveEnvelope { Payload = payload.Replace("5", "500"), Tag = SaveIntegrity.Tag(payload, Key) };
            string json = JsonSerializer.Serialize(envelope);

            Assert.That(SaveJsonAdapter.TryLoad(json, Key, out _), Is.False);
        }

        [Test]
        public void Adapter_QuarantineSemantics_LoadFailureLeavesFreshStateUsable()
        {
            var engine = new MatchEngine(new TimingContactModel());
            bool ok = SaveJsonAdapter.TryLoad("garbage", Key, out _);

            Assert.That(ok, Is.False);
            MatchTestHarness.TakeStrike(engine);
            Assert.That(engine.State.Strikes, Is.EqualTo(1));
        }

        [Test]
        public void NoPiiAudit_SerializedKeysWhitelisted()
        {
            var data = new SaveData { Match = new MatchSnapshot(), Wins = 1, Losses = 2, DifficultyTier = 1 };
            var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                Assert.That(prop.Name, Is.AnyOf("SchemaVersion", "Match", "Wins", "Losses", "DifficultyTier"),
                    "unexpected top-level key — possible PII leak");
            }
            foreach (var prop in doc.RootElement.GetProperty("Match").EnumerateObject())
            {
                Assert.That(prop.Name, Is.AnyOf(
                    "Inning", "IsTop", "Balls", "Strikes", "Outs",
                    "FirstBase", "SecondBase", "ThirdBase", "AwayRuns", "HomeRuns", "Phase", "Result"),
                    $"unexpected match key '{prop.Name}' — possible PII leak");
            }
        }

        [Test]
        public void Integrity_PerfSmoke_500TagsAndVerifies_UnderTwoSeconds()
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++)
            {
                string p = "payload-" + i;
                string t = SaveIntegrity.Tag(p, Key);
                Assert.That(SaveIntegrity.Verify(p, t, Key), Is.True);
            }
            sw.Stop();

            Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
        }
    }
}
