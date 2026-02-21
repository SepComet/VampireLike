using System.Collections.Generic;
using UnityEngine;

namespace CustomUtility
{
    public static class EnemySeparationSolverProvider
    {
        private enum SolverType
        {
            GridBucket,
            Naive
        }

        private struct LegacyRegistration
        {
            public int AgentId;
            public float BodyRadius;
        }

        private static SolverType _solverType = SolverType.GridBucket;
        private static float _gridCellSize = 1f;
        private static IEnemySeparationSolver _legacySolver = CreateSolver();
        private static IEnemySeparationSolver _simulationSolver = CreateSolver();

        private static readonly Dictionary<Transform, LegacyRegistration> LegacyRegistrations = new();
        private static readonly List<EnemySeparationAgent> LegacyAgents = new();
        private static readonly List<Transform> LegacyRecycle = new();
        private static int _legacySnapshotFrame = -1;
        private static int _nextLegacyAgentId = 1;

        public static IEnemySeparationSolver Current => _simulationSolver;
        public static string CurrentSolverName => _simulationSolver.GetType().Name;

        public static void SetSolver(IEnemySeparationSolver solver)
        {
            if (solver == null) return;

            _legacySolver = solver;
            _simulationSolver = solver;
            _legacySnapshotFrame = -1;
        }

        public static void UseGridBucketSolver(float cellSize = 1f)
        {
            _solverType = SolverType.GridBucket;
            _gridCellSize = Mathf.Max(0.1f, cellSize);
            RecreateSolvers();
        }

        public static void UseNaiveSolver()
        {
            _solverType = SolverType.Naive;
            RecreateSolvers();
        }

        public static void Register(Transform transform, float bodyRadius)
        {
            if (transform == null) return;

            if (LegacyRegistrations.TryGetValue(transform, out LegacyRegistration registration))
            {
                registration.BodyRadius = bodyRadius;
                LegacyRegistrations[transform] = registration;
            }
            else
            {
                LegacyRegistrations.Add(transform, new LegacyRegistration
                {
                    AgentId = _nextLegacyAgentId++,
                    BodyRadius = bodyRadius
                });
            }

            _legacySnapshotFrame = -1;
        }

        public static void Unregister(Transform transform)
        {
            if (transform == null) return;
            if (!LegacyRegistrations.Remove(transform)) return;

            _legacySnapshotFrame = -1;
        }

        public static Vector3 Resolve(Transform transform, Vector3 desiredPosition, Vector3 fallbackDirection,
            int iterations)
        {
            if (transform == null) return desiredPosition;
            if (!LegacyRegistrations.TryGetValue(transform, out LegacyRegistration registration))
            {
                return desiredPosition;
            }

            EnsureLegacySnapshot();
            return _legacySolver.Resolve(registration.AgentId, desiredPosition, fallbackDirection, iterations);
        }

        public static void SetSimulationAgents(IReadOnlyList<EnemySeparationAgent> agents)
        {
            _simulationSolver.SetAgents(agents);
        }

        public static Vector3 ResolveSimulation(int agentId, Vector3 desiredPosition, Vector3 fallbackDirection,
            int iterations)
        {
            return _simulationSolver.Resolve(agentId, desiredPosition, fallbackDirection, iterations);
        }

        private static void EnsureLegacySnapshot()
        {
            int frame = Time.frameCount;
            if (_legacySnapshotFrame == frame) return;

            _legacySnapshotFrame = frame;
            LegacyAgents.Clear();
            LegacyRecycle.Clear();

            foreach (var pair in LegacyRegistrations)
            {
                Transform transform = pair.Key;
                if (transform == null)
                {
                    LegacyRecycle.Add(pair.Key);
                    continue;
                }

                Vector3 position = transform.position;
                position.y = 0f;

                LegacyAgents.Add(new EnemySeparationAgent
                {
                    AgentId = pair.Value.AgentId,
                    Position = position,
                    Radius = Mathf.Max(0.01f, pair.Value.BodyRadius)
                });
            }

            for (int i = 0; i < LegacyRecycle.Count; i++)
            {
                LegacyRegistrations.Remove(LegacyRecycle[i]);
            }

            _legacySolver.SetAgents(LegacyAgents);
        }

        private static void RecreateSolvers()
        {
            _legacySolver = CreateSolver();
            _simulationSolver = CreateSolver();
            _legacySnapshotFrame = -1;
        }

        private static IEnemySeparationSolver CreateSolver()
        {
            if (_solverType == SolverType.Naive)
            {
                return new NaiveEnemySeparationSolver();
            }

            return new GridBucketEnemySeparationSolver(_gridCellSize);
        }
    }
}
