using System;
using System.Collections.Generic;
using System.Linq;
using CustomEvent;
using DataTable;
using Definition.Enum;
using Entity;
using GameFramework.DataTable;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UI;
using UnityEngine;
using UnityGameFramework.Runtime;
using Random = UnityEngine.Random;

namespace Procedure
{
    public class GameStateShop : GameStateBase
    {
        #region Property

        public override GameStateType GameStateType => GameStateType.Shop;

        private ProcedureGame _procedureGame;

        private DRGoods[] _drGoods;

        private DRProp[] _drProps;
        private IDataTable<DRProp> _propDataTable;

        private IDataTable<DRWeapon> _weaponDataTable;

        private ShopFormController _shopFormController;
        private ShopFormContext _shopFormContext;

        private LevelUpFormController _levelUpFormController;
        private LevelUpFormContext _levelUpFormContext;

        private int _shopRefreshTime = 0;

        private Player _player => _procedureGame.Player;

        private bool _shopOver = false;

        #endregion

        #region Methods

        #region LevelUp

        private LevelUpFormContext BuildLevelUpFormContext(int count = 4)
        {
            List<LevelUpPropContext> props = new List<LevelUpPropContext>();
            int total = _drProps.Length;
            if (total <= 0)
            {
                Log.Error("GameStateShop::BuildLevelUpFormContext(): _drProps == null");
                return null;
            }

            count = Mathf.Min(count, total);

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, total);
                DRProp drProp = _drProps[index];
                props.Add(new LevelUpPropContext
                {
                    PropId = drProp.Id,
                    Title = drProp.Title,
                    Type = "Prop",
                    Icon = null,
                    Description = GoodsItemContext.CreatePropDescription(drProp.Modifiers),
                    IconAssetName = drProp.IconAssetName
                });
            }

            return new LevelUpFormContext
            {
                Level = _player.CurrentLevel,
                Props = props
            };
        }

        #endregion

        #region Shop

        private void RefreshGoodsItems()
        {
            _shopFormContext = BuildShopFormContext();
            _shopFormContext.RefreshPrice = (_shopRefreshTime + 1) * _procedureGame.CurrentLevel;
            _shopRefreshTime++;
            _shopFormController.OpenUI(_shopFormContext);
        }

        private ShopFormContext BuildShopFormContext()
        {
            int currentLevel = _procedureGame.CurrentLevel;
            var context = new ShopFormContext
            {
                CurrentLevel = currentLevel,
                RefreshPrice = currentLevel,
                PlayerCoin = _player.Coin,
                GoodsItems = InitRandomGoodsItems()
            };
            return context;
        }

        private List<GoodsItemContext> InitRandomGoodsItems(int count = -1)
        {
            if (_drGoods == null || _drGoods.Length == 0)
            {
                Log.Error("GameStateShop::InitRandomGoodsItems(): _drGoods == null");
                return null;
            }

            count = Mathf.Max(count, Random.Range(4, 6));
            int totalCount = _drGoods.Length;
            List<GoodsItemContext> items = new List<GoodsItemContext>(count);
            if (totalCount <= 0) return items;
            count = Mathf.Min(count, totalCount);

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, totalCount);
                DRGoods drGoods = _drGoods[index];
                GoodsItemContext goodsItem = new GoodsItemContext
                {
                    Price = Random.Range(drGoods.MinPrice, drGoods.MaxPrice)
                };

                if (drGoods.GoodsType == GoodsType.Prop)
                {
                    DRProp drProp = _propDataTable.GetDataRow(drGoods.GoodsTypeId);
                    goodsItem.Title = drProp.Title;
                    goodsItem.Type = "Prop";
                    GameEntry.SpriteCache.GetSprite(drProp.IconAssetName, sprite => goodsItem.Icon = sprite);
                    goodsItem.Description = GoodsItemContext.CreatePropDescription(drProp.Modifiers);
                }
                else if (drGoods.GoodsType == GoodsType.Weapon)
                {
                    DRWeapon drWeapon = _weaponDataTable.GetDataRow(drGoods.GoodsTypeId);
                    goodsItem.Title = drWeapon.Title;
                    goodsItem.Type = "Weapon";
                    GameEntry.SpriteCache.GetSprite(drWeapon.IconAssetName, sprite => goodsItem.Icon = sprite);
                    goodsItem.Description = GoodsItemContext.CreateWeaponDescription(drWeapon);
                }

                items.Add(goodsItem);
            }

            return items;
        }

        #endregion

        #endregion

        #region FSM

        public override void OnInit(ProcedureGame master)
        {
            Log.Debug("GameStateShop::OnInit");
            _procedureGame = master;

            _levelUpFormController = new LevelUpFormController();
            _shopFormController = new ShopFormController();

            _drGoods = GameEntry.DataTable.GetDataTable<DRGoods>().ToArray();
            _propDataTable = GameEntry.DataTable.GetDataTable<DRProp>();
            _drProps = _propDataTable.ToArray();
            _weaponDataTable = GameEntry.DataTable.GetDataTable<DRWeapon>();

            _shopOver = false;
        }

        public override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateShop::OnEnter");

            GameEntry.Event.Subscribe(ShopRefreshEventArgs.EventId, ShopRefresh);
            GameEntry.Event.Subscribe(ShopPurchaseEventArgs.EventId, ShopPurchase);
            GameEntry.Event.Subscribe(ShopContinueEventArgs.EventId, ShopContinue);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);

            if (_procedureGame.PlayerPendingLevel != 0)
            {
                _levelUpFormContext = BuildLevelUpFormContext();
                _levelUpFormController.OpenUI(_levelUpFormContext);
            }
            else
            {
                _shopFormContext = BuildShopFormContext();
                _shopFormController.OpenUI(_shopFormContext);
            }
            
            _shopOver = false;
        }

        public override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            if (_shopOver)
            {
                _shopFormController?.CloseUI();
                _procedureGame.ShopToBattle();
            }
        }

        public override void OnLeave(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateShop::OnLeave");

            _shopFormContext = null;
            _shopFormController.CloseUI();
            _shopFormController = null;

            _levelUpFormContext = null;
            _levelUpFormController.CloseUI();
            _levelUpFormController = null;

            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
            GameEntry.Event.Unsubscribe(ShopRefreshEventArgs.EventId, ShopRefresh);
            GameEntry.Event.Unsubscribe(ShopPurchaseEventArgs.EventId, ShopPurchase);
            GameEntry.Event.Unsubscribe(ShopContinueEventArgs.EventId, ShopContinue);
        }

        public override void OnDestroy(IFsm<IProcedureManager> procedureOwner)
        {
            _procedureGame = null;
            _shopFormController = null;
            _shopFormContext = null;

            _levelUpFormController = null;
            _levelUpFormContext = null;

            _drGoods = null;
            _propDataTable = null;
            _weaponDataTable = null;

            Log.Debug("GameStateShop::OnDestroy");
        }

        #endregion

        #region Event Handlers

        private void ShopRefresh(object sender, EventArgs e)
        {
            if (!(e is ShopRefreshEventArgs args)) return;

            if (_player.Coin < args.Cost) return;
            _player.Coin -= args.Cost;
            RefreshGoodsItems();
        }


        private void ShopPurchase(object sender, EventArgs e)
        {
            if (!(e is ShopPurchaseEventArgs args)) return;

            int index = args.GoodsIndex;

            if (index < 0 && index >= _shopFormContext.GoodsItems.Count)
            {
                Log.Warning("GameStateShop::ShopPurchase: Invalid index");
                return;
            }

            if (_player.Coin < _shopFormContext.GoodsItems[index].Price) return;
            _player.Coin -= _shopFormContext.GoodsItems[index].Price;
            _shopFormContext.GoodsItems.RemoveAt(index);
            _shopFormController.OpenUI(_shopFormContext);
            // TODO: OnGoodsPurchased
        }

        private void ShopContinue(object sender, EventArgs e)
        {
            if (!(e is ShopContinueEventArgs)) return;

            _shopOver = true;
        }

        private void CloseUIFormComplete(object sender, EventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args)) return;

            if (args.UIFormAssetName == nameof(UIFormType.LevelUpForm))
            {

                if (--_procedureGame.PlayerPendingLevel != 0)
                {
                    _levelUpFormContext = BuildLevelUpFormContext();
                    _levelUpFormController.OpenUI(_levelUpFormContext);
                }
                else
                {
                    _levelUpFormContext = BuildLevelUpFormContext();
                    _levelUpFormController.OpenUI(_levelUpFormContext);
                }
            }
        }
        
        #endregion
    }
}