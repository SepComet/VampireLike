using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Simulation.Tests.PlayMode
{
    public class SimulationWorldPlayModeTests
    {
        private const string GameAssemblyName = "VampireLike";
        private const string RuntimeAssemblyName = "UnityGameFramework.Runtime";
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly System.Type SimulationWorldType =
            System.Type.GetType($"Simulation.SimulationWorld, {GameAssemblyName}");

        private static readonly System.Type SimulationTickContextType =
            System.Type.GetType($"Simulation.SimulationTickContext, {GameAssemblyName}");

        private static readonly System.Type EnemySimDataType =
            System.Type.GetType($"Simulation.EnemySimData, {GameAssemblyName}");

        private static readonly System.Type ProjectileSimDataType =
            System.Type.GetType($"Simulation.ProjectileSimData, {GameAssemblyName}");

        private static readonly System.Type EnemyProjectileType =
            System.Type.GetType($"Entity.EnemyProjectile, {GameAssemblyName}");

        private static readonly System.Type EnemyProjectileDataType =
            System.Type.GetType($"Entity.EntityData.EnemyProjectileData, {GameAssemblyName}");

        private static readonly System.Type CampTypeType =
            System.Type.GetType($"Definition.Enum.CampType, {GameAssemblyName}");

        private static readonly System.Type GameEntryType =
            System.Type.GetType($"GameEntry, {GameAssemblyName}");

        private static readonly System.Type EnemySeparationSolverProviderType =
            System.Type.GetType($"CustomUtility.EnemySeparationSolverProvider, {GameAssemblyName}");

        private static readonly System.Type EnemyManagerComponentType =
            System.Type.GetType($"CustomComponent.EnemyManagerComponent, {GameAssemblyName}");

        private static readonly System.Type PlayerType =
            System.Type.GetType($"Entity.Player, {GameAssemblyName}");

        private static readonly System.Type EntityBaseType =
            System.Type.GetType($"Entity.EntityBase, {GameAssemblyName}");

        private static readonly System.Type HealthComponentType =
            System.Type.GetType($"Components.HealthComponent, {GameAssemblyName}");

        private static readonly System.Type ProcedureComponentType =
            System.Type.GetType($"UnityGameFramework.Runtime.ProcedureComponent, {RuntimeAssemblyName}");

        private static readonly System.Type ProcedureGameType =
            System.Type.GetType($"Procedure.ProcedureGame, {GameAssemblyName}");

        private static readonly System.Type GameStateTypeType =
            System.Type.GetType($"Procedure.GameStateType, {GameAssemblyName}");

        private static readonly System.Type ProcedureManagerType =
            System.Type.GetType("GameFramework.Procedure.ProcedureManager, GameFramework");

        private static readonly System.Type ProcedureManagerInterfaceType =
            System.Type.GetType("GameFramework.Procedure.IProcedureManager, GameFramework");

        private static readonly System.Type FsmOpenGenericType =
            System.Type.GetType("GameFramework.Fsm.Fsm`1, GameFramework");

        private static readonly MethodInfo UpsertEnemyMethod =
            SimulationWorldType?.GetMethod("UpsertEnemy", NonPublicInstance);

        private static readonly MethodInfo RemoveEnemyByEntityIdMethod =
            SimulationWorldType?.GetMethod("RemoveEnemyByEntityId", NonPublicInstance);

        private static readonly MethodInfo UpsertProjectileMethod =
            SimulationWorldType?.GetMethod("UpsertProjectile", NonPublicInstance);

        private static readonly MethodInfo RemoveProjectileByEntityIdMethod =
            SimulationWorldType?.GetMethod("RemoveProjectileByEntityId", NonPublicInstance);

        private static readonly MethodInfo TryGetEnemyDataMethod =
            SimulationWorldType?.GetMethod("TryGetEnemyData", NonPublicInstance);

        private static readonly MethodInfo TickMethod =
            SimulationWorldType?.GetMethod("Tick", PublicInstance);

        private static readonly MethodInfo TryGetNearestEnemyEntityIdMethod =
            SimulationWorldType?.GetMethod("TryGetNearestEnemyEntityId", PublicInstance);

        private static readonly MethodInfo TryRequestAreaCollisionMethod =
            SimulationWorldType?.GetMethod("TryRequestAreaCollision", PublicInstance);

        private static readonly MethodInfo ClearSimulationStateMethod =
            SimulationWorldType?.GetMethod("ClearSimulationState", PublicInstance);

        private static readonly MethodInfo UseGridBucketSolverMethod =
            EnemySeparationSolverProviderType?.GetMethod("UseGridBucketSolver", PublicStatic);

        private static readonly FieldInfo EntitySyncField =
            SimulationWorldType?.GetField("_entitySync", NonPublicInstance);

        private static readonly FieldInfo TransformSyncField =
            SimulationWorldType?.GetField("_transformSync", NonPublicInstance);

        private static readonly FieldInfo HitPresentationField =
            SimulationWorldType?.GetField("_hitPresentation", NonPublicInstance);

        private static readonly FieldInfo UseSimulationMovementField =
            SimulationWorldType?.GetField("_useSimulationMovement", NonPublicInstance);

        private static readonly PropertyInfo EnemiesProperty =
            SimulationWorldType?.GetProperty("Enemies", PublicInstance);

        private static readonly PropertyInfo ProjectilesProperty =
            SimulationWorldType?.GetProperty("Projectiles", PublicInstance);

        private static readonly PropertyInfo CollisionCandidateCountProperty =
            SimulationWorldType?.GetProperty("CollisionCandidateCount", PublicInstance);

        private static readonly PropertyInfo UseSimulationMovementProperty =
            SimulationWorldType?.GetProperty("UseSimulationMovement", PublicInstance);

        private static readonly PropertyInfo LastResolvedAreaHitCountProperty =
            SimulationWorldType?.GetProperty("LastResolvedAreaHitCount", PublicInstance);

        private static readonly FieldInfo CollisionQueryInputsField =
            SimulationWorldType?.GetField("_collisionQueryInputs", NonPublicInstance);

        private static readonly FieldInfo AreaCollisionRequestsField =
            SimulationWorldType?.GetField("_areaCollisionRequests", NonPublicInstance);

        private static readonly MethodInfo EnemyProjectileOnUpdateMethod =
            EnemyProjectileType?.GetMethod("OnUpdate", NonPublicInstance);

        private static readonly FieldInfo EnemyProjectileDataField =
            EnemyProjectileType?.GetField("_projectileData", NonPublicInstance);

        private static readonly FieldInfo EnemyProjectileIsActiveField =
            EnemyProjectileType?.GetField("_isActive", NonPublicInstance);

        private static readonly FieldInfo EnemyProjectileIsSimulationDrivenField =
            EnemyProjectileType?.GetField("_isSimulationDriven", NonPublicInstance);

        private static readonly PropertyInfo GameEntrySimulationWorldProperty =
            GameEntryType?.GetProperty("SimulationWorld", PublicStatic);

        private static readonly MethodInfo GameEntryGetSimulationWorldMethod =
            GameEntrySimulationWorldProperty?.GetGetMethod(true);

        private static readonly MethodInfo GameEntrySetSimulationWorldMethod =
            GameEntrySimulationWorldProperty?.GetSetMethod(true);

        private static readonly PropertyInfo GameEntryEnemyManagerProperty =
            GameEntryType?.GetProperty("EnemyManager", PublicStatic);

        private static readonly MethodInfo GameEntryGetEnemyManagerMethod =
            GameEntryEnemyManagerProperty?.GetGetMethod(true);

        private static readonly MethodInfo GameEntrySetEnemyManagerMethod =
            GameEntryEnemyManagerProperty?.GetSetMethod(true);

        private static readonly PropertyInfo GameEntryProcedureProperty =
            GameEntryType?.GetProperty("Procedure", PublicStatic);

        private static readonly MethodInfo GameEntryGetProcedureMethod =
            GameEntryProcedureProperty?.GetGetMethod(true);

        private static readonly MethodInfo GameEntrySetProcedureMethod =
            GameEntryProcedureProperty?.GetSetMethod(true);

        private static readonly MethodInfo HealthComponentOnInitMethod =
            HealthComponentType?.GetMethod("OnInit", PublicInstance);

        private static readonly FieldInfo ProjectileMaxDistanceFromPlayerField =
            SimulationWorldType?.GetField("_projectileMaxDistanceFromPlayer", NonPublicInstance);

        private static readonly FieldInfo ProjectileMaxVerticalOffsetFromPlayerField =
            SimulationWorldType?.GetField("_projectileMaxVerticalOffsetFromPlayer", NonPublicInstance);

        private GameObject _worldGameObject;
        private Component _worldComponent;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.NotNull(SimulationWorldType, "SimulationWorld type lookup failed.");
            Assert.NotNull(SimulationTickContextType, "SimulationTickContext type lookup failed.");
            Assert.NotNull(EnemySimDataType, "EnemySimData type lookup failed.");
            Assert.NotNull(ProjectileSimDataType, "ProjectileSimData type lookup failed.");
            Assert.NotNull(EnemyProjectileType, "EnemyProjectile type lookup failed.");
            Assert.NotNull(EnemyProjectileDataType, "EnemyProjectileData type lookup failed.");
            Assert.NotNull(CampTypeType, "CampType type lookup failed.");
            Assert.NotNull(GameEntryType, "GameEntry type lookup failed.");
            Assert.NotNull(EnemySeparationSolverProviderType, "EnemySeparationSolverProvider type lookup failed.");
            Assert.NotNull(EnemyManagerComponentType, "EnemyManagerComponent type lookup failed.");
            Assert.NotNull(PlayerType, "Player type lookup failed.");
            Assert.NotNull(EntityBaseType, "EntityBase type lookup failed.");
            Assert.NotNull(HealthComponentType, "HealthComponent type lookup failed.");
            Assert.NotNull(ProcedureComponentType, "ProcedureComponent type lookup failed.");
            Assert.NotNull(ProcedureGameType, "ProcedureGame type lookup failed.");
            Assert.NotNull(GameStateTypeType, "GameStateType type lookup failed.");
            Assert.NotNull(ProcedureManagerType, "ProcedureManager type lookup failed.");
            Assert.NotNull(ProcedureManagerInterfaceType, "IProcedureManager type lookup failed.");
            Assert.NotNull(FsmOpenGenericType, "Fsm`1 type lookup failed.");
            Assert.NotNull(UpsertEnemyMethod, "UpsertEnemy reflection lookup failed.");
            Assert.NotNull(RemoveEnemyByEntityIdMethod, "RemoveEnemyByEntityId reflection lookup failed.");
            Assert.NotNull(UpsertProjectileMethod, "UpsertProjectile reflection lookup failed.");
            Assert.NotNull(RemoveProjectileByEntityIdMethod, "RemoveProjectileByEntityId reflection lookup failed.");
            Assert.NotNull(TryGetEnemyDataMethod, "TryGetEnemyData reflection lookup failed.");
            Assert.NotNull(TickMethod, "Tick reflection lookup failed.");
            Assert.NotNull(TryGetNearestEnemyEntityIdMethod, "TryGetNearestEnemyEntityId reflection lookup failed.");
            Assert.NotNull(TryRequestAreaCollisionMethod, "TryRequestAreaCollision reflection lookup failed.");
            Assert.NotNull(ClearSimulationStateMethod, "ClearSimulationState reflection lookup failed.");
            Assert.NotNull(UseGridBucketSolverMethod, "UseGridBucketSolver reflection lookup failed.");
            Assert.NotNull(EnemiesProperty, "Enemies property reflection lookup failed.");
            Assert.NotNull(ProjectilesProperty, "Projectiles property reflection lookup failed.");
            Assert.NotNull(CollisionCandidateCountProperty, "CollisionCandidateCount property reflection lookup failed.");
            Assert.NotNull(UseSimulationMovementProperty, "UseSimulationMovement property reflection lookup failed.");
            Assert.NotNull(UseSimulationMovementField, "_useSimulationMovement field reflection lookup failed.");
            Assert.NotNull(LastResolvedAreaHitCountProperty, "LastResolvedAreaHitCount property reflection lookup failed.");
            Assert.NotNull(CollisionQueryInputsField, "Collision query inputs field reflection lookup failed.");
            Assert.NotNull(AreaCollisionRequestsField, "Area collision requests field reflection lookup failed.");
            Assert.NotNull(ProjectileMaxDistanceFromPlayerField,
                "Projectile max distance field reflection lookup failed.");
            Assert.NotNull(ProjectileMaxVerticalOffsetFromPlayerField,
                "Projectile max vertical offset field reflection lookup failed.");
            Assert.NotNull(EnemyProjectileOnUpdateMethod, "EnemyProjectile.OnUpdate reflection lookup failed.");
            Assert.NotNull(EnemyProjectileDataField, "EnemyProjectile _projectileData reflection lookup failed.");
            Assert.NotNull(EnemyProjectileIsActiveField, "EnemyProjectile _isActive reflection lookup failed.");
            Assert.NotNull(EnemyProjectileIsSimulationDrivenField,
                "EnemyProjectile _isSimulationDriven reflection lookup failed.");
            Assert.NotNull(GameEntrySimulationWorldProperty, "GameEntry.SimulationWorld property lookup failed.");
            Assert.NotNull(GameEntryGetSimulationWorldMethod,
                "GameEntry.SimulationWorld getter reflection lookup failed.");
            Assert.NotNull(GameEntrySetSimulationWorldMethod,
                "GameEntry.SimulationWorld setter reflection lookup failed.");
            Assert.NotNull(GameEntryEnemyManagerProperty, "GameEntry.EnemyManager property lookup failed.");
            Assert.NotNull(GameEntryGetEnemyManagerMethod, "GameEntry.EnemyManager getter reflection lookup failed.");
            Assert.NotNull(GameEntrySetEnemyManagerMethod, "GameEntry.EnemyManager setter reflection lookup failed.");
            Assert.NotNull(GameEntryProcedureProperty, "GameEntry.Procedure property lookup failed.");
            Assert.NotNull(GameEntryGetProcedureMethod, "GameEntry.Procedure getter reflection lookup failed.");
            Assert.NotNull(GameEntrySetProcedureMethod, "GameEntry.Procedure setter reflection lookup failed.");
            Assert.NotNull(HealthComponentOnInitMethod, "HealthComponent.OnInit reflection lookup failed.");

            _worldGameObject = new GameObject("SimulationWorldPlayModeTests");
            _worldComponent = _worldGameObject.AddComponent(SimulationWorldType);

            // Isolate PlayMode regression to simulation behavior only.
            EntitySyncField?.SetValue(_worldComponent, null);
            TransformSyncField?.SetValue(_worldComponent, null);
            HitPresentationField?.SetValue(_worldComponent, null);

            SetUseSimulationMovement(true);
            UseGridBucketSolverMethod.Invoke(null, new object[] { 1f });
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_worldComponent != null)
            {
                EntitySyncField?.SetValue(_worldComponent, null);
                TransformSyncField?.SetValue(_worldComponent, null);
                HitPresentationField?.SetValue(_worldComponent, null);
            }

            if (_worldGameObject != null)
            {
                Object.Destroy(_worldGameObject);
            }

            _worldComponent = null;
            _worldGameObject = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TickEnemies_ChasesPlayer_WhenOutOfAttackRange()
        {
            UpsertEnemy(CreateEnemy(entityId: 3001, position: Vector3.zero, speed: 2f, attackRange: 1f));

            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: new Vector3(10f, 0f, 0f));

            object enemy = GetEnemyAt(0);
            Assert.That((int)GetField(enemy, "State"), Is.EqualTo(1));
            Vector3 position = (Vector3)GetField(enemy, "Position");
            Vector3 forward = (Vector3)GetField(enemy, "Forward");
            Assert.That(position.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(position.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(forward.x, Is.EqualTo(1f).Within(0.0001f));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickEnemies_StopsMovement_WhenInAttackRange()
        {
            Vector3 startPosition = Vector3.zero;
            UpsertEnemy(CreateEnemy(entityId: 3002, position: startPosition, speed: 3f, attackRange: 2f));

            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: new Vector3(1f, 0f, 0f));

            object enemy = GetEnemyAt(0);
            Assert.That((int)GetField(enemy, "State"), Is.EqualTo(2));
            Vector3 position = (Vector3)GetField(enemy, "Position");
            Assert.That(position.x, Is.EqualTo(startPosition.x).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(startPosition.y).Within(0.0001f));
            Assert.That(position.z, Is.EqualTo(startPosition.z).Within(0.0001f));
            yield break;
        }

        [UnityTest]
        public IEnumerator RemoveEnemyByEntityId_RemapIndex_ForMovedEnemy()
        {
            UpsertEnemy(CreateEnemy(entityId: 3101, position: new Vector3(0f, 0f, 0f), speed: 1f, attackRange: 1f));
            UpsertEnemy(CreateEnemy(entityId: 3102, position: new Vector3(2f, 0f, 0f), speed: 1f, attackRange: 1f));
            UpsertEnemy(CreateEnemy(entityId: 3103, position: new Vector3(4f, 0f, 0f), speed: 1f, attackRange: 1f));

            bool removed = RemoveEnemyByEntityId(3102);
            bool removedEntityExists = TryGetEnemyData(3102, out _);
            bool movedEntityExists = TryGetEnemyData(3103, out object movedEnemy);

            Assert.IsTrue(removed);
            Assert.That(GetEnemiesCount(), Is.EqualTo(2));
            Assert.IsFalse(removedEntityExists);
            Assert.IsTrue(movedEntityExists);
            Assert.That((int)GetField(movedEnemy, "EntityId"), Is.EqualTo(3103));
            Assert.That((int)GetField(GetEnemyAt(1), "EntityId"), Is.EqualTo(3103));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickEnemies_ChasesPlayer_WhenJobSimulationChannelEnabled()
        {
            UpsertEnemy(CreateEnemy(entityId: 3201, position: Vector3.zero, speed: 2f, attackRange: 1f));

            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: new Vector3(10f, 0f, 0f));

            object enemy = GetEnemyAt(0);
            Assert.That((int)GetField(enemy, "State"), Is.EqualTo(1));
            Vector3 position = (Vector3)GetField(enemy, "Position");
            Vector3 forward = (Vector3)GetField(enemy, "Forward");
            Assert.That(position.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(position.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(forward.x, Is.EqualTo(1f).Within(0.0001f));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickEnemies_MatchesOutput_AfterClearSimulationState()
        {
            UpsertEnemy(CreateEnemy(entityId: 3251, position: Vector3.zero, speed: 2f, attackRange: 1f));
            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: new Vector3(10f, 0f, 0f));

            object nonBurstEnemy = GetEnemyAt(0);
            int nonBurstState = (int)GetField(nonBurstEnemy, "State");
            Vector3 nonBurstPosition = (Vector3)GetField(nonBurstEnemy, "Position");
            Vector3 nonBurstForward = (Vector3)GetField(nonBurstEnemy, "Forward");

            ClearSimulationStateMethod.Invoke(_worldComponent, null);

            SetUseSimulationMovement(true);
            UpsertEnemy(CreateEnemy(entityId: 3251, position: Vector3.zero, speed: 2f, attackRange: 1f));
            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: new Vector3(10f, 0f, 0f));

            object burstEnemy = GetEnemyAt(0);
            int burstState = (int)GetField(burstEnemy, "State");
            Vector3 burstPosition = (Vector3)GetField(burstEnemy, "Position");
            Vector3 burstForward = (Vector3)GetField(burstEnemy, "Forward");

            Assert.That(burstState, Is.EqualTo(nonBurstState));
            Assert.That((burstPosition - nonBurstPosition).sqrMagnitude, Is.LessThanOrEqualTo(1e-8f));
            Assert.That((burstForward - nonBurstForward).sqrMagnitude, Is.LessThanOrEqualTo(1e-8f));
            yield break;
        }

        [UnityTest]
        public IEnumerator TryGetNearestEnemyEntityId_SelectsNearestBucketCandidate_WhenJobSimulationEnabled()
        {
            UpsertEnemy(CreateEnemy(entityId: 3301, position: new Vector3(1f, 0f, 0f), speed: 0f, attackRange: 1f));
            UpsertEnemy(CreateEnemy(entityId: 3302, position: new Vector3(6f, 0f, 0f), speed: 0f, attackRange: 1f));

            InvokeTick(deltaTime: 0.016f, realDeltaTime: 0.016f, playerPosition: Vector3.zero);

            object[] parameters = { Vector3.zero, 100f, 0 };
            bool found = (bool)TryGetNearestEnemyEntityIdMethod.Invoke(_worldComponent, parameters);
            int nearestEntityId = (int)parameters[2];

            Assert.IsTrue(found);
            Assert.That(nearestEntityId, Is.EqualTo(3301));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickEnemies_SeparatesOverlappedEnemies_WhenJobSimulationEnabled()
        {
            UpsertEnemy(CreateEnemy(entityId: 3401, position: new Vector3(0f, 0f, 0f), speed: 1f, attackRange: 0.1f,
                avoidEnemyOverlap: true, enemyBodyRadius: 0.45f, separationIterations: 2));
            UpsertEnemy(CreateEnemy(entityId: 3402, position: new Vector3(0.1f, 0f, 0f), speed: 1f, attackRange: 0.1f,
                avoidEnemyOverlap: true, enemyBodyRadius: 0.45f, separationIterations: 2));

            InvokeTick(deltaTime: 0.1f, realDeltaTime: 0.1f, playerPosition: new Vector3(10f, 0f, 0f));

            object enemyA = GetEnemyAt(0);
            object enemyB = GetEnemyAt(1);
            Vector3 posA = (Vector3)GetField(enemyA, "Position");
            Vector3 posB = (Vector3)GetField(enemyB, "Position");
            posA.y = 0f;
            posB.y = 0f;
            float distance = Vector3.Distance(posA, posB);
            Assert.That(distance, Is.GreaterThanOrEqualTo(0.89f));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickEnemies_SeparatesOverlappedEnemies_WhenPlayerIsStaticAndInRange()
        {
            UpsertEnemy(CreateEnemy(entityId: 3411, position: new Vector3(0f, 0f, 0f), speed: 1f, attackRange: 10f,
                avoidEnemyOverlap: true, enemyBodyRadius: 0.45f, separationIterations: 3));
            UpsertEnemy(CreateEnemy(entityId: 3412, position: new Vector3(0.05f, 0f, 0f), speed: 1f, attackRange: 10f,
                avoidEnemyOverlap: true, enemyBodyRadius: 0.45f, separationIterations: 3));

            InvokeTick(deltaTime: 0.1f, realDeltaTime: 0.1f, playerPosition: Vector3.zero);

            object enemyA = GetEnemyAt(0);
            object enemyB = GetEnemyAt(1);
            Assert.That((int)GetField(enemyA, "State"), Is.EqualTo(2));
            Assert.That((int)GetField(enemyB, "State"), Is.EqualTo(2));

            Vector3 posA = (Vector3)GetField(enemyA, "Position");
            Vector3 posB = (Vector3)GetField(enemyB, "Position");
            posA.y = 0f;
            posB.y = 0f;
            float distance = Vector3.Distance(posA, posB);
            Assert.That(distance, Is.GreaterThanOrEqualTo(0.5f));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_MovesAndUpdatesLifetime_WhenJobSimulationEnabled()
        {
            UpsertProjectile(CreateProjectile(entityId: 5401, position: Vector3.zero, forward: Vector3.right,
                velocity: new Vector3(2f, 0f, 0f), speed: 0f, lifeTime: 2f, age: 0f, active: true,
                remainingLifetime: 2f, state: 0));

            InvokeTick(deltaTime: 0.5f, realDeltaTime: 0.5f, playerPosition: Vector3.zero);

            Assert.That(GetProjectilesCount(), Is.EqualTo(1));
            object projectile = GetProjectileAt(0);
            Vector3 position = (Vector3)GetField(projectile, "Position");
            float age = (float)GetField(projectile, "Age");
            float remainingLifetime = (float)GetField(projectile, "RemainingLifetime");
            bool active = (bool)GetField(projectile, "Active");

            Assert.That(position.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(age, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(remainingLifetime, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.IsTrue(active);
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_ContinuesFromLatestState_AcrossConsecutiveTicks()
        {
            UpsertProjectile(CreateProjectile(entityId: 5410, position: Vector3.zero, forward: Vector3.right,
                velocity: new Vector3(2f, 0f, 0f), speed: 0f, lifeTime: 5f, age: 0f, active: true,
                remainingLifetime: 5f, state: 0));

            InvokeTick(deltaTime: 0.5f, realDeltaTime: 0.5f, playerPosition: Vector3.zero);
            object afterJobEnabled = GetProjectileAt(0);
            Vector3 positionAfterJobEnabled = (Vector3)GetField(afterJobEnabled, "Position");
            float ageAfterJobEnabled = (float)GetField(afterJobEnabled, "Age");
            Assert.That(positionAfterJobEnabled.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ageAfterJobEnabled, Is.EqualTo(0.5f).Within(0.0001f));

            InvokeTick(deltaTime: 0.5f, realDeltaTime: 0.5f, playerPosition: Vector3.zero);
            object afterSecondTick = GetProjectileAt(0);
            Vector3 positionAfterSecondTick = (Vector3)GetField(afterSecondTick, "Position");
            float ageAfterSecondTick = (float)GetField(afterSecondTick, "Age");
            float remainingLifetimeAfterSecondTick = (float)GetField(afterSecondTick, "RemainingLifetime");
            bool activeAfterSecondTick = (bool)GetField(afterSecondTick, "Active");

            Assert.That(positionAfterSecondTick.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(ageAfterSecondTick, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(remainingLifetimeAfterSecondTick, Is.EqualTo(4f).Within(0.0001f));
            Assert.IsTrue(activeAfterSecondTick);
            yield break;
        }

        [UnityTest]
        public IEnumerator EnemyProjectile_TogglesCollider_WhenSimulationMovementSwitchesAtRuntime()
        {
            SetUseSimulationMovement(false);

            GameObject projectileObject = new GameObject("EnemyProjectileColliderTogglePlayMode");
            Component projectileComponent = null;
            try
            {
                projectileComponent = projectileObject.AddComponent(EnemyProjectileType);
                Collider projectileCollider = projectileObject.AddComponent<BoxCollider>();
                projectileCollider.enabled = true;

                object previousSimulationWorld = GetGameEntrySimulationWorld();
                SetGameEntrySimulationWorld(_worldComponent);
                try
                {
                    object neutralCamp = System.Enum.Parse(CampTypeType, "Neutral");
                    object projectileData = System.Activator.CreateInstance(
                        EnemyProjectileDataType,
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new object[] { 8001, 1, neutralCamp, 1, 0f, 10f, Vector3.forward },
                        null);

                    EnemyProjectileDataField.SetValue(projectileComponent, projectileData);
                    EnemyProjectileIsActiveField.SetValue(projectileComponent, true);
                    EnemyProjectileIsSimulationDrivenField.SetValue(projectileComponent, false);

                    InvokeEnemyProjectileUpdate(projectileComponent, 0.016f, 0.016f);
                    Assert.IsTrue(projectileCollider.enabled);

                    SetUseSimulationMovement(true);
                    InvokeEnemyProjectileUpdate(projectileComponent, 0.016f, 0.016f);
                    Assert.IsFalse(projectileCollider.enabled);

                    SetUseSimulationMovement(false);
                    InvokeEnemyProjectileUpdate(projectileComponent, 0.016f, 0.016f);
                    Assert.IsTrue(projectileCollider.enabled);
                }
                finally
                {
                    SetGameEntrySimulationWorld(previousSimulationWorld);
                }
            }
            finally
            {
                if (projectileObject != null)
                {
                    Object.Destroy(projectileObject);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveProjectileByEntityId_RemapIndex_ForMovedProjectile()
        {
            UpsertProjectile(CreateProjectile(entityId: 5405, position: new Vector3(0f, 0f, 0f),
                forward: Vector3.forward, velocity: Vector3.zero, speed: 0f, lifeTime: 3f, age: 0f, active: true,
                remainingLifetime: 3f, state: 0));
            UpsertProjectile(CreateProjectile(entityId: 5406, position: new Vector3(1f, 0f, 0f),
                forward: Vector3.forward, velocity: Vector3.zero, speed: 0f, lifeTime: 3f, age: 0f, active: true,
                remainingLifetime: 3f, state: 0));
            UpsertProjectile(CreateProjectile(entityId: 5407, position: new Vector3(2f, 0f, 0f),
                forward: Vector3.forward, velocity: Vector3.zero, speed: 0f, lifeTime: 3f, age: 0f, active: true,
                remainingLifetime: 3f, state: 0));

            bool removed = RemoveProjectileByEntityId(5406);
            bool removedMoved = RemoveProjectileByEntityId(5407);

            Assert.IsTrue(removed);
            Assert.That(GetProjectilesCount(), Is.EqualTo(1));
            Assert.IsTrue(removedMoved);
            Assert.That((int)GetField(GetProjectileAt(0), "EntityId"), Is.EqualTo(5405));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_RecyclesWhenExceedingPlayerDistance_WhenJobSimulationEnabled()
        {
            ProjectileMaxDistanceFromPlayerField.SetValue(_worldComponent, 5f);
            ProjectileMaxVerticalOffsetFromPlayerField.SetValue(_worldComponent, 1000f);
            UpsertProjectile(CreateProjectile(entityId: 5408, position: new Vector3(6f, 0f, 0f),
                forward: Vector3.forward, velocity: Vector3.zero, speed: 0f, lifeTime: 3f, age: 0f, active: true,
                remainingLifetime: 3f, state: 0));

            InvokeTick(deltaTime: 0.016f, realDeltaTime: 0.016f, playerPosition: Vector3.zero);

            Assert.That(GetProjectilesCount(), Is.EqualTo(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_RecyclesWhenExceedingVerticalOffset_WhenJobSimulationEnabled()
        {
            ProjectileMaxDistanceFromPlayerField.SetValue(_worldComponent, 0f);
            ProjectileMaxVerticalOffsetFromPlayerField.SetValue(_worldComponent, 1f);
            UpsertProjectile(CreateProjectile(entityId: 5409, position: new Vector3(0f, 2f, 0f),
                forward: Vector3.forward, velocity: Vector3.zero, speed: 0f, lifeTime: 3f, age: 0f, active: true,
                remainingLifetime: 3f, state: 0));

            InvokeTick(deltaTime: 0.016f, realDeltaTime: 0.016f, playerPosition: Vector3.zero);

            Assert.That(GetProjectilesCount(), Is.EqualTo(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_RecyclesExpiredProjectile_WhenJobSimulationEnabled()
        {
            UpsertProjectile(CreateProjectile(entityId: 5402, position: Vector3.zero, forward: Vector3.forward,
                velocity: Vector3.zero, speed: 0f, lifeTime: 1f, age: 0.95f, active: true, remainingLifetime: 0.05f,
                state: 0));

            InvokeTick(deltaTime: 0.1f, realDeltaTime: 0.1f, playerPosition: Vector3.zero);

            Assert.That(GetProjectilesCount(), Is.EqualTo(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_BuildsCollisionCandidatesAgainstEnemies_WhenJobSimulationEnabled()
        {
            UpsertEnemy(CreateEnemy(entityId: 5501, position: Vector3.zero, speed: 0f, attackRange: 1f));
            UpsertProjectile(CreateProjectile(entityId: 5502, position: Vector3.zero, forward: Vector3.forward,
                velocity: Vector3.zero, speed: 0f, lifeTime: 2f, age: 0f, active: true, remainingLifetime: 2f,
                state: 0));

            InvokeTick(deltaTime: 0.016f, realDeltaTime: 0.016f, playerPosition: Vector3.zero);

            Assert.That(GetCollisionCandidateCount(), Is.GreaterThan(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_BuildsCollisionCandidates_WithLatestEnemyMovement_WhenJobSimulationEnabled()
        {
            UpsertEnemy(CreateEnemy(entityId: 5511, position: new Vector3(2f, 0f, 0f), speed: 1f, attackRange: 0.1f));
            UpsertProjectile(CreateProjectile(entityId: 5512, position: new Vector3(1f, 0f, 0f),
                forward: Vector3.forward, velocity: Vector3.zero, speed: 0f, lifeTime: 2f, age: 0f, active: true,
                remainingLifetime: 2f, state: 0));

            InvokeTick(deltaTime: 1f, realDeltaTime: 1f, playerPosition: Vector3.zero);

            Assert.That(GetCollisionCandidateCount(), Is.GreaterThan(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_ExpiresAfterCollisionCandidateConsumed_WhenJobSimulationEnabled()
        {
            UpsertEnemy(CreateEnemy(entityId: 5503, position: Vector3.zero, speed: 0f, attackRange: 1f));
            UpsertProjectile(CreateProjectile(entityId: 5504, position: Vector3.zero, forward: Vector3.forward,
                velocity: Vector3.zero, speed: 0f, lifeTime: 10f, age: 0f, active: true, remainingLifetime: 10f,
                state: 0));

            InvokeTick(deltaTime: 0.016f, realDeltaTime: 0.016f, playerPosition: Vector3.zero);

            Assert.That(GetProjectilesCount(), Is.EqualTo(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator TickProjectiles_LimitsCandidatesToMaxTargets_IncludingPlayerCandidate()
        {
            object previousEnemyManager = GetGameEntryEnemyManager();
            GameObject enemyManagerObject = new GameObject("EnemyManagerMaxTargetsPlayMode");
            GameObject playerObject = new GameObject("PlayerTargetMaxTargetsPlayMode");
            try
            {
                Component enemyManager = enemyManagerObject.AddComponent(EnemyManagerComponentType);
                Component player = playerObject.AddComponent(PlayerType);
                Component healthComponent = playerObject.AddComponent(HealthComponentType);
                HealthComponentOnInitMethod.Invoke(healthComponent, new object[] { 100, null });
                SetPrivateField(player, "_healthComponent", healthComponent);
                SetPrivateField(player, "m_CachedTransform", playerObject.transform);
                SetPrivateField(player, "m_Available", true);

                object enemyById = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(int), EntityBaseType));
                enemyById.GetType().GetMethod("Add")?.Invoke(enemyById, new object[] { -1, player });
                object enemies = Activator.CreateInstance(typeof(List<>).MakeGenericType(EntityBaseType));
                enemies.GetType().GetMethod("Add")?.Invoke(enemies, new object[] { player });
                SetPrivateField(enemyManager, "_enemyById", enemyById);
                SetPrivateField(enemyManager, "_enemies", enemies);
                SetGameEntryEnemyManager(enemyManager);

                UpsertEnemy(CreateEnemy(entityId: 5521, position: Vector3.zero, speed: 0f, attackRange: 1f));
                UpsertProjectile(CreateProjectile(entityId: 5522, position: Vector3.zero, forward: Vector3.forward,
                    velocity: Vector3.zero, speed: 0f, lifeTime: 1f, age: 0f, active: true, remainingLifetime: 1f,
                    state: 0));

                InvokeTick(deltaTime: 0.016f, realDeltaTime: 0.016f, playerPosition: Vector3.zero);

                Assert.That(GetCollisionCandidateCount(), Is.EqualTo(1));
            }
            finally
            {
                SetGameEntryEnemyManager(previousEnemyManager);
                Object.Destroy(enemyManagerObject);
                Object.Destroy(playerObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TryRequestAreaCollision_ReturnsFalse_WhenSimulationMovementDisabled()
        {
            SetUseSimulationMovement(false);
            object[] requestArgs = { 5530, 5530, Vector3.zero, 1f, 1 };
            bool requestResult = (bool)TryRequestAreaCollisionMethod.Invoke(_worldComponent, requestArgs);

            Assert.IsFalse(requestResult);
            Assert.IsFalse((bool)UseSimulationMovementProperty.GetValue(_worldComponent));

            yield break;
        }

        [UnityTest]
        public IEnumerator EnqueueAreaQuery_CapturesInactiveSourceSnapshot_WhenSourceEntityUnavailable()
        {
            UpsertEnemy(CreateEnemy(entityId: 5531, position: Vector3.zero, speed: 0f, attackRange: 1f));

            object[] enqueueArgs = { 99999, 99999, Vector3.zero, 1f, 1 };
            bool enqueueResult = (bool)TryRequestAreaCollisionMethod.Invoke(_worldComponent, enqueueArgs);
            Assert.IsTrue(enqueueResult);

            object areaCollisionRequests = AreaCollisionRequestsField.GetValue(_worldComponent);
            Assert.NotNull(areaCollisionRequests);
            PropertyInfo requestCountProperty = areaCollisionRequests.GetType().GetProperty("Count", PublicInstance);
            int requestCount = (int)requestCountProperty.GetValue(areaCollisionRequests);
            Assert.That(requestCount, Is.GreaterThan(0));

            PropertyInfo requestItemProperty = areaCollisionRequests.GetType().GetProperty("Item", PublicInstance);
            object firstRequest = requestItemProperty.GetValue(areaCollisionRequests, new object[] { 0 });
            FieldInfo requestSnapshotField =
                firstRequest.GetType().GetField("SourceWasActiveAtQueryTime", PublicInstance);
            Assert.NotNull(requestSnapshotField);
            bool requestSnapshot = (bool)requestSnapshotField.GetValue(firstRequest);
            Assert.IsFalse(requestSnapshot);

            InvokeTick(deltaTime: 0.016f, realDeltaTime: 0.016f, playerPosition: Vector3.zero);
            Assert.That(GetLastResolvedAreaHitCount(), Is.EqualTo(0));
            yield break;
        }

        private object CreateEnemy(int entityId, Vector3 position, float speed, float attackRange,
            bool avoidEnemyOverlap = false, float enemyBodyRadius = 0.45f, int separationIterations = 1)
        {
            object enemy = System.Activator.CreateInstance(EnemySimDataType);
            SetField(ref enemy, "EntityId", entityId);
            SetField(ref enemy, "Position", position);
            SetField(ref enemy, "Forward", Vector3.forward);
            SetField(ref enemy, "Rotation", Quaternion.identity);
            SetField(ref enemy, "Speed", speed);
            SetField(ref enemy, "AttackRange", attackRange);
            SetField(ref enemy, "AvoidEnemyOverlap", avoidEnemyOverlap);
            SetField(ref enemy, "EnemyBodyRadius", enemyBodyRadius);
            SetField(ref enemy, "SeparationIterations", separationIterations);
            SetField(ref enemy, "TargetType", 0);
            SetField(ref enemy, "State", 0);
            return enemy;
        }

        private object CreateProjectile(int entityId, Vector3 position, Vector3 forward, Vector3 velocity, float speed,
            float lifeTime, float age, bool active, float remainingLifetime, int state)
        {
            object projectile = System.Activator.CreateInstance(ProjectileSimDataType);
            SetField(ref projectile, "EntityId", entityId);
            SetField(ref projectile, "OwnerEntityId", 0);
            SetField(ref projectile, "Position", position);
            SetField(ref projectile, "Forward", forward);
            SetField(ref projectile, "Velocity", velocity);
            SetField(ref projectile, "Speed", speed);
            SetField(ref projectile, "LifeTime", lifeTime);
            SetField(ref projectile, "Age", age);
            SetField(ref projectile, "Active", active);
            SetField(ref projectile, "RemainingLifetime", remainingLifetime);
            SetField(ref projectile, "State", state);
            return projectile;
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

        private void SetUseSimulationMovement(bool enabled)
        {
            UseSimulationMovementField.SetValue(_worldComponent, enabled);
        }

        private void UpsertEnemy(object enemy)
        {
            UpsertEnemyMethod.Invoke(_worldComponent, new[] { enemy });
        }

        private void UpsertProjectile(object projectile)
        {
            UpsertProjectileMethod.Invoke(_worldComponent, new[] { projectile });
        }

        private bool RemoveEnemyByEntityId(int entityId)
        {
            return (bool)RemoveEnemyByEntityIdMethod.Invoke(_worldComponent, new object[] { entityId });
        }

        private bool RemoveProjectileByEntityId(int entityId)
        {
            return (bool)RemoveProjectileByEntityIdMethod.Invoke(_worldComponent, new object[] { entityId });
        }

        private static object GetGameEntrySimulationWorld()
        {
            return GameEntryGetSimulationWorldMethod.Invoke(null, null);
        }

        private static void SetGameEntrySimulationWorld(object simulationWorld)
        {
            GameEntrySetSimulationWorldMethod.Invoke(null, new[] { simulationWorld });
        }

        private static object GetGameEntryEnemyManager()
        {
            return GameEntryGetEnemyManagerMethod.Invoke(null, null);
        }

        private static void SetGameEntryEnemyManager(object enemyManager)
        {
            GameEntrySetEnemyManagerMethod.Invoke(null, new[] { enemyManager });
        }

        private static object GetGameEntryProcedure()
        {
            return GameEntryGetProcedureMethod.Invoke(null, null);
        }

        private static void SetGameEntryProcedure(object procedureComponent)
        {
            GameEntrySetProcedureMethod.Invoke(null, new[] { procedureComponent });
        }

        private static void InvokeEnemyProjectileUpdate(Component projectileComponent, float elapseSeconds,
            float realElapseSeconds)
        {
            EnemyProjectileOnUpdateMethod.Invoke(projectileComponent, new object[] { elapseSeconds, realElapseSeconds });
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

        private object GetProjectileAt(int index)
        {
            object projectiles = ProjectilesProperty.GetValue(_worldComponent);
            PropertyInfo itemProperty = projectiles.GetType().GetProperty("Item", PublicInstance);
            return itemProperty.GetValue(projectiles, new object[] { index });
        }

        private int GetProjectilesCount()
        {
            object projectiles = ProjectilesProperty.GetValue(_worldComponent);
            PropertyInfo countProperty = projectiles.GetType().GetProperty("Count", PublicInstance);
            return (int)countProperty.GetValue(projectiles);
        }

        private int GetCollisionCandidateCount()
        {
            return (int)CollisionCandidateCountProperty.GetValue(_worldComponent);
        }

        private int GetLastResolvedAreaHitCount()
        {
            return (int)LastResolvedAreaHitCountProperty.GetValue(_worldComponent);
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, NonPublicInstance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"Field '{fieldName}' was not found on type '{target.GetType().FullName}'.");
        }
    }
}
