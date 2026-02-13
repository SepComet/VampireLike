using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class PlayerLevelUpEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PlayerLevelUpEventArgs).GetHashCode();

        public override int Id => EventId;

        public PlayerLevelUpEventArgs()
        {
        }

        public static PlayerLevelUpEventArgs Create()
        {
            var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();

            return args;
        }

        public override void Clear()
        {
        }
    }
}