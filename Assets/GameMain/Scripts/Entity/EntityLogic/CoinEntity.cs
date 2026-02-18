using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class CoinEntity : EntityBase
    {
        private CoinData _coinData;
        private bool _isCollected;
        
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            _isCollected = false;

            if (userData is CoinData coinData)
            {
                _coinData = coinData;
            }
            else
            {
                Log.Error("CoinEntity.OnShow() called with wrong type.");
            }
            
            this.CachedTransform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }

        public bool TryCollect(Player player, float collectDistance = 0f)
        {
            if (_isCollected || player == null || !player.Available || _coinData == null) return false;

            if (collectDistance > 0f)
            {
                float distance = Vector3.Distance(player.CachedTransform.position, CachedTransform.position);
                if (distance > collectDistance) return false;
            }

            _isCollected = true;
            player.Coin += _coinData.Value;
            GameEntry.Entity.HideEntity(this);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other.GetComponent<Player>());
        }
    }
}
