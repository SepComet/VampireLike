using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class LevelUpPropSelectedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(LevelUpPropSelectedEventArgs).GetHashCode();

        public override int Id => EventId;

        public int SelectedId { get; private set; }

        public LevelUpPropSelectedEventArgs()
        {
            SelectedId = -1;
        }

        public static LevelUpPropSelectedEventArgs Create(int propId)
        {
            var args = ReferencePool.Acquire<LevelUpPropSelectedEventArgs>();
            args.SelectedId = propId;
            return args;
        }

        public override void Clear()
        {
            SelectedId = -1;
        }
    }
}