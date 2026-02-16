using System;
using System.IO;
using System.Text;
using Definition.DataStruct;
using Definition.Enum;
using Entity;
using GameFramework;
using StarForce;
using UnityGameFramework.Runtime;

namespace DataTable
{
    /// <summary>
    /// 武器表。
    /// </summary>
    public class DRWeapon : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取武器编号。
        /// </summary>
        public override int Id => m_Id;

        /// <summary>
        /// 获取武器名称。
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// 获取图标资源名称。
        /// </summary>
        public string IconAssetName { get; private set; }

        public ItemRarity Rarity { get; private set; }
        public int Price { get; private set; }
        public float PriceRandomPercent { get; private set; }

        /// <summary>
        /// 获取武器伤害。
        /// </summary>
        public int Attack { get; private set; }

        /// <summary>
        /// 获取武器冷却。
        /// </summary>
        public float Cooldown { get; private set; }

        /// <summary>
        /// 获取武器攻击范围。
        /// </summary>
        public float AttackRange { get; private set; }

        /// <summary>
        /// 获取武器攻击音效。
        /// </summary>
        public int AttackSoundId { get; private set; }

        /// <summary>
        /// 获取武器额外参数。
        /// </summary>
        public string Pramas { get; private set; }

        /// <summary>
        /// 获取武器额外属性。
        /// </summary>
        public StatModifier[] Modifiers { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            Title = columnStrings[index++];
            IconAssetName = columnStrings[index++];
            Rarity = EnumUtility<ItemRarity>.Get(columnStrings[index++]);
            Price = int.Parse(columnStrings[index++]);
            PriceRandomPercent = float.Parse(columnStrings[index++]);
            Attack = int.Parse(columnStrings[index++]);
            Cooldown = float.Parse(columnStrings[index++]);
            AttackRange = float.Parse(columnStrings[index++]);
            AttackSoundId = int.Parse(columnStrings[index++]);
            Pramas = columnStrings[index++];
            Modifiers = Utility.Json.ToObject<StatModifier[]>(columnStrings[index++]);

            GeneratePropertyArray();

            return true;
        }

        private void GeneratePropertyArray()
        {
        }
    }
}