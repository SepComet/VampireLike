using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class ShopRefreshEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ShopRefreshEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public int Cost { get; private set; }

        public ShopRefreshEventArgs()
        {
            Cost = 0;
        }
        
        public static ShopRefreshEventArgs Create(int cost)
        {
            var args = ReferencePool.Acquire<ShopRefreshEventArgs>();
            
            args.Cost = cost;
            
            return args;
        }
        
        public override void Clear()
        {
            Cost = 0;
        }
        
    }
}