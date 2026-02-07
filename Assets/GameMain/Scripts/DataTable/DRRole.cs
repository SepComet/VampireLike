using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Definition.DataStruct;
using Definition.Enum;
using GameFramework;
using StarForce;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRRole : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取角色编号。
        /// </summary>
        public override int Id => m_Id;

        /// <summary>
        /// 获取资源名称。
        /// </summary>
        public string RoleName { get; private set; }

        /// <summary>
        /// 获取图标名称。
        /// </summary>
        public string IconName { get; private set; }

        /// <summary>
        /// 获取初始最大生命。
        /// </summary>
        public int MaxHealth { get; private set; }

        /// <summary>
        /// 获取初始速度。
        /// </summary>
        public float Speed { get; private set; }

        /// <summary>
        /// 获取初始金币。
        /// </summary>
        public int Coin { get; private set; }

        /// <summary>
        /// 获取最大持有武器数。 
        /// </summary>
        public int WeaponCapacity { get; private set; }

        /// <summary>
        /// 获取初始属性。
        /// </summary>
        public StatModifier[] InitialProperties { get; private set; }

        /// <summary>
        /// 获取初始物品。
        /// </summary>
        public KeyValuePair<int, int>[] InitialItemIds { get; private set; }

        public int[] ExpRequires { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            RoleName = columnStrings[index++];
            IconName = columnStrings[index++];
            MaxHealth = int.Parse(columnStrings[index++]);
            Speed = float.Parse(columnStrings[index++]);
            Coin = int.Parse(columnStrings[index++]);
            WeaponCapacity = int.Parse(columnStrings[index++]);
            InitialProperties = Utility.Json.ToObject<StatModifier[]>(columnStrings[index++]);
            GenerateItems(columnStrings[index++]);
            GenerateExpRequires(ref columnStrings, index);

            GeneratePropertyArray();
            return true;
        }

        private void GeneratePropertyArray()
        {
        }

        private void GenerateItems(string raw)
        {
            if (!raw.StartsWith('[') || !raw.EndsWith(']'))
            {
                throw new ArgumentException("Input must be enclosed in square brackets.");
            }

            if (raw.Length <= 2) return;

            string[] items = raw.Substring(1, raw.Length - 2).Split(";");
            int length = items.Length;
            InitialItemIds = new KeyValuePair<int, int>[length];
            for (int i = 0; i < length; i++)
            {
                string input = items[i];
                if (string.IsNullOrEmpty(input))
                {
                    Log.Warning("Invalid item: {0}", input);
                    continue;
                }

                if (!input.StartsWith('[') || !input.EndsWith(']'))
                {
                    throw new ArgumentException("Input must be enclosed in square brackets.");
                }

                string inner = input.Substring(1, input.Length - 2);

                // 如果是空列表 "[]"
                if (inner.Length == 0) continue;

                string[] index = inner.Split(",");
                var item = new KeyValuePair<int, int>(int.Parse(index[0]), int.Parse(index[1]));
                InitialItemIds[i] = item;
            }
        }

        private void GenerateExpRequires(ref string[] columnStrings, int index)
        {
            List<int> expRequires = new List<int>();
            int baseExp = int.Parse(columnStrings[index]);
            for (int i = index; i < columnStrings.Length; i++)
            {
                expRequires.Add(int.Parse(columnStrings[i]) - baseExp);
            }

            ExpRequires = expRequires.ToArray();
        }
    }
}