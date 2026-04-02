using GameFramework;
using GameFramework.Event;
using UnityEngine;

namespace CustomEvent
{
    public sealed class ProjectileHitPresentationEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ProjectileHitPresentationEventArgs).GetHashCode();

        public override int Id => EventId;

        public int ProjectileEntityId { get; private set; }
        public int SourceEntityId { get; private set; }
        public int SourceOwnerEntityId { get; private set; }
        public int TargetEntityId { get; private set; }
        public int Damage { get; private set; }
        public Vector3 HitPosition { get; private set; }
        public bool ShowHitMarker { get; private set; }
        public bool ShowHitEffect { get; private set; }
        public int EffectEntityTypeId { get; private set; }

        public static ProjectileHitPresentationEventArgs Create(int projectileEntityId, int sourceEntityId,
            int sourceOwnerEntityId, int targetEntityId, int damage, in Vector3 hitPosition, bool showHitMarker,
            bool showHitEffect, int effectEntityTypeId)
        {
            ProjectileHitPresentationEventArgs args = ReferencePool.Acquire<ProjectileHitPresentationEventArgs>();
            args.ProjectileEntityId = projectileEntityId;
            args.SourceEntityId = sourceEntityId;
            args.SourceOwnerEntityId = sourceOwnerEntityId;
            args.TargetEntityId = targetEntityId;
            args.Damage = damage;
            args.HitPosition = hitPosition;
            args.ShowHitMarker = showHitMarker;
            args.ShowHitEffect = showHitEffect;
            args.EffectEntityTypeId = effectEntityTypeId;
            return args;
        }

        public override void Clear()
        {
            ProjectileEntityId = 0;
            SourceEntityId = 0;
            SourceOwnerEntityId = 0;
            TargetEntityId = 0;
            Damage = 0;
            HitPosition = Vector3.zero;
            ShowHitMarker = false;
            ShowHitEffect = false;
            EffectEntityTypeId = 0;
        }
    }
}