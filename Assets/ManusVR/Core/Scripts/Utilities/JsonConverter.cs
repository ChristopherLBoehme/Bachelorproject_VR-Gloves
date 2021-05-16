// Copyright (c) 2018 ManusVR

using ManusVR.Core.Hands;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ManusVR.Core.Utilities
{
    public static class JsonConverter
    {
        public static Vector3 FromJsonToVector3(JsonVector3 jsonVector3)
        {
            return new Vector3(jsonVector3.x, jsonVector3.y, jsonVector3.z);
        }

        public static JsonVector3 FromVector3ToJson(Vector3 vector3)
        {
            return new JsonVector3(vector3.x, vector3.y, vector3.z);
        }

        public static Quaternion FromJsonToQuaternion(JsonQuaternion jsonQuaternion)
        {
            return new Quaternion(jsonQuaternion.x, jsonQuaternion.y, jsonQuaternion.z, jsonQuaternion.w);
        }

        public static JsonQuaternion FromQuaternionToJson(Quaternion quaternion)
        {
            return new JsonQuaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }

        public static List<Vector3> FromJsonVector3sList(List<JsonVector3> list)
        {
            return list.Select(FromJsonToVector3).ToList();
        }

        public static List<Quaternion> FromJsonQuaternionsList(List<JsonQuaternion> list)
        {
            return list.Select(FromJsonToQuaternion).ToList();
        }
    }

    [Serializable]
    public class ManusRecording
    {
        public int VersionNumber;
        public int PlayerNumber = 0;
        public bool AreArmsSwitched = false;
        public List<float> HandYawOffsets = new List<float>();
        public List<RecordingDataPacket> Data = new List<RecordingDataPacket>();
    }

    [Serializable]
    public struct RecordingDataPacket
    {
        public int FrameIndex;
        public RecordedTrackingDataNode TrackingData;
        public RecordedHandDataPacket HandDataPacket;
    }

    [Serializable]
    public struct RecordedHandDataPacket
    {
        public int FrameIndex;
        public ApolloHandData LeftHand, RightHand;
    }

    [Serializable]
    public struct RecordedTrackingDataNode
    {
        public JsonVector3 LeftTrackerPosition, RightTrackerPosition, HMDPosition;
        public JsonQuaternion LeftTrackerRotation, RightTrackerRotation, HMDRotation;
    }

    [Serializable]
    public class JsonTransforms
    {
        public List<JsonVector3> Positions = new List<JsonVector3>();
        public List<JsonQuaternion> Rotations = new List<JsonQuaternion>();
    }

    [Serializable]
    public class JsonVector3
    {
        public float x;
        public float y;
        public float z;
        public JsonVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(JsonVector3 jsonVector3)
        {
            return new Vector3(jsonVector3.x, jsonVector3.y, jsonVector3.z);
        }

        public static implicit operator JsonVector3(Vector3 vector3)
        {
            return new JsonVector3(vector3.x, vector3.y, vector3.z);
        }
    }
    [Serializable]
    public class JsonQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public JsonQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator Quaternion(JsonQuaternion jsonQuaternion)
        {
            try
            {
                return new Quaternion(jsonQuaternion.x, jsonQuaternion.y, jsonQuaternion.z, jsonQuaternion.w);
            }
            catch
            {
                return Quaternion.identity;
            }

        }

        public static implicit operator JsonQuaternion(Quaternion quaternion)
        {
            return new JsonQuaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }
    }
}