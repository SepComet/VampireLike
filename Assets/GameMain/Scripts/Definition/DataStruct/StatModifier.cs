using System;
using Definition.Enum;
using StarForce;
using UnityGameFramework.Runtime;

namespace Definition.DataStruct
{
    public class StatModifier
    {
        // None = 0,
        // MaxHealth = 1,
        // MovementSpeed = 2,
        // Attack = 3,
        // Defense = 4,
        // AttackSpeed = 5,
        // Critical = 6,
        // CriticalDamage = 7,
        // Dodge = 8,
        // AbsorbRange = 9

        private readonly string[] _statTypeNames =
        {
            "无效",
            "最大生命",
            "移动速度",
            "伤害",
            "防御",
            "冷却",
            "暴击率",
            "暴击伤害",
            "闪避",
            "金币/经验吸收范围"
        };

        public StatType StatType;
        public float Value;
        public bool IsPercent;

        public override string ToString()
        {
            if (IsPercent)
            {
                if (Value > 0)
                {
                    return $"{_statTypeNames[(int)StatType]}: <color=green>+{Value * 100}%</color>";
                }
                else
                {
                    return $"{_statTypeNames[(int)StatType]}: <color=red>+{Value * 100}%</color>";
                }
            }
            else
            {
                if (Value > 0)
                {
                    return $"{_statTypeNames[(int)StatType]}: <color=green>+{Value}</color>";
                }
                else
                {
                    return $"{_statTypeNames[(int)StatType]}: <color=red>+{Value}</color>";
                }
            }
        }

        public static StatModifier StringToModifier(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                Log.Warning("Invalid modifier: {0}", input);
                return null;
            }

            if (!input.StartsWith('[') || !input.EndsWith(']'))
            {
                throw new ArgumentException("Input must be enclosed in square brackets.");
            }

            string inner = input.Substring(1, input.Length - 2);

            // 如果是空列表 "[]"
            if (inner.Length == 0) return null;

            string[] property = inner.Split(",");
            int propertyIndex = 0;
            return new StatModifier
            {
                StatType = EnumUtility<StatType>.Get(property[propertyIndex++]),
                Value = float.Parse(property[propertyIndex++]),
                IsPercent = bool.Parse(property[propertyIndex++])
            };
        }
    }
}