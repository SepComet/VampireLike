using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuFileButtonClickEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuFileButtonClickEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuFileButtonClickEventArgs Create()
        {
            return ReferencePool.Acquire<MenuFileButtonClickEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
