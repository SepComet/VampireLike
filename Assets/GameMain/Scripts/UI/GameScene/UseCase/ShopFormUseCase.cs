using System.Collections.Generic;
using System.Linq;
using DataTable;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using Entity.EntityData;
using CustomUtility;
using GameFramework.DataTable;
using Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
using Random = UnityEngine.Random;

namespace UI
{
    internal sealed class ShopRefreshResult
    {
        public List<GoodsItemContext> GoodsItems;
        public int RefreshPrice;
    }

    internal sealed class ShopPurchaseResult
    {
        public int GoodsIndex;
        public DisplayItemContext DisplayItem;
    }

    internal class ShopFormUseCase : IUIUseCase
    {
        private readonly  ProcedureGame _procedureGame;
        private readonly GameStateShop _gameStateShop;
        
        private readonly DRGoods[] _allGoods;
        private readonly IDataTable<DRProp> _propDataTable;
        private readonly IDataTable<DRWeapon> _weaponDataTable;
        private readonly IDataTable<DRLevelRarity> _levelRarityTable;

        private readonly List<DRGoods> _validGoods = new();
        private readonly Dictionary<ItemRarity, List<DRGoods>> _goodsByRarity = new();

        private readonly List<ShopGoodsSelection> _selections = new();
        private int _refreshTime;

        private Player Player => _procedureGame.Player;

        private sealed class ShopGoodsSelection
        {
            public GoodsType GoodsType;
            public int GoodsTypeId;
            public int Price;
        }

        public ShopFormUseCase(ProcedureGame procedureGame, GameStateShop gameStateShop)
        {
            _procedureGame = procedureGame;
            _gameStateShop = gameStateShop;
            _allGoods = GameEntry.DataTable.GetDataTable<DRGoods>().ToArray();
            _propDataTable = GameEntry.DataTable.GetDataTable<DRProp>();
            _weaponDataTable = GameEntry.DataTable.GetDataTable<DRWeapon>();
            _levelRarityTable = GameEntry.DataTable.GetDataTable<DRLevelRarity>();

            BuildGoodsRarityPools();
        }

        public ShopFormRawData CreateInitialModel()
        {
            _refreshTime = 0;

            Player player = Player;
            if (_procedureGame == null || player == null)
            {
                return null;
            }

            int currentLevel = _procedureGame.CurrentLevel;
            return new ShopFormRawData
            {
                CurrentLevel = currentLevel,
                RefreshPrice = currentLevel,
                PlayerCoin = player.Coin,
                GoodsItems = BuildRandomGoodsItems(4),
                PropItems = player.Props,
                PropMaxCount = -1,
                WeaponItems = player.Weapons,
                WeaponMaxCount = player.WeaponCapacity
            };
        }

        public ShopRefreshResult TryRefresh(int cost)
        {
            Player player = Player;
            if (player == null || _procedureGame == null || player.Coin < cost)
            {
                return null;
            }

            player.Coin -= cost;

            int refreshPrice = (_refreshTime + 1) * _procedureGame.CurrentLevel;
            _refreshTime++;

            return new ShopRefreshResult
            {
                GoodsItems = BuildRandomGoodsItems(4),
                RefreshPrice = refreshPrice
            };
        }

        public ShopPurchaseResult TryPurchase(int goodsIndex)
        {
            if (goodsIndex < 0 || goodsIndex >= _selections.Count)
            {
                Log.Warning("ShopFormUseCase::TryPurchase: Invalid index");
                return null;
            }

            ShopGoodsSelection selection = _selections[goodsIndex];
            if (selection == null)
            {
                Log.Warning("ShopFormUseCase::TryPurchase: Item already purchased.");
                return null;
            }

            Player player = Player;
            if (player == null || player.Coin < selection.Price)
            {
                return null;
            }

            DisplayItemContext displayItem = ApplyGoodsPurchase(selection);
            if (displayItem == null)
            {
                return null;
            }

            player.Coin -= selection.Price;
            _selections[goodsIndex] = null;

            return new ShopPurchaseResult
            {
                GoodsIndex = goodsIndex,
                DisplayItem = displayItem
            };
        }

        public void Continue()
        {
            _gameStateShop.ShopFinish = true;
        }

        private List<GoodsItemContext> BuildRandomGoodsItems(int count)
        {
            if (_allGoods == null || _allGoods.Length == 0)
            {
                Log.Error("ShopFormUseCase::BuildRandomGoodsItems(): _allGoods is empty");
                return null;
            }

            int finalCount = Mathf.Min(count, _allGoods.Length);
            List<GoodsItemContext> goodsItems = new List<GoodsItemContext>(finalCount);
            _selections.Clear();

            int currentLevel = _procedureGame != null ? _procedureGame.CurrentLevel : 1;

            for (int i = 0; i < finalCount; i++)
            {
                ItemRarity rarity = RarityUtility.SelectRarityForLevel(_levelRarityTable, currentLevel);
                DRGoods drGoods = PickGoodsByRarity(rarity);
                if (drGoods == null)
                {
                    Log.Warning("ShopFormUseCase::BuildRandomGoodsItems: No available goods for selection.");
                    break;
                }

                GoodsItemContext goodsItem = new GoodsItemContext();
                int price;

                if (drGoods.GoodsType == GoodsType.Prop)
                {
                    DRProp drProp = _propDataTable.GetDataRow(drGoods.GoodsTypeId);
                    if (drProp == null)
                    {
                        Log.Warning($"ShopFormUseCase::BuildRandomGoodsItems: Missing DRProp, id = {drGoods.GoodsTypeId}");
                        continue;
                    }

                    price = CalculateRandomizedPrice(drProp.Price, drProp.PriceRandomPercent);
                    goodsItem.Title = drProp.Title;
                    goodsItem.Type = "Prop";
                    goodsItem.Rarity = drProp.Rarity;
                    GameEntry.SpriteCache.GetSprite(drProp.IconAssetName, sprite => goodsItem.Icon = sprite);
                    goodsItem.Description = ItemDescUtility.CreatePropDescription(drProp.Modifiers);
                }
                else if (drGoods.GoodsType == GoodsType.Weapon)
                {
                    DRWeapon drWeapon = _weaponDataTable.GetDataRow(drGoods.GoodsTypeId);
                    if (drWeapon == null)
                    {
                        Log.Warning($"ShopFormUseCase::BuildRandomGoodsItems: Missing DRWeapon, id = {drGoods.GoodsTypeId}");
                        continue;
                    }

                    price = CalculateRandomizedPrice(drWeapon.Price, drWeapon.PriceRandomPercent);
                    goodsItem.Title = drWeapon.Title;
                    goodsItem.Type = "Weapon";
                    goodsItem.Rarity = drWeapon.Rarity;
                    GameEntry.SpriteCache.GetSprite(drWeapon.IconAssetName, sprite => goodsItem.Icon = sprite);
                    goodsItem.Description = ItemDescUtility.CreateWeaponDescription(drWeapon);
                }
                else
                {
                    Log.Warning($"ShopFormUseCase::BuildRandomGoodsItems: Unsupported goods type = {drGoods.GoodsType}");
                    continue;
                }

                goodsItem.Price = price;

                _selections.Add(new ShopGoodsSelection
                {
                    GoodsType = drGoods.GoodsType,
                    GoodsTypeId = drGoods.GoodsTypeId,
                    Price = price
                });

                goodsItems.Add(goodsItem);
            }

            return goodsItems;
        }

        private void BuildGoodsRarityPools()
        {
            _goodsByRarity.Clear();
            _validGoods.Clear();

            if (_allGoods == null) return;

            foreach (DRGoods goods in _allGoods)
            {
                if (goods == null) continue;

                if (!TryGetGoodsRarity(goods, out ItemRarity rarity))
                {
                    continue;
                }

                _validGoods.Add(goods);

                if (!_goodsByRarity.TryGetValue(rarity, out List<DRGoods> list))
                {
                    list = new List<DRGoods>();
                    _goodsByRarity.Add(rarity, list);
                }

                list.Add(goods);
            }
        }

        private DRGoods PickGoodsByRarity(ItemRarity rarity)
        {
            if (_goodsByRarity.TryGetValue(rarity, out List<DRGoods> list) && list.Count > 0)
            {
                return list[Random.Range(0, list.Count)];
            }

            if (_validGoods.Count > 0)
            {
                return _validGoods[Random.Range(0, _validGoods.Count)];
            }

            return null;
        }

        private bool TryGetGoodsRarity(DRGoods goods, out ItemRarity rarity)
        {
            rarity = ItemRarity.None;
            if (goods == null)
            {
                return false;
            }

            if (goods.GoodsType == GoodsType.Prop)
            {
                DRProp drProp = _propDataTable != null ? _propDataTable.GetDataRow(goods.GoodsTypeId) : null;
                if (drProp == null)
                {
                    Log.Warning($"ShopFormUseCase::TryGetGoodsRarity: Missing DRProp, id = {goods.GoodsTypeId}");
                    return false;
                }

                rarity = drProp.Rarity;
                return true;
            }

            if (goods.GoodsType == GoodsType.Weapon)
            {
                DRWeapon drWeapon = _weaponDataTable != null ? _weaponDataTable.GetDataRow(goods.GoodsTypeId) : null;
                if (drWeapon == null)
                {
                    Log.Warning($"ShopFormUseCase::TryGetGoodsRarity: Missing DRWeapon, id = {goods.GoodsTypeId}");
                    return false;
                }

                rarity = drWeapon.Rarity;
                return true;
            }

            Log.Warning($"ShopFormUseCase::TryGetGoodsRarity: Unsupported goods type = {goods.GoodsType}");
            return false;
        }

        private static int CalculateRandomizedPrice(int basePrice, float priceRandomPercent)
        {
            float normalizedPercent = priceRandomPercent > 1f ? priceRandomPercent / 100f : priceRandomPercent;
            normalizedPercent = Mathf.Max(0f, normalizedPercent);
            float offsetPercent = Random.Range(-normalizedPercent, normalizedPercent);
            int finalPrice = Mathf.RoundToInt(basePrice * (1f + offsetPercent));
            return Mathf.Max(1, finalPrice);
        }

        private DisplayItemContext ApplyGoodsPurchase(ShopGoodsSelection goods)
        {
            if (goods == null || Player == null)
            {
                return null;
            }

            if (goods.GoodsType == GoodsType.Prop)
            {
                DRProp drProp = _propDataTable != null ? _propDataTable.GetDataRow(goods.GoodsTypeId) : null;
                if (drProp == null || drProp.Modifiers == null || drProp.Modifiers.Length == 0)
                {
                    Log.Warning("ShopFormUseCase::ApplyGoodsPurchase: Prop modifiers are empty");
                    return null;
                }

                Player.AddProp(new PropItem(drProp.Modifiers, drProp.Rarity, drProp.Title, drProp.IconAssetName));
                return new DisplayItemContext
                {
                    IconAssetName = drProp.IconAssetName,
                    Rarity = drProp.Rarity,
                    IsWeapon = false
                };
            }

            if (goods.GoodsType == GoodsType.Weapon)
            {
                if (Player.Weapons != null && Player.Weapons.Count >= Player.WeaponCapacity)
                {
                    Log.Warning("ShopFormUseCase::ApplyGoodsPurchase: Weapon capacity is full.");
                    return null;
                }

                DRWeapon drWeapon = _weaponDataTable != null ? _weaponDataTable.GetDataRow(goods.GoodsTypeId) : null;
                if (drWeapon == null)
                {
                    Log.Warning($"ShopFormUseCase::ApplyGoodsPurchase: Missing DRWeapon, id = {goods.GoodsTypeId}");
                    return null;
                }

                var weaponData = CreateWeaponData(goods.GoodsTypeId);
                if (weaponData == null)
                {
                    Log.Warning(
                        $"ShopFormUseCase::ApplyGoodsPurchase: Unsupported weapon type id = {goods.GoodsTypeId}");
                    return null;
                }

                GameEntry.Entity.ShowWeapon(weaponData);

                return new DisplayItemContext
                {
                    IconAssetName = drWeapon.IconAssetName,
                    Rarity = drWeapon.Rarity,
                    IsWeapon = true
                };
            }

            return null;
        }

        private WeaponData CreateWeaponData(int weaponTypeId)
        {
            int entityId = GameEntry.Entity.GenerateSerialId();
            int ownerId = Player.Id;
            CampType ownerCamp = CampType.Player;

            WeaponType weaponType = (WeaponType)weaponTypeId;
            switch (weaponType)
            {
                case WeaponType.WeaponKnife:
                    return new WeaponKnifeData(entityId, ownerId, ownerCamp);
                case WeaponType.WeaponHandgun:
                    return new WeaponHandgunData(entityId, ownerId, ownerCamp);
                case WeaponType.WeaponSlash:
                    return new WeaponSlashData(entityId, ownerId, ownerCamp);
                default:
                    return null;
            }
        }

        public bool TryRecycleWeapon(int argsIndex, int argsPrice)
        {
            Player player = Player;
            if (player == null)
            {
                return false;
            }

            var weapons = player.Weapons;
            if (weapons == null || weapons.Count <= 1)
            {
                Log.Warning("ShopFormUseCase::TryRecycleWeapon: Can not recycle the last weapon.");
                return false;
            }

            if (argsIndex < 0 || argsIndex >= weapons.Count)
            {
                Log.Warning($"ShopFormUseCase::TryRecycleWeapon: Invalid weapon index = {argsIndex}");
                return false;
            }

            var weapon = weapons[argsIndex];
            if (weapon == null)
            {
                Log.Warning("ShopFormUseCase::TryRecycleWeapon: Weapon is null.");
                return false;
            }

            int recyclePrice = Mathf.Max(0, argsPrice);
            if (!player.RemoveWeapon(weapon))
            {
                Log.Warning("ShopFormUseCase::TryRecycleWeapon: Remove weapon failed.");
                return false;
            }

            player.Coin += recyclePrice;
            return true;
        }
    }
}
