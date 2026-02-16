using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuSettingButtonClickEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuSettingButtonClickEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuSettingButtonClickEventArgs Create()
        {
            return ReferencePool.Acquire<MenuSettingButtonClickEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
