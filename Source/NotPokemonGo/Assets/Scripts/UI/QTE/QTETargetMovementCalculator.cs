using System;
using UnityEngine;

namespace UI.QTE
{
    public readonly struct QTETargetMovementSnapshot
    {
        public QTETargetMovementSnapshot(float normalizedProgress, bool completed, bool failed)
        {
            NormalizedProgress = Mathf.Clamp01(normalizedProgress);
            Completed = completed;
            Failed = failed;
        }

        public float NormalizedProgress { get; }

        public bool Completed { get; }

        public bool Failed { get; }
    }

    public class QTETargetMovementCalculator
    {
        private readonly float _speed;
        private readonly float _pathLength;
        private readonly float _tolerance;

        private float _distanceTravelled;

        public QTETargetMovementCalculator(float speed, float pathLength, float tolerance)
        {
            if (pathLength <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pathLength), "Path length must be greater than zero.");
            }

            if (tolerance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be non-negative.");
            }

            _speed = speed;
            _pathLength = pathLength;
            _tolerance = tolerance;
            _distanceTravelled = 0f;
        }

        public QTETargetMovementSnapshot Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time must be non-negative.");
            }

            _distanceTravelled += Mathf.Max(0f, deltaTime * _speed);

            float normalizedProgress = _distanceTravelled / _pathLength;
            bool completed = _distanceTravelled >= _pathLength;
            bool failed = _distanceTravelled > _pathLength + _tolerance;

            return new QTETargetMovementSnapshot(normalizedProgress, completed, failed);
        }

        public void Reset()
        {
            _distanceTravelled = 0f;
        }
    }
}
