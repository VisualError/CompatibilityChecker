using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using CompatibilityChecker.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CompatibilityChecker
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Lethal Company.exe")]
    public class ModNotifyBase : BaseUnityPlugin
    {
        public static ModNotifyBase instance;
        public static ManualLogSource logger;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        public static Dictionary<string, BepInEx.PluginInfo> ModList = new Dictionary<string, BepInEx.PluginInfo>();
        public static string ModListString;
        public static string[] ModListArray;
        private void Awake()
        {
            ModList = Chainloader.PluginInfos;
            foreach(BepInEx.PluginInfo info in ModList.Values)
            {
                ModListString += $"{info.Metadata.GUID}/@/";
            }
            ModListString = ModListString.Remove(ModListString.Length - 3, 3); // :3
            ModListArray = ModListString.Split("/@/");
            if (instance == null)
            {
                instance = this;
                logger = Logger;
            }
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogInfo("Modded servers will now notify you what mods are needed.");
            harmony.PatchAll(typeof(ModNotifyBase));
            harmony.PatchAll(typeof(PlayerJoinNetcode));
        }
    }
}
