using System.Numerics;
using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {
        private double DistanceToOrigin = 1;

        private double AngleToZYPlane = 0;

        private double AngleToZXPlane = 0;

        private const double DistanceScaleFactor = 1.1;

        private const double AngleChangeStepSize = Math.PI / 180 * 5;

        private Vector3D<float> targetPoint = Vector3D<float>.Zero;
        private Vector3D<float> position = new Vector3D<float>(7.0f, 4.0f, 4.0f);
        private Vector3D<float> upVector = new Vector3D<float>(0.0f, 1.0f, 0.0f);
        /// <summary>
        /// Gets the position of the camera.
        /// </summary>
        public Vector3D<float> Position
        {
            get
            {
                return position;
            }
        }

        /// <summary>
        /// Gets the up vector of the camera.
        /// </summary>
        public Vector3D<float> UpVector
        {
            get
            {
                return Vector3D.Normalize(upVector);
            }
        }

        /// <summary>
        /// Gets the target point of the camera view.
        /// </summary>
        public Vector3D<float> Target
        {
            get
            {
                // For the moment the camera is always pointed at the origin.
                return targetPoint;
            }

            set
            {
                targetPoint = value;
            }
        }

        public void MoveForward(float dist)
        {
            Vector3D<float> direction = Vector3D.Normalize(targetPoint - position);
            position += direction * dist;
            targetPoint += direction * dist;
        }

        public void MoveBack(float dist)
        {
            MoveForward(-dist);
        }

        public void MoveRight(float dist)
        {
            Vector3D<float> direction = Vector3D.Normalize(targetPoint - position);
            Vector3D<float> right = Vector3D.Normalize(Vector3D.Cross(direction, upVector));
            position += right * dist;
            targetPoint += right * dist;
        }

        public void MoveLeft(float dist)
        {
            MoveRight(-dist);
        }

        public void MoveUp(float dist)
        {
            position.Y += dist;
            targetPoint.Y += dist;
        }

        public void MoveDown(float dist)
        {
            MoveUp(-dist);
        }
        public void MoveTargetX(float delta)
        {
            targetPoint.X += delta;
        }

        public void MoveTargetY(float delta)
        {
            targetPoint.Y += delta;
        }

        public void MoveTargetZ(float delta)
        {
            targetPoint.Z += delta;
        }

        public void IncreaseZXAngle()
        {
            AngleToZXPlane += AngleChangeStepSize;
        }

        public void DecreaseZXAngle()
        {
            AngleToZXPlane -= AngleChangeStepSize;
        }

        public void IncreaseZYAngle()
        {
            AngleToZYPlane += AngleChangeStepSize;

        }

        public void DecreaseZYAngle()
        {
            AngleToZYPlane -= AngleChangeStepSize;
        }

        public void IncreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin * DistanceScaleFactor;
        }

        public void DecreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin / DistanceScaleFactor;
        }

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }
    }
}
