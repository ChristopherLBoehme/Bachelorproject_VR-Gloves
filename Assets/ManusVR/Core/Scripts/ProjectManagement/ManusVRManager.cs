using UnityEngine;

namespace ManusVR.Core.ProjectManagement
{
    [ExecuteInEditMode]
    public class ManusVRManager : MonoBehaviour
    {
        [Tooltip("Automatically add the required defines to the project.")]
        public bool AutoManageDefines = true;

        public Transform LeftTargetController { get; private set; }
        public Transform RightTargetController { get; private set; }
        
        protected virtual void Awake()
        {
            if (AutoManageDefines)
            {
                ManusVRCoreDefines.TrySettingManusVRCoreDefine();
                ManusVRCoreDefines.TrySettingSteamVRDefine();
            }

            if (!Application.isPlaying) return;

            InitializeSteamVRTracking();
        }

        protected virtual bool InitializeSteamVRTracking()
        {
#if MANUSVR_DEFINE_STEAMVR_PLUGIN_1_2_2_OR_NEWER
            SteamVR_ControllerManager steamVRControllerManager = Component.FindObjectOfType<SteamVR_ControllerManager>();

            if (steamVRControllerManager == null)
            {
                return false;
            }

            LeftTargetController = steamVRControllerManager.left.transform;
            RightTargetController = steamVRControllerManager.right.transform;
            return true;
#endif
            return false;
        }
    }

}
