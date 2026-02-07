using System;
using System.Collections.Generic;
using Definition.DataStruct;
using Definition.Enum;
using UnityEngine;

namespace Components
{
    public class StatComponent : MonoBehaviour
    {
        private Dictionary<StatType, List<StatModifier>> _statModifiers;
        private Dictionary<StatType, Action<StatModifier, bool>> _statModifierCallbacks;

        public void OnInit()
        {
            _statModifiers = new Dictionary<StatType, List<StatModifier>>();
            _statModifierCallbacks = new Dictionary<StatType, Action<StatModifier, bool>>();
        }

        public void OnReset()
        {
            _statModifiers.Clear();
            _statModifierCallbacks.Clear();
        }

        public void AddModifier(StatModifier modifier)
        {
            if (!_statModifiers.TryGetValue(modifier.StatType, out var modifiers))
            {
                _statModifiers.Add(modifier.StatType, modifiers = new List<StatModifier>());
                modifiers.Add(modifier);
            }
            else
            {
                modifiers.Add(modifier);
            }

            if (_statModifierCallbacks.TryGetValue(modifier.StatType, out var callbacks))
                callbacks?.Invoke(modifier, true);
        }

        public void RemoveModifier(StatModifier modifier)
        {
            if (_statModifiers.TryGetValue(modifier.StatType, out var modifiers))
            {
                _statModifiers[modifier.StatType].Remove(modifier);
            }

            if (_statModifierCallbacks.TryGetValue(modifier.StatType, out var callbacks))
                callbacks?.Invoke(modifier, false);
        }

        public void Subscribe(StatType statType, Action<StatModifier, bool> callback)
        {
            if (!_statModifierCallbacks.TryAdd(statType, callback))
            {
                _statModifierCallbacks[statType] += callback;
            }
        }

        public void Unsubscribe(StatType statType, Action<StatModifier, bool> callback)
        {
            if (!_statModifierCallbacks.TryGetValue(statType, out var callbacks))
            {
                return;
            }

            callbacks -= callback;
            if (callbacks == null)
            {
                _statModifierCallbacks.Remove(statType);
            }
        }

        public StatProperty GetStat(StatType statType)
        {
            var result = new StatProperty();

            if (!_statModifiers.TryGetValue(statType, out var modifiers)) return result;

            foreach (var modifier in modifiers)
            {
                if (modifier.IsPercent) result.Percent += modifier.Value;
                else result.Value += modifier.Value;
            }

            return result;
        }

        public void UpdateStat(StatProperty stat, StatModifier modifier, bool isApply)
        {
            float coef = isApply ? 1f : -1f;
            if (modifier.IsPercent)
            {
                stat.Percent += modifier.Value * coef;
            }
            else
            {
                stat.Value += modifier.Value * coef;
            }
        }
    }
}