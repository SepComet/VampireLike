using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Simulation.Tests.Editor
{
    public class SimulationWorldTickTests
    {
        private const string GameAssemblyName = "Assembly-CSharp";
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly System.Type SimulationWorldType =
            System.Type.GetType($"Simulation.SimulationWorld, {GameAssemblyName}");

        private static readonly System.Type SimulationTickContextType =
            System.Type.GetType($"Simulation.SimulationTickContext, {GameAssemblyName}");

        private static readonly System.Type EnemySimDataType =
            System.Type.GetType($"Simulation.EnemySimData, {GameAssemblyName}");

        private static readonly System.Type EnemySeparationSolverProviderType =
            System.Type.GetType($"CustomUtility.EnemySeparationSolverProvider, {GameAssemblyName}");

        private static readonly MethodInfo UpsertEnemyMethod =
            SimulationWorldType?.GetMethod("UpsertEnemy", NonPublicInstance);

        private static readonly MethodInfo RemoveEnemyByEntityIdMethod =
            SimulationWorldType?.GetMethod("RemoveEnemyByEntityId", NonPublicInstance);

        private static readonly MethodInfo TryGetEnemyDataMethod =
            SimulationWorldType?.GetMethod("TryGetEnemyData", NonPublicInstance);

        private static readonly MethodInfo TickMethod =
            SimulationWorldType?.GetMethod("Tick", PublicInstance);

        private static readonly MethodInfo SetUseSimulationMovementMethod =
            SimulationWorldType?.GetMethod("SetUseSimulationMovement", PublicInstance);

        private static readonly MethodInfo UseGridBucketSolverMethod =
            EnemySeparationSolverProviderType?.GetMethod("UseGridBucketSolver", PublicStatic);

        private static readonly FieldInfo EntitySyncField =
            SimulationWorldType?.GetField("_entitySync", NonPublicInstance);

        private static readonly FieldInfo PresentationField =
            SimulationWorldType?.GetField("_presentation", NonPublicInstance);

        private static readonly PropertyInfo EnemiesProperty =
            SimulationWorldType?.GetProperty("Enemies", PublicInstance);

        private GameObject _worldGameObject;
        private Component _worldComponent;

        [SetUp]
        public void SetUp()
        {
            Assert.NotNull(SimulationWorldType, "SimulationWorld type lookup failed.");
            Assert.NotNull(SimulationTickContextType, "SimulationTickContext type lookup failed.");
            Assert.NotNull(EnemySimDataType, "EnemySimData type lookup failed.");
            Assert.NotNull(EnemySeparationSolverProviderType, "EnemySeparationSolverProvider type lookup failed.");
            Assert.NotNull(UpsertEnemyMethod, "UpsertEnemy reflection lookup failed.");
            Assert.NotNull(RemoveEnemyByEntityIdMethod, "RemoveEnemyByEntityId reflection lookup failed.");
            Assert.NotNull(TryGetEnemyDataMethod, "TryGetEnemyData reflection lookup failed.");
            Assert.NotNull(TickMethod, "Tick reflection lookup failed.");
            Assert.NotNull(SetUseSimulationMovementMethod, "SetUseSimulationMovement reflection lookup failed.");
            Assert.NotNull(UseGridBucketSolverMethod, "UseGridBucketSolver reflection lookup failed.");
            Assert.NotNull(EnemiesProperty, "Enemies property reflection lookup failed.");

            _worldGameObject = new GameObject("SimulationWorldTickTests");
            _worldComponent = _worldGameObject.AddComponent(SimulationWorldType);
            SetUseSimulationMovementMethod.Invoke(_worldComponent, new object[] { true });
            UseGridBucketSolverMethod.Invoke(null, new object[] { 1f });
        }

        [TearDown]
        public void TearDown()
        {
            if (_worldComponent != null)
            {
                EntitySyncField?.SetValue(_worldComponent, null);
                PresentationField?.SetValue(_worldComponent, null);
            }

            if (_worldGameObject != null)
            {
                Object.DestroyImmediate(_worldGameObject);
            }

            _worldComponent = null;
            _worldGameObject = null;
        }

        [Test]
        public void TickEnemies_ChasesPlayer_WhenOutOfAttackRange()
        {
            UpsertEnemy(CreateEnemy(entityId: 1001, position: Vector3.zero, speed: 2f, attackRange: 1f));

            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: new Vector3(10f, 0f, 0f));

            object enemy = GetEnemyAt(0);
            Assert.That((int)GetField(enemy, "State"), Is.EqualTo(1));
            Vector3 position = (Vector3)GetField(enemy, "Position");
            Vector3 forward = (Vector3)GetField(enemy, "Forward");
            Assert.That(position.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(position.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(forward.x, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void TickEnemies_StopsMovement_WhenInAttackRange()
        {
            Vector3 startPosition = Vector3.zero;
            UpsertEnemy(CreateEnemy(entityId: 1002, position: startPosition, speed: 3f, attackRange: 2f));

            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: new Vector3(1f, 0f, 0f));

            object enemy = GetEnemyAt(0);
            Assert.That((int)GetField(enemy, "State"), Is.EqualTo(2));
            Vector3 position = (Vector3)GetField(enemy, "Position");
            Assert.That(position.x, Is.EqualTo(startPosition.x).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(startPosition.y).Within(0.0001f));
            Assert.That(position.z, Is.EqualTo(startPosition.z).Within(0.0001f));
        }

        [Test]
        public void RemoveEnemyByEntityId_RemapIndex_ForMovedEnemy()
        {
            UpsertEnemy(CreateEnemy(entityId: 2001, position: new Vector3(0f, 0f, 0f), speed: 1f, attackRange: 1f));
            UpsertEnemy(CreateEnemy(entityId: 2002, position: new Vector3(2f, 0f, 0f), speed: 1f, attackRange: 1f));
            UpsertEnemy(CreateEnemy(entityId: 2003, position: new Vector3(4f, 0f, 0f), speed: 1f, attackRange: 1f));

            bool removed = RemoveEnemyByEntityId(2002);
            bool removedEntityExists = TryGetEnemyData(2002, out _);
            bool movedEntityExists = TryGetEnemyData(2003, out object movedEnemy);

            Assert.IsTrue(removed);
            Assert.That(GetEnemiesCount(), Is.EqualTo(2));
            Assert.IsFalse(removedEntityExists);
            Assert.IsTrue(movedEntityExists);
            Assert.That((int)GetField(movedEnemy, "EntityId"), Is.EqualTo(2003));
            Assert.That((int)GetField(GetEnemyAt(1), "EntityId"), Is.EqualTo(2003));
        }

        private object CreateEnemy(int entityId, Vector3 position, float speed, float attackRange)
        {
            object enemy = System.Activator.CreateInstance(EnemySimDataType);
            SetField(ref enemy, "EntityId", entityId);
            SetField(ref enemy, "Position", position);
            SetField(ref enemy, "Forward", Vector3.forward);
            SetField(ref enemy, "Rotation", Quaternion.identity);
            SetField(ref enemy, "Speed", speed);
            SetField(ref enemy, "AttackRange", attackRange);
            SetField(ref enemy, "AvoidEnemyOverlap", false);
            SetField(ref enemy, "EnemyBodyRadius", 0.45f);
            SetField(ref enemy, "SeparationIterations", 1);
            SetField(ref enemy, "TargetType", 0);
            SetField(ref enemy, "State", 0);
            return enemy;
        }

        private void InvokeTick(float deltaTime, float realDeltaTime, Vector3 playerPosition)
        {
            object tickContext = System.Activator.CreateInstance(
                SimulationTickContextType,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new object[] { deltaTime, realDeltaTime, playerPosition },
                null);

            TickMethod.Invoke(_worldComponent, new[] { tickContext });
        }

        private void UpsertEnemy(object enemy)
        {
            UpsertEnemyMethod.Invoke(_worldComponent, new[] { enemy });
        }

        private bool RemoveEnemyByEntityId(int entityId)
        {
            return (bool)RemoveEnemyByEntityIdMethod.Invoke(_worldComponent, new object[] { entityId });
        }

        private bool TryGetEnemyData(int entityId, out object enemyData)
        {
            object boxedDefault = System.Activator.CreateInstance(EnemySimDataType);
            object[] parameters = { entityId, boxedDefault };
            bool result = (bool)TryGetEnemyDataMethod.Invoke(_worldComponent, parameters);
            enemyData = parameters[1];
            return result;
        }

        private object GetEnemyAt(int index)
        {
            object enemies = EnemiesProperty.GetValue(_worldComponent);
            PropertyInfo itemProperty = enemies.GetType().GetProperty("Item", PublicInstance);
            return itemProperty.GetValue(enemies, new object[] { index });
        }

        private int GetEnemiesCount()
        {
            object enemies = EnemiesProperty.GetValue(_worldComponent);
            PropertyInfo countProperty = enemies.GetType().GetProperty("Count", PublicInstance);
            return (int)countProperty.GetValue(enemies);
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PublicInstance);
            return field.GetValue(target);
        }

        private static void SetField(ref object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PublicInstance);
            field.SetValue(target, value);
        }
    }
}
