using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.QTE
{
  public class QTETargetMovementOnCanvas : QTEButtonView
  {
    private const float StartThreshold = 0.05f;
    private const float CompletionThreshold = 0.98f;
    private const int MousePointerId = -1;

    [Header("Geometry")]
    [SerializeField] private RectTransform _trackRect;   
    [SerializeField] private RectTransform _handleRect;  

    [Header("Timing")]
    [SerializeField] private float _timer = 5f;          
    [SerializeField] private float _speed = 1f;          

    [Header("Tolerance")]
    [SerializeField, Range(0f, 0.5f)]
    private float _tolerance = 0.05f;                    

    private Canvas _canvas;
    private Camera _uiCamera;
    private QTEPhasePresenter _phasePresenter;

    private float _timeLimit;
    private float _timeScale;
    private bool _isDragging;
    private bool _isCompleted;
    private bool _isFailed;

    private float _baseY;
    private float _startX;
    private float _startProgress;
    private float _minX;
    private float _maxX;
    private bool _startCaptured;

    private bool _pointerIsTouch;
    private int _activeTouchId = MousePointerId;
    private Vector2 _lastPointerPosition;

    public override event Action<QTEButtonView> Successed;
    public override event Action<QTEButtonView> Invalided;

    private void Awake()
    {
      CacheReferences();
      if (!EnsureBindings())
      {
        enabled = false;
        return;
      }

      RefreshGeometryCache(captureStartFromHandle: true);
    }

    private void OnEnable()
    {
      if (!EnsureBindings())
      {
        enabled = false;
        return;
      }

      RefreshGeometryCache(captureStartFromHandle: false);
      ResetInternalState();
    }

    public override void Initialize(QTEPhasePresenter qtePhasePresenter)
    {
      base.Initialize(qtePhasePresenter);
      _phasePresenter = qtePhasePresenter;
      RefreshGeometryCache(captureStartFromHandle: false);
      ResetInternalState();
    }

    private void Update()
    {
      if (_isCompleted || _isFailed)
        return;

      TickTimer();
      if (_isCompleted || _isFailed)
        return;

      HandleInput();
    }

    #region Time
    private void TickTimer()
    {
      CurrentTime += Time.deltaTime * _timeScale;

      if (_timeLimit > 0f && CurrentTime >= _timeLimit)
      {
        Fail();
      }
    }

    private float ResolveTimeLimit()
    {
      if (_phasePresenter != null)
      {
        float configured = _phasePresenter.QtePhaseSetup.TargetTime;
        if (configured > 0f) return configured;
      }
      return _timer;
    }

    private float ResolveTimeScale()
    {
      if (_phasePresenter != null)
      {
        float configured = _phasePresenter.QtePhaseSetup.Speed;
        if (configured > 0f) return configured;
      }
      return Mathf.Max(_speed, 0.0001f);
    }
    #endregion

    #region Input
    private void HandleInput()
    {
      if (!_isDragging)
      {
        if (GetPointerDown(out Vector2 startPosition, out bool isTouch, out int touchId) &&
            TryStartDrag(startPosition))
        {
          _isDragging = true;
          _pointerIsTouch = isTouch;
          _activeTouchId = touchId;
          ContinueDrag(startPosition);
        }

        return;
      }

      if (GetPointer(out Vector2 pointerPosition))
        ContinueDrag(pointerPosition);

      if (GetPointerUp(out _))
      {
        if (!_isCompleted)
          Fail();
        else
          StopDragging();
      }
    }

    private bool TryStartDrag(Vector2 screenPosition)
    {
      if (!ScreenToLocalOnTrack(screenPosition, out Vector2 local)) return false;
      if (!IsWithinBounds(local)) return false;

      float normalized = LocalToProgress(local.x);
      if (normalized > StartThreshold)
        return false;

      SetHandleByProgress(0f);
      return true;
    }

    private void ContinueDrag(Vector2 screenPosition)
    {
      if (!ScreenToLocalOnTrack(screenPosition, out Vector2 local)) return;

      if (!IsWithinBounds(local))
      {
        Fail();
        return;
      }

      float clampedX = Mathf.Clamp(local.x, _minX, _maxX);

      float newProgress = LocalToProgress(clampedX);
      newProgress = Mathf.Max(GetCurrentProgress(), newProgress);

      SetHandleByProgress(newProgress);

      if (newProgress >= CompletionThreshold)
        Complete();
    }

    private bool GetPointerDown(out Vector2 position, out bool isTouch, out int touchId)
    {
      if (Input.touchCount > 0)
      {
        for (int i = 0; i < Input.touchCount; i++)
        {
          Touch touch = Input.GetTouch(i);
          if (touch.phase == TouchPhase.Began)
          {
            position = touch.position;
            isTouch = true;
            touchId = touch.fingerId;
            _lastPointerPosition = position;
            return true;
          }
        }
      }

      if (Input.GetMouseButtonDown(0))
      {
        position = Input.mousePosition;
        isTouch = false;
        touchId = MousePointerId;
        _lastPointerPosition = position;
        return true;
      }

      position = default;
      isTouch = false;
      touchId = MousePointerId;
      return false;
    }

    private bool GetPointer(out Vector2 position)
    {
      if (_pointerIsTouch)
      {
        for (int i = 0; i < Input.touchCount; i++)
        {
          Touch touch = Input.GetTouch(i);
          if (touch.fingerId == _activeTouchId)
          {
            if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
            {
              position = touch.position;
              _lastPointerPosition = position;
              return false;
            }

            position = touch.position;
            _lastPointerPosition = position;
            return true;
          }
        }

        position = default;
        return false;
      }

      if (Input.GetMouseButton(0))
      {
        position = Input.mousePosition;
        _lastPointerPosition = position;
        return true;
      }

      position = default;
      return false;
    }

    private bool GetPointerUp(out Vector2 position)
    {
      if (_pointerIsTouch)
      {
        for (int i = 0; i < Input.touchCount; i++)
        {
          Touch touch = Input.GetTouch(i);
          if (touch.fingerId == _activeTouchId &&
              (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
          {
            position = touch.position;
            return true;
          }
        }

        if (_activeTouchId != MousePointerId)
        {
          position = _lastPointerPosition;
          return true;
        }

        position = default;
        return false;
      }

      if (Input.GetMouseButtonUp(0))
      {
        position = Input.mousePosition;
        return true;
      }

      position = default;
      return false;
    }
    #endregion

    #region Geometry helpers
    private bool ScreenToLocalOnTrack(Vector2 screenPosition, out Vector2 local)
    {
      CacheReferences();
      return RectTransformUtility.ScreenPointToLocalPointInRectangle(_trackRect, screenPosition, _uiCamera, out local);
    }

    private float LocalToProgress(float localX)
    {
      return RawToProgress(GetRawFromPivot(localX));
    }

    private float GetCurrentProgress()
    {
      return RawToProgress(GetRawFromPivot(_handleRect.anchoredPosition.x));
    }

    private void SetHandleByProgress(float progress)
    {
      if (_handleRect == null)
        return;

      if (Mathf.Approximately(_maxX, _minX))
      {
        _handleRect.anchoredPosition = new Vector2(_startX, _baseY);
        return;
      }

      float raw = ProgressToRaw(progress);
      float x = Mathf.Lerp(_minX, _maxX, raw);

      _handleRect.anchoredPosition = new Vector2(x, _baseY);
    }

    private bool IsWithinBounds(Vector2 localPoint)
    {
      Rect rect = _trackRect.rect;

      // Жестко по горизонтали — вне трека сразу фейл
      if (localPoint.x < rect.xMin || localPoint.x > rect.xMax)
        return false;

      // Вертикальный коридор: половина высоты + допуск от высоты
      float halfHeight = rect.height * 0.5f;
      float allowedVertical = halfHeight + rect.height * _tolerance;
      float verticalDistance = Mathf.Abs(localPoint.y);

      return verticalDistance <= allowedVertical;
    }

    private float GetRawFromPivot(float pivotX)
    {
      if (Mathf.Approximately(_maxX, _minX))
        return _startProgress;

      float raw = Mathf.InverseLerp(_minX, _maxX, Mathf.Clamp(pivotX, _minX, _maxX));
      return Mathf.Max(raw, _startProgress);
    }

    private float RawToProgress(float raw)
    {
      if (Mathf.Approximately(1f, _startProgress))
        return 1f;

      return Mathf.InverseLerp(_startProgress, 1f, Mathf.Clamp(raw, _startProgress, 1f));
    }

    private float ProgressToRaw(float progress)
    {
      if (progress <= 0f)
        return _startProgress;

      if (progress >= 1f || Mathf.Approximately(1f, _startProgress))
        return 1f;

      return Mathf.Lerp(_startProgress, 1f, Mathf.Clamp01(progress));
    }
    #endregion

    #region End states
    private void Complete()
    {
      if (_isCompleted || _isFailed) return;

      _isCompleted = true;
      StopDragging();
      SetHandleByProgress(1f);
      Successed?.Invoke(this);
    }

    private void Fail()
    {
      if (_isFailed || _isCompleted) return;

      _isFailed = true;
      StopDragging();
      Invalided?.Invoke(this);
    }
    #endregion

    #region Init / Reset
    private void CacheReferences()
    {
      if (_canvas == null)
        _canvas = GetComponentInParent<Canvas>();

      if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        _uiCamera = _canvas.worldCamera;
      else
        _uiCamera = null;
    }

    private void ResetInternalState()
    {
      CacheReferences();

      if (!EnsureBindings())
      {
        enabled = false;
        return;
      }

      RefreshGeometryCache(captureStartFromHandle: false);

      _timeLimit = ResolveTimeLimit();
      _timeScale = ResolveTimeScale();

      _isCompleted = _isFailed = false;
      StopDragging();
      CurrentTime = 0f;

      SetHandleByProgress(0f);
    }

    private bool EnsureBindings()
    {
      // Если не задан трек – берём RectTransform объекта со скриптом
      if (_trackRect == null)
        _trackRect = GetComponent<RectTransform>();

      // Если не задан хэндл – ищем ребёнка с RectTransform по имени "Handle" или любого первого
      if (_handleRect == null && _trackRect != null)
      {
        // Попробуем по имени
        var t = _trackRect.Find("Handle") as RectTransform;
        if (t != null) _handleRect = t;
        else
        {
          // Любой первый дочерний RectTransform
          for (int i = 0; i < _trackRect.childCount; i++)
          {
            var child = _trackRect.GetChild(i) as RectTransform;
            if (child != null) { _handleRect = child; break; }
          }
        }
      }

      if (_trackRect == null || _handleRect == null)
      {
        Debug.LogError("[QTETargetMovementOnCanvas] Bindings missing. " +
                       "Assign _trackRect and _handleRect in inspector. " +
                       "Hint: Handle should be a child of Track.");
        return false;
      }

      // гарантируем, что хэндл действительно под треком
      if (_handleRect.parent != _trackRect)
      {
        _handleRect.SetParent(_trackRect, worldPositionStays: false);
      }

      return true;
    }


    private void StopDragging()
    {
      _isDragging = false;
      _pointerIsTouch = false;
      _activeTouchId = MousePointerId;
      _lastPointerPosition = Vector2.zero;
    }

    private void RefreshGeometryCache(bool captureStartFromHandle)
    {
      if (_trackRect == null || _handleRect == null)
        return;

      if (!_startCaptured)
        captureStartFromHandle = true;

      NormalizeHandleAnchors();

      Rect trackRect = _trackRect.rect;
      float handleWidth = _handleRect.rect.width;
      float pivotX = _handleRect.pivot.x;

      _minX = trackRect.xMin + handleWidth * pivotX;
      _maxX = trackRect.xMax - handleWidth * (1f - pivotX);

      if (_maxX < _minX)
      {
        float center = trackRect.center.x;
        _minX = _maxX = center;
      }

      if (captureStartFromHandle)
      {
        Vector2 anchored = _handleRect.anchoredPosition;
        _baseY = anchored.y;
        _startX = Mathf.Clamp(anchored.x, _minX, _maxX);
        _handleRect.anchoredPosition = new Vector2(_startX, _baseY);
        _startCaptured = true;
      }
      else if (!_startCaptured)
      {
        Vector2 anchored = _handleRect.anchoredPosition;
        _baseY = anchored.y;
        _startX = Mathf.Clamp(anchored.x, _minX, _maxX);
        _startCaptured = true;
      }
      else
      {
        _startX = Mathf.Clamp(_startX, _minX, _maxX);
      }

      _startProgress = Mathf.Approximately(_maxX, _minX)
        ? 0f
        : Mathf.InverseLerp(_minX, _maxX, _startX);
      _startProgress = Mathf.Clamp01(_startProgress);
    }

    private void NormalizeHandleAnchors()
    {
      if (_handleRect == null)
        return;

      Vector3 localPosition = _handleRect.localPosition;
      Vector2 size = _handleRect.rect.size;
      Vector2 anchorMin = _handleRect.anchorMin;
      Vector2 anchorMax = _handleRect.anchorMax;

      bool changed = false;

      if (!Mathf.Approximately(anchorMin.x, anchorMax.x))
      {
        float anchorX = Mathf.Lerp(anchorMin.x, anchorMax.x, _handleRect.pivot.x);
        anchorMin.x = anchorMax.x = anchorX;
        changed = true;
      }

      if (!Mathf.Approximately(anchorMin.y, anchorMax.y))
      {
        float anchorY = Mathf.Lerp(anchorMin.y, anchorMax.y, _handleRect.pivot.y);
        anchorMin.y = anchorMax.y = anchorY;
        changed = true;
      }

      if (!changed)
        return;

      _handleRect.anchorMin = anchorMin;
      _handleRect.anchorMax = anchorMax;
      _handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
      _handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
      _handleRect.localPosition = localPosition;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (Application.isPlaying)
        return;

      CacheReferences();

      if (EnsureBindings())
        RefreshGeometryCache(captureStartFromHandle: true);
    }
#endif


    #endregion
  }
}
