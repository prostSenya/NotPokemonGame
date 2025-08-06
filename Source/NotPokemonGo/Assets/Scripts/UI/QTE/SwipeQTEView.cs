using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.QTE
{
    public class SwipeQTEView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image _arrow;
        [SerializeField] private float _minSwipeDistance = 50f;

        private Vector2 _startPosition;
        private QTESystem.SwipeDirection _direction;

        public event Action<bool> Completed;

        public void Initialize(QTESystem.SwipeDirection direction)
        {
            _direction = direction;
            float angle = direction switch
            {
                QTESystem.SwipeDirection.Up => 0f,
                QTESystem.SwipeDirection.Right => -90f,
                QTESystem.SwipeDirection.Down => 180f,
                QTESystem.SwipeDirection.Left => 90f,
                _ => 0f
            };
            if (_arrow != null)
                _arrow.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnPointerDown(PointerEventData eventData) =>
            _startPosition = eventData.position;

        public void OnPointerUp(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - _startPosition;
            if (delta.magnitude < _minSwipeDistance)
            {
                Completed?.Invoke(false);
                return;
            }

            var detected = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? (delta.x > 0 ? QTESystem.SwipeDirection.Right : QTESystem.SwipeDirection.Left)
                : (delta.y > 0 ? QTESystem.SwipeDirection.Up : QTESystem.SwipeDirection.Down);

            Completed?.Invoke(detected == _direction);
        }
    }
}
