
using System.Collections.Generic;
using UnityEngine;

namespace CustomUtility
{
    public static class EnemySeparationSolverProvider
    {
        private struct Registration
        {
            public float BodyRadius;
        }

        private static IEnemySeparationSolver _current = new GridBucketEnemySeparationSolver();
        private static readonly Dictionary<Transform, Registration> Registrations = new();

        public static IEnemySeparationSolver Current => _current;
        public static string CurrentSolverName => _current.GetType().Name;

        public static void SetSolver(IEnemySeparationSolver solver)
        {
            if (solver == null) return;
            _current = solver;
            ReRegisterAll();
        }

        public static void UseGridBucketSolver(float cellSize = 1f)
        {
            SetSolver(new GridBucketEnemySeparationSolver(cellSize));
        }

        public static void UseNaiveSolver()
        {
            SetSolver(new NaiveEnemySeparationSolver());
        }

        public static void Register(Transform transform, float bodyRadius)
        {
            if (transform == null) return;

            var registration = new Registration
            {
                BodyRadius = bodyRadius
            };
            Registrations[transform] = registration;
            _current.Register(transform, bodyRadius);
        }

        public static void Unregister(Transform transform)
        {
            if (transform == null) return;

            _current.Unregister(transform);
            Registrations.Remove(transform);
        }

        public static Vector3 Resolve(Transform transform, Vector3 desiredPosition, Vector3 fallbackDirection,
            int iterations)
        {
            return _current.Resolve(transform, desiredPosition, fallbackDirection, iterations);
        }

        private static void ReRegisterAll()
        {
            foreach (var pair in Registrations)
            {
                Transform transform = pair.Key;
                Registration registration = pair.Value;
                if (transform == null) continue;

                _current.Register(transform, registration.BodyRadius);
            }
        }
    }    
}
