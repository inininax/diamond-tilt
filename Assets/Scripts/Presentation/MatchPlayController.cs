using System.Collections.Generic;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using UnityEngine;

namespace DiamondTilt.Presentation
{
    public sealed class MatchPlayController : MonoBehaviour
    {
        private const int TicksPerSecond = 60;

        private MatchPlaySession _session;
        private StringTable _table;
        private readonly List<MatchEvent> _log = new List<MatchEvent>();
        private int _pendingZone = 4;
        private int _pendingSpeed = 1;
        private float _tickAccumulator;

        private void Start()
        {
            _table = StringTable.Default;

            var runner = FindFirstObjectByType<GameRunner>();
            GameServices services = runner != null ? runner.Services : null;

            var engine = new MatchEngine(new TimingContactModel());
            MatchRewardService rewards = null;
            if (services != null)
                rewards = new MatchRewardService(
                    services.Wallet, services.Missions, services.SeasonPass, services.Clock);

            uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            _session = new MatchPlaySession(engine, new Mulberry32Rng(seed), rewards, TicksPerSecond);
        }

        private void Update()
        {
            if (_session == null) return;
            _tickAccumulator += Time.deltaTime * TicksPerSecond;
            int whole = Mathf.FloorToInt(_tickAccumulator);
            if (whole > 0)
            {
                _tickAccumulator -= whole;
                _session.TickAdvance(whole);
            }

            foreach (var e in _session.DrainEvents())
            {
                _log.Insert(0, e);
                if (_log.Count > 4) _log.RemoveAt(_log.Count - 1);
            }
        }

        private void OnGUI()
        {
            if (_session == null) return;

            var hud = HudMapper.From(_session.State);
            DrawHud(hud);

            switch (_session.Phase)
            {
                case SessionPhase.WaitingToPitch:
                    DrawZoneGrid();
                    break;
                case SessionPhase.BallIncoming:
                    DrawIncomingBall();
                    if (GUI.Button(new Rect(Screen.width / 2 - 110, Screen.height * 0.62f, 220, 90), "SWING!"))
                        _session.PlayerSwing();
                    break;
                case SessionPhase.BetweenPlays:
                    GUI.Label(new Rect(Screen.width / 2 - 60, Screen.height / 2 - 20, 120, 40), "...");
                    break;
                case SessionPhase.MatchOver:
                    DrawResult(hud);
                    break;
            }

            DrawEventLog();
        }

        private static readonly GUIStyle TitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
        };

        private static GUIStyle BigLabel(int size)
        {
            return new GUIStyle(GUI.skin.label) { fontSize = size, alignment = TextAnchor.MiddleCenter };
        }

        private void DrawHud(HudSnapshot hud)
        {
            GUI.Box(new Rect(0, 0, Screen.width, 92), string.Empty);
            GUI.Label(new Rect(16, 8, 220, 40),
                $"{hud.InningLabel(_table)}", BigLabel(24));
            GUI.Label(new Rect(16, 44, 220, 40),
                $"{hud.CountLabel}  OUT {hud.Outs}", BigLabel(22));
            GUI.Label(new Rect(Screen.width / 2 - 110, 12, 220, 48),
                $"{hud.AwayRuns} : {hud.HomeRuns}", BigLabel(34));
            GUI.Label(new Rect(Screen.width - 236, 20, 220, 48),
                $"1B:{B(hud.FirstBase)} 2B:{B(hud.SecondBase)} 3B:{B(hud.ThirdBase)}",
                new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleRight });
        }

        private static string B(bool on) => on ? "●" : "○";

        private void DrawZoneGrid()
        {
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height * 0.14f, 400, 36),
                "투구할 존을 선택하세요", BigLabel(22));

            float grid = Mathf.Min(Screen.width, Screen.height) * 0.42f;
            float x0 = Screen.width / 2 - grid / 2;
            float y0 = Screen.height * 0.22f;
            float cell = grid / 3;

            for (int row = 0; row < ZoneGrid.Rows; row++)
            {
                for (int col = 0; col < ZoneGrid.Columns; col++)
                {
                    int zone = row * ZoneGrid.Columns + col + 1;
                    var rect = new Rect(x0 + col * cell, y0 + row * cell, cell - 6, cell - 6);
                    if (GUI.Button(rect, zone.ToString(), BigLabel(26)))
                    {
                        _pendingZone = zone;
                        _session.PlayerPitch(zone, _pendingSpeed);
                    }
                }
            }

            string[] speeds = { "느리게", "보통", "빠르게" };
            for (int i = 0; i < 3; i++)
            {
                var rect = new Rect(x0 + i * (grid / 3), y0 + grid + 10, grid / 3 - 6, 44);
                bool selected = _pendingSpeed == i;
                var style = new GUIStyle(GUI.skin.button) { fontSize = 18 };
                if (selected) style.fontStyle = FontStyle.Bold;
                if (GUI.Button(rect, selected ? $"[{speeds[i]}]" : speeds[i], style))
                    _pendingSpeed = i;
            }
        }

        private void DrawIncomingBall()
        {
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height * 0.14f, 400, 36),
                "타이밍에 맞춰 SWING!", BigLabel(22));

            float barW = Screen.width * 0.7f;
            float x0 = Screen.width * 0.15f;
            float y0 = Screen.height * 0.42f;
            GUI.Box(new Rect(x0, y0, barW, 26), string.Empty);

            float total = _session.FlightTicks + TouchToIntent.MaxOffsetTicks;
            float t = Mathf.Clamp01((float)(_session.CurrentTick - (_session.IncomingArrivalTick - _session.FlightTicks)) / total);
            float ballX = x0 + t * (barW - 24);
            GUI.Box(new Rect(ballX, y0 - 4, 24, 34), string.Empty);

            GUI.Label(new Rect(x0 + barW - 46, y0 + 30, 92, 28), "← 접점",
                new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.UpperRight });
        }

        private void DrawResult(HudSnapshot hud)
        {
            string verdict = _session.State.Result == Winner.Home ? _table.Get("hud.result.win")
                : _session.State.Result == Winner.Away ? _table.Get("hud.result.lose")
                : _table.Get("hud.result.draw");

            GUI.Box(new Rect(Screen.width / 2 - 240, Screen.height * 0.3f, 480, 150), string.Empty);
            GUI.Label(new Rect(Screen.width / 2 - 240, Screen.height * 0.33f, 480, 60), verdict,
                new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(Screen.width / 2 - 240, Screen.height * 0.42f, 480, 40),
                $"최종 스코어  {hud.ScoreLabel}", BigLabel(24));

            if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height * 0.52f, 200, 60), "다시 하기",
                new GUIStyle(GUI.skin.button) { fontSize = 22 }))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        private void DrawEventLog()
        {
            GUI.Box(new Rect(0, Screen.height - 118, Screen.width, 118), string.Empty);
            var table = _table;
            int line = 0;
            foreach (var e in _log)
            {
                GUI.Label(new Rect(12, Screen.height - 112 + line * 26, Screen.width - 24, 26),
                    Describe(e, table), new GUIStyle(GUI.skin.label) { fontSize = 17 });
                line++;
            }
        }

        private static string Describe(MatchEvent e, StringTable table)
        {
            string half = e.IsTop ? table.Get("hud.inning.top.suffix") : table.Get("hud.inning.bottom.suffix");
            string side = e.IsTop ? "원정" : "홈";
            return e.Type switch
            {
                MatchEventType.BallCalled => $"{e.Inning}{half} 볼",
                MatchEventType.StrikeCalled => $"{e.Inning}{half} 스트라이크",
                MatchEventType.BatterWalked => $"{e.Inning}{half} 볼넷!",
                MatchEventType.BatterStruckOut => $"{e.Inning}{half} 삼진!",
                MatchEventType.BatterOut => $"{e.Inning}{half} 아웃",
                MatchEventType.RunnerOut => $"{e.Inning}{half} 주자 아웃!",
                MatchEventType.HitRecorded => $"{side} 안타!",
                MatchEventType.HomerunRecorded => $"{side} 홈런!!!",
                MatchEventType.RunScored => $"{side} 득점!",
                MatchEventType.HalfInningEnded => $"{e.Inning}{half} 종료",
                MatchEventType.MatchEnded => "경기 종료",
                _ => e.Type.ToString(),
            };
        }
    }
}
