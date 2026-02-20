using System;
using System.Collections.Generic;
using Definition;
using Entity.EntityData;
using UnityGameFramework.Runtime;
using DataTable;
using CustomUtility;

namespace Entity
{
    public static class EntityExtension
    {
        private static int s_SerialId = -10;

        private const string EntityNamespace = "Entity.";
        private static readonly Dictionary<string, Type> _typeDict;
        private static readonly Dictionary<int, string> _assetNameDict;

        static EntityExtension()
        {
            _typeDict = new Dictionary<string, Type>();
            _assetNameDict = new Dictionary<int, string>();
        }

        public static EntityBase GetGameEntity(this EntityComponent entityComponent, int entityId)
        {
            UnityGameFramework.Runtime.Entity entity = entityComponent.GetEntity(entityId);
            if (entity == null)
            {
                return null;
            }

            return (EntityBase)entity.Logic;
        }

        public static void HideEntity(this EntityComponent entityComponent, EntityBase entity)
        {
            entityComponent.HideEntity(entity.Entity);
        }

        public static void AttachEntity(this EntityComponent entityComponent, EntityBase entityBase, int ownerId,
            string parentTransformPath = null, object userData = null)
        {
            entityComponent.AttachEntity(entityBase.Entity, ownerId, parentTransformPath, userData);
        }

        public static void ShowPlayer(this EntityComponent entityComponent, PlayerData data)
        {
            entityComponent.ShowEntity(typeof(Player), "Player", Constant.AssetPriority.PlayerAsset, data);
        }

        public static void ShowEnemy(this EntityComponent entityComponent, EnemyData data)
        {
            if (data == null)
            {
                Log.Warning("Enemy data is invalid.");
                return;
            }

            var enemyType = TryGetType(data.EnemyType.ToString());
            var assetName = TryGetAssetName(data.EntityTypeId);

            entityComponent.ShowEntity(
                entityId: data.Id,
                entityLogicType: enemyType,
                entityAssetName: AssetUtility.GetEntityAsset(assetName),
                entityGroupName: "Enemy",
                priority: Constant.AssetPriority.BulletAsset,
                userData: data);
        }

        public static void ShowWeapon(this EntityComponent entityComponent, WeaponData data)
        {
            if (data == null)
            {
                Log.Warning("Weapon data is invalid.");
                return;
            }

            var weaponType = TryGetType("Weapon." + data.WeaponType);
            var assetName = TryGetAssetName(data.EntityTypeId);

            entityComponent.ShowEntity(
                entityId: data.Id,
                entityLogicType: weaponType,
                entityAssetName: AssetUtility.GetEntityAsset(assetName),
                entityGroupName: "Weapon",
                priority: Constant.AssetPriority.BulletAsset,
                userData: data);
        }

        // public static void ShowBullet(this EntityComponent entityComponent, BulletData data)
        // {
        //     if (data == null)
        //     {
        //         Log.Warning("Bullet data is invalid.");
        //         return;
        //     }
        //
        //     var bulletType = TryGetType(data.BulletType.ToString());
        //     string assetName = TryGetAssetName(data.EntityTypeId);
        //
        //     entityComponent.ShowEntity(
        //         entityId: data.Id,
        //         entityLogicType: bulletType,
        //         entityAssetName: AssetUtility.GetEntityAsset(assetName),
        //         entityGroupName: "Bullet",
        //         priority: Constant.AssetPriority.BulletAsset,
        //         userData: data);
        // }

        public static void ShowEffect(this EntityComponent entityComponent, EffectData data)
        {
            entityComponent.ShowEntity(typeof(Effect), "Effect", Constant.AssetPriority.EffectAsset, data);
        }

        public static void ShowCoin(this EntityComponent entityComponent, CoinData data)
        {
            entityComponent.ShowEntity(typeof(CoinEntity), "Drop", Constant.AssetPriority.EffectAsset, data);
        }

        public static void ShowExp(this EntityComponent entityComponent, ExpData data)
        {
            entityComponent.ShowEntity(typeof(ExpEntity), "Drop", Constant.AssetPriority.EffectAsset, data);
        }

        private static void ShowEntity(this EntityComponent entityComponent, Type logicType, string entityGroup,
            int priority, EntityDataBase data)
        {
            if (data == null)
            {
                Log.Warning("Data is invalid.");
                return;
            }

            DREntity drEntity = GameEntry.DataTable.GetDataTableRow<DREntity>(data.TypeId);
            if (drEntity == null)
            {
                Log.Warning("Can not load entity id '{0}' from data table.", data.TypeId.ToString());
                return;
            }

            entityComponent.ShowEntity(data.Id, logicType, AssetUtility.GetEntityAsset(drEntity.AssetName), entityGroup,
                priority, data);
        }

        private static Type TryGetType(string rawTypeName)
        {
            string typeName = EntityNamespace + rawTypeName;
            if (!_typeDict.TryGetValue(typeName, out Type type))
            {
                type = Type.GetType(typeName);
                if (type == null)
                {
                    Log.Warning("Can not load entity type '{0}'.", typeName);
                    return null;
                }

                _typeDict.Add(typeName, type);
            }

            return type;
        }

        private static string TryGetAssetName(int entityId)
        {
            if (!_assetNameDict.TryGetValue(entityId, out string assetName))
            {
                DREntity drEntity = GameEntry.DataTable.GetDataTableRow<DREntity>(entityId);
                if (drEntity == null)
                {
                    Log.Warning("Can not load bullet entity id '{0}' from data table.", entityId.ToString());
                    return null;
                }

                assetName = drEntity.AssetName;
                _assetNameDict.Add(entityId, assetName);
            }

            return assetName;
        }

        public static int GenerateSerialId(this EntityComponent entityComponent)
        {
            return --s_SerialId;
        }
    }
}