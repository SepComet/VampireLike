using GameFramework.ObjectPool;
using UnityEngine;

namespace Entity.Weapon
{
    public sealed class HandgunHitMarkerPooledInstance : MonoBehaviour
    {
        private Renderer _renderer;
        private IObjectPool<HandgunHitMarkerPoolObject> _ownerPool;
        private float _expireTime;
        private bool _isActive;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            if (Time.time < _expireTime)
            {
                return;
            }

            ReturnToPool();
        }

        public void ApplyMaterial(Material material)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_renderer == null)
            {
                return;
            }

            _renderer.sharedMaterial = material;
        }

        public void Activate(float duration, IObjectPool<HandgunHitMarkerPoolObject> pool)
        {
            _ownerPool = pool;
            _expireTime = Time.time + Mathf.Max(0.01f, duration);
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            _isActive = false;
            _ownerPool = null;
        }

        private void ReturnToPool()
        {
            _isActive = false;
            IObjectPool<HandgunHitMarkerPoolObject> pool = _ownerPool;
            _ownerPool = null;
            transform.SetParent(null, false);

            if (pool == null)
            {
                gameObject.SetActive(false);
                return;
            }

            pool.Unspawn(this);
        }
    }
}
