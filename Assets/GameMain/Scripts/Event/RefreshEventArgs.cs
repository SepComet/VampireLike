using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class RefreshEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(RefreshEventArgs).GetHashCode();

        public override int Id => EventId;
        
        public int Cost { get; private set; }

        public RefreshEventArgs()
        {
            Cost = 0;
        }
        
        public static RefreshEventArgs Create(int cost)
        {
            var args = ReferencePool.Acquire<RefreshEventArgs>();
            
            args.Cost = cost;
            
            return args;
        }
        
        public override void Clear()
        {
            Cost = 0;
        }
        
    }
}