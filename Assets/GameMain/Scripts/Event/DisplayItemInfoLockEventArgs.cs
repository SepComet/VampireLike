using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class DisplayItemInfoLockEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DisplayItemInfoLockEventArgs).GetHashCode();

        public override int Id => EventId;

        public DisplayItemInfoLockEventArgs()
        {
        }

        public static DisplayItemInfoLockEventArgs Create()
        {
            var args = ReferencePool.Acquire<DisplayItemInfoLockEventArgs>();

            return args;
        }

        public override void Clear()
        {
        }
    }
}