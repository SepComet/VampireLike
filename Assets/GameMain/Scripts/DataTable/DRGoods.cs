using Definition.Enum;
using StarForce;
using UnityGameFramework.Runtime;

namespace DataTable
{
    public class DRGoods : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取商品Id。
        /// </summary>
        public override int Id => m_Id;

        /// <summary>
        /// 获取商品类型。
        /// </summary>
        public GoodsType GoodsType { get; private set; }

        /// <summary>
        /// 获取最低价格
        /// </summary>
        public int MinPrice { get; private set; }

        /// <summary>
        /// 获取最高价格
        /// </summary>
        public int MaxPrice { get; private set; }

        /// <summary>
        /// 获取商品对应具体类型的表Id
        /// </summary>
        public int GoodsTypeId { get; set; }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] fields = dataRowString.Split(DataTableExtension.DataSplitSeparators);

            int index = 0;

            index++;
            m_Id = int.Parse(fields[index++]);
            index++;
            GoodsType = EnumUtility<GoodsType>.Get(fields[index++]);
            MinPrice = int.Parse(fields[index++]);
            MaxPrice = int.Parse(fields[index++]);
            GoodsTypeId = int.Parse(fields[index++]);

            return true;
        }
    }
}