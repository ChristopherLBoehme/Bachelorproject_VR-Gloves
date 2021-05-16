// Copyright (c) 2018 ManusVR

using ManusVR.Core.Apollo;
using UnityEngine;

namespace ManusVR.Core.Hands
{
    public class Finger : MonoBehaviour
    {
        public ApolloHandData.FingerName Index { get; set; }
        public device_type_t DeviceType { get; set; }
        public GameObject[] PhalangesGameObjects = new GameObject[4];
        public Hand Hand { get; set; }

        /// <summary>
        /// Rotate the phalange on the given position
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="targetRotation"></param>
        public virtual void RotatePhalange(int pos, Quaternion targetRotation)
        {
            if (Index == ApolloHandData.FingerName.Thumb && pos == 1)
            {
                return;
            }

            PhalangesGameObjects[pos].transform.localRotation = targetRotation;
        }
    }
}
