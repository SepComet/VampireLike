using System.Collections;
using System.Reflection;
using Entity;
using Entity.EntityData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests.PlayMode
{
    public class SimulationWorldPlayModeTests
    {
        private static readonly BindingFlags NonPublicInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo ProjectileDataField =
            typeof(EnemyProjectile).GetField("_projectileData", NonPublicInstance);

        private static readonly FieldInfo ProjectileIsActiveField =
            typeof(EnemyProjectile).GetField("_isActive", NonPublicInstance);

        private static readonly FieldInfo ProjectileDirectionField =
            typeof(EnemyProjectile).GetField("_direction", NonPublicInstance);

        private static readonly MethodInfo EnemyProjectileOnUpdateMethod =
            typeof(EnemyProjectile).GetMethod("OnUpdate", NonPublicInstance);

        private GameObject _worldObject;
        private SimulationWorld _world;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _worldObject = new GameObject("SimulationWorldPlayModeTests");
            _world = _worldObject.AddComponent<SimulationWorld>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_worldObject != null)
            {
                Object.Destroy(_worldObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Tick_AppliesSyncedPlayerMovementInput()
        {
            GameObject playerObject = new GameObject("Player");
            try
            {
                playerObject.transform.position = Vector3.zero;

                _world.SyncPlayerMovementInput(playerObject.transform, true, Vector3.right, 6f);
                _world.Tick(new SimulationTickContext(0.5f, 0.5f, playerObject.transform.position));

                yield return null;

                Assert.That(playerObject.transform.position.x, Is.EqualTo(3f).Within(0.001f));
                Assert.That(playerObject.transform.position.z, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(playerObject);
            }
        }

        [UnityTest]
        public IEnumerator EnemyProjectile_OnUpdate_DoesNotSelfDriveOrExpire()
        {
            GameObject projectileObject = new GameObject("EnemyProjectile");
            try
            {
                EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>();
                projectileObject.transform.position = Vector3.zero;

                ProjectileDataField.SetValue(projectile,
                    new EnemyProjectileData(3001, 1001, Definition.Enum.CampType.Enemy, 5, 10f, 0.1f, Vector3.right));
                ProjectileIsActiveField.SetValue(projectile, true);
                ProjectileDirectionField.SetValue(projectile, Vector3.right);

                EnemyProjectileOnUpdateMethod.Invoke(projectile, new object[] { 1f, 1f });

                yield return null;

                Assert.That(projectileObject.transform.position.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(projectileObject.transform.position.y, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(projectileObject.transform.position.z, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(projectile.IsActive, Is.True);
            }
            finally
            {
                Object.Destroy(projectileObject);
            }
        }

        [UnityTest]
        public IEnumerator Tick_RespectsEnemyMovementSyncFromComponentShell()
        {
            _world.SyncEnemyMovementInput(4001, false, Vector3.left, 5f, true, 0.45f, 2);

            MethodInfo upsertEnemyMethod = typeof(SimulationWorld).GetMethod("UpsertEnemy", NonPublicInstance);
            upsertEnemyMethod.Invoke(_world, new object[]
            {
                new EnemySimData
                {
                    EntityId = 4001,
                    Position = Vector3.zero,
                    Forward = Vector3.forward,
                    Rotation = Quaternion.identity,
                    Speed = 5f,
                    AttackRange = 0.5f,
                    AvoidEnemyOverlap = true,
                    EnemyBodyRadius = 0.45f,
                    SeparationIterations = 2
                }
            });

            _world.SyncEnemyMovementInput(4001, false, Vector3.left, 5f, true, 0.45f, 2);
            _world.Tick(new SimulationTickContext(1f, 1f, new Vector3(8f, 0f, 0f)));

            yield return null;

            Assert.That(_world.Enemies.Count, Is.EqualTo(1));
            Assert.That(_world.Enemies[0].Position.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_world.Enemies[0].Position.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_world.Enemies[0].Position.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_world.Enemies[0].State, Is.EqualTo(0));
        }
    }
}
