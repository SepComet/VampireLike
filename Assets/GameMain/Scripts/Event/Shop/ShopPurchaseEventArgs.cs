using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class ShopPurchaseEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ShopPurchaseEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public int GoodsIndex { get; private set; }

        public ShopPurchaseEventArgs()
        {
            GoodsIndex = -1;
        }
        
        public static ShopPurchaseEventArgs Create(int index)
        {
            var args = ReferencePool.Acquire<ShopPurchaseEventArgs>();
            
            args.GoodsIndex = index;
            
            return args;
        }
        
        public override void Clear()
        {
            GoodsIndex = -1;
        }
        
    }
}