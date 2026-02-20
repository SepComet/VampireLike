using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class ShopWeaponRecycleEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ShopWeaponRecycleEventArgs).GetHashCode();

        public override int Id => EventId;

        public int Index { get; private set; }

        public int Price { get; private set; }

        public ShopWeaponRecycleEventArgs()
        {
            Index = -1;
            Price = 0;
        }

        public static ShopWeaponRecycleEventArgs Create(int index, int price)
        {
            var args = ReferencePool.Acquire<ShopWeaponRecycleEventArgs>();
            args.Index = index;
            args.Price = price;

            return args;
        }

        public override void Clear()
        {
            Index = -1;
            Price = 0;
        }
    }
}