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
    public class ShopFormController : UIFormControllerCommonBase<ShopFormContext, ShopForm>
    {
        private ShopFormUseCase _useCase;
        private ShopFormRawData _rawData;

        protected override UIFormType UIFormTypeId => UIFormType.ShopForm;

        protected override void RefreshUI(ShopForm form, ShopFormContext context)
        {
            form.RefreshUI(context);
        }

        protected override void SubscribeCustomEvents()
        {
            GameEntry.Event.Subscribe(RefreshEventArgs.EventId, Refresh);
            GameEntry.Event.Subscribe(ShopPurchaseEventArgs.EventId, ShopPurchase);
            GameEntry.Event.Subscribe(ShopContinueEventArgs.EventId, ShopContinue);
            GameEntry.Event.Subscribe(DisplayItemShowEventArgs.EventId, DisplayItemShow);
            GameEntry.Event.Subscribe(DisplayItemHideEventArgs.EventId, DisplayItemHide);
        }

        protected override void UnsubscribeCustomEvents()
        {
            GameEntry.Event.Unsubscribe(RefreshEventArgs.EventId, Refresh);
            GameEntry.Event.Unsubscribe(ShopPurchaseEventArgs.EventId, ShopPurchase);
            GameEntry.Event.Unsubscribe(ShopContinueEventArgs.EventId, ShopContinue);
            GameEntry.Event.Unsubscribe(DisplayItemShowEventArgs.EventId, DisplayItemShow);
            GameEntry.Event.Unsubscribe(DisplayItemHideEventArgs.EventId, DisplayItemHide);
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
                PropListContext = BuildDisplayListAreaContext(DisplayListAreaType.Prop, rawData.PropItems, rawData.PropMaxCount),
                WeaponListContext = BuildDisplayListAreaContext(DisplayListAreaType.Weapon, rawData.WeaponItems, rawData.WeaponMaxCount)
            };
        }

        private static DisplayListAreaContext BuildDisplayListAreaContext(DisplayListAreaType listType, IReadOnlyList<object> items,
            int maxCount)
        {
            string title = GetDisplayListTitle(listType);
            if (items == null)
            {
                return new DisplayListAreaContext
                {
                    Title = title,
                    CurrentCount = 0,
                    MaxCount = maxCount,
                    ItemContexts = System.Array.Empty<DisplayItemContext>()
                };
            }

            DisplayItemContext[] itemContexts = new DisplayItemContext[items.Count];
            switch (listType)
            {
                case DisplayListAreaType.Weapon:
                    if (items is IReadOnlyList<WeaponBase> weapons)
                    {
                        for (int i = 0; i < weapons.Count; i++)
                        {
                            WeaponBase weapon = weapons[i];
                            if (weapon == null) break;
                            itemContexts[i] = BuildWeaponItem(weapon);
                        }
                    }
                    break;

                case DisplayListAreaType.Prop:
                    if (items is IReadOnlyList<PropItem> propItems)
                    {
                        for (int i = 0; i < propItems.Count; i++)
                        {
                            PropItem propItem = propItems[i];
                            if (propItem == null) break;
                            itemContexts[i] = BuildPropItem(propItem);
                        }
                    }
                    break;
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

        private static string GetDisplayListTitle(DisplayListAreaType listType)
        {
            return listType switch
            {
                DisplayListAreaType.Weapon => "武器",
                DisplayListAreaType.Prop => "道具",
                _ => string.Empty
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

        public override void BindUseCase(IUIUseCase useCase)
        {
            if (!(useCase is ShopFormUseCase shopFormUseCase))
            {
                Log.Error("LevelUpForm.BindUseCase() useCase is invalid.");
                return;
            }

            _useCase = shopFormUseCase;
        }

        #endregion

        #region Service

        private void RefreshGoodsItems(ShopRefreshResult result)
        {
            if (Context == null || result == null)
            {
                return;
            }

            Context.GoodsItems = result.GoodsItems;
            Context.RefreshPrice = result.RefreshPrice;

            if (Form == null)
            {
                return;
            }

            Form.RefreshGoodsItems(result.GoodsItems);
            Form.RefreshRefreshPrice(result.RefreshPrice);
        }

        private void ApplyGoodsPurchased(ShopPurchaseResult result)
        {
            if (Context == null || result == null)
            {
                return;
            }

            if (Context.GoodsItems != null && result.GoodsIndex >= 0 && result.GoodsIndex < Context.GoodsItems.Count)
            {
                Context.GoodsItems[result.GoodsIndex] = null;
            }

            if (result.DisplayItem != null)
            {
                if (result.DisplayItem.IsWeapon)
                {
                    AppendDisplayItemContext(Context.WeaponListContext, result.DisplayItem);
                }
                else
                {
                    AppendDisplayItemContext(Context.PropListContext, result.DisplayItem);
                }
            }

            Form?.ApplyGoodsPurchased(result.GoodsIndex, result.DisplayItem);
        }

        #endregion

        #region Event Handlers

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
            if (!(e is DisplayItemShowEventArgs args) || _rawData == null) return;

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
