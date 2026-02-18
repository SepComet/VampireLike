using Definition.DataStruct;
using Definition.Enum;
using Newtonsoft.Json;
using StarForce;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRLevelUpReward : DataRowBase
    {
        private int m_Id;

        public override int Id => m_Id;

        public string Title { get; private set; }

        public string IconAssetName { get; private set; }
        
        public ItemRarity Rarity { get; private set; }

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
            Modifiers = JsonConvert.DeserializeObject<StatModifier[]>(fields[index++]);

            return true;
        }
    }
}