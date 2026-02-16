using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuQuitButtonClickEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuQuitButtonClickEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuQuitButtonClickEventArgs Create()
        {
            return ReferencePool.Acquire<MenuQuitButtonClickEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
