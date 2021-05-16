using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

// State object for receiving data from remote device.  
namespace ManusVR.Core.Apollo
{
    public delegate void newDataEvent(ApolloJointData data, GloveLaterality side);
    public delegate void newRawDataEvent(ApolloRawData data, GloveLaterality side);
    public delegate void newDeviceInfo(ApolloDeviceInfo data);

    public enum GloveLaterality
    {
        UNKNOWN = 0,
        GLOVE_LEFT,
        GLOVE_RIGHT,
    }

    public class Apollo : MonoBehaviour
    {
        // The interval at which the client checks it connection to apollo (in seconds)
        private const float slowConnectionDelay = 0.5f;
        private const float fastConnectionDelay = 0.05f;

        public string _ip = "127.0.0.1";
        public int _port = 49010;
        // keep track of how many connection attempts have been made to Apollo
        private int _connectionAttempts = 0;
        private static int _filterGestureAttempts = 0;

        private static bool _showDebugLog = false;
        private static bool _usePinchFilter = false;

        // Id that Unity communicates to Apollo when handshaking
        private const UInt16 _clientId = 44178;
        // Apollo sessions and TCP class
        private static UInt64 _session;
        private static ApolloTCP _apolloTCP;

        // The types of events that can be send to Apollo
        private enum EventType : UInt16
        {
            SOURCE_LIST = 0,
            DONGLE_LIST,
            HANDSHAKE,
            GLOVE_ADD_STREAM,
            GLOVE_SET_STREAM_DATA,
            GLOVE_SET_STREAM_RAW,
            GLOVE_IDENTIFY,
            SET_STREAM_DATA_RIGHT,
            APOLLO_START_STREAM,
            APOLLO_STOP_STREAM,
            GLOVE_ADD_FILTER_CONVERT,
            GLOVE_ADD_FILTER_GESTURE,
            RUMBLE,
        }

        // The data related to an Event waiting for an reply from Apollo
        struct eventData
        {
            public EventType type;
            public UInt64 endpointID;

            // create an event without any data
            public eventData(EventType type) : this()
            {
                this.type = type;
            }
            // create an event with a glove ID (sourceID)
            public eventData(EventType type, UInt64 endpointID) : this()
            {
                this.type = type;
                this.endpointID = endpointID;
            }
        }

        // The only instance of Apollo that should be active in the scene
        private static Apollo _instance = null;

        // The coroutine checking the connection with Apollo
        private Coroutine _apolloCheck;

        // Easily generate random Event ID
        private static UInt16 randomEventId { get { return (UInt16)UnityEngine.Random.Range(1, 65535); } }
        // Dictionary for keeping track of all events currently waiting for a reply from Apollo
        private static Dictionary<UInt16, eventData> eventList = new Dictionary<ushort, eventData>();
        // Dictionary for all gloves and their state currently know to this session instance
        private static Dictionary<UInt64, GloveInfo> sourceList = new Dictionary<UInt64, GloveInfo>();

        // Keeps track of the current (connection) state with Apollo
        private static ApolloState apolloState = ApolloState.APOLLO_0_CONNECTING;
        private static UInt64 dongleId = 0;

        // Delegate that gets called when new handdata comes in
        private static newDataEvent newDataEvent = null;
        private static newRawDataEvent newRawDataEvent = null;
        private static newDeviceInfo newDeviceInfo = null;

        private bool running = true;

        // State enum for Apollo
        private enum ApolloState
        {
            APOLLO_0_CONNECTING = 0,
            APOLLO_1_HANDSHAKING,
            APOLLO_2_GLOVE_SETUP,
            APOLLO_3_STREAMING,
        }

        // State enum for a glove
        private enum GloveState
        {
            GLOVE_0_DISCONNECTED = 0,
            GLOVE_1_GET_INFO,
            GLOVE_2_FILTER_GESTURE,
            GLOVE_3_FILTER_CONVERT,
            GLOVE_4_ADD_TO_STREAM,
            GLOVE_5_SET_STREAM_DATA,
            GLOVE_6_SET_STREAM_RAW,
            GLOVE_7_SETUP,
        }

        private class GloveInfo
        {
            public GloveState state;
            public UInt64 endpointID;
            public GloveLaterality handedness;

            public GloveInfo(GloveState state)
            {
                this.state = state;
                this.handedness = GloveLaterality.UNKNOWN;
            }
        }

        /// <summary>
        /// Get the active instance of Apollo
        /// </summary>
        /// <returns></returns>
        public static Apollo GetInstance(bool usePinch)
        {
            // Enable or disable debugging
            _usePinchFilter = usePinch;
            ApolloTCP.ShowDebugLog = _showDebugLog;

            if (_instance != null)
                return _instance;

            // Create a new instance of apollo if there is no active version
            var apolloGameObject = new GameObject("Apollo");
            return _instance = apolloGameObject.AddComponent<Apollo>();
        }

        void Start()
        {
            CreateApolloSession();

            // initialise the TCP client connecting to Apollo
            _apolloTCP = new ApolloTCP(receivePacket, _ip, _port);

            // Try to start Apollo
            StartCoroutine(StartApollo(2));

            // start periodic check for the Apollo connection and state
            _apolloCheck = StartCoroutine(ApolloCheck());
        }

        // Stop the connection with Apollo when the program shuts down
        public void OnApplicationQuit()
        {
            // stop the tcp client
            ApolloTCP.StopClient();

            // stop the apollo checking coroutine
            StopAllCoroutines();
            running = false;
        }

        #region DATA_LISTENER

        public void RegisterDataListener(newDataEvent function)
        {
            if (newDataEvent == null)
                newDataEvent = function;
            else
                newDataEvent += function;
        }

        public void RegisterDataListener(newRawDataEvent function)
        {
            if (newRawDataEvent == null)
                newRawDataEvent = function;
            else
                newRawDataEvent += function;
        }

        public void RegisterDataListener(newDeviceInfo function)
        {
            if (newDeviceInfo == null)
                newDeviceInfo = function;
            else
                newDeviceInfo += function;
        }

        public void UnRegisterDataListener(newDataEvent function)
        {
            if (newDataEvent != null)
                newDataEvent -= function;
        }

        public void UnRegisterDataListener(newRawDataEvent function)
        {
            if (newRawDataEvent != null)
                newRawDataEvent -= function;
        }

        public void UnRegisterDataListener(newDeviceInfo function)
        {
            if (newDeviceInfo != null)
                newDeviceInfo -= function;
        }

        #endregion

        // Creates a new Apollo session
        void CreateApolloSession()
        {
            // register error handler first because OpenSession can give errors
            ApolloSDK.registerErrorHandler(new IntPtr(), errorHandler);

            // start new instance of the apollo Network SDK
            _session = ApolloSDK.apolloOpenSession(_clientId);
            log("Started new ApolloSDK session: " + _session);

            // register all the callback functions from the NetSDK
            ApolloSDK.registerHandshakePacketHandler(_session, new IntPtr(), handshakeHandler);
            ApolloSDK.registerSuccessHandler(_session, new IntPtr(), successHandler);
            ApolloSDK.registerFailHandler(_session, new IntPtr(), failHandler);
            ApolloSDK.registerDeviceIdListHandler(_session, new IntPtr(), deviceListHandler);
            ApolloSDK.registerDongleIdListHandler(_session, new IntPtr(), dongleIdListHandler);
            ApolloSDK.registerDataStreamHandler(_session, new IntPtr(), dataStreamHandler);
            ApolloSDK.registerRawStreamHandler(_session, new IntPtr(), rawStreamHandler);
            ApolloSDK.registerSourceInfoHandler(_session, new IntPtr(), sourceInfoHandler);
            ApolloSDK.registerDeviceInfoHandler(_session, new IntPtr(), deviceInfoHandler);
        }

        // Callback from the TCP server when a full packet is received
        private static void receivePacket(byte[] data)
        {
            ApolloSDK.apolloProcessPacket(_session, new ApolloPacketBinary(data));
        }

        public static void rumble(GloveLaterality side, ushort duration, ushort power)
        {
            // try to find a glove with the inserted handedness
            foreach (var glove in sourceList)
                if (glove.Value.handedness == side)
                {
                    var eventId = randomEventId;

                    var packet = ApolloSDK.generateDeviceVibrate(_session, glove.Key, duration, power, eventId);
                    sendPacket(packet);
                    eventList.Add(eventId, new eventData(EventType.RUMBLE));
                }
        }

        #region CALLBACK_HANDLERS

        // Data stream packet from Apollo
        public static void dataStreamHandler(IntPtr cbr, UInt64 session, ApolloJointData jointData)
        {
            if (newDataEvent != null)
                newDataEvent(jointData, sourceList[jointData.deviceID].handedness);
        }

        // Raw stream packet from Apollo
        public static void rawStreamHandler(IntPtr cbr, UInt64 session, ApolloRawData rawData)
        {
            if (newRawDataEvent != null)
                newRawDataEvent(rawData, sourceList[rawData.deviceID].handedness);
        }

        public static void deviceInfoHandler(IntPtr cbr, UInt64 session, UInt16 eventID, ApolloDeviceInfo info)
        {
            //log(string.Format("Battery: {0} signal: {1} deviceId: {2}", info.batteryPercent, info.signalAttenuationDb, info.deviceID));

            // pass the received information 
            if (newDeviceInfo != null)
                newDeviceInfo(info);
        }

        public static void dongleIdListHandler(IntPtr cbr, UInt64 session, UInt16 eventID, U64Array dongleList)
        {
            UInt64[] dongles = dongleList.array;

            if (dongles.Length == 0)
            {
                warning("No dongles found, please insert one");
                // no dongles found, reset the values
                dongleId = 0;
            }
            else if (dongles.Length == 1)
            {
                // if the new dongle ID is not already known
                if (dongleId != dongles[0])
                {
                    // safe the first (and only) dongle id
                    log("New dongle discovered: " + dongles[0]);
                    dongleId = dongles[0];
                }
            }
            else
            {
                warning("More then 1 dongle connected, please make sure you have only one dongle connected to your PC");
                // more then one dongle found, don't know which one to pick so reset the dongleId
                dongleId = 0;
            }
        }

        public static void deviceListHandler(IntPtr cbr, UInt64 session, UInt16 eventID, U64Array sourceList)
        {
            UInt64[] sources = sourceList.array;
            //log("Received sourceList packet");
            if (sources.Length == 0)
            {
                //log("Empty sources list received");
                // no gloves found, reset the values
                if (Apollo.sourceList.Count > 0)
                {
                    log("No gloves connected, cleared gloveList");
                    Apollo.sourceList.Clear();
                    // Apollo state machine will automatically stop streaming if a new glove is found, so this is safe
                }
            }
            else
            {
                // check if new gloves are found
                foreach (var source in sources)
                {
                    // make sure the glove is not alread in the list
                    if (!Apollo.sourceList.ContainsKey(source))
                    {
                        log("New glove connected: " + source);
                        // a new glove is connected, only setup the state, fill in the info later
                        Apollo.sourceList.Add(source, new GloveInfo(GloveState.GLOVE_1_GET_INFO));
                    }
                }

                // check if each glove in my list is still known to Apollo
                foreach (var gloveId in Apollo.sourceList.Keys)
                {
                    // check if a glove in the Apollo list is known in the current sources
                    bool inList = false;
                    foreach (var source in sources)
                        if (source == gloveId)
                            inList = true;

                    if (!inList)
                    {
                        // A glove is disconnected
                        log("A glove is disconnected: " + gloveId);
                        Apollo.sourceList.Remove(gloveId);
                        // Apollo state machine will automatically stop streaming if a new glove is found, so this is safe
                    }
                }
            }
        }

        public static void sourceInfoHandler(IntPtr cbr, UInt64 session, UInt16 eventID, ApolloSourceInfo info)
        {
            // retreive the gloveID from the list
            var gloveId = eventList[eventID].endpointID;
            // update the information about this glove
            sourceList[gloveId].endpointID = info.endpoint;

            bool success = false;
            // extract the handedness
            switch (info.side)
            {
                case apollo_laterality_t.SIDE_LEFT:
                    log(string.Format("{0} made LEFT eId {1}", gloveId, eventID));
                    sourceList[gloveId].handedness = GloveLaterality.GLOVE_LEFT;
                    success = true;
                    break;
                case apollo_laterality_t.SIDE_RIGHT:
                    log(string.Format("{0} made RIGHT eId {1}", gloveId, eventID));
                    sourceList[gloveId].handedness = GloveLaterality.GLOVE_RIGHT;
                    success = true;
                    break;
            }

            if (success)
                // increment this glovestate
                sourceList[gloveId].state += 1;// GloveState.GLOVE_2_FILTER_GESTURE;
            else
                warning("Couldn't parse the glove info packet correctly, side: " + info.side);


            // delete this event from the list
            eventList.Remove(eventID);
        }

        // Called when a handshake packet is return from Apollo
        public static void handshakeHandler(IntPtr cbr, UInt64 session, IntPtr pckToReturn)
        {
            log("Received Handshake packet, Start ACK");
            ApolloPacketBinary ackPacket = (ApolloPacketBinary)Marshal.PtrToStructure(pckToReturn, typeof(ApolloPacketBinary));
            // DON'T destroy the packet, because it is owned by the SDK
            sendPacket(ackPacket, false);

            // if the state is not already handshaken or higher, set it up
            if (apolloState < ApolloState.APOLLO_2_GLOVE_SETUP)
                // force retry of handshake
                //apolloState = ApolloState.APOLLO_1_HANDSHAKING;
                apolloState = ApolloState.APOLLO_2_GLOVE_SETUP;
        }

        // Called when a success message is returned from Apollo
        private static void successHandler(IntPtr cbr, UInt64 session, UInt16 eventID, ApolloSuccessPayload successPayload)
        {
            // Dynamic event handling
            if (eventList.ContainsKey(eventID))
            {
                eventData thisEvent = eventList[eventID];
                switch (thisEvent.type)
                {
                    case EventType.GLOVE_ADD_STREAM:
                        // get the gloveId from the event and increment the state
                        sourceList[thisEvent.endpointID].state = GloveState.GLOVE_5_SET_STREAM_DATA;

                        log("GLOVE_ADD_STREAM Success");
                        break;
                    case EventType.GLOVE_SET_STREAM_DATA:
                        // get the gloveId from the event and increment the state
                        sourceList[thisEvent.endpointID].state = GloveState.GLOVE_6_SET_STREAM_RAW;

                        log("GLOVE_SET_STREAM_DATA Success");
                        break;
                    case EventType.GLOVE_SET_STREAM_RAW:
                        // get the gloveId from the event and increment the state
                        sourceList[thisEvent.endpointID].state = GloveState.GLOVE_7_SETUP;

                        log("GLOVE_SET_STREAM_RAW Success");
                        break;
                    case EventType.GLOVE_ADD_FILTER_GESTURE:
                        // retreive the new endpoint
                        var gestureEndpoint = successPayload.uint64;
                        // set the new endpoint
                        sourceList[thisEvent.endpointID].endpointID = gestureEndpoint;
                        // increment to the next state
                        sourceList[thisEvent.endpointID].state = GloveState.GLOVE_3_FILTER_CONVERT;

                        log("GLOVE_ADD_FILTER_GESTURE Success ID: " + gestureEndpoint);
                        break;
                    case EventType.GLOVE_ADD_FILTER_CONVERT:
                        // retreive the new endpoint
                        var convertEndpoint = successPayload.uint64;
                        // set the new endpoint
                        sourceList[thisEvent.endpointID].endpointID = convertEndpoint;
                        // increment to the next state
                        sourceList[thisEvent.endpointID].state = GloveState.GLOVE_4_ADD_TO_STREAM;

                        log("GLOVE_ADD_FILTER_CONVERT Success ID: " + convertEndpoint);
                        break;
                    case EventType.APOLLO_START_STREAM:
                        log("APOLLO_START_STREAM Success");
                        apolloState = ApolloState.APOLLO_3_STREAMING;
                        break;
                    case EventType.APOLLO_STOP_STREAM:
                        log("APOLLO_STOP_STREAM Success");
                        apolloState = ApolloState.APOLLO_2_GLOVE_SETUP;
                        break;
                    case EventType.RUMBLE:
                        // Don't do anything on rumble success
                        break;
                    default:
                        log("Unknow success evenType: " + thisEvent.type);
                        break;
                }
                // event is handled, remove from list
                eventList.Remove(eventID);
            }
            else
            {
                // 0 is default value, when no eventID is given
                if (eventID != 0)
                    warning("Event ID [" + eventID + "] not found in eventList");
            }
        }

        // Called when a fail message is returned from Apollo
        private static void failHandler(IntPtr cbr, UInt64 session, UInt16 eventID, string failMsg)
        {
            warning("Fail(" + eventID + "," + eventList[eventID].type + "): " + failMsg);
            // This event is handled (failed) so remove from the eventList if it exists
            if (eventList.ContainsKey(eventID))
                eventList.Remove(eventID);

            // Restart client if it failed to setup raw/data streams
            ApolloTCP.StopClient();
        }

        // Called when the NetSDK encounters an error
        public static void errorHandler(IntPtr cbr, UInt64 session, UInt16 errCode, string errMsg)
        {
            Debug.LogWarning("Error code: " + errCode + " Message: " + errMsg);
            //ApolloTCP.StopClient();

        }


        #endregion
        #region APOLLO_STATE_MACHINE

        // Reset every variable related to connecting with Apollo
        private void resetApolloState()
        {
            apolloState = ApolloState.APOLLO_0_CONNECTING;
            // empty the source and event List
            sourceList.Clear();
            eventList.Clear();

            // reset the apollo session
            ApolloSDK.apolloCloseSession(_session);
            CreateApolloSession();

            // make sure the TCP server is reset
            ApolloTCP.StopClient();

            _connectionAttempts = 0;
        }

        private bool IsApolloRunning()
        {
            Process[] running = Process.GetProcesses();
            foreach (Process process in running)
            {
                try
                {
                    if (!process.HasExited && process.ProcessName == "Apollo")
                    {
                        return true;
                    }
                }
                // Prevent from erroring on Windows 7
                catch (System.InvalidOperationException)
                {
                    //do nothing
                }
            }
            return false;
        }

        // Start Apollo if it was not found in the active processes
        private IEnumerator StartApollo(float delay)
        {
            // The installation path of Apollo
            const string path = "C:/Program Files/ManusVR/Apollo/Apollo.exe";

            if (IsApolloRunning() || apolloState > ApolloState.APOLLO_0_CONNECTING)
                yield break;

            try
            {
                // Stop the apollo check coroutine before starting the apollo
                if (_apolloCheck != null)
                    StopCoroutine(_apolloCheck);
                log("Could not find Apollo, attempting to start " + path);
                Process.Start(path);
            }

            catch (FileNotFoundException e)
            {
                log(e.ToString());
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
            yield return new WaitForSeconds(delay);
            // Start the apollo connection coroutine
            if (_apolloCheck != null)
                _apolloCheck = StartCoroutine(ApolloCheck());

        }

        // Constantly running check for the connection with Apollo
        private IEnumerator ApolloCheck()
        {
            while (true)
            {
                // init the connection delay default fast
                float connectionDelay = fastConnectionDelay;

                // if the connection gets broken somehow and not already in disconnected state
                if (apolloState > ApolloState.APOLLO_0_CONNECTING && !ApolloTCP.connected)
                {
                    resetApolloState();
                    log("Connnection reset by server");
                }

                // if apollo is properly connected/handshaken
                if (apolloState > ApolloState.APOLLO_1_HANDSHAKING)
                {
                    // check available dongles/sources
                    sendPacket(ApolloSDK.generateListDongleIDs(_session));
                    // If we found a dongle, start requesting all devices
                    if (dongleId != 0)
                        sendPacket(ApolloSDK.generateListDeviceIDs(_session, dongleId));

                    // request device information
                    foreach (var source in sourceList)
                    {
                        // if a glove in setup is found, request device info
                        if (source.Value.state == GloveState.GLOVE_7_SETUP)
                        {
                            sendPacket(ApolloSDK.generateGetDeviceInfo(_session, source.Key));
                        }
                    }
                }

                switch (apolloState)
                {
                    case ApolloState.APOLLO_0_CONNECTING:
                        // slow the check down to give Apollo time to init
                        if (_connectionAttempts >= 3 / fastConnectionDelay)
                            connectionDelay = slowConnectionDelay;

                        // try connecting and skip if already connected
                        if (!ApolloTCP.connected)
                        {
                            _connectionAttempts++;
                            _apolloTCP.ConnectClient(); // (re)try to get a connection
                        }
                        else
                        {
                            apolloState = ApolloState.APOLLO_1_HANDSHAKING;
                        }
                        break;
                    case ApolloState.APOLLO_1_HANDSHAKING:
                        // slow the check down to give Apollo time to init
                        // if this is not the first attempt
                        if (_connectionAttempts > 3 / fastConnectionDelay)
                            connectionDelay = slowConnectionDelay;
                        // Connecting is succesfull so reset connectionAttempts
                        _connectionAttempts = 0;

                        log("Start handshake");
                        // initiate handshake
                        sendPacket(ApolloSDK.generateHandshake(_session));
                        break;
                    case ApolloState.APOLLO_2_GLOVE_SETUP:
                        // setup the gloves
                        setupGloves();

                        // start the stream if all gloves are setup
                        bool startStream = false;
                        foreach (var info in sourceList.Values)
                        {
                            // if a glove still in setup is found, never start the stream
                            if (info.state >= GloveState.GLOVE_1_GET_INFO && info.state < GloveState.GLOVE_7_SETUP)
                            {
                                startStream = false;
                                break;
                            }

                            // if a glove that is set is found, we can start the stream
                            if (info.state == GloveState.GLOVE_7_SETUP)
                                startStream = true;
                        }

                        if (startStream)
                        {
                            log("(New) gloves discovered and all are setup, start streaming");
                            var eventId = randomEventId;
                            // send out the packet to Apollo
                            sendPacket(ApolloSDK.generateStartStreams(_session, eventId));

                            // register am event in the list
                            eventList.Add(eventId, new eventData(EventType.APOLLO_START_STREAM));
                        }
                        break;
                    case ApolloState.APOLLO_3_STREAMING:
                        // if apollo is streaming and a new glove is found, turn off the stream and set all active gloves to discovered
                        bool stopStream = false;

                        // if a glove that is new is found, we can stop the stream
                        foreach (var info in sourceList.Values)
                            if (info.state == GloveState.GLOVE_1_GET_INFO)
                                stopStream = true;

                        if (stopStream)
                        {
                            log("(New) gloves discovered, start streaming");
                            var eventId = randomEventId;
                            // send out the packet to Apollo
                            sendPacket(ApolloSDK.generateStopStreams(_session, eventId));

                            // register am event in the list
                            eventList.Add(eventId, new eventData(EventType.APOLLO_STOP_STREAM));
                        }
                        break;
                }

                // stop executing the coroutine if not running
                if (running)
                    yield return new WaitForSeconds(connectionDelay);
                else
                    yield break;

            }
        }

        // Checks all the gloves status and handles their connection with Apollo
        private static void setupGloves()
        {
            foreach (var glove in sourceList)
            {
                // generate a random eventID and prepare the variables
                var eventId = randomEventId;
                var gloveId = glove.Key;
                var endpointID = glove.Value.endpointID;

                switch (glove.Value.state)
                {
                    case GloveState.GLOVE_0_DISCONNECTED:
                        log(gloveId + ": Unused state, should not happen");
                        break;
                    case GloveState.GLOVE_1_GET_INFO:
                        log(gloveId + ": GET_INFO: " + eventId);
                        // request the information about this source to identify it
                        sendPacket(ApolloSDK.generateGetSourceInfo(_session, gloveId, eventId));

                        // create the event
                        eventList.Add(eventId, new eventData(EventType.GLOVE_IDENTIFY, gloveId));
                        break;
                    case GloveState.GLOVE_2_FILTER_GESTURE:
                        if (!_usePinchFilter)
                        {
                            sourceList[gloveId].state = GloveState.GLOVE_3_FILTER_CONVERT;
                            log(gloveId + ": SKIPPED FILTER GESTURE: " + eventId);
                            break;
                        }
                        log(gloveId + ": SETUP FILTER GESTURE: " + eventId);

                        // prepare the arguments for the gesture filter
                        var gestureArray = new ApolloGestureArray(new apollo_gesture_t[] { apollo_gesture_t.GESTURE_INDEX_PINCH });
                        // create a filter handle for the packet generation
                        var gestureFilterHandle = ApolloSDK.generateGestureFilter(_session, gestureArray.numGestures, gestureArray.gestures, 0, 1);

                        // create the unmanaged sources/filters lists
                        var gestureSources = new U64Array(endpointID);
                        var gestureFilters = new U64Array(gestureFilterHandle);
                        // Add the filters in Apollo
                        sendPacket(ApolloSDK.generateAddFilters(_session, gestureSources.pointer, gestureSources.size, gestureFilters.pointer, gestureFilters.size, eventId));

                        // create an event
                        eventList.Add(eventId, new eventData(EventType.GLOVE_ADD_FILTER_GESTURE, gloveId));

                        _filterGestureAttempts++;
                        // Skip this fase if it was not able to setup the filter
                        if (_filterGestureAttempts >= 3)
                            sourceList[gloveId].state = GloveState.GLOVE_3_FILTER_CONVERT;
                        break;
                    case GloveState.GLOVE_3_FILTER_CONVERT:
                        log(gloveId + ": SETUP FILTER CONVERT: " + eventId);
                        // create a filter handle for the packet generation
                        var convertFilterHandle = ApolloSDK.generateMeshMappingFilter(_session, getUE4MannequinMeshConfig());

                        // create the unmanaged sources/filters lists
                        var convertSources = new U64Array(endpointID);
                        var convertFilters = new U64Array(convertFilterHandle);

                        // Add the filters in Apollo
                        sendPacket(ApolloSDK.generateAddFilters(_session, convertSources.pointer, convertSources.size, convertFilters.pointer, convertFilters.size, eventId));

                        // create an event
                        eventList.Add(eventId, new eventData(EventType.GLOVE_ADD_FILTER_CONVERT, gloveId));
                        break;
                    case GloveState.GLOVE_4_ADD_TO_STREAM:
                        // add this glove to the stream
                        // create a packet with one gloveId and send the packet out

                        //var glovesList = new U64Array(new ulong[] { endpointID, gloveId });
                        var glovesList = new U64Array(endpointID);
                        sendPacket(ApolloSDK.generateAddStreams(_session, glovesList.pointer, glovesList.size, eventId));

                        // create an event in the list
                        eventList.Add(eventId, new eventData(EventType.GLOVE_ADD_STREAM, gloveId));

                        log(gloveId + ": ADD_TO_STREAM: " + eventId + " | endpointID: " + endpointID);
                        break;
                    case GloveState.GLOVE_5_SET_STREAM_DATA:
                        // enable the data-stream for this glove
                        // create and send the packet
                        sendPacket(ApolloSDK.generateSetStreamData(_session, endpointID, true, eventId));

                        // create an event in the list
                        eventList.Add(eventId, new eventData(EventType.GLOVE_SET_STREAM_DATA, gloveId));

                        log(gloveId + ": SET_STREAM_DATA: " + eventId + " | endpointID: " + endpointID);
                        break;
                    case GloveState.GLOVE_6_SET_STREAM_RAW:
                        // enable the raw-stream for this glove
                        // create and send the packet
                        sendPacket(ApolloSDK.generateSetStreamRaw(_session, endpointID, true, eventId));

                        // create an event in the list
                        eventList.Add(eventId, new eventData(EventType.GLOVE_SET_STREAM_RAW, gloveId));

                        log(gloveId + ": SET_STREAM_RAW: " + eventId + " | endpointID: " + endpointID);
                        break;
                    case GloveState.GLOVE_7_SETUP:
                        log(gloveId + ": SETUP");
                        // glove is completely setup, don't do anything
                        break;
                    default:
                        log("Unknow state '" + glove.Value.state + "' for source: " + glove.Key);
                        break;
                }
            }
        }

        #endregion
        #region MESH_CONFIG

        //!
        //! \brief ApolloMeshNodeConfig defines the coordinate frame in a node/bone of the mesh
        //!
        //! We assume that hand mesh bones are oriented in such a way that one axis points towards the tips of the fingers,
        //! one axis points out of the hand perpendicular to the palmar plane or the plane in which the finger nails would lie (in case of the thumb),
        //! and one axis perpendicular to the other two, usually designating the axis of rotation of the finger joints.
        //!
        //! For the world frame, forward = from viewer, right = right of viewer
        //! Some engines flip a coordinate frame axis to convert a right-handed coordinate frame to a left-handed coordinate frame
        //! This negation axis should also be specified for your application
        //!
        //! \example The bone that you try to describe has its x-Axis pointing towards the finger tip, its y-Axis pointing towards the palm and rotates around the z-Axis, which points left.
        //! In this case, upDirection = COOR_AXIS_Y_NEG (the negation of the y-Axis points to the back of the hand), forwardDirection = COOR_AXIS_X_POS,
        //! and rightDirection = COOR_AXIS_Z_NEG (the negation of the z-Axis points right according to the above definition)
        //!
        //! \attention the upAxis for the thumb refers to the direction outward from the back of the thumb,
        //! and its rightAxis refers to the direction pointing right when looking at the back of the thumb (i.e. the side on which the nail is)
        //!
        //! \note for an illustration check out the online documentation for the Apollo Network SDK
        //!
        private static ApolloMeshConfig getUE4MannequinMeshConfig()
        {
            ApolloMeshConfig meshConfig;

            meshConfig.rightWrist.upDirection = coor_axis_t.COOR_AXIS_Y_POS;
            meshConfig.rightWrist.forwardDirection = coor_axis_t.COOR_AXIS_X_POS;
            meshConfig.rightWrist.rightDirection = coor_axis_t.COOR_AXIS_Z_NEG;

            // for this mesh, right thumb and right finger coordinate systems are equal to right wrist
            meshConfig.rightThumb = meshConfig.rightWrist; // note: up-axis is defined as outward of nail, not world up-axis
            meshConfig.rightFinger = meshConfig.rightWrist;

            meshConfig.leftWrist.upDirection = coor_axis_t.COOR_AXIS_Y_NEG;
            meshConfig.leftWrist.forwardDirection = coor_axis_t.COOR_AXIS_X_NEG;
            meshConfig.leftWrist.rightDirection = coor_axis_t.COOR_AXIS_Z_NEG;

            // for this mesh, left thumb and left finger coordinate systems are equal to right wrist
            meshConfig.leftThumb = meshConfig.leftWrist; // note: up-axis is defined as outward of nail, not world up-axis
            meshConfig.leftFinger = meshConfig.leftWrist;

            meshConfig.world.upDirection = coor_axis_t.COOR_AXIS_Y_POS;
            meshConfig.world.forwardDirection = coor_axis_t.COOR_AXIS_Z_POS;
            meshConfig.world.rightDirection = coor_axis_t.COOR_AXIS_X_POS; // coor_handed_t.COOR_LEFT_HANDED;

            // Unity negates the x-axis
            // https://gamedev.stackexchange.com/questions/39906/why-does-unity-obj-import-flip-my-x-coordinate
            // https://fogbugz.unity3d.com/default.asp?983147_lnn32r51vrk1cpna
            meshConfig.negateAxisX = true;
            meshConfig.negateAxisY = false;
            meshConfig.negateAxisZ = false;

            return meshConfig;
        }

        /*
        public coor_axis_t LeftFingerUp;
        public coor_axis_t LeftFingerForward;
        public coor_axis_t LeftRightDirection;*/

        // Sends a packet to the TCP server
        private static void sendPacket(ApolloPacketBinary packet, bool destroyPacket = true)
        {
            _apolloTCP.Send(ApolloSDK.WrapPacket(packet));
            // Dispose of the packet (pointer) if requested
            if (destroyPacket)
                ApolloSDK.apolloDisposePacket(packet);
        }

        #endregion
        #region LOGGIN

        private static void log(string message)
        {
            if (_showDebugLog)
                Debug.Log(message);
        }

        private static void warning(string message)
        {
            Debug.LogWarning(message);
        }

        #endregion
    }
}
