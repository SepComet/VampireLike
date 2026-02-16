using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;

namespace UI
{
    public class DisplayItemObject : ObjectBase
    {
        public static DisplayItemObject Create(object target)
        {
            DisplayItemObject displayItemObject = ReferencePool.Acquire<DisplayItemObject>();
            displayItemObject.Initialize(target);
            return displayItemObject;
        }

        protected override void Release(bool isShutdown)
        {
            DisplayItem item = (DisplayItem)Target;
            if (item == null)
            {
                return;
            }

            Object.Destroy(item.gameObject);
        }
    }
}
