using UnityEngine;

namespace Entity.Weapon
{
    public sealed class LightningStrikeAttackEffect : IWeaponAttackEffect
    {
        private readonly float _duration = 0.22f;
        private readonly float _yOffset = 0.2f;
        private readonly float _ringLineWidth = 0.045f;
        private readonly float _boltLineWidth = 0.08f;
        private readonly int _ringSegments = 32;
        private readonly float _boltHeight = 3f;
        private readonly Color _ringColor = new(0.35f, 0.82f, 1f, 0.92f);
        private readonly Color _boltColor = new(0.85f, 0.96f, 1f, 0.97f);

        public void Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius)
        {
            float safeRadius = Mathf.Max(0.1f, radius);
            Vector3 center = new Vector3(position.x, position.y + _yOffset, position.z);

            GameObject root = new GameObject("LightningStrikeEffect");
            root.transform.position = center;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material ringMaterial = new Material(shader);
            ringMaterial.color = _ringColor;

            LineRenderer ring = root.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = _ringSegments;
            ring.startWidth = _ringLineWidth;
            ring.endWidth = _ringLineWidth;
            ring.startColor = _ringColor;
            ring.endColor = _ringColor;
            ring.material = ringMaterial;

            float step = Mathf.PI * 2f / _ringSegments;
            for (int i = 0; i < _ringSegments; i++)
            {
                float angle = i * step;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * safeRadius, 0f, Mathf.Sin(angle) * safeRadius);
                ring.SetPosition(i, center + offset);
            }

            GameObject bolt = new GameObject("Bolt");
            bolt.transform.SetParent(root.transform, false);

            Material boltMaterial = new Material(shader);
            boltMaterial.color = _boltColor;

            LineRenderer boltLine = bolt.AddComponent<LineRenderer>();
            boltLine.loop = false;
            boltLine.useWorldSpace = true;
            boltLine.positionCount = 4;
            boltLine.startWidth = _boltLineWidth;
            boltLine.endWidth = _boltLineWidth * 0.55f;
            boltLine.startColor = _boltColor;
            boltLine.endColor = _boltColor;
            boltLine.material = boltMaterial;

            Vector3 top = center + Vector3.up * _boltHeight;
            Vector3 middleA = center + Vector3.up * (_boltHeight * 0.66f) +
                              new Vector3(Random.Range(-0.18f, 0.18f), 0f, Random.Range(-0.18f, 0.18f));
            Vector3 middleB = center + Vector3.up * (_boltHeight * 0.3f) +
                              new Vector3(Random.Range(-0.12f, 0.12f), 0f, Random.Range(-0.12f, 0.12f));

            boltLine.SetPosition(0, top);
            boltLine.SetPosition(1, middleA);
            boltLine.SetPosition(2, middleB);
            boltLine.SetPosition(3, center);

            Object.Destroy(root, Mathf.Max(0.01f, _duration));
        }
    }
}
