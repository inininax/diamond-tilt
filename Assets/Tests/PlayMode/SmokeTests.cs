using System.Collections;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using DiamondTilt.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiamondTilt.Tests
{
    public sealed class SmokeTests
    {
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(31337u);

        [UnityTest]
        public IEnumerator GameRunner_ConstructsOperationalServices()
        {
            var go = new GameObject("GameRunner", typeof(GameRunner));
            yield return null;

            var runner = UnityEngine.Object.FindObjectOfType<GameRunner>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(runner.Services, Is.Not.Null);
            Assert.That(runner.Services.Wallet, Is.Not.Null);
            Assert.That(runner.Services.SeasonPass, Is.Not.Null);

            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator FullMatch_CompletesWithinTenSeconds()
        {
            var engine = new MatchEngine(new TimingContactModel());
            var pitcher = new SeededPitcherAI();
            var batter = CountAwareBatterAI.ForDifficulty(Difficulty.Normal);
            var rng = new Mulberry32Rng(4242u);
            float deadline = Time.realtimeSinceStartup + 10f;
            int pitches = 0;

            while (engine.State.Phase == MatchPhase.InProgress && Time.realtimeSinceStartup < deadline)
            {
                var pitch = pitcher.SelectPitch(engine.State, rng);
                engine.ThrowPitch(pitch, batter.DecideSwing(pitch, engine.State, rng));
                pitches++;
                if (pitches % 50 == 0) yield return null;
            }

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
        }

        [UnityTest]
        public IEnumerator SaveStorage_RoundTrip_PreservesBalanceOnDevice()
        {
            var clock = new UnityClock();
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            var services = new GameServices(save, Key, clock);

            services.Wallet.Grant(CurrencyType.Gems, 42, "smoke", clock);

            SaveStorage.Store(services, Key);
            var loaded = SaveStorage.LoadOrDefault(Key);

            Assert.That(loaded.WalletGems, Is.EqualTo(42));
            Assert.That(Wallet.VerifyChain(loaded.Ledger, Key), Is.True);

            yield return null;
        }
    }
}
