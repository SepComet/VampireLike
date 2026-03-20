using UnityEngine;

namespace Entity.Weapon
{
    public sealed class LanceThrustAttackEffect : IWeaponAttackEffect
    {
        private readonly float _duration = 0.18f;
        private readonly float _yOffset = 0.06f;
        private readonly float _lineWidth = 0.05f;
        private readonly Color _color = new(0.2f, 0.95f, 0.7f, 0.92f);

        public LanceThrustAttackEffect()
        {
        }

        public LanceThrustAttackEffect(float duration, float yOffset, float lineWidth, Color color)
        {
            _duration = duration;
            _yOffset = yOffset;
            _lineWidth = lineWidth;
            _color = color;
        }

        public void Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius)
        {
            if (weapon is not WeaponLance lance) return;

            Vector3 forward = lance.StrikeDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float halfWidth = Mathf.Max(0.1f, lance.HitHalfWidth);
            float halfLength = Mathf.Max(0.1f, lance.PierceLength * 0.5f);
            Vector3 center = position + Vector3.up * _yOffset;

            Vector3 frontCenter = center + forward * halfLength;
            Vector3 backCenter = center - forward * halfLength;

            Vector3[] corners =
            {
                frontCenter - right * halfWidth,
                frontCenter + right * halfWidth,
                backCenter + right * halfWidth,
                backCenter - right * halfWidth,
            };

            GameObject indicator = new GameObject("LanceThrustIndicator");
            indicator.transform.position = center;

            LineRenderer line = indicator.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = corners.Length;
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

            for (int i = 0; i < corners.Length; i++)
            {
                line.SetPosition(i, corners[i]);
            }

            Object.Destroy(indicator, Mathf.Max(0.01f, _duration));
        }
    }
}
