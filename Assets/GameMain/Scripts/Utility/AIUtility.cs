//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using UnityEngine;
using UnityGameFramework.Runtime;
using Random = UnityEngine.Random;

namespace StarForce
{
    /// <summary>
    /// AI 工具类。
    /// </summary>
    public static class AIUtility
    {
        private static Dictionary<CampPair, RelationType> s_CampPairToRelation =
            new Dictionary<CampPair, RelationType>();

        private static Dictionary<KeyValuePair<CampType, RelationType>, CampType[]> s_CampAndRelationToCamps =
            new Dictionary<KeyValuePair<CampType, RelationType>, CampType[]>();

        static AIUtility()
        {
            s_CampPairToRelation.Add(new CampPair(CampType.Player, CampType.Player), RelationType.Friendly);
            s_CampPairToRelation.Add(new CampPair(CampType.Player, CampType.Enemy), RelationType.Hostile);
            s_CampPairToRelation.Add(new CampPair(CampType.Player, CampType.Neutral), RelationType.Neutral);
            s_CampPairToRelation.Add(new CampPair(CampType.Player, CampType.Player2), RelationType.Hostile);
            s_CampPairToRelation.Add(new CampPair(CampType.Player, CampType.Enemy2), RelationType.Hostile);
            s_CampPairToRelation.Add(new CampPair(CampType.Player, CampType.Neutral2), RelationType.Neutral);

            s_CampPairToRelation.Add(new CampPair(CampType.Enemy, CampType.Enemy), RelationType.Friendly);
            s_CampPairToRelation.Add(new CampPair(CampType.Enemy, CampType.Neutral), RelationType.Neutral);
            s_CampPairToRelation.Add(new CampPair(CampType.Enemy, CampType.Player2), RelationType.Hostile);
            s_CampPairToRelation.Add(new CampPair(CampType.Enemy, CampType.Enemy2), RelationType.Hostile);
            s_CampPairToRelation.Add(new CampPair(CampType.Enemy, CampType.Neutral2), RelationType.Neutral);

            s_CampPairToRelation.Add(new CampPair(CampType.Neutral, CampType.Neutral), RelationType.Neutral);
            s_CampPairToRelation.Add(new CampPair(CampType.Neutral, CampType.Player2), RelationType.Neutral);
            s_CampPairToRelation.Add(new CampPair(CampType.Neutral, CampType.Enemy2), RelationType.Neutral);
            s_CampPairToRelation.Add(new CampPair(CampType.Neutral, CampType.Neutral2), RelationType.Hostile);

            s_CampPairToRelation.Add(new CampPair(CampType.Player2, CampType.Player2), RelationType.Friendly);
            s_CampPairToRelation.Add(new CampPair(CampType.Player2, CampType.Enemy2), RelationType.Hostile);
            s_CampPairToRelation.Add(new CampPair(CampType.Player2, CampType.Neutral2), RelationType.Neutral);

            s_CampPairToRelation.Add(new CampPair(CampType.Enemy2, CampType.Enemy2), RelationType.Friendly);
            s_CampPairToRelation.Add(new CampPair(CampType.Enemy2, CampType.Neutral2), RelationType.Neutral);

            s_CampPairToRelation.Add(new CampPair(CampType.Neutral2, CampType.Neutral2), RelationType.Neutral);
        }

        /// <summary>
        /// 获取两个阵营之间的关系。
        /// </summary>
        /// <param name="first">阵营一。</param>
        /// <param name="second">阵营二。</param>
        /// <returns>阵营间关系。</returns>
        public static RelationType GetRelation(CampType first, CampType second)
        {
            if (first > second)
            {
                CampType temp = first;
                first = second;
                second = temp;
            }

            RelationType relationType;
            if (s_CampPairToRelation.TryGetValue(new CampPair(first, second), out relationType))
            {
                return relationType;
            }

            Log.Warning("Unknown relation between '{0}' and '{1}'.", first.ToString(), second.ToString());
            return RelationType.Unknown;
        }

        /// <summary>
        /// 获取和指定具有特定关系的所有阵营。
        /// </summary>
        /// <param name="camp">指定阵营。</param>
        /// <param name="relation">关系。</param>
        /// <returns>满足条件的阵营数组。</returns>
        public static CampType[] GetCamps(CampType camp, RelationType relation)
        {
            KeyValuePair<CampType, RelationType> key = new KeyValuePair<CampType, RelationType>(camp, relation);
            CampType[] result = null;
            if (s_CampAndRelationToCamps.TryGetValue(key, out result))
            {
                return result;
            }

            // TODO: GC Alloc
            List<CampType> camps = new List<CampType>();
            Array campTypes = Enum.GetValues(typeof(CampType));
            for (int i = 0; i < campTypes.Length; i++)
            {
                CampType campType = (CampType)campTypes.GetValue(i);
                if (GetRelation(camp, campType) == relation)
                {
                    camps.Add(campType);
                }
            }

            // TODO: GC Alloc
            result = camps.ToArray();
            s_CampAndRelationToCamps[key] = result;

            return result;
        }

        /// <summary>
        /// 获取实体间的距离。
        /// </summary>
        /// <returns>实体间的距离。</returns>
        public static float GetDistance(EntityBase fromEntity, EntityBase toEntity)
        {
            Transform fromTransform = fromEntity.CachedTransform;
            Transform toTransform = toEntity.CachedTransform;
            return (toTransform.position - fromTransform.position).magnitude;
        }

        /// <summary>
        /// 获取实体间的平方距离。
        /// </summary>
        /// <returns>实体间的平方距离。</returns>
        public static float GetSqrMagnitude(EntityBase fromEntity, EntityBase toEntity)
        {
            Transform fromTransform = fromEntity.CachedTransform;
            Transform toTransform = toEntity.CachedTransform;
            return (toTransform.position - fromTransform.position).sqrMagnitude;
        }

        public static void PerformCollision(TargetableObject entity, EntityBase other)
        {
            if (entity == null || other == null)
            {
                return;
            }

            // TargetableObject target = other as TargetableObject;
            // if (target != null)
            // {
            //     ImpactData entityImpactData = entity.GetImpactData();
            //     ImpactData targetImpactData = target.GetImpactData();
            //     if (GetRelation(entityImpactData.Camp, targetImpactData.Camp) == RelationType.Friendly)
            //     {
            //         return;
            //     }
            //
            //     int entityDamageHP = CalcDamageHP(targetImpactData.Attack, entityImpactData.Defense);
            //     int targetDamageHP = CalcDamageHP(entityImpactData.Attack, targetImpactData.Defense);
            //
            //     entity.ApplyDamage(target, entityDamageHP);
            //     target.ApplyDamage(entity, targetDamageHP);
            //     return;
            // }

            Bullet bullet = other as Bullet;
            if (bullet != null)
            {
                ImpactData entityImpactData = entity.GetImpactData();
                ImpactData bulletImpactData = bullet.GetImpactData();
                if (GetRelation(entityImpactData.Camp, bulletImpactData.Camp) == RelationType.Friendly)
                {
                    return;
                }

                int entityDamageHP = CalcDamageHP(bulletImpactData.AttackBase, bulletImpactData.AttackStat,
                    entityImpactData.DefenseStat, entityImpactData.DodgeStat);

                entity.ApplyDamage(bullet, entityDamageHP);
                GameEntry.Entity.HideEntity(bullet);
                return;
            }

            WeaponBase weapon = other as WeaponBase;
            if (weapon != null)
            {
                if (!weapon.IsAttacking) return;
                ImpactData entityImpactData = entity.GetImpactData();
                ImpactData weaponImpactData = weapon.GetImpactData();
                if (GetRelation(entityImpactData.Camp, weaponImpactData.Camp) == RelationType.Friendly)
                {
                    return;
                }

                int entityDamageHP = CalcDamageHP(weaponImpactData.AttackBase, weaponImpactData.AttackStat,
                    entityImpactData.DefenseStat, entityImpactData.DefenseStat);

                entity.ApplyDamage(weapon, entityDamageHP);
                return;
            }
        }

        private static int CalcDamageHP(int attack, StatProperty attackStat, StatProperty defenseStat,
            StatProperty dodgeStat)
        {
            // 1. 处理闪避（闪避率取值 (0, 0.9)，不允许拉满闪避）
            if (dodgeStat != null)
            {
                if (Random.value < Mathf.Clamp(dodgeStat.Percent, 0, 0.9f)) return 0;    
            }
            
            // 2. 处理攻击加成 最终伤害 = (基础伤害 + 伤害提升固定值) * 伤害提升率
            float damage = attack;
            if (attackStat != null)
            {
                damage = (attack + attackStat.Value) * attackStat.Percent;    
            }
            
            // 3. 处理防御加成 最终伤害 = (基础伤害 - 防御提升固定值) / 防御提升率
            if (defenseStat != null)
            {
                damage = (damage - defenseStat.Value) / defenseStat.Percent;    
            }
            
            // 将伤害限制为正整数，避免堆防御导致无法造成伤害
            if (damage < 1) return 1;
            else return Mathf.CeilToInt(damage);
        }

        [StructLayout(LayoutKind.Auto)]
        private struct CampPair
        {
            private readonly CampType m_First;
            private readonly CampType m_Second;

            public CampPair(CampType first, CampType second)
            {
                m_First = first;
                m_Second = second;
            }

            public CampType First => m_First;

            public CampType Second => m_Second;
        }
    }
}