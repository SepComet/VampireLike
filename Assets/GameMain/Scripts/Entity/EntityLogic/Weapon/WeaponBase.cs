using System;
using Definition.DataStruct;
using Entity.EntityData;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity
{
    public enum WeaponStateType
    {
        Disabled,
        Check,
        Attack,
        Transition,
        Idle,
        Check_OutRange,
        Check_InRange,
    }

    /// <summary>
    /// 武器类。
    /// </summary>
    public abstract class WeaponBase : EntityBase
    {
        protected const string AttachPoint = "Weapon Point";
        protected bool _isAttacking = false;
        protected bool _isEnabled = false;
        public WeaponData WeaponData;
        public bool IsAttacking => _isAttacking;
        protected WeaponStateBase _currentState;

        #region FSM

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }

        //protected abstract void OnShow(object userData);
        // {
        //     base.OnShow(userData);
        //
        //     _weaponData = userData as WeaponData;
        //     if (_weaponData == null)
        //     {
        //         Log.Error("Weapon data is invalid.");
        //         return;
        //     }
        // }

        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);

            Name = Utility.Text.Format("Weapon of {0}", parentEntity.Name);
            CachedTransform.localPosition = Vector3.zero;
        }

        #endregion

        public abstract ImpactData GetImpactData();
        protected abstract void Attack();
        protected abstract void Check();
        protected abstract void TransitionTo(WeaponStateType newState);

        public void SetEnabled(bool value)
        {
            this._isEnabled = value;
            TransitionTo(WeaponStateType.Idle);
        }
    }

    public abstract class WeaponStateBase
    {
        public abstract WeaponStateType State { get; }
        public abstract void OnInit(WeaponBase weapon);
        public abstract void OnEnter();
        public abstract void OnUpdate(float elapseSeconds, float realElapseSeconds);
        public abstract void OnLeave();
        public override string ToString() => State.ToString();
    }
}