using System.Collections.Generic;
using CustomEvent;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;

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

        [SerializeField] private ShopGoodsItem[] _goodsItems;

        [SerializeField] private DisplayListArea _propListArea;

        [SerializeField] private DisplayListArea _weaponListArea;

        private ShopFormContext _context;

        #endregion


        public void RefreshUI(ShopFormContext context)
        {
            _context = context;

            _titleText.text = $"商店 (Lv.{context.CurrentLevel})";
            _continueButtonText.text = $"继续 (Lv.{context.CurrentLevel + 1})";
            RefreshRefreshPrice(context.RefreshPrice);
            _playerCoinText.text = $"<sprite name=\"coin\" index=0> {context.PlayerCoin}";

            RefreshGoodsItems(context.GoodsItems);

            if (_propListArea != null && context.PropListContext != null)
            {
                _propListArea.OnInit(context.PropListContext);
            }

            if (_weaponListArea != null && context.WeaponListContext != null)
            {
                _weaponListArea.OnInit(context.WeaponListContext);
            }
        }

        internal void RefreshGoodsItems(List<GoodsItemContext> goodsItems)
        {
            foreach (var item in _goodsItems)
            {
                item.gameObject.SetActive(false);
            }

            if (goodsItems == null)
            {
                return;
            }

            int count = Mathf.Min(_goodsItems.Length, goodsItems.Count);
            for (int i = 0; i < count; i++)
            {
                _goodsItems[i].Init(goodsItems[i]);
                _goodsItems[i].gameObject.SetActive(true);
            }
        }

        internal void RefreshRefreshPrice(int refreshPrice)
        {
            _refreshPriceText.text = $"刷新 -{refreshPrice} <sprite name=\"coin\" index=0>";
        }

        internal void ApplyGoodsPurchased(int goodsIndex, DisplayItemContext displayItem)
        {
            SetGoodsItemVisible(goodsIndex, false);

            if (displayItem == null)
            {
                return;
            }

            if (displayItem.IsWeapon)
            {
                AddWeaponDisplayItem(displayItem);
            }
            else
            {
                AddPropDisplayItem(displayItem);
            }
        }

        private void SetGoodsItemVisible(int index, bool visible)
        {
            if (_goodsItems == null || index < 0 || index >= _goodsItems.Length)
            {
                Log.Warning("ShopForm.SetGoodsItemVisible: Invalid index.");
                return;
            }

            _goodsItems[index].gameObject.SetActive(visible);
        }

        private void AddPropDisplayItem(DisplayItemContext context)
        {
            if (_propListArea == null || context == null) return;
            _propListArea.AddItem(context);
        }

        private void AddWeaponDisplayItem(DisplayItemContext context)
        {
            if (_weaponListArea == null || context == null) return;
            _weaponListArea.AddItem(context);
        }

        internal void RemoveWeaponDisplayItem(int index)
        {
            if (_weaponListArea == null) return;
            _weaponListArea.RemoveItemAt(index);
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
            GameEntry.Event.Fire(this, RefreshEventArgs.Create(_context.RefreshPrice));
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

            Log.Warning("ShopForm requires ShopFormContext as userData.");
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            _context = null;

            GameEntry.Event.Unsubscribe(PlayerCoinChangeEventArgs.EventId, OnPlayerCoinChange);

            _propListArea?.OnReset();
            _weaponListArea?.OnReset();

            base.OnClose(isShutdown, userData);
        }

        #endregion

        #region Event Handlers

        private void OnPlayerCoinChange(object sender, GameEventArgs e)
        {
            if (!(e is PlayerCoinChangeEventArgs args)) return;
            if (args.CoinCount == _currentCoin) return;

            _currentCoin = args.CoinCount;
            _playerCoinText.text = $"<sprite name=\"coin\" index=0> {_currentCoin}";
        }

        #endregion
    }
}
