using System.Collections.Generic;
using Components;
using Definition.DataStruct;
using Definition.Enum;
using Entity.EntityData;
using GameFramework;
using CustomUtility;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Entity.Weapon
{
    public enum WeaponStateType
    {
        Disabled,
        Attack,
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

        [SerializeField] protected bool _isEnabled = false;

        public WeaponData WeaponData { get; protected set; }

        public bool IsAttacking => _isAttacking;

        protected ITargetSelector TargetSelector { get; set; }

        protected Dictionary<WeaponStateType, WeaponStateBase> _states;
        
        protected WeaponStateBase _currentState;

        protected EntityBase _target;

        protected float _currAttackTimer;

        protected float _sqrRange;

        protected StatProperty AttackStat { get; private set; } = new();

        private StatComponent _attackStatComponent;
        private System.Action<StatModifier, bool> _attackStatCallback;
        
        private static readonly List<EntityBase> s_EmptyCandidates = new();

        #region Lifecycle

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            if (TargetSelector == null)
            {
                TargetSelector = CreateSelector(DefaultTargetSelectorType);
            }
        }

        protected sealed override void OnShow(object userData)
        {
            base.OnShow(userData);

            if (!OnWeaponShow(userData)) return;

            EnsureStatesBuilt();
            TransitionTo(InitialState);
        }

        protected sealed override void OnHide(bool isShutdown, object userData)
        {
            ReleaseAttackStatSubscription();
            OnWeaponHide(userData);
            base.OnHide(isShutdown, userData);
        }

        protected sealed override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);

            Name = Utility.Text.Format("Weapon of {0}", parentEntity.Name);
            CachedTransform.localPosition = Vector3.zero;

            OnWeaponAttach(parentEntity, parentTransform, userData);
        }

        protected sealed override void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            OnWeaponDetach(parentEntity, userData);
            base.OnDetachFrom(parentEntity, userData);
        }

        protected virtual bool OnWeaponShow(object userData) => true;

        protected virtual void OnWeaponHide(object userData)
        {
        }

        protected virtual void OnWeaponAttach(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
        }

        protected virtual void OnWeaponDetach(EntityLogic parentEntity, object userData)
        {
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (!_isEnabled) return;
            _currentState?.OnUpdate(elapseSeconds, realElapseSeconds);
        }

        #endregion

        public abstract ImpactData GetImpactData();
        protected abstract void Attack();
        protected abstract void Check();
        protected abstract void BuildStates();

        protected virtual WeaponStateType InitialState => WeaponStateType.Idle;
        protected virtual TargetSelectorType DefaultTargetSelectorType => TargetSelectorType.Nearest;

        protected void RegisterState(WeaponStateBase state)
        {
            if (state == null)
            {
                Log.Error("Weapon state is invalid.");
                return;
            }

            state.OnInit(this);
            _states[state.State] = state;
        }

        protected void EnsureStatesBuilt()
        {
            if (_states != null) return;

            _states = new Dictionary<WeaponStateType, WeaponStateBase>();
            BuildStates();
        }

        protected virtual void TransitionTo(WeaponStateType newState)
        {
            if (_states == null || !_states.TryGetValue(newState, out var nextState))
            {
                Log.Error("Weapon state not found: {0}", newState);
                return;
            }

            _currentState?.OnLeave();
            _currentState = nextState;
            _currentState.OnEnter();
        }

        public void SetEnabled(bool value)
        {
            _isEnabled = value;

            OnEnabledChanged(value);

            if (_states == null || _states.Count == 0) return;

            if (value)
            {
                TransitionTo(InitialState);
                return;
            }

            if (_states.ContainsKey(WeaponStateType.Disabled))
            {
                TransitionTo(WeaponStateType.Disabled);
            }
        }

        protected virtual void OnEnabledChanged(bool enabled)
        {
        }

        protected T RequireWeaponData<T>(object userData) where T : WeaponData
        {
            var data = userData as T;
            if (data == null)
            {
                Log.Error("{0} data is invalid.", typeof(T).Name);
            }

            return data;
        }

        protected virtual IEnumerable<EntityBase> GetCandidates()
        {
            var enemyManager = GameEntry.EnemyManager;
            return enemyManager != null ? enemyManager.Enemies : s_EmptyCandidates;
        }

        protected EntityBase SelectTarget(float maxSqrRange)
        {
            return TargetSelector != null ? TargetSelector.SelectTarget(this, GetCandidates(), maxSqrRange) : null;
        }

        protected float GetSqrDistance(EntityBase target)
        {
            return target != null ? AIUtility.GetSqrMagnitudeXZ(this, target) : float.MaxValue;
        }

        protected bool IsInRange(EntityBase target, float sqrRange)
        {
            if (target == null) return false;
            return AIUtility.GetSqrMagnitudeXZ(this, target) < sqrRange;
        }

        protected bool TryQueueAreaCollisionQuery(in Vector3 center, float radius, int maxTargets = 16)
        {
            var simulationWorld = GameEntry.SimulationWorld;
            if (simulationWorld == null)
            {
                return false;
            }

            int ownerEntityId = WeaponData != null ? WeaponData.OwnerId : Id;
            return simulationWorld.TryRequestAreaCollision(Id, ownerEntityId, in center, radius, maxTargets);
        }

        protected bool TryQueueSectorCollisionQuery(in Vector3 center, float radius, in Vector3 direction,
            float halfAngleDeg, int maxTargets = 16)
        {
            var simulationWorld = GameEntry.SimulationWorld;
            if (simulationWorld == null)
            {
                return false;
            }

            int ownerEntityId = WeaponData != null ? WeaponData.OwnerId : Id;
            return simulationWorld.TryRequestSectorCollision(Id, ownerEntityId, in center, radius, in direction,
                halfAngleDeg, maxTargets);
        }

        protected void SetTargetSelector(TargetSelectorType selectorType)
        {
            TargetSelector = CreateSelector(selectorType);
        }

        protected static ITargetSelector CreateSelector(TargetSelectorType selectorType)
        {
            switch (selectorType)
            {
                case TargetSelectorType.HighestHealth:
                    return new HighestHealthTargetSelector();
                case TargetSelectorType.LowestHealth:
                    return new LowestHealthTargetSelector();
                case TargetSelectorType.Nearest:
                default:
                    return new NearestTargetSelector();
            }
        }

        protected void BindAttackStatFromOwner(EntityLogic parentEntity)
        {
            ReleaseAttackStatSubscription();
            AttackStat = new StatProperty();

            if (parentEntity is not Player player) return;

            var statComponent = player.GetComponent<StatComponent>();
            if (statComponent == null) return;

            AttackStat = statComponent.GetStat(StatType.Attack);
            _attackStatComponent = statComponent;
            _attackStatCallback = (modifier, isApply) =>
                statComponent.UpdateStat(AttackStat, modifier, isApply);
            statComponent.Subscribe(StatType.Attack, _attackStatCallback);
        }

        protected void ReleaseAttackStatSubscription()
        {
            if (_attackStatComponent != null && _attackStatCallback != null)
            {
                _attackStatComponent.Unsubscribe(StatType.Attack, _attackStatCallback);
            }

            _attackStatComponent = null;
            _attackStatCallback = null;
            AttackStat = new StatProperty();
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


