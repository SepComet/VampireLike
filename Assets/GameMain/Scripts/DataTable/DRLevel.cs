using System;
using Definition.DataStruct;
using Definition.Enum;
using StarForce;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRLevel : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取关卡编号。
        /// </summary>
        public override int Id => m_Id;

        /// <summary>
        /// 获取关卡内会生成的实体编号
        /// </summary>
        public int[] EntityIds { get; private set; }

        /// <summary>
        /// 获取关卡内每次生成实体的数量
        /// </summary>
        public int[] EntityCounts { get; private set; }

        /// <summary>
        /// 获取关卡内生成实体的间隔时间
        /// </summary>
        public float[] Intervals { get; private set; }

        /// <summary>
        /// 获取关卡时间。
        /// </summary>
        public int Duration { get; private set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            GenerateEntityIds(columnStrings[index++]);
            GenerateEntityCounts(columnStrings[index++]);
            GenerateIntervals(columnStrings[index++]);
            Duration = int.Parse(columnStrings[index++]);

            GeneratePropertyArray();
            return true;
        }

        private void GeneratePropertyArray()
        {
        }

        private void GenerateEntityIds(string raw)
        {
            if (!raw.StartsWith('[') || !raw.EndsWith(']'))
            {
                throw new ArgumentException("Input must be enclosed in square brackets.");
            }

            if (raw.Length == 2) return;
            string[] entityIds = raw.Substring(1, raw.Length - 2).Split(",");
            int length = entityIds.Length;
            EntityIds = new int[length];
            for (int i = 0; i < length; i++)
            {
                EntityIds[i] = int.Parse(entityIds[i]);
            }
        }

        private void GenerateEntityCounts(string raw)
        {
            if (!raw.StartsWith('[') || !raw.EndsWith(']'))
            {
                throw new ArgumentException("Input must be enclosed in square brackets.");
            }

            if (raw.Length == 2) return;
            string[] entityCounts = raw.Substring(1, raw.Length - 2).Split(",");
            int length = entityCounts.Length;
            EntityCounts = new int[length];
            for (int i = 0; i < length; i++)
            {
                EntityCounts[i] = int.Parse(entityCounts[i]);
            }
        }

        private void GenerateIntervals(string raw)
        {
            if (!raw.StartsWith('[') || !raw.EndsWith(']'))
            {
                throw new ArgumentException("Input must be enclosed in square brackets.");
            }

            if (raw.Length == 2) return;
            string[] intervals = raw.Substring(1, raw.Length - 2).Split(",");
            int length = intervals.Length;
            Intervals = new float[length];
            for (int i = 0; i < length; i++)
            {
                Intervals[i] = float.Parse(intervals[i]);
            }
        }
    }
}