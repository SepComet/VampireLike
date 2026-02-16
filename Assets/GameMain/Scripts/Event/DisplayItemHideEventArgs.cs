using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class DisplayItemHideEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DisplayItemHideEventArgs).GetHashCode();

        public override int Id => EventId;


        public DisplayItemHideEventArgs()
        {
        }

        public static DisplayItemHideEventArgs Create()
        {
            var args = ReferencePool.Acquire<DisplayItemHideEventArgs>();

            return args;
        }

        public override void Clear()
        {
        }
    }
}