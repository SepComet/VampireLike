using UnityEngine;

namespace Entity.Weapon
{
    public sealed class KnifeRangeAttackEffect : IWeaponAttackEffect
    {
        private readonly float _duration = 0.2f;
        private readonly float _yOffset = 0.05f;
        private readonly float _lineWidth = 0.05f;
        private readonly int _segments = 48;
        private readonly Color _color = new(0.1f, 1f, 0.1f, 0.9f);

        public KnifeRangeAttackEffect()
        {
        }

        public KnifeRangeAttackEffect(float duration, float yOffset, float lineWidth, int segments, Color color)
        {
            _duration = duration;
            _yOffset = yOffset;
            _lineWidth = lineWidth;
            _segments = Mathf.Max(8, segments);
            _color = color;
        }

        public void Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius)
        {
            float safeRadius = Mathf.Max(0.1f, radius);

            GameObject indicator = new GameObject("KnifeRangeIndicator");
            indicator.transform.position = new Vector3(position.x, position.y + _yOffset, position.z);

            LineRenderer line = indicator.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = _segments;
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
            line.startColor = _color;
            line.endColor = _color;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            material.color = _color;
            line.material = material;

            float step = Mathf.PI * 2f / _segments;
            for (int i = 0; i < _segments; i++)
            {
                float angle = i * step;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * safeRadius, 0f, Mathf.Sin(angle) * safeRadius);
                line.SetPosition(i, indicator.transform.position + offset);
            }

            Object.Destroy(indicator, Mathf.Max(0.01f, _duration));
        }
    }
}