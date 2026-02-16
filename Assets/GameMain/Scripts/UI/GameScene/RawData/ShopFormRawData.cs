using System.Collections.Generic;
using Definition.DataStruct;
using Entity;

namespace UI
{
    public class ShopFormRawData
    {
        public int CurrentLevel;
        public int RefreshPrice;
        public int PlayerCoin;
        public List<GoodsItemContext> GoodsItems;
        public IReadOnlyList<PropItem> PropItems;
        public int PropMaxCount = -1;
        public IReadOnlyList<WeaponBase> WeaponItems;
        public int WeaponMaxCount = -1;
    }    
}
