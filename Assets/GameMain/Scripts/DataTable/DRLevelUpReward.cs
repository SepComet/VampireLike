using Definition.DataStruct;
using Newtonsoft.Json;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRLevelUpReward : DataRowBase
    {
        private int m_Id;

        public override int Id => m_Id;

        public string Title { get; private set; }

        public string IconAssetName { get; private set; }

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
            Modifiers = JsonConvert.DeserializeObject<StatModifier[]>(fields[index++]);

            return true;
        }
    }
}