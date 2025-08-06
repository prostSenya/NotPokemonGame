using System;
using QTESystem;
using Services;
using UnityEngine;

namespace UI.QTE
{
    public class AimRingPresenter : QTEPresenter
    {
        private AimRingView _view;
        private GameObject _canvasInstance;

        public override void Enable(QTEConfig qteConfig, ICoroutineRunner coroutineRunner)
        {
            if (qteConfig.QteCanvas != null)
                _canvasInstance = GameObject.Instantiate(qteConfig.QteCanvas.gameObject);

            if (qteConfig.QteCanvas != null && qteConfig.QteCanvas.AimRingViewPrefab != null)
            {
                _view = GameObject.Instantiate(qteConfig.QteCanvas.AimRingViewPrefab, _canvasInstance.transform);
                QTESetup setup = qteConfig.QteSetup[0];
                _view.Initialize(setup.Offset, setup.TargetTime, Vector2.zero);
                _view.Successed += OnSuccess;
                _view.Invalided += OnFail;
            }
            else
            {
                Completed?.Invoke(false);
            }
        }

        public override void Disable()
        {
            if (_view != null)
            {
                _view.Successed -= OnSuccess;
                _view.Invalided -= OnFail;
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
            }

            if (_canvasInstance != null)
            {
                UnityEngine.Object.Destroy(_canvasInstance);
                _canvasInstance = null;
            }
        }

        private void OnSuccess(AimRingView view)
        {
            Disable();
            Completed?.Invoke(true);
        }

        private void OnFail(AimRingView view)
        {
            Disable();
            Completed?.Invoke(false);
        }
    }
}
