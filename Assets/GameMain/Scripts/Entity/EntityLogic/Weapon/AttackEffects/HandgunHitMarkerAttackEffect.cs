using UnityEngine;

namespace Entity.Weapon
{
    public sealed class HandgunHitMarkerAttackEffect : IWeaponAttackEffect
    {
        private readonly float _size;
        private readonly float _yOffset;
        private readonly float _duration;
        private readonly Color _color;

        public HandgunHitMarkerAttackEffect(float size, float yOffset, float duration, Color color)
        {
            _size = size;
            _yOffset = yOffset;
            _duration = duration;
            _color = color;
        }

        public void Play(WeaponBase weapon, Vector3 position, EntityBase target, float radius)
        {
            if (target == null) return;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "HandgunHitMarker";

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            marker.transform.SetParent(target.CachedTransform, false);
            marker.transform.localPosition = new Vector3(0f, _yOffset, 0f);
            marker.transform.localScale = Vector3.one * Mathf.Max(0.01f, _size);

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                Material material = new Material(shader);
                material.color = _color;
                renderer.material = material;
            }

            Object.Destroy(marker, Mathf.Max(0.01f, _duration));
        }
    }
}