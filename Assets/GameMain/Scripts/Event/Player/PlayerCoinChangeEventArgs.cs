using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class PlayerCoinChangeEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PlayerCoinChangeEventArgs).GetHashCode();

        public override int Id => EventId;

        public int Gain { get; private set; }

        public int CoinCount { get; private set; }

        public PlayerCoinChangeEventArgs()
        {
            Gain = 0;
            CoinCount = 0;
        }

        public static PlayerCoinChangeEventArgs Create(int gain, int coinCount)
        {
            var args = ReferencePool.Acquire<PlayerCoinChangeEventArgs>();
            args.Gain = gain;
            args.CoinCount = coinCount;

            return args;
        }

        public override void Clear()
        {
            Gain = 0;
            CoinCount = 0;
        }
    }
}