using System;
using System.Collections.Generic;
using Services.QTEServices;
using UI.QTE;
using UnityEngine.UI;

namespace QTESystem
{
    [Serializable]
    public class QTESetup
    {
        public QTEMode qteMode;
        public float Speed;
        public AimRingView AimRingView;
        public Image Overlay;

        public float TargetTime;
        public float Offset;
        public float TimeToNextTarget;

        public SwipeDirection Direction;
    }
}