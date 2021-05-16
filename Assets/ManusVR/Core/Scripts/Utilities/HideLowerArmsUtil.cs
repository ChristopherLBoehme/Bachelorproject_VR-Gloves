using ManusVR.Core.Tracking;
using UnityEngine;

namespace ManusVR.Core.Utilities
{
    public class HideLowerArmsUtil : MonoBehaviour
    {
        public TrackingManager trackingManager;

        // Start is called before the first frame update
        void Start()
        {
            trackingManager = FindObjectOfType<TrackingManager>();
            if (trackingManager.trackerLocation == TrackingManager.TrackerLocation.Hand)
            {
                trackingManager.leftLowerArm.position += -Vector3.up * 100;
                trackingManager.rightLowerArm.position += -Vector3.up * 100;
            }
        }
    }
}
