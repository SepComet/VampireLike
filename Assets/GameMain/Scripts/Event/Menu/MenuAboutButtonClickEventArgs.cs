using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuAboutButtonClickEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuAboutButtonClickEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuAboutButtonClickEventArgs Create()
        {
            return ReferencePool.Acquire<MenuAboutButtonClickEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
