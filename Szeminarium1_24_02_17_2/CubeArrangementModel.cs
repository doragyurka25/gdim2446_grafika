namespace Szeminarium1_24_02_17_2
{
    internal class CubeArrangementModel
    {
        /// <summary>
        /// Gets or sets wheather the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabeld { get; set; } = false;

        /// <summary>
        /// The time of the simulation. It helps to calculate time dependent values.
        /// </summary>
        private double Time { get; set; } = 0;

        /// <summary>
        /// The value by which the center cube is scaled. It varies between 0.8 and 1.2 with respect to the original size.
        /// </summary>
        public double CenterCubeScale { get; private set; } = 1;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeAngleOwnRevolution { get; private set; } = 0;

        public bool isLayerRotating { get; private set; } = false;

        public float CurrentAngle { get; private set; } = 0.0f;//radianban van

        private float targetAngle = 0.0f;
        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeAngleRevolutionOnGlobalY { get; private set; } = 0;


        public void rotateLayer(float dir)
        {
            if (isLayerRotating) return;

            isLayerRotating = true;
            targetAngle = CurrentAngle + (dir * (float)(Math.PI / 2.0));
        }

        public void ResetAngle()
        {
            CurrentAngle = 0.0f;
            targetAngle = 0.0f;
        }
        internal void AdvanceTime(double deltaTime)
        {
            // we do not advance the simulation when animation is stopped
            //if (!AnimationEnabeld)
            //  return;

            if (isLayerRotating)
            {
                float step = 5.0f * (float)deltaTime;

                if (CurrentAngle < targetAngle)
                {
                    CurrentAngle += step;
                    if (CurrentAngle >= targetAngle)
                    {
                        CurrentAngle = targetAngle;
                        isLayerRotating = false;
                    }
                }
                else if (CurrentAngle > targetAngle)
                {
                    CurrentAngle -= step;
                    if (CurrentAngle <= targetAngle)
                    {
                        CurrentAngle = targetAngle;
                        isLayerRotating = false;
                    }
                }
            }

            if (!AnimationEnabeld)
                return;
            // set a simulation time
            Time += deltaTime;

            // lets produce an oscillating scale in time
            CenterCubeScale = 1 + 0.2 * Math.Sin(1.5 * Time);

            DiamondCubeAngleOwnRevolution = Time * 10;

            DiamondCubeAngleRevolutionOnGlobalY = -Time;
        }
    }
}
