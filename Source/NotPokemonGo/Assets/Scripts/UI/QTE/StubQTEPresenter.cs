using QTESystem;
using Services;

namespace UI.QTE
{
    public class StubQTEPresenter : QTEPresenter
    {
        public override void Enable(QTEConfig qteConfig, ICoroutineRunner coroutineRunner)
        {
            Completed?.Invoke(false);
        }
    }
}
