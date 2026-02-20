using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class DisplayItemInfoHideEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DisplayItemInfoHideEventArgs).GetHashCode();

        public override int Id => EventId;

        public bool Force = false;

        public DisplayItemInfoHideEventArgs()
        {
            Force = false;
        }

        public static DisplayItemInfoHideEventArgs Create(bool force = false)
        {
            var args = ReferencePool.Acquire<DisplayItemInfoHideEventArgs>();
            args.Force = force;

            return args;
        }

        public override void Clear()
        {
            Force = false;
        }
    }
}