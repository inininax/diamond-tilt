using System;
using System.Reflection;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class SerializationContractTests
    {
        private static readonly Type[] PersistedTypes =
        {
            typeof(MatchSnapshot),
            typeof(SaveData),
            typeof(LedgerEntry),
            typeof(SeasonPassState),
            typeof(DailyMissionState),
            typeof(SubscriptionState),
        };

        [Test]
        public void PersistedDtos_AreMarkedSerializable()
        {
            foreach (var type in PersistedTypes)
            {
                Assert.That(type.IsDefined(typeof(SerializableAttribute)), Is.True, type.Name);
            }
        }

        [Test]
        public void PersistedDtos_UseFieldsNotProperties_JsonUtilityContract()
        {
            foreach (var type in PersistedTypes)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Assert.That(properties, Is.Empty,
                    $"{type.Name} must expose persisted data as public fields (JsonUtility contract)");
            }
        }
    }
}
