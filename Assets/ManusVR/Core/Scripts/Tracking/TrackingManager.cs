// Copyright (c) 2018 ManusVR

using ManusVR.Core.Apollo;
using ManusVR.Core.Hands;
using ManusVR.Core.ProjectManagement;
using ManusVR.Core.Utilities;
using System;
using System.Collections;
using UnityEngine;

namespace ManusVR.Core.Tracking
{
    public class TrackingManager : MonoBehaviour
    {
        public enum TrackerLocation
        {
            Wrist,
            Hand
        }

        [Tooltip("To what bodypart is the tracker attached?")]
        public TrackerLocation trackerLocation = TrackerLocation.Hand;

        [Header("Custom Offsets")]
        [Tooltip("The positional offset between the left hand and the vive tracker.")]
        public Vector3 leftPositionOffset;
        [Tooltip("The rotational offset between the left hand and the vive tracker.")]
        public Quaternion leftRotationOffset = Quaternion.identity;
        [Tooltip("The positional offset between the right hand and the vive tracker.")]
        public Vector3 rightPositionOffset;
        [Tooltip("The rotational offset between the right hand and the vive tracker.")]
        public Quaternion rightRotationOffset = Quaternion.identity;

        [Header("Custom Transform references")]
        [Tooltip("The left arm that will be moved when the controllers are moving.")]
        public Transform leftLowerArm;
        [Tooltip("The right arm that will be moved when the controllers are moving.")]
        public Transform rightLowerArm;
        [Tooltip("This left controller will act as an target for the hands to move towards.")]
        public Transform leftController;
        [Tooltip("This right controller will act as an target for the hands to move towards.")]
        public Transform rightController;

        [Tooltip("The hand controller will be used to set the type of rotation data it will use to rotate the hand.")]
        public HandController handController;

        protected ManusVRManager ManusVRManager;

        protected virtual void Start()
        {
            ManusVRManager = ManusVRManager ?? GetComponentInParent<ManusVRManager>();

            leftLowerArm = leftLowerArm != null ? leftLowerArm : transform.FindDeepChild("lowerarm_l");
            rightLowerArm = rightLowerArm != null ? rightLowerArm : transform.FindDeepChild("lowerarm_r");
            leftController = leftController != null ? leftController : ManusVRManager.LeftTargetController;
            rightController = rightController != null ? rightController : ManusVRManager.RightTargetController;

            handController = handController ?? GetComponent<HandController>();

            if (leftController == null || rightController == null)
            {
                Debug.LogWarning("Can not set all of the references to the controllers, is SteamVR imported in the project?");
                return;
            }

            InitialiseTracking();
        }

        protected virtual void InitialiseTracking()
        {
            if (handController == null) return;

            foreach (device_type_t deviceType in Enum.GetValues(typeof(device_type_t)))
            {
                handController.Hands.TryGetValue(deviceType, out Hand hand);

                if (hand == null)
                {
                    Debug.LogError("Hand not found!");
                    continue;
                }

                hand.UseHandTracker = trackerLocation == TrackerLocation.Hand;

                if (trackerLocation == TrackerLocation.Hand)
                {
                    InitialiseHandTracking(hand);
                }
                else
                {
                    InitialiseWristTracking(deviceType);
                }
            }
        }

        protected void InitialiseHandTracking(Hand hand)
        {
            bool isLeft = hand.DeviceType == device_type_t.GLOVE_LEFT;
            TransformFollow tFollow;
            if (isLeft)
            {
                AttachTransformFollow(hand.transform, leftController, leftPositionOffset, leftRotationOffset, out tFollow);
            }
            else
            {
                AttachTransformFollow(hand.transform, rightController, rightPositionOffset, rightRotationOffset, out tFollow);
            }
            AddDefaultOffset(tFollow, isLeft);
        }

        protected void InitialiseWristTracking(device_type_t deviceType)
        {
            bool isLeft = deviceType == device_type_t.GLOVE_LEFT;

            TransformFollow tFollow;
            if (isLeft)
            {
                AttachTransformFollow(leftLowerArm, leftController, leftPositionOffset, leftRotationOffset, out tFollow);
            }
            else
            {
                AttachTransformFollow(rightLowerArm, rightController, rightPositionOffset, rightRotationOffset, out tFollow);
            }
            AddDefaultOffset(tFollow, isLeft);
        }

        private void AttachTransformFollow(Transform transformToMove, Transform transformToFollow, out TransformFollow transformFollow)
        {
            transformFollow = transformToMove?.gameObject.AddComponent<TransformFollow>();
            if (transformFollow == null) return;

            transformFollow.transformToMove = transformToMove;
            transformFollow.transformToFollow = transformToFollow;
        }

        private void AttachTransformFollow(Transform transformToMove, Transform transformToFollow, Vector3 positionOffset, Quaternion rotationOffset, out TransformFollow transformFollow)
        {
            AttachTransformFollow(transformToMove, transformToFollow, out transformFollow);
            transformFollow.positionOffset = positionOffset;
            transformFollow.rotationQuat = rotationOffset;
        }

        /// <summary>
        /// Add default offsets to tracked transforms.
        /// These are magic numbers that we found to give the most accurate tracking.
        /// </summary>
        /// <param name="transformFollowToOffset"></param>
        /// <param name="isLeft"></param>
        private void AddDefaultOffset(TransformFollow transformFollowToOffset, bool isLeft)
        {
            switch (trackerLocation)
            {
                case TrackerLocation.Wrist:
                    if (isLeft)
                    {
                        transformFollowToOffset.positionOffset += new Vector3(0, -0.04f, 0.25f);
                        transformFollowToOffset.rotationOffset += new Vector3(-90, -90, 0);
                    }
                    else
                    {
                        transformFollowToOffset.positionOffset += new Vector3(0, -0.04f, 0.25f);
                        transformFollowToOffset.rotationOffset += new Vector3(90, 90, 0);
                    }
                    break;
                case TrackerLocation.Hand:
                    if (isLeft)
                    {
                        transformFollowToOffset.positionOffset += new Vector3(0, -0.07f, 0.06f);
                        transformFollowToOffset.rotationOffset += new Vector3(0, 90, 205);
                    }
                    else
                    {
                        transformFollowToOffset.positionOffset += new Vector3(0, -0.07f, 0.06f);
                        transformFollowToOffset.rotationOffset += new Vector3(0, 90, 25);
                    }
                    break;
            }
        }
    }
}