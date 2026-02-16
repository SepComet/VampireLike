using System.Collections.Generic;
using CustomEvent;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using Game.Utility;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class ShopFormController : UIFormControllerBase<ShopFormContext>
    {
        private ShopFormUseCase _useCase;

        private bool _pendingRefresh;

        private int? _shopFormSerialId;

        private ShopForm _shopForm;

        private ShopFormRawData _rawData;

        private ShopFormContext _context;

        private bool _isBindEvent;

        private void SubscribeEvents()
        {
            if (_isBindEvent) return;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            GameEntry.Event.Subscribe(RefreshEventArgs.EventId, Refresh);
            GameEntry.Event.Subscribe(ShopPurchaseEventArgs.EventId, ShopPurchase);
            GameEntry.Event.Subscribe(ShopContinueEventArgs.EventId, ShopContinue);
            GameEntry.Event.Subscribe(DisplayItemShowEventArgs.EventId, DisplayItemShow);
            GameEntry.Event.Subscribe(DisplayItemHideEventArgs.EventId, DisplayItemHide);

            _isBindEvent = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isBindEvent) return;

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            GameEntry.Event.Unsubscribe(RefreshEventArgs.EventId, Refresh);
            GameEntry.Event.Unsubscribe(ShopPurchaseEventArgs.EventId, ShopPurchase);
            GameEntry.Event.Unsubscribe(ShopContinueEventArgs.EventId, ShopContinue);
            GameEntry.Event.Unsubscribe(DisplayItemShowEventArgs.EventId, DisplayItemShow);
            GameEntry.Event.Unsubscribe(DisplayItemHideEventArgs.EventId, DisplayItemHide);

            _isBindEvent = false;
        }

        #region BuildContext

        private ShopFormContext BuildContext(ShopFormRawData rawData)
        {
            if (rawData == null)
            {
                return null;
            }

            _rawData = rawData;

            return new ShopFormContext
            {
                CurrentLevel = rawData.CurrentLevel,
                RefreshPrice = rawData.RefreshPrice,
                PlayerCoin = rawData.PlayerCoin,
                GoodsItems = rawData.GoodsItems,
                PropListContext = BuildDisplayListAreaContext("道具", rawData.PropItems, rawData.PropMaxCount),
                WeaponListContext = BuildDisplayListAreaContext("武器", rawData.WeaponItems, rawData.WeaponMaxCount)
            };
        }

        private static DisplayListAreaContext BuildDisplayListAreaContext(string title, IReadOnlyList<object> items,
            int maxCount)
        {
            DisplayItemContext[] itemContexts = new DisplayItemContext[items.Count];
            if (title == "武器")
            {
                if (items is IReadOnlyList<WeaponBase> weapons)
                {
                    for (int i = 0; i < weapons.Count; i++)
                    {
                        WeaponBase weapon = weapons[i];
                        if (weapon == null) break;
                        itemContexts[i] = BuildWeaponItem(weapon);
                    }
                }
            }
            else if (title == "道具")
            {
                if (items is IReadOnlyList<PropItem> propItems)
                {
                    for (int i = 0; i < propItems.Count; i++)
                    {
                        PropItem propItem = propItems[i];
                        if (propItem == null) break;
                        itemContexts[i] = BuildPropItem(propItem);
                    }
                }
            }

            int currentCount = itemContexts.Length;
            return new DisplayListAreaContext
            {
                Title = title,
                CurrentCount = currentCount,
                MaxCount = maxCount,
                ItemContexts = itemContexts
            };
        }

        private static DisplayItemContext BuildPropItem(PropItem propItem)
        {
            string iconAssetName = null;
            ItemRarity rarity = ItemRarity.None;

            if (propItem != null)
            {
                iconAssetName = propItem.IconAssetName;
                rarity = propItem.Rarity;
            }

            return new DisplayItemContext
            {
                IconAssetName = iconAssetName,
                Rarity = rarity,
                IsWeapon = false
            };
        }


        private static DisplayItemContext BuildWeaponItem(WeaponBase weaponBase)
        {
            string iconAssetName = null;
            ItemRarity rarity = ItemRarity.None;

            if (weaponBase != null && weaponBase.WeaponData != null)
            {
                iconAssetName = weaponBase.WeaponData.IconAssetName;
                rarity = weaponBase.WeaponData.Rarity;
            }

            return new DisplayItemContext
            {
                IconAssetName = iconAssetName,
                Rarity = rarity,
                IsWeapon = true
            };
        }

        private static void AppendDisplayItemContext(DisplayListAreaContext listContext, DisplayItemContext newItem)
        {
            if (listContext == null || newItem == null)
            {
                return;
            }

            int oldCount = listContext.ItemContexts != null ? listContext.ItemContexts.Length : 0;
            DisplayItemContext[] newContexts = new DisplayItemContext[oldCount + 1];
            if (oldCount > 0)
            {
                System.Array.Copy(listContext.ItemContexts, newContexts, oldCount);
            }

            newContexts[oldCount] = newItem;
            listContext.ItemContexts = newContexts;
            listContext.CurrentCount = oldCount + 1;
        }

        #endregion


        #region UI Methods

        protected override int? OpenUIInternal(ShopFormContext context)
        {
            if (context == null)
            {
                Log.Warning("ShopFormController.OpenUI() context is null.");
                return null;
            }

            _context = context;

            if (_shopForm != null && _shopFormSerialId.HasValue &&
                GameEntry.UI.HasUIForm(_shopFormSerialId.Value))
            {
                _shopForm.RefreshUI(_context);
                return _shopFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            SubscribeEvents();
            _shopFormSerialId = GameEntry.UI.OpenUIForm(UIFormType.ShopForm, context);
            return _shopFormSerialId;
        }

        public int? OpenUI(ShopFormRawData rawData)
        {
            ShopFormContext context = BuildContext(rawData);
            return OpenUIInternal(context);
        }

        public override int? OpenUI(object userData = null)
        {
            if (userData is ShopFormContext context)
            {
                return OpenUIInternal(context);
            }

            if (userData is ShopFormRawData rawDataFromUserData)
            {
                return OpenUI(rawDataFromUserData);
            }

            if (userData != null)
            {
                Log.Warning("ShopFormController.OpenUI() userData type is invalid.");
                return null;
            }

            if (_useCase == null)
            {
                Log.Error("ShopForm.OpenUI():: useCase is null.");
                return null;
            }

            ShopFormRawData rawData = _useCase.CreateInitialModel();
            return OpenUI(rawData);
        }

        public override void CloseUI()
        {
            _pendingRefresh = false;
            UnsubscribeEvents();
            if (_shopFormSerialId.HasValue)
            {
                if (GameEntry.UI.HasUIForm(_shopFormSerialId.Value))
                {
                    GameEntry.UI.CloseUIForm(_shopFormSerialId.Value);
                }

                _shopForm = null;
                _shopFormSerialId = null;
                return;
            }

            if (_shopForm != null)
            {
                _shopForm.Close();
                _shopForm = null;
            }
        }

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is ShopFormUseCase shopFormUseCase))
            {
                Log.Error("LevelUpForm.BindUseCase() useCase is invalid.");
                return;
            }

            _useCase = shopFormUseCase;
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_shopForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _shopForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        #endregion

        #region Service

        private void RefreshGoodsItems(ShopRefreshResult result)
        {
            if (_context == null || result == null)
            {
                return;
            }

            _context.GoodsItems = result.GoodsItems;
            _context.RefreshPrice = result.RefreshPrice;

            if (_shopForm == null)
            {
                return;
            }

            _shopForm.RefreshGoodsItems(result.GoodsItems);
            _shopForm.RefreshRefreshPrice(result.RefreshPrice);
        }

        private void ApplyGoodsPurchased(ShopPurchaseResult result)
        {
            if (_context == null || result == null)
            {
                return;
            }

            if (_context.GoodsItems != null && result.GoodsIndex >= 0 && result.GoodsIndex < _context.GoodsItems.Count)
            {
                _context.GoodsItems[result.GoodsIndex] = null;
            }

            if (result.DisplayItem != null)
            {
                if (result.DisplayItem.IsWeapon)
                {
                    AppendDisplayItemContext(_context.WeaponListContext, result.DisplayItem);
                }
                else
                {
                    AppendDisplayItemContext(_context.PropListContext, result.DisplayItem);
                }
            }

            _shopForm?.ApplyGoodsPurchased(result.GoodsIndex, result.DisplayItem);
        }

        #endregion

        #region Event Handlers

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args)) return;

            if (!_shopFormSerialId.HasValue) return;

            if (args.UIForm == null || args.UIForm.SerialId != _shopFormSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _shopForm = args.UIForm.Logic as ShopForm;

            if (_shopForm == null)
            {
                Log.Warning("ShopFormController open success but form logic is invalid.");
                return;
            }

            if (_pendingRefresh)
            {
                TryRefreshUI();
            }
        }

        private void CloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args))
            {
                return;
            }

            if (args.SerialId != _shopFormSerialId)
            {
                return;
            }

            _shopForm = null;
            _shopFormSerialId = null;
        }

        private void Refresh(object sender, GameEventArgs e)
        {
            if (!(sender is ShopForm))
            {
                return;
            }

            if (!(e is RefreshEventArgs args))
            {
                return;
            }

            ShopRefreshResult result = _useCase.TryRefresh(args.Cost);
            if (result == null)
            {
                return;
            }

            RefreshGoodsItems(result);
        }

        private void ShopPurchase(object sender, GameEventArgs e)
        {
            if (!(sender is ShopForm))
            {
                return;
            }

            if (!(e is ShopPurchaseEventArgs args))
            {
                return;
            }

            ShopPurchaseResult result = _useCase.TryPurchase(args.GoodsIndex);
            if (result == null)
            {
                return;
            }

            ApplyGoodsPurchased(result);
        }

        private void ShopContinue(object sender, GameEventArgs e)
        {
            if (!(sender is ShopForm))
            {
                return;
            }

            if (!(e is ShopContinueEventArgs))
            {
                return;
            }

            _useCase?.Continue();
        }

        private void DisplayItemShow(object sender, GameEventArgs e)
        {
            if (!(e is DisplayItemShowEventArgs args)) return;

            DisplayItemInfoFormRawData rawData = new();
            rawData.TargetPos = args.TargetPos;
            if (args.IsWeapon)
            {
                var weaponData = _rawData.WeaponItems[args.Index].WeaponData;
                rawData.IconAssetName = weaponData.IconAssetName;
                rawData.Title = weaponData.Title;
                rawData.Rarity = weaponData.Rarity;
                rawData.TypeText = "武器";
                rawData.Description = ItemDescUtility.CreateWeaponDescription(weaponData);
                rawData.Price = 0;
                rawData.IsWeapon = true;
            }
            else
            {
                var propItem = _rawData.PropItems[args.Index];
                rawData.IconAssetName = propItem.IconAssetName;
                rawData.Title = propItem.Title;
                rawData.Rarity = propItem.Rarity;
                rawData.TypeText = "道具";
                rawData.Description = ItemDescUtility.CreatePropDescription(propItem);
                rawData.Price = 0;
                rawData.IsWeapon = false;
            }

            GameEntry.UIRouter.OpenUI(UIFormType.DisplayItemInfoForm, rawData);
        }


        private void DisplayItemHide(object sender, GameEventArgs e)
        {
            if (!(e is DisplayItemHideEventArgs)) return;

            GameEntry.UIRouter.CloseUI(UIFormType.DisplayItemInfoForm);
        }

        #endregion
    }
}
