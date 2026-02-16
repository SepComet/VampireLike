using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuSelectRoleConfirmEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuSelectRoleConfirmEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuSelectRoleConfirmEventArgs Create()
        {
            return ReferencePool.Acquire<MenuSelectRoleConfirmEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
