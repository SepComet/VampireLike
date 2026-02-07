using GameFramework;
using GameFramework.Event;

namespace CustomEvent
{
    public class PlayerHealthChangeEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PlayerHealthChangeEventArgs).GetHashCode();

        public override int Id => EventId;

        public int Damage { get; private set; }

        public int CurrentHealth { get; private set; }

        public int MaxHealth { get; private set; }

        public PlayerHealthChangeEventArgs()
        {
            Damage = 0;
            CurrentHealth = 0;
            MaxHealth = 0;
        }

        public static PlayerHealthChangeEventArgs Create(int damage, int currentHealth, int maxHealth)
        {
            var args = ReferencePool.Acquire<PlayerHealthChangeEventArgs>();
            args.Damage = damage;
            args.CurrentHealth = currentHealth;
            args.MaxHealth = maxHealth;

            return args;
        }

        public override void Clear()
        {
            Damage = 0;
            CurrentHealth = 0;
            MaxHealth = 0;
        }
    }
}