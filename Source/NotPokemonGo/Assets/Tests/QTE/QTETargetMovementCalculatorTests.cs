using NUnit.Framework;
using UI.QTE;

namespace Tests.QTE
{
    public class QTETargetMovementCalculatorTests
    {
        [Test]
        public void Tick_IncreasesProgressAlongPath()
        {
            var calculator = new QTETargetMovementCalculator(speed: 0.5f, pathLength: 1f, tolerance: 0.2f);

            QTETargetMovementSnapshot firstStep = calculator.Tick(1f);
            Assert.That(firstStep.NormalizedProgress, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(firstStep.Completed, Is.False);
            Assert.That(firstStep.Failed, Is.False);

            QTETargetMovementSnapshot secondStep = calculator.Tick(1f);
            Assert.That(secondStep.NormalizedProgress, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(secondStep.Completed, Is.True);
            Assert.That(secondStep.Failed, Is.False);
        }

        [Test]
        public void Tick_FailsWhenExceedingTolerance()
        {
            var calculator = new QTETargetMovementCalculator(speed: 1f, pathLength: 1f, tolerance: 0.1f);

            calculator.Tick(1f);
            QTETargetMovementSnapshot overshoot = calculator.Tick(0.2f);

            Assert.That(overshoot.Failed, Is.True);
            Assert.That(overshoot.NormalizedProgress, Is.EqualTo(1f));
        }
    }
}
