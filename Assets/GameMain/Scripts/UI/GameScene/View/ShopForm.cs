using CustomEvent;
using GameFramework.Event;
using TMPro;
using UnityEngine;

namespace UI
{
    public class ShopForm : UGuiForm
    {
        #region Property

        [SerializeField] private TMP_Text _titleText;

        [SerializeField] private TMP_Text _continueButtonText;

        [SerializeField] private TMP_Text _refreshPriceText;

        [SerializeField] private TMP_Text _playerCoinText;
        private int _currentCoin;

        [SerializeField] private GoodsItem[] _goodsItems;

        private ShopFormContext _context;

        #endregion


        public void RefreshUI(ShopFormContext context)
        {
            _context = context;

            _titleText.text = $"商店 (Lv.{context.CurrentLevel})";
            _continueButtonText.text = $"继续 (Lv.{context.CurrentLevel + 1})";
            _refreshPriceText.text = $"-{context.RefreshPrice}";
            _playerCoinText.text = context.PlayerCoin.ToString();

            foreach (var item in _goodsItems)
            {
                item.gameObject.SetActive(false);
            }

            if (_context.GoodsItems == null) return;
            for (int i = 0; i < _context.GoodsItems.Count; i++)
            {
                _goodsItems[i].Init(context.GoodsItems[i]);
                _goodsItems[i].gameObject.SetActive(true);
            }
        }

        #region ButtonClick

        public void OnContinueButtonClick()
        {
            GameEntry.Event.Fire(this, ShopContinueEventArgs.Create());
        }

        public void OnPurchaseButtonClick(int index)
        {
            GameEntry.Event.Fire(this, ShopPurchaseEventArgs.Create(index));
        }

        public void OnRefreshButtonClick()
        {
            GameEntry.Event.Fire(this, ShopRefreshEventArgs.Create(_context.RefreshPrice));
        }

        #endregion

        #region FSM

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            GameEntry.Event.Subscribe(PlayerCoinChangeEventArgs.EventId, OnPlayerCoinChange);

            if (userData is ShopFormContext context)
            {
                RefreshUI(context);
                return;
            }

            UnityGameFramework.Runtime.Log.Warning("ShopForm requires ShopFormContext as userData.");
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            _context = null;

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