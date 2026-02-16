using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class MenuSelectRoleSelectedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuSelectRoleSelectedEventArgs).GetHashCode();

        public override int Id => EventId;

        public int RoleId { get; private set; } = -1;

        public static MenuSelectRoleSelectedEventArgs Create(int roleId)
        {
            var args = ReferencePool.Acquire<MenuSelectRoleSelectedEventArgs>();
            args.RoleId = roleId;
            return args;
        }

        public override void Clear()
        {
            RoleId = -1;
        }
    }
}
