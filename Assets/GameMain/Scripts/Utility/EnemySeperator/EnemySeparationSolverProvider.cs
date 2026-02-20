
using System.Collections.Generic;
using Components;
using UnityEngine;

namespace CustomUtility
{
    public static class EnemySeparationSolverProvider
    {
        private struct Registration
        {
            public Transform Transform;
            public float BodyRadius;
        }

        private static IEnemySeparationSolver _current = new GridBucketEnemySeparationSolver();
        private static readonly Dictionary<MovementComponent, Registration> Registrations = new();

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

        public static void Register(MovementComponent mover, Transform transform, float bodyRadius)
        {
            if (mover == null || transform == null) return;

            var registration = new Registration
            {
                Transform = transform,
                BodyRadius = bodyRadius
            };
            Registrations[mover] = registration;
            _current.Register(mover, transform, bodyRadius);
        }

        public static void Unregister(MovementComponent mover)
        {
            if (mover == null) return;

            _current.Unregister(mover);
            Registrations.Remove(mover);
        }

        public static Vector3 Resolve(MovementComponent mover, Vector3 desiredPosition, Vector3 fallbackDirection,
            int iterations)
        {
            return _current.Resolve(mover, desiredPosition, fallbackDirection, iterations);
        }

        private static void ReRegisterAll()
        {
            foreach (var pair in Registrations)
            {
                MovementComponent mover = pair.Key;
                Registration registration = pair.Value;
                if (mover == null || registration.Transform == null) continue;

                _current.Register(mover, registration.Transform, registration.BodyRadius);
            }
        }
    }    
}
