using GameFramework.ObjectPool;
using UnityEngine;

namespace Entity.Weapon
{
    public sealed class HandgunHitMarkerAttackEffect : IWeaponAttackEffect
    {
        private const string PoolName = "Weapon.HandgunHitMarker";
        private const float PoolAutoReleaseInterval = 60f;
        private const int PoolCapacity = 128;
        private const float PoolExpireTime = 120f;
        private const int PoolPriority = 0;

        private static IObjectPool<HandgunHitMarkerPoolObject> s_Pool;

        private readonly float _size;
        private readonly float _yOffset;
        private readonly float _duration;
        private readonly Color _color;
        private Material _sharedMaterial;

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
            if (!TrySpawnMarker(out HandgunHitMarkerPooledInstance markerInstance))
            {
                return;
            }

            Transform targetTransform = target.CachedTransform;
            Vector3 worldPosition = targetTransform != null ? targetTransform.position : position;
            markerInstance.transform.SetParent(null, false);
            markerInstance.transform.position = worldPosition + Vector3.up * _yOffset;
            markerInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, _size);
            markerInstance.ApplyMaterial(GetSharedMaterial());
            markerInstance.Activate(Mathf.Max(0.01f, _duration), s_Pool);
        }

        private bool TrySpawnMarker(out HandgunHitMarkerPooledInstance markerInstance)
        {
            markerInstance = null;
            IObjectPool<HandgunHitMarkerPoolObject> pool = EnsurePool();
            if (pool == null)
            {
                return false;
            }

            HandgunHitMarkerPoolObject pooledObject = pool.Spawn();
            if (pooledObject != null)
            {
                markerInstance = pooledObject.Target as HandgunHitMarkerPooledInstance;
                if (markerInstance != null)
                {
                    return true;
                }
            }

            GameObject markerGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerGameObject.name = "HandgunHitMarker";
            Collider collider = markerGameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            markerInstance = markerGameObject.AddComponent<HandgunHitMarkerPooledInstance>();
            markerGameObject.SetActive(false);
            pool.Register(HandgunHitMarkerPoolObject.Create(markerInstance), true);
            return true;
        }

        private static IObjectPool<HandgunHitMarkerPoolObject> EnsurePool()
        {
            var poolComponent = GameEntry.ObjectPool;
            if (poolComponent == null)
            {
                return null;
            }

            if (s_Pool != null && poolComponent.HasObjectPool<HandgunHitMarkerPoolObject>(PoolName))
            {
                return s_Pool;
            }

            s_Pool = poolComponent.HasObjectPool<HandgunHitMarkerPoolObject>(PoolName)
                ? poolComponent.GetObjectPool<HandgunHitMarkerPoolObject>(PoolName)
                : poolComponent.CreateSingleSpawnObjectPool<HandgunHitMarkerPoolObject>(
                    PoolName,
                    PoolAutoReleaseInterval,
                    PoolCapacity,
                    PoolExpireTime,
                    PoolPriority);
            return s_Pool;
        }

        private Material GetSharedMaterial()
        {
            if (_sharedMaterial != null)
            {
                return _sharedMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            _sharedMaterial = new Material(shader)
            {
                color = _color
            };
            return _sharedMaterial;
        }
    }
}
