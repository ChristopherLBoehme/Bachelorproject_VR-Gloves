using ManusVR.Core.Apollo;
using ManusVR.Core.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace ManusVR.Core.Hands
{
    public class HandController : MonoBehaviour
    {
        [Tooltip("A reference to the left hand transform.")]
        public Transform leftHand;
        [Tooltip("A reference to the right hand transform.")]
        public Transform rightHand;

        [Tooltip("If Apollo's pinch pose detection should be enabled. This can be used with Apollo version 2019.2 and up.")]
        public bool usePinchFilter = false; // Note: the usePinchFilter setting becomes an issue with multiple players.

        protected int playerNumber = HandDataManager.invalidPlayerNumber;
        public int PlayerNumber => this.playerNumber;

        protected Dictionary<device_type_t, Hand> hands = new Dictionary<device_type_t, Hand>();
        public Dictionary<device_type_t, Hand> Hands => this.hands;

        public static readonly device_type_t[] deviceTypes =
        {
            device_type_t.GLOVE_LEFT,
            device_type_t.GLOVE_RIGHT
        };

        private bool debugController = false;

        public void OverridePlayerNumber(int playerNumber)
        {
            this.playerNumber = playerNumber;
        }

        protected virtual void OnDestroy()
        {
            if (HandDataManager.IsPlayerNumberValid(playerNumber))
            {
                HandDataManager.ReleasePlayerNumber(playerNumber);
            }
        }

        protected virtual void Awake()
        {
            Log("Waking up the HandController of player " + playerNumber + ".");

            if (!InitialiseHand(ref leftHand, device_type_t.GLOVE_LEFT, "hand_l")
                || !InitialiseHand(ref rightHand, device_type_t.GLOVE_RIGHT, "hand_r"))
            {
                this.enabled = false;
            }
            else
            {
                HandDataManager.UsePinchFilter = usePinchFilter;
                HandDataManager.Initialise(HandDataManager.DataStreamType.Apollo);
            }
        }

        protected virtual bool InitialiseHand(ref Transform handTransform, device_type_t deviceType, string defaultName)
        {
            // Make sure the hand object exists and the controller has access to it.
            ManusRigger.ManusRigger rigger =
                transform.root.gameObject.GetComponentInChildren<ManusRigger.ManusRigger>();

            if (rigger)
            {
                handTransform = rigger.GetWristTransform(deviceType);
            }

            if (handTransform == null)
            {
                // Try to find the hand as a child.
                handTransform = transform.FindDeepChild(defaultName);

                if (handTransform == null)
                {
                    // The hand could not be found.
                    Debug.LogError("The HandController for " + gameObject +
                                   " could not find the transform for the hand with device type " + deviceType +
                                   ". The name it was looking for was " + defaultName +
                                   ". Is the mesh using the Epic skeleton layout?");

                    return false;
                }
            }

            // Make sure the hand component exists and the controller has access to it.
            Hand hand = handTransform.gameObject.GetComponent<Hand>();

            if (hand == null)
            {
                // The hand object doesn't have a hand component. Add one.
                hand = handTransform.gameObject.AddComponent<Hand>();

                if (hand == null)
                {
                    // The hand could not be added.
                    Debug.LogError("The HandController for " + gameObject +
                                   " could not add a hand component with device type " + deviceType + ".");

                    return false;
                }
            }

            if (hands.ContainsKey(deviceType))
            {
                Debug.LogError("More than one hand of device type " + deviceType +
                               " was initialised in the HandController for " + gameObject + ".");

                return false;
            }

            hand.DeviceType = deviceType;

            hands.Add(deviceType, hand);

            return true;
        }

        protected virtual void Update()
        {
            // Make sure this controller has a valid player number.
            if (!HandDataManager.IsPlayerNumberValid(playerNumber)
                && !HandDataManager.GetPlayerNumber(out playerNumber))
            {
                Log("The HandController for " + gameObject + " failed to get a player number.");

                return;
            }

            // Animate the hands.
            foreach (device_type_t deviceType in deviceTypes)
            {
                if (!HandDataManager.CanGetHandData(playerNumber, deviceType))
                {
                    Log("The HandController for " + gameObject + " could not get hand data for the hand with device type " + deviceType + ".");

                    continue;
                }

                ApolloHandData handData = HandDataManager.GetHandData(playerNumber, deviceType);

                hands[deviceType].AnimateHand(handData);
            }
        }

        protected virtual void Log(string message)
        {
            if (debugController)
            {
                Debug.Log(message);
            }
        }
    }
}
