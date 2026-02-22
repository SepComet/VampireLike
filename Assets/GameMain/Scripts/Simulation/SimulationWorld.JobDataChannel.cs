using System;
using Unity.Collections;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SimulationWorld
    {
        private struct EnemyJobInputData
        {
            public int EntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public Quaternion Rotation;
            public float Speed;
            public float AttackRange;
            public bool AvoidEnemyOverlap;
            public float EnemyBodyRadius;
            public int SeparationIterations;
            public int TargetType;
            public int State;
        }

        private struct EnemyJobOutputData
        {
            public int EntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public Quaternion Rotation;
            public float Speed;
            public float AttackRange;
            public bool AvoidEnemyOverlap;
            public float EnemyBodyRadius;
            public int SeparationIterations;
            public int TargetType;
            public int State;
        }

        private struct ProjectileJobInputData
        {
            public int EntityId;
            public int OwnerEntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public float Speed;
            public float RemainingLifetime;
            public int State;
        }

        private struct ProjectileJobOutputData
        {
            public int EntityId;
            public int OwnerEntityId;
            public Vector3 Position;
            public Vector3 Forward;
            public float Speed;
            public float RemainingLifetime;
            public int State;
        }

        private NativeList<EnemyJobInputData> _enemyJobInputs;
        private NativeList<EnemyJobOutputData> _enemyJobOutputs;
        private NativeList<ProjectileJobInputData> _projectileJobInputs;
        private NativeList<ProjectileJobOutputData> _projectileJobOutputs;

        private void InitializeJobDataChannels()
        {
            if (AreJobDataChannelsUsable())
            {
                return;
            }

            DisposeJobDataChannels();
            _enemyJobInputs = new NativeList<EnemyJobInputData>(64, Allocator.Persistent);
            _enemyJobOutputs = new NativeList<EnemyJobOutputData>(64, Allocator.Persistent);
            _projectileJobInputs = new NativeList<ProjectileJobInputData>(64, Allocator.Persistent);
            _projectileJobOutputs = new NativeList<ProjectileJobOutputData>(64, Allocator.Persistent);
        }

        private void DisposeJobDataChannels()
        {
            if (_enemyJobInputs.IsCreated)
            {
                _enemyJobInputs.Dispose();
            }
            _enemyJobInputs = default;

            if (_enemyJobOutputs.IsCreated)
            {
                _enemyJobOutputs.Dispose();
            }
            _enemyJobOutputs = default;

            if (_projectileJobInputs.IsCreated)
            {
                _projectileJobInputs.Dispose();
            }
            _projectileJobInputs = default;

            if (_projectileJobOutputs.IsCreated)
            {
                _projectileJobOutputs.Dispose();
            }
            _projectileJobOutputs = default;
        }

        private void ClearJobDataChannels()
        {
            if (!AreJobDataChannelsUsable())
            {
                return;
            }

            if (_enemyJobInputs.IsCreated)
            {
                _enemyJobInputs.Clear();
            }

            if (_enemyJobOutputs.IsCreated)
            {
                _enemyJobOutputs.Clear();
            }

            if (_projectileJobInputs.IsCreated)
            {
                _projectileJobInputs.Clear();
            }

            if (_projectileJobOutputs.IsCreated)
            {
                _projectileJobOutputs.Clear();
            }
        }

        private void SyncSimulationToJobInput()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobInputs, _enemies.Count);
            EnsureCapacity(ref _projectileJobInputs, _projectiles.Count);

            _enemyJobInputs.Clear();
            _projectileJobInputs.Clear();

            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemyJobInputs.Add(ConvertToEnemyJobInput(_enemies[i]));
            }

            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectileJobInputs.Add(ConvertToProjectileJobInput(_projectiles[i]));
            }
        }

        private void SyncSimulationToJobOutput()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _enemyJobOutputs, _enemies.Count);
            EnsureCapacity(ref _projectileJobOutputs, _projectiles.Count);

            _enemyJobOutputs.Clear();
            _projectileJobOutputs.Clear();

            for (int i = 0; i < _enemies.Count; i++)
            {
                _enemyJobOutputs.Add(ConvertToEnemyJobOutput(_enemies[i]));
            }

            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectileJobOutputs.Add(ConvertToProjectileJobOutput(_projectiles[i]));
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

        private void SyncProjectilesToJobOutput()
        {
            InitializeJobDataChannels();
            EnsureCapacity(ref _projectileJobOutputs, _projectiles.Count);
            _projectileJobOutputs.Clear();

            for (int i = 0; i < _projectiles.Count; i++)
            {
                _projectileJobOutputs.Add(ConvertToProjectileJobOutput(_projectiles[i]));
            }
        }

        private void ApplyJobOutputToSimulation()
        {
            int enemyCount = Mathf.Min(_enemies.Count, _enemyJobOutputs.Length);
            for (int i = 0; i < enemyCount; i++)
            {
                _enemies[i] = ConvertToEnemySimData(_enemyJobOutputs[i]);
            }

            int projectileCount = Mathf.Min(_projectiles.Count, _projectileJobOutputs.Length);
            for (int i = 0; i < projectileCount; i++)
            {
                _projectiles[i] = ConvertToProjectileSimData(_projectileJobOutputs[i]);
            }
        }

        private bool AreJobDataChannelsUsable()
        {
            return IsNativeListUsable(_enemyJobInputs) &&
                   IsNativeListUsable(_enemyJobOutputs) &&
                   IsNativeListUsable(_projectileJobInputs) &&
                   IsNativeListUsable(_projectileJobOutputs);
        }

        private static void EnsureCapacity<T>(ref NativeList<T> nativeList, int targetCount) where T : unmanaged
        {
            if (!nativeList.IsCreated || targetCount <= 0)
            {
                return;
            }

            if (nativeList.Capacity < targetCount)
            {
                nativeList.Capacity = targetCount;
            }
        }

        private static bool IsNativeListUsable<T>(NativeList<T> nativeList) where T : unmanaged
        {
            if (!nativeList.IsCreated)
            {
                return false;
            }

            try
            {
                _ = nativeList.Length;
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private static EnemyJobInputData ConvertToEnemyJobInput(in EnemySimData enemy)
        {
            return new EnemyJobInputData
            {
                EntityId = enemy.EntityId,
                Position = enemy.Position,
                Forward = enemy.Forward,
                Rotation = enemy.Rotation,
                Speed = enemy.Speed,
                AttackRange = enemy.AttackRange,
                AvoidEnemyOverlap = enemy.AvoidEnemyOverlap,
                EnemyBodyRadius = enemy.EnemyBodyRadius,
                SeparationIterations = enemy.SeparationIterations,
                TargetType = enemy.TargetType,
                State = enemy.State
            };
        }

        private static EnemyJobOutputData ConvertToEnemyJobOutput(in EnemySimData enemy)
        {
            return new EnemyJobOutputData
            {
                EntityId = enemy.EntityId,
                Position = enemy.Position,
                Forward = enemy.Forward,
                Rotation = enemy.Rotation,
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
                Position = enemy.Position,
                Forward = enemy.Forward,
                Rotation = enemy.Rotation,
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
                Position = projectile.Position,
                Forward = projectile.Forward,
                Speed = projectile.Speed,
                RemainingLifetime = projectile.RemainingLifetime,
                State = projectile.State
            };
        }

        private static ProjectileJobOutputData ConvertToProjectileJobOutput(in ProjectileSimData projectile)
        {
            return new ProjectileJobOutputData
            {
                EntityId = projectile.EntityId,
                OwnerEntityId = projectile.OwnerEntityId,
                Position = projectile.Position,
                Forward = projectile.Forward,
                Speed = projectile.Speed,
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
                Position = projectile.Position,
                Forward = projectile.Forward,
                Speed = projectile.Speed,
                RemainingLifetime = projectile.RemainingLifetime,
                State = projectile.State
            };
        }
    }
}
