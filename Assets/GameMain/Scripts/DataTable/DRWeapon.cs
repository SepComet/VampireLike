using System;
using System.Collections.Generic;
using Definition.DataStruct;
using Definition.Enum;
using GameFramework;
using CustomUtility;
using Newtonsoft.Json.Linq;
using UnityEngine;
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

        public int EntityTypeId { get; private set; }
        
        /// <summary>
        /// 获取武器名称。
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// 获取图标资源名称。
        /// </summary>
        public string IconAssetName { get; private set; }

        /// <summary>
        /// 获取武器稀有度
        /// </summary>
        public ItemRarity Rarity { get; private set; }
        
        /// <summary>
        /// 获取武器价值
        /// </summary>
        public int Price { get; private set; }
        
        /// <summary>
        /// 获取武器价值浮动率
        /// </summary>
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
        public Dictionary<string, string> Pramas { get; private set; }

        /// <summary>
        /// 获取武器额外参数 Json。
        /// </summary>
        public string ParamsJson { get; private set; }

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
            EntityTypeId = int.Parse(columnStrings[index++]);
            Title = columnStrings[index++];
            IconAssetName = columnStrings[index++];
            Rarity = EnumUtility<ItemRarity>.Get(columnStrings[index++]);
            Price = int.Parse(columnStrings[index++]);
            PriceRandomPercent = float.Parse(columnStrings[index++]);
            Attack = int.Parse(columnStrings[index++]);
            Cooldown = float.Parse(columnStrings[index++]);
            AttackRange = float.Parse(columnStrings[index++]);
            AttackSoundId = int.Parse(columnStrings[index++]);
            ParamsJson = columnStrings[index++];
            Pramas = DeserializeParams(ParamsJson);
            Modifiers = Utility.Json.ToObject<StatModifier[]>(columnStrings[index++]);

            GeneratePropertyArray();

            return true;
        }

        private void GeneratePropertyArray()
        {
        }
        
        /// <summary>
        /// 解参数
        /// </summary>
        /// <param name="rawParams"></param>
        /// <returns></returns>
        private Dictionary<string, string> DeserializeParams(string rawParams)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(rawParams))
            {
                return dict;
            }

            try
            {
                JObject paramObject = Utility.Json.ToObject<JObject>(rawParams);
                if (paramObject == null)
                {
                    return dict;
                }

                foreach (var pair in paramObject)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    {
                        continue;
                    }

                    dict[pair.Key.ToLower()] = pair.Value.ToString();
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Failed to parse weapon params json '{0}'. Error: {1}", rawParams, exception.Message);
            }

            return dict;
        }
    }
}
