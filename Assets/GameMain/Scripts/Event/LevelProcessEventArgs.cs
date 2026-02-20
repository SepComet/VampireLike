using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class LevelProcessEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(LevelProcessEventArgs).GetHashCode();

        public override int Id => EventId;

        public int LevelTimeLeft { get; private set; }

        public LevelProcessEventArgs()
        {
            LevelTimeLeft = 60;
        }

        public static LevelProcessEventArgs Create(int timeLeft)
        {
            var args = ReferencePool.Acquire<LevelProcessEventArgs>();
            args.LevelTimeLeft = timeLeft;
            
            return args;
        }
        
        public override void Clear()
        {
            LevelTimeLeft = 60;
        }
    }
}