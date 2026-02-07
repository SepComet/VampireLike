using Entity.EntityData;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public class CoinEntity : EntityBase
    {
        private CoinData _coinData;
        
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

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

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<Player>();
            if (player != null && player.Available)
            {
                player.Coin += _coinData.Value;
                GameEntry.Entity.HideEntity(this);
            }
        }
    }
}