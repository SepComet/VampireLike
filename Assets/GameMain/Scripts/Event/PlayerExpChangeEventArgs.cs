using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class PlayerExpChangeEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PlayerExpChangeEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public int CurrentExp { get; private set; }

        public int MaxExp { get; private set; }

        public PlayerExpChangeEventArgs()
        {
            CurrentExp = 0;
            MaxExp = 0;
        }

        public static PlayerExpChangeEventArgs Create(int currentExp, int maxExp)
        {
            var args = ReferencePool.Acquire<PlayerExpChangeEventArgs>();
            args.CurrentExp = currentExp;
            args.MaxExp = maxExp;

            return args;
        }

        public override void Clear()
        {
            CurrentExp = 0;
            MaxExp = 0;
        }
    }
}