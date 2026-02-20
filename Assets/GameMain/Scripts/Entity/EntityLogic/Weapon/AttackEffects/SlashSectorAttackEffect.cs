using UnityEngine;

namespace Entity.Weapon
{
    public sealed class SlashSectorAttackEffect : IWeaponAttackEffect
    {
        private readonly float _duration = 0.2f;
        private readonly float _yOffset = 0.06f;
        private readonly float _lineWidth = 0.05f;
        private readonly int _segments = 24;
        private readonly Color _color = new(1f, 0.45f, 0.2f, 0.9f);

        public SlashSectorAttackEffect()
        {
        }

        public SlashSectorAttackEffect(float duration, float yOffset, float lineWidth, int segments, Color color)
        {
            _duration = duration;
            _yOffset = yOffset;
            _lineWidth = lineWidth;
            _segments = Mathf.Max(6, segments);
            _color = color;
        }

        public void Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius)
        {
            if (weapon is not WeaponSlash slash) return;

            float safeRadius = Mathf.Max(0.1f, radius);
            float sectorAngle = Mathf.Clamp(slash.SectorAngle, 1f, 360f);

            GameObject indicator = new GameObject("SlashSectorIndicator");
            Vector3 center = new Vector3(position.x, position.y + _yOffset, position.z);
            indicator.transform.position = center;

            LineRenderer line = indicator.AddComponent<LineRenderer>();
            line.loop = false;
            line.useWorldSpace = true;
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

            float half = sectorAngle * 0.5f;
            int arcPoints = _segments + 1;
            int positionCount = arcPoints + 2;
            line.positionCount = positionCount;

            Vector3 forward = weapon.CachedTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            Quaternion baseRotation = Quaternion.LookRotation(forward, Vector3.up);

            line.SetPosition(0, center);
            for (int i = 0; i < arcPoints; i++)
            {
                float t = arcPoints <= 1 ? 0f : i / (float)(arcPoints - 1);
                float angle = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 worldDir = baseRotation * dir;
                line.SetPosition(i + 1, center + worldDir * safeRadius);
            }

            line.SetPosition(positionCount - 1, center);

            UnityEngine.Object.Destroy(indicator, Mathf.Max(0.01f, _duration));
        }
    }
}