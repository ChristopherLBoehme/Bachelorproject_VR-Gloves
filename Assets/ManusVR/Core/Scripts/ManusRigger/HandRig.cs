using ManusVR.Core.Hands;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace ManusVR.Core.ManusRigger
{
    [Serializable]
    public class HandRig
    {
        public Transform WristTransform;

        public List<FingerRig> Fingers => new List<FingerRig>() { Thumb, Index, Middle, Ring, Pinky };

        public FingerRig Thumb = new FingerRig(), Index = new FingerRig(), Middle = new FingerRig(), Ring = new FingerRig(), Pinky = new FingerRig();

        public HandRig()
        {

        }

        public FingerRig GetFingerRig(ApolloHandData.FingerName finger)
        {
            switch (finger)
            {
                case ApolloHandData.FingerName.Thumb:
                    return Thumb;
                case ApolloHandData.FingerName.Index:
                    return Index;
                case ApolloHandData.FingerName.Middle:
                    return Middle;
                case ApolloHandData.FingerName.Ring:
                    return Ring;
                case ApolloHandData.FingerName.Pinky:
                    return Pinky;
                default:
                    throw new ArgumentOutOfRangeException("finger", finger, null);
            }
        }
    }

    [Serializable]
    public class FingerRig
    {
        public List<Transform> Transforms => new List<Transform>() { Proximal, Intermedial, Distal };

        public Transform Proximal, Intermedial, Distal;
    }
}
