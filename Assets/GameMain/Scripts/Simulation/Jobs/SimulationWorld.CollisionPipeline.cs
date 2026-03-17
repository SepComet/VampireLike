using Unity.Jobs;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        // Shared collision pipeline configuration and runtime state.
        // Request buffering, broad-phase scheduling, resolve, and presentation
        // dispatch live in dedicated partial files under Jobs/.
        private const int PlayerEntityId = -1;
        private JobHandle _collisionCandidateQueryHandle;
        private bool _collisionCandidateQueryScheduled;

        [Header("Projectile Collision Query")]
        [Tooltip("Projectile broad-phase collision query radius.")]
        [SerializeField]
        private float _projectileCollisionQueryRadius = 0.35f;

        [Tooltip("Maximum retained candidates per projectile query.")]
        [SerializeField]
        private int _projectileMaxCandidatesPerQuery = 1;

        [Tooltip("Broad-phase bucket cell size. <=0 derives from query radius.")]
        [SerializeField]
        private float _projectileCollisionCellSize = 0f;

        [Header("Projectile Hit Event Dispatch")]
        [Tooltip("Dispatch projectile hit presentation event.")]
        [SerializeField]
        private bool _dispatchProjectileHitPresentationEvent = true;

        [Tooltip("Request hit marker when projectile hits.")]
        [SerializeField]
        private bool _dispatchProjectileHitMarkerEvent = true;

        [Tooltip("Request hit effect when projectile hits.")]
        [SerializeField]
        private bool _dispatchProjectileHitEffectEvent = true;

        [Tooltip("Default hit effect entity type id in presentation event. 0 means not specified.")]
        [SerializeField]
        private int _projectileHitPresentationEffectTypeId = 0;
    }
}
