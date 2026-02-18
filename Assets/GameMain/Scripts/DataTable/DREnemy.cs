using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DREnemy : DataRowBase
    {
        private int m_id;
        public override int Id => m_id;
        public int MaxHealth { get; private set; }
        public int HpAddPerLevel { get; private set; }
        public float Speed { get; private set; }
        public int DropCoin { get; private set; }
        public int DropExp { get; private set; }
        public float DropPercent { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;
            index++;
            m_id = int.Parse(columnStrings[index++]);
            index++;
            MaxHealth = int.Parse(columnStrings[index++]);
            HpAddPerLevel = int.Parse(columnStrings[index++]);
            Speed = float.Parse(columnStrings[index++]);
            DropCoin = int.Parse(columnStrings[index++]);
            DropExp = int.Parse(columnStrings[index++]);
            DropPercent = float.Parse(columnStrings[index++]);

            return true;
        }
    }
}
