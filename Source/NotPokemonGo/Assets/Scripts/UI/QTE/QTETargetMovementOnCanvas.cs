using System;
using QTESystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.QTE
{
  public class QTETargetMovementOnCanvas : QTEButtonView, IBeginDragHandler, IDragHandler, IEndDragHandler
  {
    [SerializeField] private RectTransform _marker;
    [SerializeField] private Image _progressFill;
    [SerializeField] private Image _timeFill;

    public override event Action<QTEButtonView> Successed;
    public override event Action<QTEButtonView, QTEInvalidReason> Invalided;
    public event Action<float> OnProgressChanged;

    private QTETargetMovementPathConfig _config;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Camera _uiCamera;
    private Vector2[] _polyline;
    private float[] _segmentLengths;
    private float _totalLength;
    private float _toleranceSqr;
    private float _timeLimit;
    private float _elapsedTime;
    private float _progress;
    private bool _isRunning;
    private bool _isDragging;
    private bool _hasCompleted;
    private bool _pendingSuccess;
    private bool _pendingFailure;
    private QTEInvalidReason _pendingFailureReason;

    public override void Initialize(QTEPhasePresenter qtePhasePresenter)
    {
      base.Initialize(qtePhasePresenter);

      if (_rectTransform == null)
      {
        _rectTransform = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
          _uiCamera = _canvas.worldCamera;
      }

      Configure(qtePhasePresenter.QtePhaseSetup.TargetMovementPathConfig);
    }

    private void Update()
    {
      if (_pendingFailure)
      {
        _pendingFailure = false;
        Fail(_pendingFailureReason);
        return;
      }

      if (_pendingSuccess)
      {
        _pendingSuccess = false;
        UpdateProgress(1f);
        return;
      }

      if (_isRunning == false || _hasCompleted)
        return;

      if (_timeLimit > 0f)
      {
        _elapsedTime += Time.deltaTime;
        UpdateTimeIndicator();

        if (_elapsedTime >= _timeLimit)
        {
          Fail(QTEInvalidReason.Timeout);
        }
      }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
      if (_isRunning == false || eventData.button != PointerEventData.InputButton.Left)
        return;

      _isDragging = true;
      ProcessPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
      if (_isRunning == false || _isDragging == false)
        return;

      ProcessPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
      if (_isDragging == false)
        return;

      _isDragging = false;
    }

    private void Configure(QTETargetMovementPathConfig config)
    {
      _config = config;
      _elapsedTime = 0f;
      _progress = 0f;
      _hasCompleted = false;
      _pendingSuccess = false;
      _pendingFailure = false;
      _isDragging = false;

      if (_config == null)
      {
        _isRunning = false;
        _pendingFailure = true;
        _pendingFailureReason = QTEInvalidReason.Unknown;
        return;
      }

      int pointCount = BuildPolyline();
      _toleranceSqr = _config.Tolerance * _config.Tolerance;
      _timeLimit = _config.TimeLimit;

      if (pointCount == 0)
      {
        _isRunning = false;
        _pendingFailure = true;
        _pendingFailureReason = QTEInvalidReason.Unknown;
        return;
      }

      SetProgressInternal(0f, notify: false);
      UpdateTimeIndicator();

      if (_marker != null && _polyline != null && _polyline.Length > 0)
        _marker.anchoredPosition = _polyline[0];

      if (_totalLength <= Mathf.Epsilon)
      {
        _isRunning = false;
        _pendingSuccess = true;
      }
      else
      {
        _isRunning = true;
      }
    }

    private int BuildPolyline()
    {
      var points = _config.SplinePoints;
      int pointCount = points == null ? 0 : points.Count;

      if (pointCount == 0)
      {
        _polyline = Array.Empty<Vector2>();
        _segmentLengths = Array.Empty<float>();
        _totalLength = 0f;
        return 0;
      }

      EnsurePolylineCapacity(pointCount);

      RectTransform pathSpace = _config.PathSpace;
      for (int i = 0; i < pointCount; i++)
      {
        _polyline[i] = ConvertToLocal(points[i], pathSpace);
      }

      int segmentCount = Mathf.Max(0, pointCount - 1);
      EnsureSegmentCapacity(segmentCount);

      _totalLength = 0f;
      for (int i = 0; i < segmentCount; i++)
      {
        Vector2 segment = _polyline[i + 1] - _polyline[i];
        float length = segment.magnitude;
        _segmentLengths[i] = length;
        _totalLength += length;
      }

      return pointCount;
    }

    private void ProcessPointer(PointerEventData eventData)
    {
      if (_polyline == null || _polyline.Length == 0)
        return;

      if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, _uiCamera, out Vector2 localPoint) == false)
        return;

      Vector2 projectedPoint;
      float sqrDistance;
      float progress = CalculateProgress(localPoint, out projectedPoint, out sqrDistance);

      if (_marker != null)
        _marker.anchoredPosition = projectedPoint;

      if (sqrDistance > _toleranceSqr)
      {
        Fail(QTEInvalidReason.OutOfBounds);
        return;
      }

      if (progress > _progress)
        UpdateProgress(progress);
    }

    private float CalculateProgress(Vector2 position, out Vector2 projectedPoint, out float sqrDistance)
    {
      if (_polyline.Length == 1)
      {
        projectedPoint = _polyline[0];
        Vector2 diffSingle = position - projectedPoint;
        sqrDistance = diffSingle.sqrMagnitude;
        return _totalLength <= Mathf.Epsilon ? 1f : 0f;
      }

      float bestDistance = float.MaxValue;
      float bestProgress = 0f;
      Vector2 bestPoint = _polyline[0];
      float accumulated = 0f;

      int lastIndex = _polyline.Length - 1;
      for (int i = 0; i < lastIndex; i++)
      {
        Vector2 start = _polyline[i];
        Vector2 end = _polyline[i + 1];
        Vector2 segment = end - start;
        float segmentLength = _segmentLengths[i];

        float t = 0f;
        Vector2 closestPoint = start;

        if (segmentLength > 0f)
        {
          float projection = Vector2.Dot(position - start, segment);
          float denom = segmentLength * segmentLength;
          projection /= denom;

          if (projection <= 0f)
          {
            t = 0f;
            closestPoint = start;
          }
          else if (projection >= 1f)
          {
            t = 1f;
            closestPoint = end;
          }
          else
          {
            t = projection;
            closestPoint = new Vector2(start.x + segment.x * t, start.y + segment.y * t);
          }
        }

        Vector2 diff = position - closestPoint;
        float distanceSqr = diff.sqrMagnitude;
        if (distanceSqr < bestDistance)
        {
          bestDistance = distanceSqr;
          bestPoint = closestPoint;
          bestProgress = _totalLength > 0f ? (accumulated + segmentLength * t) / _totalLength : 1f;
        }

        accumulated += segmentLength;
      }

      projectedPoint = bestPoint;
      sqrDistance = bestDistance;
      return Mathf.Clamp01(bestProgress);
    }

    private void UpdateProgress(float progress)
    {
      SetProgressInternal(progress, notify: true);
    }

    private void SetProgressInternal(float progress, bool notify)
    {
      float clamped = Mathf.Clamp01(progress);
      _progress = clamped;

      if (_progressFill != null)
        _progressFill.fillAmount = clamped;

      if (notify)
      {
        OnProgressChanged?.Invoke(clamped);

        if (clamped >= 1f)
          Complete();
      }
    }

    private void UpdateTimeIndicator()
    {
      if (_timeFill == null || _timeLimit <= 0f)
        return;

      float remaining = Mathf.Clamp01(1f - (_elapsedTime / _timeLimit));
      _timeFill.fillAmount = remaining;
    }

    private void Complete()
    {
      if (_hasCompleted)
        return;

      _hasCompleted = true;
      _isRunning = false;
      Successed?.Invoke(this);
    }

    private void Fail(QTEInvalidReason reason)
    {
      if (_hasCompleted)
        return;

      _hasCompleted = true;
      _isRunning = false;
      _isDragging = false;
      Invalided?.Invoke(this, reason);
    }

    private Vector2 ConvertToLocal(Vector2 point, RectTransform space)
    {
      if (space == null)
        return point;

      Vector3 world = space.TransformPoint(new Vector3(point.x, point.y, 0f));
      Vector3 local = _rectTransform.InverseTransformPoint(world);
      return new Vector2(local.x, local.y);
    }

    private void EnsurePolylineCapacity(int count)
    {
      if (_polyline == null || _polyline.Length != count)
        _polyline = count > 0 ? new Vector2[count] : Array.Empty<Vector2>();
    }

    private void EnsureSegmentCapacity(int count)
    {
      if (_segmentLengths == null || _segmentLengths.Length != count)
        _segmentLengths = count > 0 ? new float[count] : Array.Empty<float>();
    }
  }
}
