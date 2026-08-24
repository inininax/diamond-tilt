using DiamondTilt.Core;
using UnityEngine;

namespace DiamondTilt.Presentation
{
    public sealed class FieldPresenter : MonoBehaviour
    {
        private static readonly Vector3 PlatePos = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 MoundPos = new Vector3(0f, 0.15f, 18.4f);
        private static readonly Vector3 FirstBase = new Vector3(19.4f, 0f, 19.4f);
        private static readonly Vector3 SecondBase = new Vector3(0f, 0f, 38.8f);
        private static readonly Vector3 ThirdBase = new Vector3(-19.4f, 0f, 19.4f);

        private Transform _field;
        private Transform _ball;
        private TrailRenderer _ballTrail;
        private PlayerFigure _pitcher;
        private PlayerFigure _batter;
        private PlayerFigure _catcher;
        private readonly PlayerFigure[] _runnerFigures = new PlayerFigure[3];
        private bool[] _occupied = new bool[3];

        private bool _pitchFlying;
        private float _pitchT;
        private float _pitchDuration = 0.7f;

        private bool _contactFlying;
        private float _contactT;
        private float _contactDuration = 1.6f;
        private Vector3[] _contactPoints;
        private Vector3 _contactOrigin;

        public void Build()
        {
            _field = new GameObject("Field").transform;
            BuildGround();
            BuildWallAndStands();
            BuildFigures();
            BuildBall();
            PlaceCamera();
        }

        public void ResetInning()
        {
            for (int i = 0; i < 3; i++) _occupied[i] = false;
            _runnerFigures[0]?.gameObject.SetActive(false);
            _runnerFigures[1]?.gameObject.SetActive(false);
            _runnerFigures[2]?.gameObject.SetActive(false);
        }

        public void BeginPitch(float durationSeconds)
        {
            _pitchDuration = Mathf.Max(0.25f, durationSeconds);
            _pitchT = 0f;
            _pitchFlying = true;
            if (_ballTrail != null) _ballTrail.Clear();
            _ball.gameObject.SetActive(true);
            _ball.position = MoundPos + new Vector3(0f, 1.55f, 0f);
            if (_pitcher != null) _pitcher.SwingBat();
        }

        public void BeginContact(LaunchParams flight)
        {
            _pitchFlying = false;

            int steps = 26;
            var samples = BallFlight.Sample(flight, steps);
            _contactPoints = new Vector3[steps];
            for (int i = 0; i < steps; i++)
            {
                var v = samples[i];
                _contactPoints[i] = new Vector3((float)v.X, Mathf.Max(0.06f, (float)v.Y), (float)v.Z);
            }

            _contactOrigin = new Vector3(0.2f, 1.0f, 0.4f);
            _contactDuration = Mathf.Max(0.8f, (float)BallFlight.FlightTimeNoDrag(flight));
            _contactT = 0f;
            _contactFlying = true;
            if (_ballTrail != null) _ballTrail.Clear();
            _ball.position = _contactOrigin;

            if (_batter != null) _batter.SwingBat();
        }

        public void BeginTake()
        {
            _pitchFlying = true;
            _pitchDuration = Mathf.Max(0.2f, _pitchDuration);
        }

        public void ResolvePlay(MatchState state, PlayOutcome? outcome)
        {
            _pitchFlying = false;
            _contactFlying = false;

            bool[] now = { state.FirstBase, state.SecondBase, state.ThirdBase };
            Vector3[] basePos = { FirstBase, SecondBase, ThirdBase };

            if (outcome == PlayOutcome.Homerun || outcome == PlayOutcome.Triple ||
                outcome == PlayOutcome.Double || outcome == PlayOutcome.Single ||
                outcome == PlayOutcome.LineSingle)
            {
                int basesToAdvance = outcome == PlayOutcome.Homerun ? 4
                    : outcome == PlayOutcome.Double ? 2
                    : outcome == PlayOutcome.Triple ? 3 : 1;

                for (int i = 2; i >= 0; i--)
                {
                    if (!_occupied[i]) continue;
                    int dest = i + 1 + (basesToAdvance - 1);
                    if (dest >= 3)
                    {
                        AnimateRunnerHome(i);
                        _occupied[i] = false;
                    }
                    else if (!now[dest] && i != dest)
                    {
                        AnimateRunner(i, basePos[dest]);
                        _occupied[i] = false;
                        _occupied[dest] = true;
                    }
                }

                if (outcome == PlayOutcome.Homerun)
                {
                    _batter?.RunTo(PlatePos + new Vector3(0f, 0f, 0.6f), 1.1f);
                    for (int i = 0; i < 3; i++) _occupied[i] = false;
                }
                else
                {
                    var target = outcome == PlayOutcome.Double ? SecondBase
                        : outcome == PlayOutcome.Triple ? ThirdBase : FirstBase;
                    _batter?.RunTo(target + new Vector3(0.4f, 0f, -0.4f), 0.9f);
                    if (basesToAdvance == 1 && !now[0]) _occupied[0] = true;
                    else if (basesToAdvance == 2 && !now[1]) _occupied[1] = true;
                    else if (basesToAdvance == 3 && !now[2]) _occupied[2] = true;
                }
            }
            else if (outcome == PlayOutcome.Grounder)
            {
                if (_occupied[0] && state.Outs < 3) { AnimateRunnerOut(0); _occupied[0] = false; }
                if (_occupied[2] && state.Outs < 3 && state.ThirdBase == false)
                {
                    AnimateRunnerHome(2);
                    _occupied[2] = false;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                _occupied[i] = now[i];
                if (now[i]) PlaceRunnerFigure(i, basePos[i]);
                else if (_runnerFigures[i] != null) _runnerFigures[i].gameObject.SetActive(false);
            }
        }

        private void AnimateRunner(int fromIndex, Vector3 target)
        {
            var fig = EnsureRunnerFigure(fromIndex);
            fig.gameObject.SetActive(true);
            fig.RunTo(target, 0.85f);
        }

        private void AnimateRunnerHome(int fromIndex)
        {
            var fig = EnsureRunnerFigure(fromIndex);
            fig.gameObject.SetActive(true);
            fig.RunTo(PlatePos + new Vector3(-0.4f, 0f, 0.4f), 0.85f);
        }

        private void AnimateRunnerOut(int fromIndex)
        {
            if (_runnerFigures[fromIndex] != null)
                _runnerFigures[fromIndex].RunTo(new Vector3(6f, 0f, 22f), 0.6f);
        }

        private PlayerFigure EnsureRunnerFigure(int index)
        {
            if (_runnerFigures[index] == null)
            {
                _runnerFigures[index] = PlayerFigure.Create(_field, $"runner{index + 1}",
                    new Color(0.85f, 0.25f, 0.22f), Vector3.zero, 1f);
            }
            return _runnerFigures[index];
        }

        private void PlaceRunnerFigure(int index, Vector3 pos)
        {
            var fig = EnsureRunnerFigure(index);
            fig.gameObject.SetActive(true);
            fig.SnapTo(pos + new Vector3(0.35f, 0f, -0.35f));
        }

        private MatchState _pendingResolveState;
        private PlayOutcome? _pendingResolveOutcome;
        private float _pendingResolveTimer = -1f;

        public void ResolvePlayDelayed(MatchState state, PlayOutcome? outcome, float delaySeconds)
        {
            _pendingResolveState = state;
            _pendingResolveOutcome = outcome;
            _pendingResolveTimer = delaySeconds;
        }

        private void Update()
        {
            if (_pendingResolveTimer >= 0f)
            {
                _pendingResolveTimer -= Time.deltaTime;
                if (_pendingResolveTimer < 0f && _pendingResolveState != null)
                {
                    ResolvePlay(_pendingResolveState, _pendingResolveOutcome);
                    _pendingResolveState = null;
                }
            }

            if (_pitchFlying)
            {
                _pitchT += Time.deltaTime / _pitchDuration;
                float t = Mathf.Clamp01(_pitchT);
                var from = MoundPos + new Vector3(0f, 1.55f, 0f);
                var to = PlatePos + new Vector3(0.2f, 1.0f, 0.4f);
                _ball.position = Vector3.Lerp(from, to, t) + new Vector3(0f, Mathf.Sin(t * Mathf.PI) * 0.25f, 0f);
            }

            if (_contactFlying && _contactPoints != null)
            {
                _contactT += Time.deltaTime / _contactDuration;
                float t = Mathf.Clamp01(_contactT);
                float f = t * (_contactPoints.Length - 1);
                int i0 = Mathf.FloorToInt(f);
                int i1 = Mathf.Min(i0 + 1, _contactPoints.Length - 1);
                _ball.position = Vector3.Lerp(_contactPoints[i0], _contactPoints[i1], f - i0);
                if (t >= 1f) _contactFlying = false;
            }
        }

        private void BuildGround()
        {
            var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "grass";
            grass.transform.SetParent(_field, false);
            grass.transform.position = new Vector3(0f, 0f, 45f);
            grass.transform.localScale = new Vector3(26f, 1f, 26f);
            SetColor(grass, new Color(0.16f, 0.45f, 0.18f));

            var dirt = GameObject.CreatePrimitive(PrimitiveType.Plane);
            dirt.name = "dirt";
            dirt.transform.SetParent(_field, false);
            dirt.transform.position = new Vector3(0f, 0.01f, 17f);
            dirt.transform.localScale = new Vector3(6f, 1f, 6f);
            dirt.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            SetColor(dirt, new Color(0.62f, 0.45f, 0.28f));

            var mound = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mound.name = "mound";
            mound.transform.SetParent(_field, false);
            mound.transform.position = MoundPos + new Vector3(0f, 0.07f, 0f);
            mound.transform.localScale = new Vector3(3f, 0.14f, 3f);
            SetColor(mound, new Color(0.66f, 0.48f, 0.3f));

            AddBase(FirstBase);
            AddBase(SecondBase);
            AddBase(ThirdBase);
            AddPlate();

            AddLine(new Vector3(1.4f, 0f, 1.4f), new Vector3(70f, 0f, 70f));
            AddLine(new Vector3(-1.4f, 0f, 1.4f), new Vector3(-70f, 0f, 70f));
        }

        private void AddBase(Vector3 pos)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "base";
            b.transform.SetParent(_field, false);
            b.transform.position = pos + new Vector3(0f, 0.06f, 0f);
            b.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
            SetColor(b, Color.white);
        }

        private void AddPlate()
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "plate";
            p.transform.SetParent(_field, false);
            p.transform.position = PlatePos + new Vector3(0f, 0.06f, 0f);
            p.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
            SetColor(p, Color.white);
        }

        private void AddLine(Vector3 from, Vector3 to)
        {
            var mid = (from + to) / 2f;
            float length = Vector3.Distance(from, to);
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "foulline";
            line.transform.SetParent(_field, false);
            line.transform.position = mid + new Vector3(0f, 0.02f, 0f);
            line.transform.localScale = new Vector3(0.24f, 0.02f, length);
            line.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg, 0f);
            SetColor(line, Color.white);
        }

        private void BuildWallAndStands()
        {
            var wallColor = new Color(0.22f, 0.28f, 0.42f);
            int segments = 22;
            for (int i = 0; i < segments; i++)
            {
                float a0 = Mathf.Lerp(-52f, 52f, i / (float)segments) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(-52f, 52f, (i + 1) / (float)segments) * Mathf.Deg2Rad;
                var p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * 100f;
                var p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * 100f;
                var mid = (p0 + p1) / 2f;

                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = "wall";
                seg.transform.SetParent(_field, false);
                seg.transform.position = mid + new Vector3(0f, 1.5f, 0f);
                seg.transform.localScale = new Vector3(Vector3.Distance(p0, p1) + 0.3f, 3f, 1.2f);
                seg.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(p1.x - p0.x, p1.z - p0.z) * Mathf.Rad2Deg, 0f);
                SetColor(seg, wallColor);
            }

            for (int tier = 0; tier < 3; tier++)
            {
                int seats = 26;
                float radius = 106f + tier * 7f;
                for (int i = 0; i < seats; i++)
                {
                    float a = Mathf.Lerp(-55f, 55f, i / (float)(seats - 1)) * Mathf.Deg2Rad;
                    var pos = new Vector3(Mathf.Sin(a), 2.2f + tier * 2.1f, Mathf.Cos(a)) * (radius / 106f) * 106f;
                    pos.z = Mathf.Cos(a) * radius;
                    pos.x = Mathf.Sin(a) * radius;

                    var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stand.name = "stand";
                    stand.transform.SetParent(_field, false);
                    stand.transform.position = pos;
                    stand.transform.localScale = new Vector3(9f, 2.1f, 5f);
                    stand.transform.rotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
                    SetColor(stand, new Color(0.5f + 0.1f * (i % 3), 0.34f, 0.4f - 0.05f * tier));
                }
            }
        }

        private void BuildFigures()
        {
            var defense = new Color(0.28f, 0.34f, 0.62f);
            var attack = new Color(0.85f, 0.28f, 0.24f);

            _pitcher = PlayerFigure.Create(_field, "pitcher", defense, MoundPos + new Vector3(0f, 0.14f, -0.4f));
            _catcher = PlayerFigure.Create(_field, "catcher", defense, PlatePos + new Vector3(0f, 0f, -1.6f), 0.95f);

            PlayerFigure.Create(_field, "1b", defense, FirstBase + new Vector3(5f, 0f, 6f));
            PlayerFigure.Create(_field, "2b", defense, new Vector3(8f, 0f, 33f));
            PlayerFigure.Create(_field, "ss", defense, new Vector3(-8f, 0f, 33f));
            PlayerFigure.Create(_field, "3b", defense, ThirdBase + new Vector3(-5f, 0f, 6f));
            PlayerFigure.Create(_field, "lf", defense, new Vector3(-34f, 0f, 66f));
            PlayerFigure.Create(_field, "cf", defense, new Vector3(0f, 0f, 76f));
            PlayerFigure.Create(_field, "rf", defense, new Vector3(34f, 0f, 66f));

            _batter = PlayerFigure.Create(_field, "batter", attack, PlatePos + new Vector3(0.85f, 0f, 0.25f), 1f, withBat: true);
            _batter.transform.rotation = Quaternion.Euler(0f, 200f, 0f);

            var ump = PlayerFigure.Create(_field, "umpire", new Color(0.15f, 0.15f, 0.18f),
                PlatePos + new Vector3(-0.7f, 0f, -1.9f), 0.95f);
        }

        private void BuildBall()
        {
            var ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGo.name = "ball";
            ballGo.transform.SetParent(_field, false);
            ballGo.transform.localScale = Vector3.one * 0.22f;
            SetColor(ballGo, Color.white);
            _ball = ballGo.transform;
            _ball.gameObject.SetActive(false);

            _ballTrail = ballGo.AddComponent<TrailRenderer>();
            _ballTrail.time = 0.5f;
            _ballTrail.startWidth = 0.09f;
            _ballTrail.endWidth = 0.01f;
            _ballTrail.material = new Material(Shader.Find("Sprites/Default"));
            _ballTrail.startColor = Color.white;
            _ballTrail.endColor = new Color(1f, 1f, 1f, 0f);
        }

        public void PlaceCamera()
        {
            var camGO = GameObject.Find("Main Camera");
            if (camGO == null) return;
            camGO.transform.position = new Vector3(0f, 4.6f, -8.5f);
            camGO.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
        }

        private static void SetColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }
    }
}
