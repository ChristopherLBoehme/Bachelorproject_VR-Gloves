// Copyright (c) 2018 ManusVR

using ManusVR.Core.Apollo;
using ManusVR.Core.ManusRigger;
using ManusVR.Core.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ManusVR.Core.Hands
{
    public enum ReturnValue
    {
        Success,
        Failure
    }

    public class Hand : MonoBehaviour
    {
        public bool UseHandTracker
        {
            get => useHandTracker;
            set
            {
                useHandTracker = value;
                Log("Handtracker " + (value ? "enabled" : "disabled"));
            }
        }

        protected bool useHandTracker;

        [Header("Custom Settings")]
        [SerializeField]
        private HandSettings handSettings;
        public float HandYawOffset
        {
            get => handSettings != null ? handSettings.handYawOffset : 0.0f;
            set
            {
                if (handSettings != null)
                {
                    handSettings.handYawOffset = value;
                }
            }
        }

        public Vector3 preRotWrist = new Vector3();
        public Vector3 postRotWrist = new Vector3();
        public Vector3 preRotThumb = new Vector3();

        [SerializeField]
        private KeyCode rotateHandLeft = KeyCode.None;
        [SerializeField]
        private KeyCode rotateHandRight = KeyCode.None;

        public Transform[][] FingerTransforms { get; private set; } = null;

        public device_type_t DeviceType = device_type_t.GLOVE_RIGHT;

        public Dictionary<ApolloHandData.FingerName, Finger> FingerControllers { get; } = new Dictionary<ApolloHandData.FingerName, Finger>();

        public Rigidbody HandRigidbody => Wrist.Rigidbody;

        public Transform WristTransform
        {
            get
            {
                if (_wristTransform == null)
                {
                    FindWrist(transform.root.gameObject.GetComponentInChildren<ManusRigger.ManusRigger>());
                }

                return _wristTransform;
            }
        }

        private Transform _wristTransform = null;

        public Wrist Wrist { get; private set; }

        public Transform Thumb
        {
            get
            {
                if (FingerTransforms == null)
                {
                    FindFingers(transform.root.gameObject.GetComponentInChildren<ManusRigger.ManusRigger>());
                }

                return FingerTransforms[0][1];
            }
        }

        private bool debugHand = false;

        protected virtual void Awake()
        {
            Application.runInBackground = true;
        }

        protected virtual void Start()
        {
            ReturnValue checkIfLeftResult = CheckIfLeftHand(out var isLeft);

            if (checkIfLeftResult == ReturnValue.Success && isLeft)
            {
                preRotWrist = new Vector3(157, 0f, 0.0f);
            }
            else
            {
                preRotWrist = new Vector3(-23f, 0f, 0.0f);
            }

            ManusRigger.ManusRigger rigger = transform.root.gameObject.GetComponentInChildren<ManusRigger.ManusRigger>();
            FindWrist(rigger);
            FindFingers(rigger);
            Wrist = AddWristComponent();

            foreach (ApolloHandData.FingerName finger in Enum.GetValues(typeof(ApolloHandData.FingerName)))
            {
                FingerControllers.Add(finger, CreateFinger(finger));
            }

            if (checkIfLeftResult != ReturnValue.Success) return;

            rotateHandLeft = isLeft ? KeyCode.S : KeyCode.Q;
            rotateHandRight = isLeft ? KeyCode.A : KeyCode.W;

            if (handSettings == null)
            {
                handSettings = isLeft ? Resources.Load<HandSettings>("HandSettingsLeft") : Resources.Load<HandSettings>("HandSettingsRight");

                if (handSettings == null)
                {
                    Debug.LogError("Failed to load the default hand settings file for the " + (isLeft ? "left" : "right") + " hand, and no custom file was set.");
                }
            }
        }

        protected virtual Wrist AddWristComponent()
        {
            if (_wristTransform == null)
            {
                Debug.LogError("Attempted to add a wrist component to the hand with laterality " + DeviceType + ", but the wrist transform for this hand is missing.");

                return null;
            }

            Wrist newWrist = _wristTransform.gameObject.AddComponent<Wrist>();

            newWrist.DeviceType = DeviceType;
            newWrist.Hand = this;

            return newWrist;
        }

        protected virtual void FindWrist(ManusRigger.ManusRigger rigger)
        {
            // Get the cached transform from the manus rigger.
            if (rigger)
            {
                _wristTransform = rigger.GetWristTransform(DeviceType);
            }

            // Get the transform manually if the rigger wasn't assigned or the WristTransform was not found.
            if (_wristTransform || CheckIfLeftHand(out bool isLeft) != ReturnValue.Success) return;

            Log("Looking for the wrist transform (starting at " + transform.name + ").");

            string wristName = isLeft ? "hand_l" : "hand_r";

            _wristTransform = transform.name == wristName ? transform : transform.FindDeepChild(wristName);

            if (_wristTransform == null)
            {
                Debug.LogError("Attempted to add a wrist controller for the hand with laterality " + DeviceType + ", but a wrist transform with the name " + wristName + " could not be found.");
            }
            else
            {
                Log("Found the wrist transform.");
            }
        }

        protected virtual void FindFingers(ManusRigger.ManusRigger rigger)
        {
            string[] fingers =
            {
                "thumb_0",
                "index_0",
                "middle_0",
                "ring_0",
                "pinky_0"
            };

            // Associate the game transforms with the skeletal model.
            FingerTransforms = new Transform[5][];
            for (var i = 0; i < 5; i++)
            {
                FingerTransforms[i] = new Transform[5];
                for (var j = 1; j < 4; j++)
                {
                    //Get the cached transform from the manus rigger.
                    if (rigger)
                    {
                        FingerTransforms[i][j] = rigger.GetFingerTransform(DeviceType, (ApolloHandData.FingerName)i, (PhalangeType)j);
                    }

                    //Manually find the transform if rigger is null or the transform is still not assigned/
                    if (!FingerTransforms[i][j])
                    {
                        var postfix = DeviceType == device_type_t.GLOVE_LEFT ? "_l" : "_r";
                        var finger = fingers[i] + j + postfix;
                        FingerTransforms[i][j] = transform.FindDeepChild(finger);
                    }
                }
            }
        }

        /// <summary>
        /// Animate this hand using the given hand data.
        /// </summary>
        /// <param name="handData">The hand data that should be used to animate the hand.</param>
        public virtual void AnimateHand(ApolloHandData handData)
        {
            if (!useHandTracker)
            {
                UpdateHandYawOffset();
                RotateWrist(handData.processedWristImu);
            }

            const int thumb = (int)ApolloHandData.FingerName.Thumb;
            const int CMC = (int)ApolloHandData.JointNameThumb.CMC;
            RotateThumb(handData.fingers[thumb].joints[CMC]);

            RotateFingers(handData);
        }

        protected virtual void UpdateHandYawOffset()
        {
            const float speed = 30;

            if (Input.GetKey(rotateHandLeft) && handSettings != null)
            {
                handSettings.handYawOffset -= Time.deltaTime * speed;
            }

            if (Input.GetKey(rotateHandRight) && handSettings != null)
            {
                handSettings.handYawOffset += Time.deltaTime * speed;
            }
        }

        protected virtual void RotateWrist(Quaternion processedWristImu)
        {
            if (Wrist == null) return;

            Quaternion yawOffset = Quaternion.Euler(0, handSettings != null ? handSettings.handYawOffset : 0.0f, 0);
            Quaternion preRotation = Quaternion.Euler(preRotWrist) * yawOffset;
            Quaternion postRotation = Quaternion.Euler(postRotWrist);

            Wrist.RotateWrist(preRotation * processedWristImu * postRotation);
        }

        protected virtual void RotateThumb(Quaternion thumbCmcRotation)
        {
            // Note that this has become a local rotation with Apollo.
            Thumb.localRotation = Quaternion.Euler(preRotThumb) * thumbCmcRotation;
        }

        protected virtual void RotateFingers(ApolloHandData handData)
        {
            for (int fingerNum = 0; fingerNum < FingerControllers.Count; fingerNum++) // Go over all fingerData, including the thumb.
            {
                for (int jointNum = 1; jointNum <= 3; jointNum++) // CMC to IP for the thumb, MCP to DIP for the other fingerData.
                {
                    ApolloHandData.FingerName convertedFingerNum = (ApolloHandData.FingerName)fingerNum;

                    FingerControllers[convertedFingerNum].RotatePhalange(jointNum, handData.fingers[fingerNum].joints[jointNum]);
                }
            }
        }

        protected virtual Finger CreateFinger(ApolloHandData.FingerName fingerName)
        {
            Finger newFinger = FingerTransforms[(int)fingerName][1].gameObject.AddComponent<Finger>();

            newFinger.Index = fingerName;
            newFinger.DeviceType = DeviceType;
            newFinger.PhalangesGameObjects[1] = FingerTransforms[(int)fingerName][1].gameObject;
            newFinger.PhalangesGameObjects[2] = FingerTransforms[(int)fingerName][2].gameObject;
            newFinger.PhalangesGameObjects[3] = FingerTransforms[(int)fingerName][3].gameObject;
            newFinger.Hand = this;

            return newFinger;
        }

        /// <summary>
        /// Check if this hand is the left hand or not. Checks for invalid device type values.
        /// </summary>
        /// <param name="isLeft">If this device is the left hand.</param>
        /// <returns>Success if the function successfully checked the device type of this hand, and Failure if the device type had an invalid value.</returns>
        public ReturnValue CheckIfLeftHand(out bool isLeft) // MANUS_TODO: keep this in a central place as a static?
        {
            isLeft = true;

            switch (DeviceType)
            {
                case device_type_t.GLOVE_LEFT:
                    isLeft = true;

                    return ReturnValue.Success;

                case device_type_t.GLOVE_RIGHT:
                    isLeft = false;

                    return ReturnValue.Success;

                default:
                    Debug.Log("Attempted to check if a hand had the GLOVE_LEFT device type, but the hand's DeviceType is set to an unrecognised value. Its value was " + DeviceType + ".");

                    return ReturnValue.Failure;
            }
        }

        private void Log(string message)
        {
            if (debugHand)
            {
                Debug.Log(message);
            }
        }
    }
}
