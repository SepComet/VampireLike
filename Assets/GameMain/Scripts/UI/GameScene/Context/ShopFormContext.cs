using System.Collections.Generic;

namespace UI
{
    public class ShopFormContext : UIContext
    {
        public int CurrentLevel;
        public int RefreshPrice;
        public int PlayerCoin;
        public List<GoodsItemContext> GoodsItems;
        public DisplayListAreaContext PropListContext;
        public DisplayListAreaContext WeaponListContext;
    }
}
