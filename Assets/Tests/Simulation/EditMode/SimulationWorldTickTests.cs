using System.Reflection;
using Components;
using NUnit.Framework;
using UnityEngine;

namespace Simulation.Tests.Editor
{
    public class SimulationWorldTickTests
    {
        private static readonly BindingFlags NonPublicInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly MethodInfo UpsertEnemyMethod =
            typeof(SimulationWorld).GetMethod("UpsertEnemy", NonPublicInstance);

        private static readonly MethodInfo UpsertProjectileMethod =
            typeof(SimulationWorld).GetMethod("UpsertProjectile", NonPublicInstance);

        private GameObject _worldObject;
        private SimulationWorld _world;

        [SetUp]
        public void SetUp()
        {
            _worldObject = new GameObject("SimulationWorldTests");
            _world = _worldObject.AddComponent<SimulationWorld>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_worldObject != null)
            {
                Object.DestroyImmediate(_worldObject);
            }
        }

        [Test]
        public void MovementComponent_OnUpdate_DoesNotMoveTransformDirectly()
        {
            GameObject moverObject = new GameObject("Mover");
            try
            {
                MovementComponent movement = moverObject.AddComponent<MovementComponent>();
                moverObject.transform.position = new Vector3(1f, 0f, 2f);

                movement.OnInit(5f, moverObject.transform);
                movement.SetMove(true);
                movement.SetDirection(Vector3.right);

                movement.OnUpdate(1f, 1f);

                Assert.That(moverObject.transform.position.x, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(moverObject.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(moverObject.transform.position.z, Is.EqualTo(2f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(moverObject);
            }
        }

        [Test]
        public void Tick_AdvancesEnemyAndUpdatesNearestTargetQuery()
        {
            UpsertEnemy(new EnemySimData
            {
                EntityId = 1001,
                Position = new Vector3(0f, 0f, 0f),
                Forward = Vector3.forward,
                Rotation = Quaternion.identity,
                Speed = 3f,
                AttackRange = 0.5f,
                AvoidEnemyOverlap = true,
                EnemyBodyRadius = 0.45f,
                SeparationIterations = 2
            });

            _world.Tick(new SimulationTickContext(1f, 1f, new Vector3(10f, 0f, 0f)));

            Assert.That(_world.Enemies.Count, Is.EqualTo(1));
            EnemySimData enemy = _world.Enemies[0];
            Assert.That(enemy.State, Is.EqualTo(1));
            Assert.That(enemy.Position.x, Is.EqualTo(3f).Within(0.001f));
            Assert.That(_world.TryGetNearestEnemyEntityId(new Vector3(3.2f, 0f, 0f), 1f, out int enemyEntityId),
                Is.True);
            Assert.That(enemyEntityId, Is.EqualTo(1001));
        }

        [Test]
        public void SyncEnemyMovementInput_DisablesEnemyMovement()
        {
            UpsertEnemy(new EnemySimData
            {
                EntityId = 1002,
                Position = Vector3.zero,
                Forward = Vector3.forward,
                Rotation = Quaternion.identity,
                Speed = 4f,
                AttackRange = 0.5f,
                AvoidEnemyOverlap = true,
                EnemyBodyRadius = 0.45f,
                SeparationIterations = 2
            });

            _world.SyncEnemyMovementInput(1002, false, Vector3.left, 4f, true, 0.45f, 2);
            _world.Tick(new SimulationTickContext(1f, 1f, new Vector3(10f, 0f, 0f)));

            EnemySimData enemy = _world.Enemies[0];
            Assert.That(enemy.State, Is.EqualTo(0));
            Assert.That(enemy.Position.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(enemy.Position.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(enemy.Position.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Tick_ExpiresProjectileThroughSimulationLifetime()
        {
            UpsertProjectile(new ProjectileSimData
            {
                EntityId = 2001,
                OwnerEntityId = 1001,
                Position = Vector3.zero,
                Forward = Vector3.right,
                Velocity = Vector3.right * 4f,
                Speed = 4f,
                LifeTime = 0.25f,
                Age = 0f,
                Active = true,
                RemainingLifetime = 0.25f,
                State = 0
            });

            _world.Tick(new SimulationTickContext(0.5f, 0.5f, Vector3.zero));

            Assert.That(_world.Projectiles.Count, Is.EqualTo(1));
            ProjectileSimData projectile = _world.Projectiles[0];
            Assert.That(projectile.Active, Is.False);
            Assert.That(projectile.State, Is.EqualTo(1));
            Assert.That(projectile.Age, Is.GreaterThanOrEqualTo(projectile.LifeTime));
        }

        [Test]
        public void ClearSimulationState_ClearsPublicStateContainers()
        {
            UpsertEnemy(new EnemySimData { EntityId = 1, Position = Vector3.zero, Forward = Vector3.forward });
            UpsertProjectile(new ProjectileSimData { EntityId = 2, Active = true, State = 0 });

            _world.ClearSimulationState();

            Assert.That(_world.Enemies, Is.Empty);
            Assert.That(_world.Projectiles, Is.Empty);
            Assert.That(_world.Pickups, Is.Empty);
        }

        private void UpsertEnemy(EnemySimData simData)
        {
            Assert.NotNull(UpsertEnemyMethod);
            UpsertEnemyMethod.Invoke(_world, new object[] { simData });
        }

        private void UpsertProjectile(ProjectileSimData simData)
        {
            Assert.NotNull(UpsertProjectileMethod);
            UpsertProjectileMethod.Invoke(_world, new object[] { simData });
        }
    }
}

