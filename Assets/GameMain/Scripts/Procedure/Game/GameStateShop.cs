using System.Collections.Generic;
using System.Linq;
using DataTable;
using Definition.Enum;
using Entity;
using GameFramework.DataTable;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UI;
using UnityGameFramework.Runtime;
using Random = UnityEngine.Random;

namespace Procedure
{
    public struct ShopFormContext
    {
        public GameStateShop GameStateShop;
        public int CurrentLevel;
        public int RefreshPrice;
        public int PlayerCoin;
        public List<GoodsItemViewData> GoodsItems;
    }

    public class GameStateShop : GameStateBase
    {
        public override GameStateType GameStateType => GameStateType.Shop;

        private ProcedureGame _procedureGame = null;
        private bool _shopOver = false;
        private ShopForm _shopForm = null;

        private DRGoods[] _drGoods = null;
        private IDataTable<DRProp> _propDataTable = null;
        private IDataTable<DRWeapon> _weaponDataTable = null;

        private Player _player = null;

        private ShopFormContext _shopFormContext;

        public void ShopOver()
        {
            if (_shopOver) return;
            _shopOver = true;
        }

        public void PurchaseGoods(int index)
        {
            if (_player.Coin < _shopFormContext.GoodsItems[index].Price) return;
            _player.Coin -= _shopFormContext.GoodsItems[index].Price;
            //TODO:OnGoodsPurchased
        }

        public void RefreshGoods()
        {
            if (_player.Coin < _shopFormContext.RefreshPrice) return;
            _player.Coin -= _shopFormContext.RefreshPrice;
            _shopFormContext = new ShopFormContext()
            {
                GameStateShop = this,
                CurrentLevel = _procedureGame.CurrentLevel,
                RefreshPrice = _shopFormContext.RefreshPrice + _procedureGame.CurrentLevel,
                PlayerCoin = _player.Coin,
                GoodsItems = InitRandomGoodsItems()
            };
            _shopForm.UpdateForm(_shopFormContext);
        }

        private List<GoodsItemViewData> InitRandomGoodsItems()
        {
            // 1. 随机生成商店商品数量
            int count = Random.Range(4, 6);

            // 2. 获取数据表中配置的商品总数
            int totalCount = _drGoods.Length;

            // 3. 创建要返回的商品列表
            List<GoodsItemViewData> items = new List<GoodsItemViewData>(count);

            // 4. 填充商品列表
            for (int i = 0; i < count; i++)
            {
                // 4.1 获取要添加的商品Id 
                int index = Random.Range(0, totalCount);

                // 4.2 从数据表中获取商品数据
                var drGoods = _drGoods[index];

                // 4.3 构建商品数据类
                var goodsItem = new GoodsItemViewData();

                // 4.4 填充商品数据（价格、名字、类型、图标、描述）
                goodsItem.Price = Random.Range(drGoods.MinPrice, drGoods.MaxPrice);

                if (drGoods.GoodsType == GoodsType.Prop)
                {
                    DRProp drProp = _propDataTable.GetDataRow(drGoods.GoodsTypeId);

                    goodsItem.Title = drProp.Title;
                    goodsItem.Type = "道具";
                    GameEntry.SpriteCache.GetSprite(drProp.IconAssetName, sprite => goodsItem.Icon = sprite);
                    goodsItem.Description = GoodsItemViewData.CreatePropDescription(drProp.Modifiers);
                }
                else if (drGoods.GoodsType is GoodsType.Weapon)
                {
                    DRWeapon drWeapon = _weaponDataTable.GetDataRow(drGoods.GoodsTypeId);

                    goodsItem.Title = drWeapon.Title;
                    goodsItem.Type = "武器";
                    GameEntry.SpriteCache.GetSprite(drWeapon.IconAssetName, sprite => goodsItem.Icon = sprite);
                    goodsItem.Description = GoodsItemViewData.CreateWeaponDescription(drWeapon);
                }
                else
                {
                    Log.Warning("Goods type not supported.");
                }

                // 4.5 添加到商品列表
                items.Add(goodsItem);
            }

            return items;
        }

        #region FSM

        public override void OnInit(ProcedureGame master)
        {
            Log.Debug("GameStateShop::OnInit");
            _procedureGame = master;
            _shopOver = false;
            _drGoods = GameEntry.DataTable.GetDataTable<DRGoods>().ToArray();
            _propDataTable = GameEntry.DataTable.GetDataTable<DRProp>();
            _weaponDataTable = GameEntry.DataTable.GetDataTable<DRWeapon>();
        }

        public override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateShop::OnEnter");

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);

            _shopOver = false;
            _player = _procedureGame.Player;

            _shopFormContext = new ShopFormContext()
            {
                GameStateShop = this,
                CurrentLevel = _procedureGame.CurrentLevel,
                RefreshPrice = _procedureGame.CurrentLevel,
                PlayerCoin = _player.Coin,
                GoodsItems = InitRandomGoodsItems()
            };
            GameEntry.UI.OpenUIForm(UIFormType.ShopForm, _shopFormContext);
        }

        public override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            Log.Debug("GameStateShop::OnUpdate");

            if (_shopOver)
            {
                _shopForm.Close();
                _procedureGame.ShopToBattle();
            }
        }

        public override void OnLeave(IFsm<IProcedureManager> procedureOwner)
        {
            Log.Debug("GameStateShop::OnLeave");

            _shopForm = null;

            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
        }

        public override void OnDestroy(IFsm<IProcedureManager> procedureOwner)
        {
            _procedureGame = null;
            _drGoods = null;
            _propDataTable = null;
            _weaponDataTable = null;

            Log.Debug("GameStateShop::OnDestroy");
        }

        #endregion

        #region Event Handlers

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args)) return;
            if (!(args.UserData is ShopFormContext data)) return;
            if (data.GameStateShop == this)
            {
                _shopForm = args.UIForm.Logic as ShopForm;
            }
        }

        #endregion
    }
}