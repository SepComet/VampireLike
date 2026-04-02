using CustomEvent;
using Definition.DataStruct;
using Entity;
using Entity.Weapon;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Collision Presentation

        private void DispatchProjectileHitPresentationEvent(int projectileEntityId, int sourceEntityId,
            int sourceOwnerEntityId, int targetEntityId, int damage, in Vector3 hitPosition)
        {
            if (!_dispatchProjectileHitPresentationEvent)
            {
                return;
            }

            var eventComponent = GameEntry.Event;
            if (eventComponent == null)
            {
                return;
            }

            eventComponent.Fire(this, ProjectileHitPresentationEventArgs.Create(
                projectileEntityId,
                sourceEntityId,
                sourceOwnerEntityId,
                targetEntityId,
                damage,
                hitPosition,
                _dispatchProjectileHitMarkerEvent,
                _dispatchProjectileHitEffectEvent,
                _projectileHitPresentationEffectTypeId));
        }

        private static bool TryResolveImpactSource(EntityBase sourceEntity, EntityBase ownerEntity,
            out EntityBase attacker, out ImpactData impactData)
        {
            if (TryResolveImpactFromEntity(sourceEntity, out impactData))
            {
                attacker = sourceEntity;
                return true;
            }

            if (TryResolveImpactFromEntity(ownerEntity, out impactData))
            {
                attacker = ownerEntity;
                return true;
            }

            attacker = null;
            impactData = default;
            return false;
        }

        private static bool TryResolveImpactFromEntity(EntityBase entity, out ImpactData impactData)
        {
            if (entity is WeaponBase weapon)
            {
                impactData = weapon.GetImpactData();
                return true;
            }

            if (entity is EnemyProjectile enemyProjectile)
            {
                impactData = enemyProjectile.GetImpactData();
                return true;
            }

            if (entity is TargetableObject targetableObject)
            {
                impactData = targetableObject.GetImpactData();
                return true;
            }

            impactData = default;
            return false;
        }

        private static bool TryGetAliveTargetableEntity(int entityId, out TargetableObject target)
        {
            target = null;

            var enemyManager = GameEntry.EnemyManager;
            if (enemyManager != null && enemyManager.TryGetEnemy(entityId, out EntityBase enemyEntity))
            {
                if (enemyEntity is TargetableObject enemyTarget && enemyTarget.Available && !enemyTarget.IsDead)
                {
                    target = enemyTarget;
                    return true;
                }
            }

            EntityBase entity = TryGetEntityById(entityId);
            if (entity is TargetableObject targetable && targetable.Available && !targetable.IsDead)
            {
                target = targetable;
                return true;
            }

            return false;
        }

        private static EntityBase TryGetEntityById(int entityId)
        {
            var entityComponent = GameEntry.Entity;
            return entityComponent != null ? entityComponent.GetGameEntity(entityId) : null;
        }

        #endregion
    }
}
