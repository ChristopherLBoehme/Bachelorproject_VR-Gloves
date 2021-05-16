// Copyright (c) 2018 ManusVR

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ManusVR.Core.Apollo
{
    #region DATA_STRUCTS

    [StructLayout(LayoutKind.Sequential)]
    public struct ApolloSuccessPayload
    {
        public IntPtr pointer;

        public string text => Marshal.PtrToStringAuto(pointer);

        public UInt64 uint64 => (UInt64)Marshal.ReadInt64(pointer);
    }

    // char* payload;
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct ApolloPacketBinary
    {
        // Char*
        public IntPtr payload;
        public UInt32 bytes;

        public byte[] array
        {
            get
            {
                byte[] managedArray = new byte[bytes];
                Marshal.Copy(payload, managedArray, 0, (int)bytes);
                return managedArray;
            }
        }

        public ApolloPacketBinary(byte[] data)
        {
            // allocate and copy the data
            payload = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, payload, data.Length);
            bytes = (UInt32)data.Length;
        }
    }

    // char* payload;
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct ApolloGestureArray
    {
        // Char*
        public IntPtr gestures;
        public byte numGestures;

        public ApolloGestureArray(apollo_gesture_t[] gestureArray)
        {
            // allocate and copy the data
            gestures = Marshal.AllocHGlobal(gestureArray.Length * sizeof(apollo_gesture_t));

            int[] newArray = Array.ConvertAll(gestureArray, item => (int)item);

            Marshal.Copy(newArray, 0, gestures, gestureArray.Length);
            numGestures = (byte)gestureArray.Length;
        }
    }

    /// <summary>
    /// Pins the managed array so it can be used unmanaged
    /// DO NOT DIRECTLY MARSHAL THIS STRUCT 
    /// (only request the pointer and size)
    /// </summary>
    /// <typeparam name="type">Type of the array</typeparam>
    public struct unmanagedArray<type>
    {
        private IntPtr _pointer;
        private UInt32 _size;
        private GCHandle _handle;

        public IntPtr pointer => _pointer;
        public UInt32 size => _size;

        public unmanagedArray(type[] data)
        {
            // pin the mannaged data to a specific address in memory
            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            // retreive the pointer
            _pointer = _handle.AddrOfPinnedObject();
            // set the size of the data
            _size = (UInt32)data.Length;
        }

        public void free()
        {
            if (_handle.IsAllocated)
                _handle.Free();
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct U64Array
    {
        // const uint64_t* const entries;
        private IntPtr _pointer;
        private UInt32 _size;

        public IntPtr pointer => _pointer;
        public UInt32 size => _size;

        public UInt64[] array
        {
            get
            {
                // if the list is a nullptr, return empty list
                if (_pointer == IntPtr.Zero)
                    return new UInt64[0];

                // first copy to int64 array because Copy doesn't understand unsigned
                Int64[] Int64Array = new Int64[_size];
                Marshal.Copy(_pointer, Int64Array, 0, (int)_size);
                // after copying, cast to Uint64 array
                UInt64[] Uint64Array = Array.ConvertAll(Int64Array, item => (UInt64)item);

                return Uint64Array;
            }
        }
        // marshal an array with data.Length elements
        public U64Array(UInt64[] data)
        {
            // first convert to Int64 array because Copy doesn't understand unsigned
            Int64[] Int64Array = Array.ConvertAll(data, item => (Int64)item);
            _pointer = Marshal.AllocHGlobal(data.Length * sizeof(UInt64));
            Marshal.Copy(Int64Array, 0, _pointer, data.Length);
            _size = (UInt32)data.Length;
        }
        // create an array with 1 element
        public U64Array(UInt64 element)
        {
            // only copy 1 entry, and use all lengths as 1
            _pointer = Marshal.AllocHGlobal(sizeof(UInt64));
            Marshal.Copy(new Int64[] { (Int64)element }, 0, _pointer, 1);
            _size = 1;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ApolloFilterChainInfo
    {
        public byte numFilters;         /// number of filters in this chain
            // apollo_filter_t[]
        public IntPtr filters;   /// filter types as present in the chain, with index [0] the filter that directly follows the sources
        public byte numSources;         /// number of inputs to this filter chain
            //  UInt64[]
        public IntPtr sources;          /// source endpoint IDs of inputs to this filter chain

        // TODO: add proper extraction of filters and sources
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ApolloSourceInfo
    {
        public UInt64 endpoint;                  /// this source's endpoint ID
        public apollo_source_t sourceType;         /// type of source
        public ApolloFilterChainInfo filterInfo;   /// filter chain information, only valid if sourceType == SOURCE_FILTERED
        public UInt64 deviceID;
        public apollo_laterality_t side;
    };

    [StructLayout(LayoutKind.Sequential), Serializable]
    public class ApolloRawData
    {
        public UInt64 endpointID;
        public UInt64 deviceID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public ApolloQuat[] imus;
        // Double[10] has some kind of weird padding at the front/back(?) of each(?) double
        // So instead we copy the data into 80 (10*sizeOf(double)) bytes and convert
        // them to double in a seperate function
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] flexRaw;

        public ApolloRawData()
        {
            imus = new ApolloQuat[2];
            flexRaw = new byte[80];
        }

        // Check if this struct is initialised
        public bool IsValid => imus != null && flexRaw != null;

        public double flex(int finger)
        {
            if (flexRaw == null || flexRaw.Length == 0)
            {
                return 0.0;
            }

            double flexValue = -1.0D;

            if (finger > 9 || finger < 0)
            {
                Debug.LogError($"Finger should be between 0 and 9, got: {finger}");
            }
            else
            {
                flexValue = BitConverter.ToDouble(flexRaw, finger * sizeof(double));

                // verify the conversion
                if (flexValue < 0.0D || flexValue > 1.0D)
                {
                    Debug.LogWarning($"flex sensor [{finger}] value {flexValue} is not between 0 and 1");
                }
            }

            return flexValue;
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    public class ApolloJointData
    {
        public UInt64 endpointID;
        public UInt64 deviceID;
        public ApolloQuat wrist;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public ApolloFinger[] fingers;

        public ApolloJointData()
        {
            fingers = new ApolloFinger[5];
            for (int i = 0; i < 5; i++)
            {
                fingers[i].joints = new pose_t[5];
            }
        }

        // Check if this struct is initialised
        public bool IsValid => fingers != null;

        public override string ToString()
        {
            string ret = "";
            ret += "id: " + endpointID;
            ret += " wrist: " + wrist.ToString();

            ret += " finger: ";
            foreach (var finger in fingers)
            {
                ret += finger.ToString() + " / ";
            }

            return ret;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    public struct ApolloDeviceInfo
    {
        public UInt64 deviceID;              /// this device's hardware identifier
        public UInt64 pairedDongleID;        /// hardware identifier of the dongle that this device is paired to
        public apollo_laterality_t hand;       /// info on whether this device is a left or a right glove
        public apollo_dev_t devType;           /// the device's fabrication type
        public byte batteryPercent;         /// battery charge in percent
        public UInt16 signalAttenuationDb;   /// signal attenuation in dB
    };

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct ApolloFinger
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public pose_t[] joints;

        public override string ToString()
        {
            string ret = "";
            foreach (var joint in joints)
            {
                ret += joint.ToString() + " - ";
            }
            return ret;
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    /*! Pose structure representing an orientation and position. */
    public struct pose_t
    {
        //public vector_t translation;
        public ApolloQuat rotation;

        public static explicit operator pose_t(UnityEngine.Transform b)
        {
            pose_t a = new pose_t { rotation = b.rotation };
            return a;
        }

        public override string ToString()
        {
            return rotation.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct ApolloQuat
    {
        public float w, x, y, z;

        public static implicit operator UnityEngine.Quaternion(ApolloQuat b)
        {
            return new UnityEngine.Quaternion(b.x, b.y, b.z, b.w);
        }

        public static implicit operator ApolloQuat(UnityEngine.Quaternion b)
        {
            ApolloQuat a = new ApolloQuat { x = b.x, y = b.y, z = b.z, w = b.w };
            return a;
        }

        public override string ToString()
        {
            return $"w: {w} x: {x} y: {y} z: {z} ";
        }
    }

    public enum manus_ret_t
    {
        MANUS_SUCCESS = 0,
        MANUS_ERROR = 1,
        MANUS_INVALID_ARGUMENT = 2,
        MANUS_DISCONNECTED = 3,
        MANUS_FILESYSTEM_ERROR = 4,
        MANUS_INVALID_SESSION = 5,
        MANUS_NOT_IMPLEMENTED = 100
    }

    public enum device_type_t
    {
        GLOVE_LEFT = 0,
        GLOVE_RIGHT
    }

    public enum apollo_dev_t
    {
        DEVICE_INVALID = 0,
        DEVICE_DEBUG,
        DEVICE_REVD
    };

    public enum apollo_source_t
    {
        SOURCE_INVALID = 0,
        SOURCE_DEVICEDATA,
        SOURCE_FILTERED,
        SOURCE_FILTERED_DEFAULT
    };

    public enum apollo_filter_t
    {
        FILTER_NONE = 0,
        FILTER_COORDINATESYSTEMCONVERSION,
        FILTER_MESHMAPPING,
        FILTER_GESTURE
    };

    public enum apollo_gesture_t
    {
        GESTURE_NONE = 0,
        GESTURE_OPEN_HAND,
        GESTURE_FIST,
        GESTURE_INDEX_PINCH
    };

    public enum apollo_laterality_t
    {
        SIDE_LEFT = -1,
        SIDE_RIGHT = 1
    };

    public enum coor_axis_t
    {
        COOR_AXIS_Z_NEG = -3,
        COOR_AXIS_Y_NEG,
        COOR_AXIS_X_NEG,
        COOR_AXIS_X_POS = 1,
        COOR_AXIS_Y_POS,
        COOR_AXIS_Z_POS
    };

    public enum coor_sys_preset_t
    {
        COSYS_GLM = 0,
        COSYS_UNITY,
        COSYS_UNREAL
    };


    /*!
    *   \brief ApolloMeshNodeConfig defines the coordinate frame in a node of the mesh 
    */
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct ApolloMeshNodeConfig
    {
        public coor_axis_t upDirection;
        public coor_axis_t forwardDirection;
        public coor_axis_t rightDirection;
    };

    /*!
    *   \brief ApolloMeshConfig holds the mesh configuration for a left and right hand
    */
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct ApolloMeshConfig
    {
        public ApolloMeshNodeConfig leftWrist;
        public ApolloMeshNodeConfig leftThumb;
        public ApolloMeshNodeConfig leftFinger;

        public ApolloMeshNodeConfig rightWrist;
        public ApolloMeshNodeConfig rightThumb;
        public ApolloMeshNodeConfig rightFinger;

        public ApolloMeshNodeConfig world;
        public bool negateAxisX;
        public bool negateAxisY;
        public bool negateAxisZ;
    };

    #endregion

    /*!
    *   \brief Apollo SDK class
    *
    */
    public class ApolloSDK
    {
        #region HELPER_FUNCTIONS

        /*! Add packet size at the front of the packet
         */
        public static byte[] WrapPacket(ApolloPacketBinary packet)
        {
            byte[] data = packet.array;
            byte[] output = new byte[data.Length + 4];
            data.CopyTo(output, 4);
            BitConverter.GetBytes(data.Length).CopyTo(output, 0);
            return output;
        }

        #endregion
        #region SDK_OPERATION

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void errorHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 errCode, string errMsg);

        //!
        //! \brief register a callback function to handle errors returned by the Apollo SDK
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t errCode, const char * errMsg))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerErrorHandler(IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]errorHandlerDelegate handlerFun);

        //!
        //! \brief start an Apollo SDK session
        //! \param clientID the client ID by which client software identifies to Apollo
        //! \return a handle to an Apollo SDK session, which is used by callback functions to indicate the call's origin session
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 apolloOpenSession(UInt16 clientID);

        //!
        //! \brief close an Apollo SDK session
        //! \param session the Apollo session handle of the sesison to be closed
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void apolloCloseSession(UInt64 session);

        //!
        //! \brief clears the allocated memory of a packet binary
        //! \param donePacket the packet binary to have its payload deallocated
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void apolloDisposePacket(ApolloPacketBinary donePacket);

        #endregion
        #region PACKET_PROCCESING

        //!
        //! \brief process a packet received from an Apollo server application
        //! \param session handle of the session that is supposed to handle the packet
        //! \param packetBinary TCP transaction of Apollo Packets
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void apolloProcessPacket(UInt64 session, ApolloPacketBinary packetBinary);

        #endregion
        #region PACKET_GENERATION

        //!
        //! \brief generate a packet to initiate a handshake with an Apollo server
        //! \param session handle of the session that is supposed to generate the packet
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateHandshake(UInt64 session, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request starting of streams from an Apollo server
        //! \param session handle of the session that is supposed to generate the packet
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateStartStreams(UInt64 session, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request stopping streams from an Apollo server
        //! \param session handle of the session that is supposed to generate the packet
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateStopStreams(UInt64 session, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to add sources to a stream
        //! \param session handle of the session that is supposed to generate the packet
        //! \param sources list of source endpoint IDs to be added to the stream
        //! \param numSources amount of sources being listed in the sources argument
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateAddStreams(UInt64 session, IntPtr sources, UInt32 numSources, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to remove sources from a stream
        //! \param session handle of the session that is supposed to generate the packet
        //! \param sources list of source endpoint IDs to be removed from the stream
        //! \param numSources amount of sources being listed in the sources argument
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateRemoveStreams(UInt64 session, IntPtr sources, UInt32 numSources, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to enable or disable a raw data stream
        //! \param session handle of the session that is supposed to generate the packet
        //! \param source source endpoint ID for which stream status should be updated
        //! \param rawEnabled true if raw stream should be enabled, false if it should be disabled
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateSetStreamRaw(UInt64 session, UInt64 source, bool rawEnabled, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to enable or disable a standard data stream
        //! \param session handle of the session that is supposed to generate the packet
        //! \param source source endpoint ID for which stream status should be updated
        //! \param dataEnabled true if standard data stream should be enabled, false if it should be disabled
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateSetStreamData(UInt64 session, UInt64 source, bool rawEnabled, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to add a filter chain to a source
        //! \param session handle of the session that is supposed to generate the packet
        //! \param sources list of source endpoint IDs to be used as input to the filter chain
        //! \param numSources amount of sources being listed in the sources argument
        //! \param filterHandles list of filter handles identifying the filter to be added after the given sources; to be added in the ordering provided here
        //! \param numFilters number of entries being listed in the filterHandles argument
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateAddFilters(UInt64 session, IntPtr sources, UInt32 numSources, IntPtr filterHandles, UInt32 numFilters, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to remove a filter chain with a given endpoint identifier
        //! \param session handle of the session that is supposed to generate the packet
        //! \param endpoint source identifier for the filter chain to be removed
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateRemoveFilter(UInt64 session, UInt64 endpoint, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to list all dongle IDs connected to it
        //! \param session handle of the session that is supposed to generate the packet
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateListDongleIDs(UInt64 session, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to list all devices connected to a dongle (or to all dongles if 0 is passed as dongleID)
        //! \param session handle of the session that is supposed to generate the packet
        //! \param dongleID identifier of the dongle whose devices should be listed
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateListDeviceIDs(UInt64 session, UInt64 dongleID, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to have a device vibrate
        //! \param session handle of the session that is supposed to generate the packet
        //! \param deviceID identifier of the device to vibrate
        //! \param duration vibration duration in milliseconds
        //! \param power strength of vibration with 0 corresponding to 0% strength and maximum 16-bit integer being 100% srength
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateDeviceVibrate(UInt64 session, UInt64 deviceID, UInt16 duration, UInt16 power, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to provide details over a specified device
        //! \param session handle of the session that is supposed to generate the packet
        //! \param deviceID identifier of the device for which detailed information is requested
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server. Apollo should respond with a Device Info Packet
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateGetDeviceInfo(UInt64 session, UInt64 deviceID, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to list all data sources available on it
        //! \param session handle of the session that is supposed to generate the packet
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateListSources(UInt64 session, UInt16 eventId = 0);

        //!
        //! \brief generate a packet to request an Apollo server to obtain information over a given source
        //! \param session handle of the session that is supposed to generate the packet
        //! \param source source endpoint ID for which information should be received
        //! \param eventId event identifier to be set on the generated packet, in order to be able to identify the according returned packet
        //! \return a byte array containing data to be transmitted over TCP to an Apollo server
        //! \note to avoid memory leaks, call apolloProcessPacket with the received ApolloPacketBinary before leaving scope
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern ApolloPacketBinary generateGetSourceInfo(UInt64 session, UInt64 source, UInt16 eventId = 0);

        #endregion
        #region FILTER_GENERATION

        //!
        //! \brief generate a filter for coordinate system conversion between Apollo and client software based on a client preset
        //! \param preset a coordinate system conversion preset for select client software, e.g. Unreal engine or Unity
        //! \return a handle that can be used in generation of a filter chain in Apollo
        //! \see generateAddFilters
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 generateCoordinateSystemConversionPresetFilter(UInt64 session, coor_sys_preset_t preset);

        //!
        //! \brief generate a filter for coordinate system conversion between Apollo and client software based on specifications of the client coordinate system
        //! \param up axial direction denoting "up" direction in client coordinate system
        //! \param view axial direction denoting the view/"depth" direction in client coordinate system
        //! \param handedness client coordinate system handedness
        //! \return a handle that can be used in generation of a filter chain in Apollo
        //! \see generateAddFilters
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 generateCoordinateSystemConversionFilter(UInt64 session, coor_axis_t up, coor_axis_t view, coor_axis_t right);

        //!
        //! \brief generate a filter to map the mesh in the client software to the handmodel in apollo based on a client preset
        //! \param preset a mesh model preset for select client software, e.g. Unreal engine or Unity
        //! \return a handle that can be used in generation of a filter chain in Apollo
        //! \see generateAddFilters
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 generateMeshMappingFilter(UInt64 session, ApolloMeshConfig meshConfig);

        //!
        //! \brief generate a filter for recognising and blending into defined gesture poses
        //! \param session handle of the session that is supposed to generate the filter
        //! \param numGestures number of entries in the gestures argument
        //! \param gestures list of gestures to be handled by the filter
        //! \param blendFloor gesture probability starting from which the gesture pose begins to get blended in
        //! \param blendCeil gesture probability at which the blend pose is fully blended in
        //! \return a handle that can be used in generation of a filter chain in Apollo
        //! \see generateAddFilters
        //!!!! byte = Uint8_t
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt64 generateGestureFilter(UInt64 session, byte numGestures, IntPtr apollo_gesture_t, double blendFloor = 0, double blendCeil = 1);

        #endregion
        #region CALLBACKS

        //!
        //! \brief register handler function to respond to handshake packets incoming from Apollo
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, const ApolloPacketBinary* const pckToReturn))
        //! \note this function should simply send out the pckToReturn argument to the Apollo server - pckToReturn is still owned by the SDK and must NOT be deleted by the handler
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerHandshakePacketHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]handshakePacketHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to positive return packets incoming from Apollo
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t eventID, const char * successMsg))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerSuccessHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]successHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to failure indicator packets incoming from Apollo
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t eventID, const char * failMsg))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerFailHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]failHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to streamed standard data packets incoming from Apollo
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, const ApolloJointData* const jointData))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerDataStreamHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]dataStreamHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to streamed raw data packets incoming from Apollo
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, const ApolloRawData* const rawData))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerRawStreamHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]rawStreamHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to info packets incoming from Apollo that hold a dongle list
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t eventID, const U64Array* const dongleList))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerDongleIdListHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]dongleIdListHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to info packets incoming from Apollo that hold a device list
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t eventID, const U64Array* const deviceList))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerDeviceIdListHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]deviceIdListHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to info packets incoming from Apollo that hold a source list
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t eventID, const U64Array* const sourceList))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerSourcesListHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]sourcesListHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to device information packets incoming from Apollo
        //! \param session handle of the session that is supposed to generate the packet
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t eventID, const ApolloSourceInfo* const info))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerSourceInfoHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]sourceInfoHandlerDelegate handlerFun);

        //!
        //! \brief register handler function to respond to info packets incoming from Apollo, which hold information about a device
        //! \param session handle of the session with which the handler function is to be registered
        //! \param callbackReturn pointer to be sent as callbackReturn argument to the registered callback function
        //! \param handlerFun function pointer to a void function with the arguments (void* callbackReturn, apollo_handle_t session, uint16_t eventID, const ApolloDeviceInfo* const deviceInfo))
        //!
        [DllImport("ApolloSDK", CallingConvention = CallingConvention.Cdecl)]
        public static extern void registerDeviceInfoHandler(UInt64 session, IntPtr cbr,
            [MarshalAs(UnmanagedType.FunctionPtr)]deviceInfoHandlerDelegate handlerFun);

        #endregion
        #region CALLBACK_DELEGATES
        /*
         * Delegate naming structure:
         * remove `register` from beginning
         * remove capital letter
         * add `Delegate` add end
         * 
         * Example:
         * Function: registerSourceInfoHandler
         * Delegate:         sourceInfoHandlerDelegate
         */

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void handshakePacketHandlerDelegate(IntPtr cbr, UInt64 session, IntPtr pckToReturn);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void successHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 eventID, ApolloSuccessPayload successMsg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void failHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 eventID, string failMsg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dataStreamHandlerDelegate(IntPtr cbr, UInt64 session, ApolloJointData jointData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void rawStreamHandlerDelegate(IntPtr cbr, UInt64 session, ApolloRawData rawData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dongleIdListHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 eventID, U64Array dongleList);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void deviceIdListHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 eventID, U64Array deviceList);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void sourcesListHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 eventID, U64Array sourceList);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void sourceInfoHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 eventID, ApolloSourceInfo info);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void deviceInfoHandlerDelegate(IntPtr cbr, UInt64 session, UInt16 eventID, ApolloDeviceInfo info);

        #endregion

    }
}
