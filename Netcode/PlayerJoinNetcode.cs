using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

namespace CompatibilityChecker.Netcode
{
    [HarmonyPatch]
    class PlayerJoinNetcode
    {
        static string[] serverModList = null;
        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
        [HarmonyPrefix]
        public static bool OnLobbyCreated(Result result, ref Lobby lobby)
        {
            if(result == Result.OK)
            {
                lobby.SetData("mods", ModNotifyBase.ModListString);
                ModNotifyBase.logger.LogInfo($"Set lobby mods to: {ModNotifyBase.ModListString}");
            }
            return true;
        }
        [HarmonyPatch(typeof(GameNetworkManager), "LobbyDataIsJoinable")]
        [HarmonyPrefix]
        public static bool IsJoinable(ref Lobby lobby)
        {
            string mods = lobby.GetData("mods");
            if (!mods.IsNullOrWhiteSpace())
            {
                ModNotifyBase.logger.LogInfo("Lobby is modded. Getting mod List.");
                serverModList = mods.Split("/@/");
                foreach(string mod in serverModList)
                {
                    ModNotifyBase.logger.LogInfo(mod);
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.SetLoadingScreen))]
        [HarmonyPostfix]
        public static void SetLoadingScreenPatch(ref MenuManager __instance, ref RoomEnter result)
        {
            if (result == RoomEnter.Error && serverModList != null)
            {
                string[] missingMods = serverModList.Except(ModNotifyBase.ModListArray).ToArray();
                string list = missingMods == null || missingMods.Length == 0 ? "None..?" : string.Join("\n", missingMods);
                __instance.DisplayMenuNotification($"Failed to join modded crew!\n Missing mods:\n{list}", "[ Close ]");
                serverModList = null;
            }
        }
    }
}
