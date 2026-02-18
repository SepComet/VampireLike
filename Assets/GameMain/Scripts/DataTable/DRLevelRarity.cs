using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRLevelRarity : DataRowBase
    {
        private int m_Id;

        public override int Id => m_Id;

        public int LevelMin { get; private set; }
        public int LevelMax { get; private set; }

        public int WhiteWeight { get; private set; }
        public int GreenWeight { get; private set; }
        public int BlueWeight { get; private set; }
        public int PurpleWeight { get; private set; }
        public int RedWeight { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] fields = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;
            index++;
            m_Id = int.Parse(fields[index++]);
            index++;
            LevelMin = int.Parse(fields[index++]);
            LevelMax = int.Parse(fields[index++]);
            WhiteWeight = int.Parse(fields[index++]);
            GreenWeight = int.Parse(fields[index++]);
            BlueWeight = int.Parse(fields[index++]);
            PurpleWeight = int.Parse(fields[index++]);
            RedWeight = int.Parse(fields[index++]);

            return true;
        }
    }
}
