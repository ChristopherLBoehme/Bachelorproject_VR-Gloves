using ManusVR.Core.Apollo;
using ManusVR.Core.Utilities;
using System;
using UnityEngine;

namespace ManusVR.Core.Hands
{
    /// <summary>
    /// A class that contains all the available data for a single hand.
    /// </summary>
    [Serializable]
    public class ApolloHandData
    {
        /// <summary>
        /// A number that identifies a data source in Apollo.
        /// This can be a glove, or the output of a filter chain.
        /// </summary>
        public UInt64 endpointID = 0;

        /// <summary>
        /// A number that identifies a Manus VR glove.
        /// </summary>
        public UInt64 deviceID = 0;

        /// <summary>
        /// The orientation of the wrist IMU after applying filters.
        /// This is usually the value that should be used when applying the wrist IMU value to something.
        /// </summary>
        public Quaternion processedWristImu = new Quaternion();

        /// <summary>
        /// The orientation of the wrist IMU without any filters applied to it.
        /// This value should usually not be needed.
        /// </summary>
        public Quaternion rawWristImu = new Quaternion();
        /// <summary>
        /// The orientation of the thumb IMU without any filters applied to it.
        /// The filtered thumb IMU value is automatically applied to the joint it applies to in the finger data.
        /// This raw value should usually not be needed.
        /// </summary>
        public Quaternion rawThumbImu = new Quaternion();

        /// <summary>
        /// The data of all the fingers in the hand, including the thumb.
        /// Use the FingerName enum when accessing this array.
        /// </summary>
        public Finger[] fingers = new Finger[numFingers];

        /// <summary>
        /// The number of fingers that this class has data for.
        /// A constant value that can be used when iterating over the fingers.
        /// </summary>
        public const int numFingers = 5;
        /// <summary>
        /// The number of joints each finger has.
        /// A constant value that can be used when iterating over joints of a finger.
        /// </summary>
        public const int numJointsPerFinger = 5;
        /// <summary>
        /// The number of flex sensor values each finger has.
        /// A constant value that can be used when iterating over the flex sensor values.
        /// </summary>
        public const int numFlexSensorSegmentsPerFinger = 2;

        /// <summary>
        /// The names of the fingers this class has data for.
        /// This can be used when accessing the finger array.
        /// </summary>
        public enum FingerName
        {
            Thumb = 0,
            Index = 1,
            Middle = 2,
            Ring = 3,
            Pinky = 4
        }

        /// <summary>
        /// The names of the joints of the thumb that this class has data for.
        /// This can be used when accessing the joint data array for the thumb.
        /// Note that this enum is only for the thumb, and not for the other fingers.
        /// </summary>
        public enum JointNameThumb
        {
            Carpals = 0, // The base of the carpals.
            CMC = 1,     // Carpal metacarpal joint, the joint inside the hand. The thumb IMU influences this joint.
            MCP = 2,     // Metacarpal proximal phalange joint, the joint at the base of the thumb.
            IP = 3,      // Inter phalangeal joint, the joint (more or less) in the middle of the thumb.
            Tip = 4      // The tip of the finger.
        }

        /// <summary>
        /// The names of the joints of all the fingers that this class has data for, except for the thumb.
        /// This can be used when accessing the joint data array for a finger.
        /// Note that this enum is not for the thumb.
        /// </summary>
        public enum JointNameNonThumb
        {
            Carpals = 0, // The base of the carpals.
            MCP = 1,     // Metacarpal proximal phalange joint, the joint at the base of the finger.
            PIP = 2,     // Proximal inter phalangeal joint, the joint closest to the palm after the MCP.
            DIP = 3,     // Distal inter phalangeal joint, the joint farthest from the palm.
            Tip = 4      // The tip of the finger.
        }

        /// <summary>
        /// The names of the flex sensor segments that this class has data for.
        /// This can be used when accessing the raw flex sensor values for a finger.
        /// </summary>
        public enum FlexSensorSegment
        {
            Proximal, // The segment closest to the palm of the hand.
            Medial    // The segment further away from the palm of the hand.
        }

        /// <summary>
        /// A struct that stores the data for a single finger.
        /// </summary>
        [Serializable]
        public struct Finger
        {
            /// <summary>
            /// The rotation of each of the joints in the finger.
            /// Use the JointName enum values when accessing this array.
            /// </summary>
            public JsonQuaternion[] joints;

            /// <summary>
            /// The raw values, between 0 (open) and 1 (closed), of the flex sensors in the gloves.
            /// Use the FlexSensorSegment enum values when accessing this array.
            /// </summary>
            public double[] flexSensorRaw;
        }

        /// <summary>
        /// Constructor. Makes sure hand data is always usable, even if no glove data was ever stored in it.
        /// </summary>
        public ApolloHandData()
        {
            for (int fingerNum = 0; fingerNum < fingers.Length; fingerNum++)
            {
                fingers[fingerNum].joints = new JsonQuaternion[numJointsPerFinger];
                fingers[fingerNum].flexSensorRaw = new double[numFlexSensorSegmentsPerFinger];

                for (int segmentNum = 0; segmentNum < fingers[fingerNum].flexSensorRaw.Length; segmentNum++)
                {
                    fingers[fingerNum].flexSensorRaw[segmentNum] = 0.0;
                }
            }
        }

        /// <summary>
        /// Check if this struct contains data from a glove.
        /// Note that hand data with only joint data or only raw data in it is considered valid.
        /// </summary>
        /// <returns>True if the struct contains valid data, and false if it doesn't.</returns>
        public bool IsValid()
        {
            return endpointID != 0
                && deviceID != 0;
        }

        /// <summary>
        /// Convert this struct to text.
        /// </summary>
        /// <returns>This struct in text.</returns>
        public override string ToString()
        {
            string ret = "";

            ret += "endpointID: " + endpointID;
            ret += " deviceID: " + deviceID;
            ret += " wrist (raw/processed): " + rawWristImu + " " + processedWristImu;
            ret += " thumb: " + rawThumbImu;

            ret += " finger joints: ";
            foreach (Finger finger in fingers)
            {
                foreach (Quaternion joint in finger.joints)
                {
                    ret += joint.eulerAngles + " / ";
                }
            }

            ret += " flex sensor: ";
            foreach (Finger finger in fingers)
            {
                foreach (double flexVal in finger.flexSensorRaw)
                {
                    ret += flexVal + " / ";
                }
            }

            return ret;
        }

        /// <summary>
        /// Copy the given joint data into this ApolloHandData.
        /// Data present in hand data but not in the joint data (e.g. raw flex sensor values) will not be altered.
        /// </summary>
        /// <param name="input">The joint data to copy.</param>
        public void SetJointData(ApolloJointData input)
        {
            endpointID = input.endpointID;
            deviceID = input.deviceID;

            processedWristImu = input.wrist;

            for (int fingerNum = 0; fingerNum < fingers.Length; fingerNum++)
            {
                for (int jointNum = 0; jointNum < fingers[fingerNum].joints.Length; jointNum++)
                {
                    fingers[fingerNum].joints[jointNum] = (Quaternion)input.fingers[fingerNum].joints[jointNum].rotation;
                }
            }
        }

        /// <summary>
        /// Copy the given raw data into this ApolloHandData.
        /// Data present in hand data but not in the raw data (e.g. processed IMU data) will not be altered.
        /// </summary>
        /// <param name="input">The joint data to copy.</param>
        public void SetRawData(ApolloRawData input)
        {
            endpointID = input.endpointID;
            deviceID = input.deviceID;

            rawWristImu = input.imus[0];
            rawThumbImu = input.imus[1];

            for (int fingerNum = 0; fingerNum < fingers.Length; fingerNum++)
            {
                for (int segmentNum = 0; segmentNum < fingers[fingerNum].flexSensorRaw.Length; segmentNum++)
                {
                    fingers[fingerNum].flexSensorRaw[segmentNum] = input.flex((fingerNum * ApolloHandData.numFlexSensorSegmentsPerFinger) + segmentNum);
                }
            }
        }
    } // ApolloHandData
}


