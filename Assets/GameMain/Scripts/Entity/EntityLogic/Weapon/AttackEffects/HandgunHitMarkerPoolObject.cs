using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;

namespace Entity.Weapon
{
    public sealed class HandgunHitMarkerPoolObject : ObjectBase
    {
        public static HandgunHitMarkerPoolObject Create(object target)
        {
            HandgunHitMarkerPoolObject pooledObject = ReferencePool.Acquire<HandgunHitMarkerPoolObject>();
            pooledObject.Initialize(target);
            return pooledObject;
        }

        protected override void Release(bool isShutdown)
        {
            HandgunHitMarkerPooledInstance markerInstance = Target as HandgunHitMarkerPooledInstance;
            if (markerInstance == null)
            {
                return;
            }

            Object.Destroy(markerInstance.gameObject);
        }
    }
}
