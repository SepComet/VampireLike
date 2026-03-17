using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        #region Job Data Conversion

        private void SyncSimulationStateToJobInputs()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobInputs, _enemies.Count);
            EnsureCapacity(ref _projectileJobInputs, _projectiles.Count);

            _enemyJobInputs.Clear();
            _projectileJobInputs.Clear();

            foreach (EnemySimData data in _enemies)
            {
                _enemyJobInputs.Add(ConvertToEnemyJobInput(data));
            }

            foreach (ProjectileSimData data in _projectiles)
            {
                _projectileJobInputs.Add(ConvertToProjectileJobInput(data));
            }
        }

        private void PrepareEnemyJobOutputBuffer(int enemyCount)
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobOutputs, enemyCount);
            _enemyJobOutputs.Clear();

            if (enemyCount > 0)
            {
                _enemyJobOutputs.ResizeUninitialized(enemyCount);
            }
        }

        private void PrepareProjectileJobOutputBuffer(int projectileCount)
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _projectileJobOutputs, projectileCount);
            _projectileJobOutputs.Clear();

            if (projectileCount > 0)
            {
                _projectileJobOutputs.ResizeUninitialized(projectileCount);
            }
        }

        private void CopyProjectileInputsToOutputs()
        {
            for (int i = 0; i < _projectileJobInputs.Length; i++)
            {
                ProjectileJobInputData input = _projectileJobInputs[i];
                _projectileJobOutputs[i] = new ProjectileJobOutputData
                {
                    EntityId = input.EntityId,
                    OwnerEntityId = input.OwnerEntityId,
                    Position = input.Position,
                    Forward = input.Forward,
                    Velocity = input.Velocity,
                    Speed = input.Speed,
                    LifeTime = input.LifeTime,
                    Age = input.Age,
                    Active = input.Active,
                    RemainingLifetime = input.RemainingLifetime,
                    State = input.State
                };
            }
        }

        private static EnemyJobInputData ConvertToEnemyJobInput(in EnemySimData enemy)
        {
            return new EnemyJobInputData
            {
                EntityId = enemy.EntityId,
                Position = new float3(enemy.Position.x, enemy.Position.y, enemy.Position.z),
                Forward = new float3(enemy.Forward.x, enemy.Forward.y, enemy.Forward.z),
                Rotation = new quaternion(enemy.Rotation.x, enemy.Rotation.y, enemy.Rotation.z, enemy.Rotation.w),
                Speed = enemy.Speed,
                AttackRange = enemy.AttackRange,
                AvoidEnemyOverlap = enemy.AvoidEnemyOverlap,
                EnemyBodyRadius = enemy.EnemyBodyRadius,
                SeparationIterations = enemy.SeparationIterations,
                TargetType = enemy.TargetType,
                State = enemy.State
            };
        }

        private static EnemySimData ConvertToEnemySimData(in EnemyJobOutputData enemy)
        {
            return new EnemySimData
            {
                EntityId = enemy.EntityId,
                Position = new Vector3(enemy.Position.x, enemy.Position.y, enemy.Position.z),
                Forward = new Vector3(enemy.Forward.x, enemy.Forward.y, enemy.Forward.z),
                Rotation = new Quaternion(enemy.Rotation.value.x, enemy.Rotation.value.y, enemy.Rotation.value.z,
                    enemy.Rotation.value.w),
                Speed = enemy.Speed,
                AttackRange = enemy.AttackRange,
                AvoidEnemyOverlap = enemy.AvoidEnemyOverlap,
                EnemyBodyRadius = enemy.EnemyBodyRadius,
                SeparationIterations = enemy.SeparationIterations,
                TargetType = enemy.TargetType,
                State = enemy.State
            };
        }

        private static ProjectileJobInputData ConvertToProjectileJobInput(in ProjectileSimData projectile)
        {
            return new ProjectileJobInputData
            {
                EntityId = projectile.EntityId,
                OwnerEntityId = projectile.OwnerEntityId,
                Position = new float3(projectile.Position.x, projectile.Position.y, projectile.Position.z),
                Forward = new float3(projectile.Forward.x, projectile.Forward.y, projectile.Forward.z),
                Velocity = new float3(projectile.Velocity.x, projectile.Velocity.y, projectile.Velocity.z),
                Speed = projectile.Speed,
                LifeTime = projectile.LifeTime,
                Age = projectile.Age,
                Active = projectile.Active,
                RemainingLifetime = projectile.RemainingLifetime,
                State = projectile.State
            };
        }

        private static ProjectileSimData ConvertToProjectileSimData(in ProjectileJobOutputData projectile)
        {
            return new ProjectileSimData
            {
                EntityId = projectile.EntityId,
                OwnerEntityId = projectile.OwnerEntityId,
                Position = new Vector3(projectile.Position.x, projectile.Position.y, projectile.Position.z),
                Forward = new Vector3(projectile.Forward.x, projectile.Forward.y, projectile.Forward.z),
                Velocity = new Vector3(projectile.Velocity.x, projectile.Velocity.y, projectile.Velocity.z),
                Speed = projectile.Speed,
                LifeTime = projectile.LifeTime,
                Age = projectile.Age,
                Active = projectile.Active,
                RemainingLifetime = projectile.RemainingLifetime,
                State = projectile.State
            };
        }

        #endregion
    }
}
