using System.Collections.Generic;
using CustomEvent;
using Definition.DataStruct;
using Definition.Enum;
using CustomUtility;
using Entity.Weapon;
using GameFramework.Event;
using UnityEngine;
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
            GameEntry.Event.Subscribe(ShopWeaponRecycleEventArgs.EventId, WeaponRecycle);
            GameEntry.Event.Subscribe(ShopContinueEventArgs.EventId, ShopContinue);
            GameEntry.Event.Subscribe(DisplayItemShowEventArgs.EventId, DisplayItemShow);
        }

        protected override void UnsubscribeCustomEvents()
        {
            GameEntry.Event.Unsubscribe(RefreshEventArgs.EventId, Refresh);
            GameEntry.Event.Unsubscribe(ShopPurchaseEventArgs.EventId, ShopPurchase);
            GameEntry.Event.Unsubscribe(ShopWeaponRecycleEventArgs.EventId, WeaponRecycle);
            GameEntry.Event.Unsubscribe(ShopContinueEventArgs.EventId, ShopContinue);
            GameEntry.Event.Unsubscribe(DisplayItemShowEventArgs.EventId, DisplayItemShow);
        }

        #region BuildContext

        private ShopFormContext BuildContext(ShopFormRawData rawData)
        {
            if (rawData == null)
            {
                Log.Error("ShopFormController.BuildContext() rawData is null.");
                return null;
            }

            _rawData = rawData;

            return new ShopFormContext
            {
                CurrentLevel = rawData.CurrentLevel,
                RefreshPrice = rawData.RefreshPrice,
                PlayerCoin = rawData.PlayerCoin,
                GoodsItems = rawData.GoodsItems,
                PropListContext =
                    BuildDisplayListAreaContext(DisplayListAreaType.Prop, rawData.PropItems, rawData.PropMaxCount),
                WeaponListContext = BuildDisplayListAreaContext(DisplayListAreaType.Weapon, rawData.WeaponItems,
                    rawData.WeaponMaxCount)
            };
        }

        private static DisplayListAreaContext BuildDisplayListAreaContext(DisplayListAreaType listType,
            IReadOnlyList<object> items,
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
            if (listContext == null)
            {
                Log.Error("ShopFormController.AppendDisplayItemContext() listContext is null.");
                return;
            }

            if (newItem == null)
            {
                Log.Warning("ShopFormController.AppendDisplayItemContext() newItem is null.");
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

        public override void CloseUI()
        {
            base.CloseUI();
            GameEntry.Event.Fire(this, DisplayItemInfoHideEventArgs.Create(true));
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
            if (result == null)
            {
                Log.Error("ShopFormController.RefreshGoodsItems() result is null.");
                return;
            }

            if (Context == null)
            {
                Log.Error("ShopFormController.RefreshGoodsItems() Context is null.");
                return;
            }

            Context.GoodsItems = result.GoodsItems;
            Context.RefreshPrice = result.RefreshPrice;

            if (Form == null)
            {
                Log.Error("ShopFormController.RefreshGoodsItems() Form is null.");
                return;
            }

            Form.RefreshGoodsItems(result.GoodsItems);
            Form.RefreshRefreshPrice(result.RefreshPrice);
        }

        private void ApplyGoodsPurchased(ShopPurchaseResult result)
        {
            if (result == null)
            {
                Log.Error("ShopFormController.ApplyGoodsPurchased() result is null.");
                return;
            }

            if (Context == null)
            {
                Log.Error("ShopFormController.ApplyGoodsPurchased() Context is null.");
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

        private bool IsCurrentFormEventSender(object sender)
        {
            if (sender is ShopForm shopForm)
            {
                return shopForm == Form;
            }

            if (sender is Component component && Form != null)
            {
                return component.transform.IsChildOf(Form.transform);
            }

            return false;
        }

        private bool TryGetWeaponInfoRawData(int index, Vector3 targetPos, out DisplayItemInfoFormRawData rawData)
        {
            rawData = null;

            if (_rawData?.WeaponItems == null)
            {
                Log.Error("ShopFormController.TryGetWeaponInfoRawData() WeaponItems is null.");
                return false;
            }

            if (Context == null)
            {
                Log.Error("ShopFormController.TryGetWeaponInfoRawData() Context is null.");
                return false;
            }

            if (index < 0 || index >= _rawData.WeaponItems.Count)
            {
                Log.Error($"ShopFormController.TryGetWeaponInfoRawData() invalid weapon index: {index}.");
                return false;
            }

            WeaponBase weapon = _rawData.WeaponItems[index];
            if (weapon?.WeaponData == null)
            {
                Log.Error($"ShopFormController.TryGetWeaponInfoRawData() weapon data is null at index {index}.");
                return false;
            }

            var weaponData = weapon.WeaponData;
            rawData = new DisplayItemInfoFormRawData
            {
                TargetPos = targetPos,
                Index = index,
                IconAssetName = weaponData.IconAssetName,
                Title = weaponData.Title,
                Rarity = weaponData.Rarity,
                TypeText = "武器",
                Description = ItemDescUtility.CreateWeaponDescription(weaponData),
                Price = Mathf.FloorToInt(weaponData.Price * Context.WeaponRecycleRate),
                IsWeapon = true
            };
            return true;
        }

        private bool TryGetPropInfoRawData(int index, Vector3 targetPos, out DisplayItemInfoFormRawData rawData)
        {
            rawData = null;

            if (_rawData?.PropItems == null)
            {
                Log.Error("ShopFormController.TryGetPropInfoRawData() PropItems is null.");
                return false;
            }

            if (index < 0 || index >= _rawData.PropItems.Count)
            {
                Log.Error($"ShopFormController.TryGetPropInfoRawData() invalid prop index: {index}.");
                return false;
            }

            PropItem propItem = _rawData.PropItems[index];
            if (propItem == null)
            {
                Log.Error($"ShopFormController.TryGetPropInfoRawData() prop item is null at index {index}.");
                return false;
            }

            rawData = new DisplayItemInfoFormRawData
            {
                TargetPos = targetPos,
                Index = index,
                IconAssetName = propItem.IconAssetName,
                Title = propItem.Title,
                Rarity = propItem.Rarity,
                TypeText = "道具",
                Description = ItemDescUtility.CreatePropDescription(propItem),
                Price = 0,
                IsWeapon = false
            };
            return true;
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
            if (!(e is DisplayItemShowEventArgs args))
            {
                return;
            }

            if (!IsCurrentFormEventSender(sender))
            {
                return;
            }

            if (_rawData == null)
            {
                Log.Error("ShopFormController.DisplayItemShow() _rawData is null.");
                return;
            }

            DisplayItemInfoFormRawData rawData;
            bool success = args.IsWeapon
                ? TryGetWeaponInfoRawData(args.Index, args.TargetPos, out rawData)
                : TryGetPropInfoRawData(args.Index, args.TargetPos, out rawData);

            if (!success)
            {
                return;
            }

            GameEntry.UIRouter.OpenUI(UIFormType.DisplayItemInfoForm, rawData);
        }

        private void WeaponRecycle(object sender, GameEventArgs e)
        {
            if (!(e is ShopWeaponRecycleEventArgs args))
            {
                return;
            }

            if (sender is not DisplayItemInfoForm)
            {
                return;
            }

            if (_useCase == null || Context == null)
            {
                Log.Error("ShopFormController.WeaponRecycle() controller state is invalid.");
                return;
            }

            bool success = _useCase.TryRecycleWeapon(args.Index, args.Price);
            if (!success)
            {
                return;
            }

            if (Context.WeaponListContext != null)
            {
                int currentCount = Mathf.Max(0, Context.WeaponListContext.CurrentCount - 1);
                Context.WeaponListContext.CurrentCount = currentCount;
            }

            Form?.RemoveWeaponDisplayItem(args.Index);
            GameEntry.Event.Fire(this, DisplayItemInfoHideEventArgs.Create(true));
        }

        #endregion
    }
}