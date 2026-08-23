using System;

namespace DiamondTilt.Core
{
    public enum AppScreen
    {
        Boot,
        Title,
        Match,
        Result,
        Settings,
    }

    public sealed class AppFlowController
    {
        private MatchEngine _engine;

        public AppScreen Current { get; private set; } = AppScreen.Boot;
        public Winner? LastResult { get; private set; }

        public event Action<AppScreen> ScreenChanged;

        public void AttachMatch(MatchEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Enter(AppScreen.Match);
        }

        public bool GoTo(AppScreen target)
        {
            switch (target)
            {
                case AppScreen.Title:
                    Enter(target);
                    return true;
                case AppScreen.Settings when Current is AppScreen.Title or AppScreen.Result:
                    Enter(target);
                    return true;
                case AppScreen.Result when Current == AppScreen.Match && _engine != null && _engine.State.Phase == MatchPhase.Finished:
                    Enter(target);
                    return true;
                default:
                    return false;
            }
        }

        public void PumpEvents()
        {
            if (_engine == null || Current != AppScreen.Match) return;

            foreach (var e in _engine.DrainEvents())
            {
                if (e.Type != MatchEventType.MatchEnded) continue;
                LastResult = _engine.State.Result;
                GoTo(AppScreen.Result);
                return;
            }
        }

        private void Enter(AppScreen target)
        {
            Current = target;
            ScreenChanged?.Invoke(target);
        }
    }
}
