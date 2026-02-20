using Definition.DataStruct;
using Newtonsoft.Json;
using CustomUtility;
using UnityGameFramework.Runtime;
using Definition.Enum;

namespace DataTable
{
    public class DRProp : DataRowBase
    {
        private int m_Id;

        public override int Id => m_Id;
        public string Title { get; private set; }
        public string IconAssetName { get; private set; }
        public ItemRarity Rarity { get; private set; }
        public int Price { get; private set; }
        public float PriceRandomPercent { get; private set; }
        public StatModifier[] Modifiers { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] fields = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            int index = 0;

            index++;
            m_Id = int.Parse(fields[index++]);
            index++;
            Title = fields[index++];
            IconAssetName = fields[index++];
            Rarity = EnumUtility<ItemRarity>.Get(fields[index++]);
            Price = int.Parse(fields[index++]);
            PriceRandomPercent = float.Parse(fields[index++]);
            Modifiers = JsonConvert.DeserializeObject<StatModifier[]>(fields[index++]);

            return true;
        }
    }
}