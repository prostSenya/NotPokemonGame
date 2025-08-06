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

        public void Start()
        {
            QTEConfig qteConfig = _staticDataService.GetQTEConfig(_abilityProvider.AbilityModel.AbilityType, default);
            // перенести камеру в презенторе или в другом классе

            switch (qteConfig.QTEType)
            {
                case QTEType.HoldZone:
                    _qtePresenter = new QTEPresenter1();
                    break;

                case QTEType.GetIntoCircle:
                    _qtePresenter = new QTEPresenter2();
                    break;

                default:
                    throw new ArgumentOutOfRangeException("QTEType not found");
            }

            _qtePresenter.Enable(qteConfig, _coroutineRunner);

            _qtePresenter.Completed += OnCompleted;
        }

        private void OnCompleted(bool success)
        {
            _qtePresenter.Completed -= OnCompleted;

            _qtePresenter.Disable();

            Completed?.Invoke(success);
        }
    }
}