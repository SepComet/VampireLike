using CustomEvent;
using GameFramework.Event;
using Procedure;
using TMPro;
using UnityEngine;

namespace UI
{
    public class ShopForm : UGuiForm
    {
        [SerializeField] private TMP_Text _titleText;

        [SerializeField] private TMP_Text _continueButtonText;
        
        [SerializeField] private TMP_Text _refreshPriceText;

        [SerializeField] private TMP_Text _playerCoinText;
        private int _currentCoin = 0;

        [SerializeField] private GoodsItem[] _goodsItems;

        private GameStateShop _stateShop;

        public void UpdateForm(ShopFormContext context)
        {
            _stateShop = context.GameStateShop;

            _titleText.text = $"商店(第{context.CurrentLevel}波)";
            _continueButtonText.text = $"继续(第{context.CurrentLevel + 1}波)";
            _refreshPriceText.text = $"-{context.RefreshPrice}";
            _playerCoinText.text = $"{context.PlayerCoin}";

            for (int i = 0; i < _goodsItems.Length; i++)
            {
                if (i < context.GoodsItems.Count)
                {
                    _goodsItems[i].Init(context.GameStateShop, context.GoodsItems[i]);
                    _goodsItems[i].gameObject.SetActive(true);
                }
                else
                {
                    _goodsItems[i].gameObject.SetActive(false);
                }
            }
        }
        
        #region ButtonClick

        public void OnContinueButtonClick()
        {
            _stateShop.ShopOver();
        }

        public void OnPurchaseButtonClick(int index)
        {
            _stateShop.PurchaseGoods(index);
        }

        public void OnRefreshButtonClick()
        {
            _stateShop.RefreshGoods();
        }

        #endregion

        #region FSM

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            GameEntry.Event.Subscribe(PlayerCoinChangeEventArgs.EventId, OnPlayerCoinChange);
            
            ShopFormContext context = (ShopFormContext)userData;
            UpdateForm(context);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            _stateShop = null;

            GameEntry.Event.Unsubscribe(PlayerCoinChangeEventArgs.EventId, OnPlayerCoinChange);
            
            base.OnClose(isShutdown, userData);
        }

        #endregion
        
        #region Event Handlers

        private void OnPlayerCoinChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerCoinChangeEventArgs args)) return;
            if (args.CoinCount == _currentCoin) return;
            
            _currentCoin = args.CoinCount;
            _playerCoinText.text = _currentCoin.ToString();
        }
        #endregion
    }
}