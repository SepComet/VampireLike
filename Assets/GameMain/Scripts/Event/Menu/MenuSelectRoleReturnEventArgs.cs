using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuSelectRoleReturnEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuSelectRoleReturnEventArgs).GetHashCode();

        public override int Id => EventId;

        public static MenuSelectRoleReturnEventArgs Create()
        {
            return ReferencePool.Acquire<MenuSelectRoleReturnEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
