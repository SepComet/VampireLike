using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;

namespace CustomComponent
{
    public class DamageTextItemObject : ObjectBase
    {
        public static DamageTextItemObject Create(object target)
        {
            DamageTextItemObject itemObject = ReferencePool.Acquire<DamageTextItemObject>();
            itemObject.Initialize(target);
            return itemObject;
        }

        protected override void Release(bool isShutdown)
        {
            DamageTextItem item = (DamageTextItem)Target;
            if (item == null) return;
            Object.Destroy(item.gameObject);
        }
    }
}
