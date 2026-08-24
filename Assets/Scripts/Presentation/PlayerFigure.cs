using UnityEngine;

namespace DiamondTilt.Presentation
{
    public sealed class PlayerFigure : MonoBehaviour
    {
        private Transform _body;
        private Transform _head;
        private Transform _bat;

        private Vector3 _runFrom;
        private Vector3 _runTo;
        private float _runT;
        private float _runDuration = 1f;
        private bool _running;

        private float _swingT = -1f;

        public static PlayerFigure Create(Transform parent, string name, Color uniform, Vector3 position, float scale = 1f, bool withBat = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var figure = go.AddComponent<PlayerFigure>();
            figure.Build(uniform, scale, withBat);
            return figure;
        }

        private void Build(Color uniform, float scale, bool withBat)
        {
            _body = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            _body.name = "body";
            _body.SetParent(transform, false);
            _body.localPosition = new Vector3(0f, 0.65f, 0f);
            _body.localScale = new Vector3(0.42f, 0.55f, 0.34f);
            SetColor(_body.gameObject, uniform);

            _head = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _head.name = "head";
            _head.SetParent(transform, false);
            _head.localPosition = new Vector3(0f, 1.32f, 0f);
            _head.localScale = new Vector3(0.34f, 0.36f, 0.34f);
            SetColor(_head.gameObject, new Color(0.96f, 0.82f, 0.68f));

            if (withBat)
            {
                _bat = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                _bat.name = "bat";
                _bat.SetParent(transform, false);
                _bat.localScale = new Vector3(0.07f, 0.5f, 0.07f);
                _bat.localPosition = new Vector3(0.34f, 1.0f, 0.12f);
                _bat.rotation = Quaternion.Euler(20f, 0f, -18f);
                SetColor(_bat.gameObject, new Color(0.82f, 0.62f, 0.36f));
            }

            transform.localScale = Vector3.one * scale;
        }

        private static void SetColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            renderer.material.color = color;
        }

        public void SwingBat()
        {
            if (_bat != null) _swingT = 0f;
        }

        public void RunTo(Vector3 target, float duration = 0.9f)
        {
            _runFrom = transform.position;
            _runTo = new Vector3(target.x, transform.position.y, target.z);
            _runDuration = Mathf.Max(0.15f, duration);
            _runT = 0f;
            _running = true;
        }

        public void SnapTo(Vector3 position)
        {
            _running = false;
            transform.position = new Vector3(position.x, transform.position.y, position.z);
        }

        private void Update()
        {
            if (_running)
            {
                _runT += Time.deltaTime / _runDuration;
                if (_runT >= 1f) { _runT = 1f; _running = false; }
                transform.position = Vector3.Lerp(_runFrom, _runTo, Mathf.SmoothStep(0f, 1f, _runT));

                var bob = Mathf.Abs(Mathf.Sin(_runT * 24f)) * 0.06f;
                _body.localPosition = new Vector3(0f, 0.65f + bob, 0f);
            }

            if (_swingT >= 0f)
            {
                _swingT += Time.deltaTime;
                float k = Mathf.Clamp01(_swingT / 0.18f);
                float angle = Mathf.SmoothStep(35f, -110f, k);
                if (_bat != null) _bat.localRotation = Quaternion.Euler(20f, 0f, angle);
                if (_swingT > 0.35f)
                {
                    _swingT = -1f;
                    if (_bat != null) _bat.localRotation = Quaternion.Euler(20f, 0f, -18f);
                }
            }
        }
    }
}
