// Copyright (c) 2018 ManusVR

using ManusVR.Core.Apollo;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ManusVR.Core.Hands
{
    /// <summary>
    /// This static class handles the storage of glove data. It stores them in pairs (one left glove, one right glove) called "players".
    /// **Script Usage:**
    ///  * Call GetPlayerNumber to get exclusive access to a player's data.
    ///  * Then use GetHandData, after checking CanGetHandData, to get that player's data.
    /// </summary>
    public class HandDataManager
    {
        /// <summary>
        /// Any player number that has this value is invalid.
        /// </summary>
        public const int invalidPlayerNumber = 0;

        /// <summary>
        /// A delegate the will be called when new hand data has been pushed from the custom stream.
        /// </summary>
        public static NewApolloHandData OnNewCustomStreamHandData;

        public static bool UsePinchFilter { get; set; } = false;
        public delegate void NewApolloHandData(ApolloHandData apolloHandData, GloveLaterality side);

        private static Dictionary<UInt64, int> deviceIDToPlayerIndex = new Dictionary<UInt64, int>();
        private static List<PlayerData> playerData = new List<PlayerData>();

        private static bool initialised = false;
        private static DataStreamType currentStreamType = DataStreamType.None;

        private static bool debugManager = false;

        public class PlayerData
        {
            public Dictionary<device_type_t, ApolloHandData> handData = new Dictionary<device_type_t, ApolloHandData>();
            public bool isInUse = false;
        }

        public enum DataStreamType
        {
            Apollo, Custom, None
        }

        /// <summary>
        /// Prepare the HandDataManager class for use.
        /// This can safely be called multiple times; initialisation will occur only once.
        /// </summary>
        /// <param name="usePinchFilter">If the pinch filter should be used or not.</param>
        /// <param name="streamType">The type of data stream the manager will use.</param>
        public static void Initialise(DataStreamType streamType)
        {
            SwitchStreamType(streamType);

            if (initialised)
            {
                return;
            }

            Log("Starting the HandDataManager.");

            Application.runInBackground = true;

            initialised = true;
        }

        /// <summary>
        /// Reset all of the variables that are included in the HandDataManager.
        /// </summary>
        public static void Reset()
        {
            OnNewCustomStreamHandData = null;
            deviceIDToPlayerIndex = new Dictionary<UInt64, int>();
            playerData = new List<PlayerData>();
            initialised = false;
            currentStreamType = DataStreamType.None;
        }

        /// <summary>
        /// Switch the stream type to another stream type.
        /// </summary>
        /// <param name="streamType">The new stream type that will be used.</param>
        public static void SwitchStreamType(DataStreamType streamType)
        {
            if (currentStreamType == streamType)
            {
                Log("Can not switch stream the stream type to " + streamType + " since the current stream type is already " + currentStreamType);
                return;
            }

            Apollo.Apollo apollo = Apollo.Apollo.GetInstance(UsePinchFilter);

            Log("The stream type is going to be switched to " + streamType);

            switch (streamType)
            {
                case DataStreamType.Apollo:
                    // Subscribe to the Apollo stream
                    apollo.RegisterDataListener(NewJointData);
                    apollo.RegisterDataListener(NewRawData);

                    // Unsubscribe from the custom stream
                    OnNewCustomStreamHandData -= NewCustomData;
                    break;
                case DataStreamType.Custom:
                    // Subscribe to the custom stream
                    OnNewCustomStreamHandData += NewCustomData;

                    // Unsubscribe to the Apollo stream
                    apollo.UnRegisterDataListener(NewJointData);
                    apollo.UnRegisterDataListener(NewRawData);
                    break;
                default:
                    Log(streamType + " is not implemented as stream type");
                    break;
            }
        }

        /// <summary>
        /// Feed the HandDataManager with the apollo hand data, this will only work if the stream type is set to custom.
        /// </summary>
        /// <param name="apolloHandData">The new apollo hand data.</param>
        /// <param name="side">The side for which this data applies.</param>
        public static void FeedCustomStreamHandData(ApolloHandData apolloHandData, GloveLaterality side)
        {
            if (OnNewCustomStreamHandData != null)
            {
                OnNewCustomStreamHandData(apolloHandData, side);
            }
        }

        private static void NewCustomData(ApolloHandData apolloHandData, GloveLaterality side)
        {
            //Log("Received new hand data for a " + side + " glove with device ID " + apolloHandData.deviceID + ".");
            // Get ready to store the new data.
            int playerIndex = -1;
            device_type_t deviceType = device_type_t.GLOVE_LEFT;

            if (!PrepareForNewData(true, side, apolloHandData.deviceID, out playerIndex, out deviceType))
            {
                // Failed to prepare for the new data. The new data cannot be stored.
                // Printing a log message is done in the function, so don't print anything here.

                return;
            }

            // Set the data.
            playerData[playerIndex].handData[deviceType] = apolloHandData;
        }

        private static void NewJointData(ApolloJointData data, GloveLaterality side)
        {
            //Log("Received new hand data for a " + side + " glove with device ID " + data.deviceID + ".");
            // Get ready to store the new data.
            int playerIndex = -1;
            device_type_t deviceType = device_type_t.GLOVE_LEFT;

            if (!PrepareForNewData(data.IsValid, side, data.deviceID, out playerIndex, out deviceType))
            {
                // Failed to prepare for the new data. The new data cannot be stored.
                // Printing a log message is done in the function, so don't print anything here.

                return;
            }

            // Set the data.
            playerData[playerIndex].handData[deviceType].SetJointData(data);
        }

        private static void NewRawData(ApolloRawData data, GloveLaterality side)
        {
            //Log("Received new raw data for a " + side + " glove with device ID " + data.deviceID + ".");
            // Get ready to store the new data.
            int playerIndex = -1;
            device_type_t deviceType = device_type_t.GLOVE_LEFT;

            if (!PrepareForNewData(data.IsValid, side, data.deviceID, out playerIndex, out deviceType))
            {
                // Failed to prepare for the new data. The new data cannot be stored.
                // Printing a log message is done in the function, so don't print anything here.

                return;
            }

            // Set the data.
            playerData[playerIndex].handData[deviceType].SetRawData(data);
        }

        private static void LogIfNumbersChanged(int numPlayersStored, int numGlovesKnown)
        {
            // Check if the numbers changed.
            int newNumPlayers = playerData.Count;
            int newNumGlovesKnown = GetNumberOfGlovesKnown();

            bool numPlayersChanged = newNumPlayers != numPlayersStored;
            bool numGlovesChanged = newNumGlovesKnown != numGlovesKnown;

            // Log if the numbers changed.
            if (numPlayersChanged || numGlovesChanged)
            {
                string changed =
                    (numPlayersChanged ? "players" : "")
                    + (numPlayersChanged && numGlovesChanged ? " and " : "")
                    + (numGlovesChanged ? "gloves" : "");

                string playersChanged = (numPlayersChanged ? " (was " + numPlayersStored + ")" : "");
                string glovesChanged = (numGlovesChanged ? " (was " + numGlovesKnown + ")" : "");

                Log("The number of " + changed + " has changed. The number of players is now " + playerData.Count + playersChanged + ", and the number of gloves known is " + newNumGlovesKnown + glovesChanged + ".");
            }
        }

        private static bool PrepareForNewData(bool isValid, GloveLaterality laterality, UInt64 deviceID, out int playerIndex, out device_type_t deviceType)
        {
            int numPlayersStored = playerData.Count;
            int numGlovesKnown = GetNumberOfGlovesKnown();

            playerIndex = -1;
            deviceType = device_type_t.GLOVE_LEFT;

            // Make sure the new data is usable.
            if (!isValid)
            {
                Debug.LogWarning("Received new hand data, but its contents are invalid.");

                return false;
            }

            // Check the glove laterality.
            if (!ConvertGloveLateralityToDeviceType(laterality, out deviceType))
            {
                // The laterality could not be converted.
                // A log message is printed in the function, so don't print anything here.

                return false;
            }

            // Find the player for this data.
            if (!FindExistingPlayerUsingDeviceID(deviceID, deviceType, out playerIndex))
            {
                AddNewPlayerForDeviceID(deviceID, out playerIndex);
            }

            // Make sure the hand data exists.
            if (!playerData[playerIndex].handData.ContainsKey(deviceType))
            {
                playerData[playerIndex].handData.Add(deviceType, new ApolloHandData());
            }

            LogIfNumbersChanged(numPlayersStored, numGlovesKnown);

            return true;
        }

        private static bool ConvertGloveLateralityToDeviceType(GloveLaterality input, out device_type_t output)
        {
            output = device_type_t.GLOVE_LEFT;

            switch (input)
            {
                case GloveLaterality.UNKNOWN:
                    Debug.LogError("Tried to convert a GloveLaterality to a device_type_t, but its value was UNKNOWN.");
                    return false;

                case GloveLaterality.GLOVE_LEFT:
                    output = device_type_t.GLOVE_LEFT;
                    return true;

                case GloveLaterality.GLOVE_RIGHT:
                    output = device_type_t.GLOVE_RIGHT;
                    return true;

                default:
                    Debug.LogError("Tried to convert a GloveLaterality to a device_type_t, but it had an unrecognised value. Its value was " + input + ".");
                    return false;
            }
        }

        private static bool FindExistingPlayerUsingDeviceID(UInt64 deviceID, device_type_t deviceType, out int playerIndex)
        {
            playerIndex = -1;

            if (deviceIDToPlayerIndex.ContainsKey(deviceID))
            {
                // The device ID is a known one. Get the player number from the dictionary.
                playerIndex = deviceIDToPlayerIndex[deviceID];

                return true;
            }

            // This is a new device ID.
            // See if one of the players already in the playerData list is still missing a glove of this laterality.
            for (int i = 0; i < playerData.Count; i++)
            {
                if (!playerData[i].handData.ContainsKey(deviceType))
                {
                    playerIndex = i;
                    deviceIDToPlayerIndex[deviceID] = playerIndex;

                    return true;
                }
            }

            // No known device found, and no existing player needs a device of this laterality.
            return false;
        }

        private static void AddNewPlayerForDeviceID(UInt64 deviceID, out int newPlayerIndex)
        {
            newPlayerIndex = playerData.Count;
            deviceIDToPlayerIndex[deviceID] = newPlayerIndex;

            PlayerData dataToAdd = new PlayerData();
            playerData.Add(dataToAdd);
        }

        /// <summary>
        /// Get hand data for the given player and device type.
        /// Check if data is available before calling this using CanGetHandData.
        /// </summary>
        /// <param name="playerNumber">The player number that data should be retrieved for.</param>
        /// <param name="deviceType">The device type that data should be retrieved for.</param>
        /// <returns>The hand data if it was available, and empty hand data if it was not.</returns>
        public static ApolloHandData GetHandData(int playerNumber, device_type_t deviceType)
        {
            if (!CanGetHandData(playerNumber, deviceType))
            {
                //Debug.LogError("Attempted to get hand data for player " + playerNumber + " and device type " + deviceType + ", but no data is available.");

                return new ApolloHandData();
            }

            int playerIndex = playerNumber - 1;

            return playerData[playerIndex].handData[deviceType];
        }

        /// <summary>
        /// Get an unused player number. This can be used to assign a player to a set of gloves.
        /// </summary>
        /// <param name="playerNumber">The player number that was reserved.</param>
        /// <returns>True if an unused player number was available, false if all player numbers are taken.</returns>
        public static bool GetPlayerNumber(out int playerNumber)
        {
            playerNumber = invalidPlayerNumber;

            // Check for player data that isn't linked to a controller yet.
            foreach (PlayerData data in playerData)
            {
                if (!data.isInUse)
                {
                    data.isInUse = true;
                    playerNumber = playerData.IndexOf(data) + 1;

                    return true;
                }
            }

            // No player data is available, so no player number could be selected.
            return false;
        }

        /// <summary>
        /// Release a player number that was claimed, so it can be used by something else.
        /// </summary>
        /// <param name="playerNumber">The player number that should be freed.</param>
        /// <returns>True if the player number was successfully </returns>
        public static bool ReleasePlayerNumber(int playerNumber)
        {
            if (!IsPlayerNumberValid(playerNumber))
            {
                Debug.LogError("Attempted to release a player number, but the given player number was not valid. The number was " + playerNumber + ".");

                return false;
            }

            int playerIndex = playerNumber - 1;

            if (!playerData[playerIndex].isInUse)
            {
                Debug.LogError("Attempted to release a player number that was not in use.");

                return false;
            }

            playerData[playerIndex].isInUse = false;

            return true;
        }

        /// <summary>
        /// Get the number of gloves that are currently known.
        /// </summary>
        /// <returns>The number of gloves known to HandDataManager.</returns>
        public static int GetNumberOfGlovesKnown()
        {
            int numGloves = 0;

            foreach (PlayerData pData in playerData)
            {
                numGloves += pData.handData.Count;
            }

            return numGloves;
        }

        /// <summary>
        /// Check if the given player number refers to an existing player (i.e. a set of gloves), and that player is marked as being in use.
        /// </summary>
        /// <param name="playerNumber"></param>
        /// <returns>True if the given player number is valid, false if it is not.</returns>
        public static bool IsPlayerNumberValid(int playerNumber)
        {
            return playerNumber != invalidPlayerNumber
                && playerNumber <= playerData.Count
                && playerNumber >= 0
                && playerData[playerNumber - 1].isInUse;
        }

        /// <summary>
        /// Check if hand data is available for the given player and device type.
        /// </summary>
        /// <param name="playerNumber">The player that the check should be performed for.</param>
        /// <param name="deviceType">The type of device that the check should be performed for.</param>
        /// <returns>True if data is available for the given player and device type, false if not.</returns>
        public static bool CanGetHandData(int playerNumber, device_type_t deviceType)
        {
            int playerIndex = playerNumber - 1;

            return initialised
                && IsPlayerNumberValid(playerNumber)
                && playerData[playerIndex].handData.ContainsKey(deviceType);
        }

        private static void Log(string message)
        {
            if (debugManager)
            {
                Debug.Log(message);
            }
        }
    }
}
