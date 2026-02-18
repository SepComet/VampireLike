using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class ExpEntity:EntityBase
    {
        private ExpData _expData;
        private bool _isCollected;
        
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            _isCollected = false;

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

        public bool TryCollect(Player player, float collectDistance = 0f)
        {
            if (_isCollected || player == null || !player.Available || _expData == null) return false;

            if (collectDistance > 0f)
            {
                float distance = Vector3.Distance(player.CachedTransform.position, CachedTransform.position);
                if (distance > collectDistance) return false;
            }

            _isCollected = true;
            player.Exp += _expData.Value;
            GameEntry.Entity.HideEntity(this);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other.GetComponent<Player>());
        }
    }
}
