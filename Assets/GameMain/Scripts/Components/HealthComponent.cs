using System;
using CustomEvent;
using Definition.DataStruct;
using Definition.Enum;
using UnityEngine;

namespace Components
{
    public class HealthComponent : MonoBehaviour
    {
        private int _maxHealthBase;
        private int _currentHealth;
        private bool _isDead => _currentHealth <= 0;

        private StatComponent _statComponent;

        private StatProperty _maxHealthStat;
        private Action<StatModifier, bool> _maxHealthStatCallback;
        public StatProperty MaxHealthStat => _maxHealthStat;

        private StatProperty _defenseStat;
        private Action<StatModifier, bool> _defenseStatCallback;
        public StatProperty DefenseStat => _defenseStat;

        private StatProperty _dodgeStat;
        private Action<StatModifier, bool> _dodgeStatCallback;
        public StatProperty DodgeStat => _dodgeStat;

        private bool _isPlayer => _statComponent != null;

        public float HealthRatio => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        public int MaxHealth => Mathf.CeilToInt((_maxHealthBase + _maxHealthStat.Value) * _maxHealthStat.Percent);

        public int CurrentHealth
        {
            get => _currentHealth;
            set => _currentHealth = value;
        }

        #region FSM

        public void OnInit(int maxHealth, StatComponent statComponent = null)
        {
            _maxHealthBase = maxHealth;
            _currentHealth = maxHealth;
            _statComponent = statComponent;

            _statComponent = statComponent;
            if (_statComponent != null)
            {
                _maxHealthStat = _statComponent.GetStat(StatType.MaxHealth);
                _maxHealthStatCallback = (modifier, isApply) =>
                    _statComponent.UpdateStat(_maxHealthStat, modifier, isApply);
                _statComponent.Subscribe(StatType.MaxHealth, _maxHealthStatCallback);

                _defenseStat = _statComponent.GetStat(StatType.Defense);
                _defenseStatCallback = (modifier, isApply) =>
                    _statComponent.UpdateStat(_defenseStat, modifier, isApply);
                _statComponent.Subscribe(StatType.Defense, _defenseStatCallback);

                _dodgeStat = _statComponent.GetStat(StatType.Dodge);
                _dodgeStatCallback = (modifier, isApply) =>
                    _statComponent.UpdateStat(_dodgeStat, modifier, isApply);
                _statComponent.Subscribe(StatType.Dodge, _dodgeStatCallback);
            }
            else
            {
                _maxHealthStat = new StatProperty();
                _defenseStat = new StatProperty();
                _dodgeStat = new StatProperty();
            }
        }

        public void OnReset()
        {
            if (_statComponent != null)
            {
                _statComponent.Unsubscribe(StatType.MaxHealth, _maxHealthStatCallback);
                _maxHealthStatCallback = null;

                _statComponent.Unsubscribe(StatType.Defense, _defenseStatCallback);
                _defenseStatCallback = null;

                _statComponent.Unsubscribe(StatType.Dodge, _dodgeStatCallback);
                _dodgeStatCallback = null;
            }

            _statComponent = null;
        }

        #endregion


        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            if (_dodgeStat.Value > UnityEngine.Random.value)
            {
                damage = 0;
            }
            else
            {
                int defenseValue = (int)_defenseStat.Value;
                float defensePercent = _defenseStat.Percent;
                damage -= defenseValue;
                damage = Mathf.CeilToInt(damage / defensePercent);
            }

            _currentHealth -= damage;
            CauseDamage(damage);
            if (_isPlayer)
                GameEntry.Event.Fire(this, PlayerHealthChangeEventArgs.Create(damage, CurrentHealth, MaxHealth));
        }

        private void CauseDamage(int damage)
        {
            //TODO:受击效果：跳字、粒子
        }
    }
}