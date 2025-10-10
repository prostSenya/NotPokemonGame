using System.Collections.Generic;
using UnityEngine;

namespace QTESystem
{
  [CreateAssetMenu(fileName = nameof(QTETargetMovementPathConfig), menuName = "StaticData/QTE/" + nameof(QTETargetMovementPathConfig))]
  public class QTETargetMovementPathConfig : ScriptableObject
  {
    [SerializeField] private RectTransform _pathSpace;
    [SerializeField] private Vector2[] _splinePoints;
    [SerializeField, Min(0f)] private float _tolerance = 10f;
    [SerializeField, Min(0f)] private float _timeLimit = 5f;

    public RectTransform PathSpace => _pathSpace;
    public IReadOnlyList<Vector2> SplinePoints => _splinePoints;
    public float Tolerance => _tolerance;
    public float TimeLimit => _timeLimit;
  }
}
