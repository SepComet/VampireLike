//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Entity;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    public class HideByBoundary : MonoBehaviour
    {
        private void OnTriggerExit(Collider other)
        {
            GameObject go = other.gameObject;
            EntityBase entity = go.GetComponent<EntityBase>();
            if (entity == null)
            {
                Log.Warning("Unknown GameObject '{0}', you must use entity only.", go.name);
                Destroy(go);
                return;
            }

            GameEntry.Entity.HideEntity(entity);
        }
    }
}
