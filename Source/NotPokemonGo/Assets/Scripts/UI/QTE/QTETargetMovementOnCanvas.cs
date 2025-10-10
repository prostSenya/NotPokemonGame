using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.QTE
{
  public class QTETargetMovementOnCanvas : QTEButtonView
  {
    private const float MaxValue = 1f;

    [SerializeField] private Slider _slider;
    [SerializeField] private float _timer;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _tolerance = 0.05f;

    private QTETargetMovementCalculator _movementCalculator;
    private float _remainingTime;
    private bool _isCompleted;
    private bool _isFailed;

    public override event Action<QTEButtonView> Successed;
    public override event Action<QTEButtonView> Invalided;

    private void OnEnable()
    {
      _movementCalculator = new QTETargetMovementCalculator(_speed, MaxValue, _tolerance);
      _movementCalculator.Reset();
      _remainingTime = _timer;
      _slider.value = 0f;
      _isCompleted = false;
      _isFailed = false;
    }

    private void Update()
    {
      if (_isCompleted || _isFailed)
      {
        return;
      }

      _remainingTime -= Time.deltaTime * _speed;

      if (_remainingTime <= 0f)
      {
        _isFailed = true;
        Invalided?.Invoke(this);
        Debug.Log($"Failed in QTETargetMovementOnCanvas");
        return;
      }

      QTETargetMovementSnapshot snapshot = _movementCalculator.Tick(Time.deltaTime);
      _slider.value = snapshot.NormalizedProgress;

      if (snapshot.Failed)
      {
        _isFailed = true;
        Invalided?.Invoke(this);
        Debug.Log($"Failed in QTETargetMovementOnCanvas");
      }
      else if (snapshot.Completed)
      {
        _isCompleted = true;
        Successed?.Invoke(this);
      }
    }
  }
}
