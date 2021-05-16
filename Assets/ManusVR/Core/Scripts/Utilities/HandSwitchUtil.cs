using UnityEngine;

namespace ManusVR.Core.Utilities
{
    public class HandSwitchUtil : MonoBehaviour
    {
        [Tooltip("Which key the user should press to switch the Vive tracker handedness.")]
        public KeyCode handSwitchCode = KeyCode.R;

        void Update()
        {
#if MANUSVR_DEFINE_STEAMVR_PLUGIN_1_2_2_OR_NEWER
            if (Input.GetKeyDown(handSwitchCode))
            {
                var controllers = FindObjectsOfType<SteamVR_TrackedObject>();
                if (controllers.Length != 2)
                    return;

                var firstIndex = controllers[0].index;
                controllers[0].index = controllers[1].index;
                controllers[1].index = firstIndex;
            }
#endif

        }
    }
}
