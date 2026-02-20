using UnityGameFramework.Runtime;

namespace DataTable
{
    /// <summary>
    /// Bullet config table.
    /// Id uses BulletType value.
    /// </summary>
    public class DRBullet : DataRowBase
    {
        private int m_Id;
        
        public override int Id => m_Id;

        /// <summary>
        /// 子弹实体 Id，用于在 DREntity 中查询资源路径 
        /// </summary>
        public int EntityTypeId { get; private set; }
        
        /// <summary>
        /// 子弹速度
        /// </summary>
        public float Speed { get; private set; }
        
        /// <summary>
        /// 子弹生存时间
        /// </summary>
        public float MaxAliveTime { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            EntityTypeId = int.Parse(columnStrings[index++]);
            Speed = float.Parse(columnStrings[index++]);
            MaxAliveTime = float.Parse(columnStrings[index++]);
            return true;
        }
    }
}