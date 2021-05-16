using System;
using System.Linq;
using UnityEditor;

namespace ManusVR.Core.ProjectManagement
{
    public class ManusVRCoreDefines 
    {
        const string MANUSVRCOREDEFINE_2_0_0 = "MANUSVR_DEFINE_CORE_PLUGIN_2_0_0_OR_NEWER";
        const string STEAMVRDEFINE_1_2_2 = "MANUSVR_DEFINE_STEAMVR_PLUGIN_1_2_2_OR_NEWER";

        /// <summary>
        /// Try to add the ManusVR Core defines to the project. It will only add it if it is not there yet.
        /// </summary>
        public static void TrySettingManusVRCoreDefine()
        {
            if (!ProjectContainsDefine(MANUSVRCOREDEFINE_2_0_0))
            {
                SetScriptingDefine(MANUSVRCOREDEFINE_2_0_0);
            }
        }

        /// <summary>
        /// Try to add the SteamVR defines to the project. It will only add it if it is not there yet.
        /// </summary>
        public static void TrySettingSteamVRDefine()
        {
            if (!ProjectContainsDefine(STEAMVRDEFINE_1_2_2) && IsSteamVRImported())
            {
                SetScriptingDefine(STEAMVRDEFINE_1_2_2);
            }
        }

        protected static bool ProjectContainsDefine(string define)
        {
            return GetScriptingDefineSymbols().Contains(define);
        }

        protected static void SetScriptingDefine(string define)
        {
            string symbols = GetScriptingDefineSymbols();
#if UNITY_EDITOR
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    BuildTargetGroup.Standalone, symbols + ";" + define);
#endif
        }

        protected static string GetScriptingDefineSymbols()
        {
#if UNITY_EDITOR
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
            return "";
        }

        protected static bool IsSteamVRImported()
        {
            var steamVRClassType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                        from type in assembly.GetTypes()
                        where type.Name == "SteamVR_ControllerManager"
                        select type).FirstOrDefault();

            return steamVRClassType != null;
        }

    }
}