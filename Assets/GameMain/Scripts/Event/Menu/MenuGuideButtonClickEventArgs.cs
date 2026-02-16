using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuGuideButtonClickEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuGuideButtonClickEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuGuideButtonClickEventArgs Create()
        {
            return ReferencePool.Acquire<MenuGuideButtonClickEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
