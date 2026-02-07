using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class ExpEntity:EntityBase
    {
        private ExpData _expData;
        
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            if (userData is ExpData expData)
            {
                _expData = expData;
            }
            else
            {
                Log.Error("expEntity.OnShow() called with wrong type.");
            }
            this.CachedTransform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<Player>();
            if (player != null && player.Available)
            {
                player.Exp += _expData.Value;
                GameEntry.Entity.HideEntity(this);
            }
        }
    }
}