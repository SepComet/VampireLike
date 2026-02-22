using System;
using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DREnemy : DataRowBase
    {
        private int m_id;

        public override int Id => m_id;

        public int EntityTypeId { get; private set; }

        public int MaxHealth { get; private set; }

        public int HpAddPerLevel { get; private set; }

        public int AttackDamage { get; private set; }

        public float AttackCooldown { get; private set; }

        public float AttackRange { get; private set; }

        public float Speed { get; private set; }

        public int DropCoin { get; private set; }

        public int DropExp { get; private set; }

        public float DropPercent { get; private set; }

        public Dictionary<string, string> Params { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;
            index++;
            m_id = int.Parse(columnStrings[index++]);
            index++;
            EntityTypeId = int.Parse(columnStrings[index++]);
            MaxHealth = int.Parse(columnStrings[index++]);
            HpAddPerLevel = int.Parse(columnStrings[index++]);
            AttackDamage = int.Parse(columnStrings[index++]);
            AttackCooldown = float.Parse(columnStrings[index++]);
            AttackRange = float.Parse(columnStrings[index++]);
            Speed = float.Parse(columnStrings[index++]);
            DropCoin = int.Parse(columnStrings[index++]);
            DropExp = int.Parse(columnStrings[index++]);
            DropPercent = float.Parse(columnStrings[index++]);
            Params = DeserializeParams(columnStrings[index++]);

            return true;
        }

        /// <summary>
        /// 解参数
        /// </summary>
        /// <param name="rawParams"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private Dictionary<string, string> DeserializeParams(string rawParams)
        {
            if (!rawParams.StartsWith('[') || !rawParams.EndsWith(']'))
            {
                throw new ArgumentException("Input must be enclosed in square brackets.");
            }

            var dict = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(rawParams)) return dict;

            string[] items = rawParams.Substring(1, rawParams.Length - 2).Split(";");
            foreach (var item in items)
            {
                string entry = item.Trim();
                if (string.IsNullOrEmpty(entry)) continue;

                string[] pair = entry.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length != 2) continue;
                dict.Add(pair[0].ToLower(), pair[1]);
            }

            return dict;
        }
    }
}