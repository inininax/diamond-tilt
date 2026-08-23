using System;
using System.Collections.Generic;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class ResumeFlowTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(404u);

        [Test]
        public void MidMatch_SnapshotRestore_PreservesExactProgression()
        {
            var engine = new MatchEngine(new TimingContactModel());
            for (int i = 0; i < 25 && engine.State.Phase == MatchPhase.InProgress; i++)
            {
                AutoPlayOnePitch(engine, 11u, i);
            }

            var snapshot = engine.State.ToSnapshot();
            var restored = new MatchState();
            restored.Restore(snapshot);

            Assert.That(restored.Inning, Is.EqualTo(engine.State.Inning));
            Assert.That(restored.IsTop, Is.EqualTo(engine.State.IsTop));
            Assert.That(restored.Outs + restored.Balls + restored.Strikes,
                Is.EqualTo(engine.State.Outs + engine.State.Balls + engine.State.Strikes));
        }

        [Test]
        public void ResumeFlow_RestoredEngine_CompletesMatch()
        {
            var engine = new MatchEngine(new TimingContactModel());
            for (int i = 0; i < 15 && engine.State.Phase == MatchPhase.InProgress; i++)
            {
                AutoPlayOnePitch(engine, 22u, i);
            }
            var snapshot = engine.State.ToSnapshot();

            var resumed = new MatchEngine(new TimingContactModel());
            resumed.State.Restore(snapshot);

            int guard = 4000;
            int pitchIndex = 0;
            while (resumed.State.Phase == MatchPhase.InProgress && guard-- > 0)
            {
                AutoPlayOnePitch(resumed, 33u, pitchIndex++);
            }

            Assert.That(resumed.State.Phase, Is.EqualTo(MatchPhase.Finished));
        }

        [Test]
        public void FullEconomyResume_SerializedRoundTrip_SystemsKeepWorking()
        {
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key);
            var missions = new DailyMissionSystem(null, wallet, clock);
            var season = new SeasonPassSystem(null, wallet, clock);
            var rewards = new MatchRewardService(wallet, missions, season, clock);
            var stats = new MatchStats();
            stats.Observe(new MatchEvent(MatchEventType.HomerunRecorded, 1, false));

            rewards.ApplyPostMatch(Winner.Home, stats);

            var data = new SaveData
            {
                WalletCoins = wallet.Coins,
                WalletGems = wallet.Gems,
                Ledger = new List<LedgerEntry>(wallet.Entries),
                SeasonPass = season.State,
                Missions = missions.State,
            };

            var loadedWallet = Wallet.FromEntries(Key, data.Ledger);
            var loadedMissions = new DailyMissionSystem(data.Missions, loadedWallet, clock);
            var loadedSeason = new SeasonPassSystem(data.SeasonPass, loadedWallet, clock);
            var loadedRewards = new MatchRewardService(loadedWallet, loadedMissions, loadedSeason, clock);

            long coinsBefore = loadedWallet.Coins;
            var nextStats = new MatchStats();
            nextStats.Observe(new MatchEvent(MatchEventType.HitRecorded, 2, false));
            nextStats.Observe(new MatchEvent(MatchEventType.HomerunRecorded, 2, false));

            loadedRewards.ApplyPostMatch(Winner.Home, nextStats);

            Assert.That(loadedWallet.Coins, Is.GreaterThan(coinsBefore));
            Assert.That(Wallet.VerifyChain(loadedWallet.Entries, Key), Is.True);
            Assert.That(loadedSeason.State.Xp, Is.GreaterThanOrEqualTo(data.SeasonPass.Xp));
            Assert.That(missions.ReadyMissionIds(), Does.Contain("hr_1"));
        }

        private static void AutoPlayOnePitch(MatchEngine engine, uint seed, int sequence)
        {
            var rng = new Mulberry32Rng(seed ^ (uint)(sequence * 131 + engine.State.Outs * 31));
            var pitcher = new SeededPitcherAI();
            var batter = CountAwareBatterAI.ForDifficulty(Difficulty.Normal);
            var pitch = pitcher.SelectPitch(engine.State, rng);
            engine.ThrowPitch(pitch, batter.DecideSwing(pitch, engine.State, rng));
        }
    }
}
