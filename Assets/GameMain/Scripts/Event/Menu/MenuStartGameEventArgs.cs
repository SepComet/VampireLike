using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuStartGameEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuStartGameEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuStartGameEventArgs Create()
        {
            return ReferencePool.Acquire<MenuStartGameEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
