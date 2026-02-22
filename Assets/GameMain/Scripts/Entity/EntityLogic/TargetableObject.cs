//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Components;
using Definition.DataStruct;
using Entity.EntityData;
using CustomUtility;
using UnityEngine;

namespace Entity
{
    /// <summary>
    /// 可作为目标的实体类。
    /// </summary>
    public abstract class TargetableObject : EntityBase
    {
        protected abstract TargetableObjectData _targetableObjectData { get; }

        public bool IsDead => _healthComponent.CurrentHealth <= 0;

        protected HealthComponent _healthComponent;

        public abstract ImpactData GetImpactData();

        public void ApplyDamage(EntityBase attacker, int damageHP)
        {
            float fromHPRatio = _healthComponent.HealthRatio;
            _healthComponent.TakeDamage(damageHP);
            float toHPRatio = _healthComponent.HealthRatio;
            if (fromHPRatio > toHPRatio)
            {
                GameEntry.HPBar.ShowHPBar(this, fromHPRatio, toHPRatio);
            }

            if (_healthComponent.CurrentHealth <= 0)
            {
                OnDead(attacker);
            }
        }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            _healthComponent = GetComponent<HealthComponent>();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
        }

        protected virtual void OnDead(EntityBase attacker)
        {
            GameEntry.Entity.HideEntity(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            EntityBase entity = other.GetComponentInParent<EntityBase>();
            if (entity == null || entity == this)
            {
                return;
            }

            if (entity is TargetableObject && entity.Id < Id)
            {
                // 碰撞事件由 Id 大的一方处理
                // 在这里约定 Enemy 的 Id 为非负数（通常从 0 开始）
                // 而其他的 Entity (Player, Weapon, Bullet) 的 Id 均小于 0
                return;
            }

            AIUtility.PerformCollision(this, entity);
        }
    }
}
