using CustomEvent;
using Entity;
using Entity.EntityData;
using Entity.Weapon;
using GameFramework.Event;
using UnityEngine;

namespace Simulation
{
    public partial class SimulationWorld
    {
        [Header("投射物命中表现")] [Tooltip("是否监听投射物命中表现事件。")] [SerializeField]
        private bool _projectileHitPresentationEnabled = true;

        [Tooltip("是否播放投射物命中标记。")] [SerializeField]
        private bool _projectileHitMarkerEnabled = true;

        [Tooltip("命中标记尺寸。")] [SerializeField]
        private float _projectileHitMarkerSize = 0.2f;

        [Tooltip("命中标记在目标上的高度偏移。")] [SerializeField]
        private float _projectileHitMarkerYOffset = 1.2f;

        [Tooltip("命中标记持续时间。")] [SerializeField]
        private float _projectileHitMarkerDuration = 0.15f;

        [Tooltip("命中标记颜色。")] [SerializeField]
        private Color _projectileHitMarkerColor = new(1f, 0f, 0f, 0.95f);

        [Tooltip("是否播放投射物命中特效实体。")] [SerializeField]
        private bool _projectileHitEffectEnabled;

        [Tooltip("投射物命中特效实体类型 Id（<=0 表示不启用）。")] [SerializeField]
        private int _projectileHitEffectTypeId;

        private sealed class Presentation
        {
            private readonly SimulationWorld _world;
            private HandgunHitMarkerAttackEffect _projectileHitMarkerEffect;
            private bool _isProjectileHitEventSubscribed;

            public Presentation(SimulationWorld world)
            {
                _world = world;
            }

            public void OnStart()
            {
                if (_world == null || !_world._projectileHitPresentationEnabled)
                {
                    return;
                }

                var eventComponent = GameEntry.Event;
                if (eventComponent == null)
                {
                    return;
                }

                eventComponent.Subscribe(ProjectileHitPresentationEventArgs.EventId, OnProjectileHitPresentationEvent);
                _isProjectileHitEventSubscribed = true;
            }

            public void OnDestroy()
            {
                if (!_isProjectileHitEventSubscribed)
                {
                    return;
                }

                var eventComponent = GameEntry.Event;
                if (eventComponent != null)
                {
                    eventComponent.Unsubscribe(ProjectileHitPresentationEventArgs.EventId,
                        OnProjectileHitPresentationEvent);
                }

                _isProjectileHitEventSubscribed = false;
            }

            public void OnLateUpdate()
            {
                if (_world == null || !_world.UseSimulationMovement)
                {
                    return;
                }

                var enemyManager = GameEntry.EnemyManager;
                if (enemyManager == null || enemyManager.Enemies == null)
                {
                    return;
                }

                var enemies = enemyManager.Enemies;
                foreach (var enemy in enemies)
                {
                    if (enemy is not EnemyBase enemyEntity || !enemyEntity.Available)
                    {
                        continue;
                    }

                    if (!_world.TryGetEnemyData(enemyEntity.Id, out EnemySimData enemyData))
                    {
                        continue;
                    }

                    ApplyEnemyPresentation(enemyEntity, enemyData);
                }

                if (!_world.UseJobSimulation)
                {
                    return;
                }

                var projectiles = _world._projectiles;
                for (int i = 0; i < projectiles.Count; i++)
                {
                    ProjectileSimData projectileData = projectiles[i];
                    if (!projectileData.Active || projectileData.State != ProjectileStateActive)
                    {
                        continue;
                    }

                    EntityBase projectileEntity = TryGetEntityById(projectileData.EntityId);
                    if (projectileEntity == null || !projectileEntity.Available)
                    {
                        continue;
                    }

                    ApplyProjectilePresentation(projectileEntity, projectileData);
                }
            }

            private void OnProjectileHitPresentationEvent(object sender, GameEventArgs e)
            {
                if (!_isProjectileHitEventSubscribed || _world == null ||
                    e is not ProjectileHitPresentationEventArgs args)
                {
                    return;
                }

                if (args.ShowHitMarker && _world._projectileHitMarkerEnabled)
                {
                    PlayHitMarker(args);
                }

                if (args.ShowHitEffect && _world._projectileHitEffectEnabled)
                {
                    PlayHitEffect(args);
                }
            }

            private void PlayHitMarker(ProjectileHitPresentationEventArgs args)
            {
                EntityBase targetEntity = TryGetEntityById(args.TargetEntityId);
                if (targetEntity == null || !targetEntity.Available)
                {
                    return;
                }

                _projectileHitMarkerEffect ??= new HandgunHitMarkerAttackEffect(
                    Mathf.Max(0.01f, _world._projectileHitMarkerSize),
                    _world._projectileHitMarkerYOffset,
                    Mathf.Max(0.01f, _world._projectileHitMarkerDuration),
                    _world._projectileHitMarkerColor);
                _projectileHitMarkerEffect.Play(null, args.HitPosition, targetEntity, 0f);
            }

            private void PlayHitEffect(ProjectileHitPresentationEventArgs args)
            {
                int effectTypeId = args.EffectEntityTypeId > 0 ? args.EffectEntityTypeId : _world._projectileHitEffectTypeId;
                if (effectTypeId <= 0)
                {
                    return;
                }

                var entityComponent = GameEntry.Entity;
                if (entityComponent == null)
                {
                    return;
                }

                EffectData effectData = new EffectData(entityComponent.GenerateSerialId(), effectTypeId)
                {
                    Position = args.HitPosition
                };
                entityComponent.ShowEffect(effectData);
            }

            private static void ApplyEnemyPresentation(EnemyBase enemyEntity, in EnemySimData enemyData)
            {
                Transform enemyTransform = enemyEntity.CachedTransform;
                enemyTransform.position = enemyData.Position;

                Quaternion rotation = enemyData.Rotation;
                float rotationMagnitude = Mathf.Abs(rotation.x) + Mathf.Abs(rotation.y) + Mathf.Abs(rotation.z) +
                                          Mathf.Abs(rotation.w);
                if (rotationMagnitude > float.Epsilon)
                {
                    enemyTransform.rotation = rotation;
                    return;
                }

                Vector3 forward = enemyData.Forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > float.Epsilon)
                {
                    enemyTransform.forward = forward.normalized;
                }
            }

            private static void ApplyProjectilePresentation(EntityBase projectileEntity,
                in ProjectileSimData projectileData)
            {
                Transform projectileTransform = projectileEntity.CachedTransform;
                if (projectileTransform == null)
                {
                    return;
                }

                projectileTransform.position = projectileData.Position;
                Vector3 forward = projectileData.Forward;
                if (forward.sqrMagnitude <= float.Epsilon)
                {
                    return;
                }

                projectileTransform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }
    }
}
