using QTESystem;
using Services;
using UnityEngine;

namespace UI.QTE
{
    public class SwipePresenter : QTEPresenter
    {
        private SwipeQTEView _view;
        private GameObject _canvasInstance;

        public override void Enable(QTEConfig qteConfig, ICoroutineRunner coroutineRunner)
        {
            if (qteConfig.QteCanvas != null)
                _canvasInstance = GameObject.Instantiate(qteConfig.QteCanvas.gameObject);

            if (qteConfig.QteCanvas != null && qteConfig.QteCanvas.SwipeViewPrefab != null)
            {
                _view = GameObject.Instantiate(qteConfig.QteCanvas.SwipeViewPrefab, _canvasInstance.transform);
                SwipeDirection direction = qteConfig.QteSetup.Count > 0 ? qteConfig.QteSetup[0].Direction : SwipeDirection.Up;
                _view.Initialize(direction);
                _view.Completed += OnCompleted;
            }
            else
            {
                Completed?.Invoke(false);
            }
        }

        private void OnCompleted(bool success)
        {
            Disable();
            Completed?.Invoke(success);
        }

        public override void Disable()
        {
            if (_view != null)
            {
                _view.Completed -= OnCompleted;
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
            }

            if (_canvasInstance != null)
            {
                UnityEngine.Object.Destroy(_canvasInstance);
                _canvasInstance = null;
            }
        }
    }
}
