using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class ShopContinueEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ShopContinueEventArgs).GetHashCode();

        public override int Id => EventId;

        public ShopContinueEventArgs()
        {
        }

        public static ShopContinueEventArgs Create()
        {
            var args = ReferencePool.Acquire<ShopContinueEventArgs>();

            return args;
        }

        public override void Clear()
        {
        }
    }
}