using System;
using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public sealed class StringTable
    {
        private readonly IReadOnlyDictionary<string, string> _strings;

        public string LocaleName { get; }

        public StringTable(string localeName, IReadOnlyDictionary<string, string> strings)
        {
            if (string.IsNullOrEmpty(localeName)) throw new ArgumentException("locale required", nameof(localeName));
            _strings = strings ?? throw new ArgumentNullException(nameof(strings));
            LocaleName = localeName;
        }

        public string Get(string key)
            => _strings.TryGetValue(key, out var value) ? value : key;

        public static StringTable Korean { get; } = new StringTable("ko", new Dictionary<string, string>
        {
            ["hud.inning.top.suffix"] = "초",
            ["hud.inning.bottom.suffix"] = "말",
            ["hud.result.win"] = "승리!",
            ["hud.result.lose"] = "패배",
            ["hud.result.draw"] = "무승부",
            ["title.start"] = "게임 시작",
            ["title.settings"] = "설정",
        });

        public static StringTable English { get; } = new StringTable("en", new Dictionary<string, string>
        {
            ["hud.inning.top.suffix"] = "Top",
            ["hud.inning.bottom.suffix"] = "Bot",
            ["hud.result.win"] = "You win!",
            ["hud.result.lose"] = "You lose",
            ["hud.result.draw"] = "Draw",
            ["title.start"] = "Start Game",
            ["title.settings"] = "Settings",
        });

        public static StringTable Default => Korean;

        public static StringTable ForLocale(string localeName)
        {
            switch (localeName)
            {
                case "en": return English;
                case "ko": return Korean;
                default: return Default;
            }
        }
    }
}
