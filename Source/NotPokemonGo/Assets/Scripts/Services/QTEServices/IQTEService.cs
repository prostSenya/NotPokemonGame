using System;
using QTESystem;

namespace Services.QTEServices
{
    public interface IQTEService
    {
        void Start(QTEType qteType = QTEType.AimRing);
        event Action <bool> Completed;
    }
}