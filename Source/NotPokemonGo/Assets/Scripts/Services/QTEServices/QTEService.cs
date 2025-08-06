using System;
using Abilities;
using QTESystem;
using Services.StaticDataServices;
using UI.QTE;

namespace Services.QTEServices
{
    public class QTEService : IQTEService
    {
        private readonly IStaticDataService _staticDataService;
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly IAbilityProvider _abilityProvider;

        private QTEPresenter _qtePresenter;
        public event Action<bool> Completed;

        public QTEService(
            IStaticDataService staticDataService,
            ICoroutineRunner coroutineRunner,
            IAbilityProvider abilityProvider)
        {
            _staticDataService = staticDataService;
            _coroutineRunner = coroutineRunner;
            _abilityProvider = abilityProvider;
        }

        public void Start(QTEType qteType = QTEType.AimRing)
        {
            QTEConfig qteConfig = _staticDataService.GetQTEConfig(_abilityProvider.AbilityModel.AbilityType, qteType);

            switch (qteConfig.QTEType)
            {
                case QTEType.Swipe:
                    _qtePresenter = new SwipePresenter();
                    break;

                case QTEType.AimRing:
                    _qtePresenter = new AimRingPresenter();
                    break;

                default:
                    _qtePresenter = new StubQTEPresenter();
                    break;
            }

            _qtePresenter.Completed += OnCompleted;
            _qtePresenter.Enable(qteConfig, _coroutineRunner);
        }

        private void OnCompleted(bool success)
        {
            _qtePresenter.Completed -= OnCompleted;

            _qtePresenter.Disable();

            Completed?.Invoke(success);
        }
    }
}