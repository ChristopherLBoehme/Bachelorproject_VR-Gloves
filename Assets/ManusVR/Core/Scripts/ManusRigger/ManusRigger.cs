using ManusVR.Core.Apollo;
using ManusVR.Core.Hands;
using ManusVR.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ManusVR.Core.ManusRigger
{
    public enum PhalangeType
    {
        Proximal = 1,
        Intermedial = 2,
        Distal = 3
    }

    public class ManusRigger : MonoBehaviour
    {
        [SerializeField]
        public HandRig LeftHand, RightHand;

        private Animator GetAnimator(device_type_t deviceType)
        {
            return null;
        }

        public Transform GetWristTransform(device_type_t deviceType)
        {
            HandRig hand = GetHand(deviceType);

            return hand?.WristTransform;
        }

        public Transform GetFingerTransform(device_type_t deviceType, ApolloHandData.FingerName finger, PhalangeType phalange)
        {
            HandRig hand = GetHand(deviceType);

            if (hand == null)
            {
                throw new ArgumentOutOfRangeException("phalange", phalange, null);
            }

            switch (phalange)
            {
                case PhalangeType.Proximal:
                    return hand.GetFingerRig(finger).Proximal;
                case PhalangeType.Intermedial:
                    return hand.GetFingerRig(finger).Intermedial;
                case PhalangeType.Distal:
                    return hand.GetFingerRig(finger).Distal;

                default:
                    throw new ArgumentOutOfRangeException("phalange", phalange, null);
            }
        }

        public Transform GetTransformByBone(device_type_t deviceType, HumanBodyBones boneID)
        {
            Animator anim = GetAnimator(deviceType);
            return anim?.GetBoneTransform(boneID);
        }

        private HandRig GetHand(device_type_t deviceType)
        {
            if (CheckIfGloveLeft(deviceType, out bool isLeft) == ReturnValue.Success)
            {
                return isLeft ? LeftHand : RightHand;
            }

            return null;
        }

        protected virtual void OnValidate()
        {
            List<Transform> transforms = new List<Transform>();
            transforms.AddRange(LeftHand.Fingers.SelectMany(f => f.Transforms));
            transforms.AddRange(RightHand.Fingers.SelectMany(f => f.Transforms));

            var hashset = new HashSet<Transform>();
            foreach (var t in transforms)
            {
                if (!hashset.Add(t))
                {
                    Debug.LogError("ManusRigger: Transform " + t.name + " is assigned more than once!");
                }
            }
        }

        protected virtual void Reset()
        {
            LeftHand = new HandRig();
            RightHand = new HandRig();

            string hand = "hand";
            string left = "_l";
            string right = "_r";
            string prox = "_01";
            string inter = "_02";
            string dist = "_03";

            LeftHand.WristTransform = transform.FindDeepChild(hand + left);
            RightHand.WristTransform = transform.FindDeepChild(hand + right);

            for (int i = 0; i < 5; i++)
            {
                string finger = "";
                switch (i)
                {
                    case 0:
                        finger = "thumb";
                        break;
                    case 1:
                        finger = "index";
                        break;
                    case 2:
                        finger = "middle";
                        break;
                    case 3:
                        finger = "ring";
                        break;
                    case 4:
                        finger = "pinky";
                        break;
                }

                LeftHand.GetFingerRig((ApolloHandData.FingerName)i).Proximal = transform.FindDeepChild(finger + prox + left);
                LeftHand.GetFingerRig((ApolloHandData.FingerName)i).Intermedial = transform.FindDeepChild(finger + inter + left);
                LeftHand.GetFingerRig((ApolloHandData.FingerName)i).Distal = transform.FindDeepChild(finger + dist + left);

                RightHand.GetFingerRig((ApolloHandData.FingerName)i).Proximal = transform.FindDeepChild(finger + prox + right);
                RightHand.GetFingerRig((ApolloHandData.FingerName)i).Intermedial = transform.FindDeepChild(finger + inter + right);
                RightHand.GetFingerRig((ApolloHandData.FingerName)i).Distal = transform.FindDeepChild(finger + dist + right);
            }
        }

        /// <summary>
        /// Check if the given device type is the left glove or not. Checks for invalid device type values.
        /// </summary>
        /// <param name="deviceType">The device type that should be checked.</param>
        /// <param name="isLeft">If this device is the left glove.</param>
        /// <returns>Success if the function successfully checked the device type, and Failure if the device type had an invalid value.</returns>
        protected virtual ReturnValue CheckIfGloveLeft(device_type_t deviceType, out bool isLeft)
        {
            isLeft = true;

            switch (deviceType)
            {
                case device_type_t.GLOVE_LEFT:
                    isLeft = true;

                    return ReturnValue.Success;

                case device_type_t.GLOVE_RIGHT:
                    isLeft = false;

                    return ReturnValue.Success;

                default:
                    Debug.Log("Attempted to check if a device type was GLOVE_LEFT, but it was set to an unrecognised value. Its value was " + deviceType + ".");

                    return ReturnValue.Failure;
            }
        }
    }
}
