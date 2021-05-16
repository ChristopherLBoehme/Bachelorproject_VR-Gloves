using UnityEngine;

namespace ManusVR.Core.Utilities
{
    public class TransformFollow : MonoBehaviour
    {
        public enum UpdateMoment
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        [Header("Position Variables")]
        public bool shouldFollowPosition = true;
        public bool shouldSmoothPosition = false;
        public float maxDistanceDeltaPerFrame = 0.003f;
        public Vector3 targetPosition { get; private set; }

        [Header("Rotation Variables")]
        public bool shouldFollowRotation = true;
        public Vector3 targetRotation { get; private set; }

        public UpdateMoment moment = UpdateMoment.Update;

        [Header("Offsets")]
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        public Quaternion rotationQuat;

        [Header("Transforms to Cache")]
        public Transform transformToFollow;
        public Transform transformToMove;

        // Update is called once per frame
        protected void Update()
        {
            if (moment == UpdateMoment.Update)
            {
                Follow();
            }
        }

        protected void FixedUpdate()
        {
            if (moment == UpdateMoment.FixedUpdate)
            {
                Follow();
            }
        }

        protected virtual void LateUpdate()
        {
            if (moment == UpdateMoment.LateUpdate)
            {
                Follow();
            }
        }

        protected void Follow()
        {
            if (shouldFollowPosition)
            {
                FollowPosition();
            }

            if (shouldFollowRotation)
            {
                FollowRotation();
            }
        }

        protected void FollowPosition()
        {
            Vector3 targetTransformPosition = transformToFollow.TransformPoint(positionOffset);
            Vector3 newPosition;

            if (shouldSmoothPosition)
            {
                float alpha = Mathf.Clamp01(Vector3.Distance(targetPosition, targetTransformPosition) / maxDistanceDeltaPerFrame);
                newPosition = Vector3.Lerp(targetPosition, targetTransformPosition, alpha);
            }
            else
            {
                newPosition = targetTransformPosition;
            }
            transformToMove.position = newPosition;
        }

        protected void FollowRotation()
        {
            Quaternion targetTransformRotation = transformToFollow.rotation * Quaternion.Euler(rotationOffset);
            Quaternion newRotation;

            if (shouldSmoothPosition)
            {
                float alpha = Mathf.Clamp01(Quaternion.Angle(Quaternion.Euler(targetRotation), targetTransformRotation) / maxDistanceDeltaPerFrame);
                newRotation = Quaternion.Lerp(Quaternion.Euler(targetRotation), targetTransformRotation, alpha);
            }
            else
            {
                newRotation = targetTransformRotation;
            }
            targetRotation = newRotation.eulerAngles;
            transformToMove.rotation = newRotation;
        }
    }
}
