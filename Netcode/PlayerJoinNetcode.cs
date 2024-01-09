using BepInEx;
using CompatibilityChecker.MonoBehaviours;
using CompatibilityChecker.Utils;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CompatibilityChecker.Netcode
{
    [HarmonyPatch]
    class PlayerJoinNetcode
    {
        static Dictionary<string, string> serverModList = new Dictionary<string, string>();
        public static Vector2 old = Vector2.zero;
        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
        [HarmonyPrefix]
        public static bool OnLobbyCreated(ref GameNetworkManager __instance, Result result, ref Lobby lobby)
        {
            if(result == Result.OK)
            {
                CoroutineHandler.Instance.NewCoroutine(ModDataUtil.SetLobbyData(lobby));
            }
            return true;
        }


        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartHost))]
        [HarmonyPrefix]
        public static bool StartHost(ref GameNetworkManager __instance) // This part of the code was revealed to me in a dream..
        {
            if (!ModNotifyBase.loadedMods)
            {
                CoroutineHandler.Instance.NewCoroutine(FunnyJoinUtil.StartHost());
                CoroutineHandler.Instance.NewCoroutine(ModNotifyBase.InitializeModsCoroutine());
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GameNetworkManager), "LobbyDataIsJoinable")]
        [HarmonyPrefix]
        public static bool IsJoinable(ref Lobby lobby)
        {
            serverModList.Clear(); // Clear before joining any server.

            string oldModData = lobby.GetData("mods"); // this is for old ver compatibility.
            string mods = lobby.GetData("RyokuneCompatibilityChecker");
            serverModList = ModDataUtil.ProcessModData(oldModData, mods, lobby, serverModList);
            return true;
        }

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.SetLoadingScreen))] // TODO: REFACTOR.
        [HarmonyPostfix]
        public static void SetLoadingScreenPatch(ref MenuManager __instance, ref RoomEnter result, ref bool isLoading, ref string overrideMessage)
        {
            if (!ModNotifyBase.loadedMods)
            {
                return;
            }
            if (old == Vector2.zero)
            {
                old = __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta;
            }
            if (result != RoomEnter.Error || !overrideMessage.IsNullOrWhiteSpace())
            {
                __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = old;
                serverModList.Clear(); // Clear if the error is defined.
                return;
            }
            __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = old;
            Package CompatibilityCheckerPackage = ThunderstoreAPI.GetPackage(PluginInfo.PLUGIN_NAME);
            bool newVersion = VersionUtil.ConvertToNumber(CompatibilityCheckerPackage.Versions[0].VersionNumber) > VersionUtil.ConvertToNumber(PluginInfo.PLUGIN_VERSION);
            string closeString = newVersion ? $"New CompatibilityChecker update is available! v{CompatibilityCheckerPackage.Versions[0].VersionNumber} != {PluginInfo.PLUGIN_VERSION}" : "[ Close ]";
            if (newVersion & !isLoading)
            {
                __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(256.375f, 58.244f);
            }
            if (overrideMessage.IsNullOrWhiteSpace() && GameNetworkManager.Instance.disconnectionReasonMessage.IsNullOrWhiteSpace() && !isLoading)
            {
                if (serverModList.Count == 0)
                {
                    __instance.DisplayMenuNotification($"Failed to join crew!\n Missing mods:\nCan't display!", closeString);
                }
                else
                {
                    string lobbyName = GameNetworkManager.Instance?.currentLobby.Value.GetData("name");
                    var missingMods = serverModList.Except(ModNotifyBase.ModListArray).ToList();
                    var couldBeIncompatible = ModNotifyBase.ModListArray.Except(serverModList).ToList();
                    string list = missingMods == null || missingMods.Count == 0 ? "None..?" : string.Join("\n", missingMods);
                    string incompList = couldBeIncompatible == null || couldBeIncompatible.Count == 0 ? "None." : string.Join("\n\t\t", couldBeIncompatible);
                    __instance.DisplayMenuNotification($"Modded crew\n(Check logs/console for links)!\n Missing mods:\n{list}", closeString);
                    ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"\nMissing mods from lobby \"{lobbyName}\":");
                    ModDataUtil.LogModList(missingMods);
                    ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"Mods \"{lobbyName}\" may not be compatible with:\n\t\t{incompList}");
                    serverModList.Clear(); // Clear list after displaying information.
                }
            }
            if (newVersion)
            {
                ModNotifyBase.Logger.LogWarning($"NEW VERSION OF COMPATIBILITY CHECKER IS AVAILABE. PLEASE UPDATE TO {CompatibilityCheckerPackage.Versions[0].VersionNumber}");
            }
        }

        [HarmonyPatch(typeof(MenuManager), "connectionTimeOut")]
        [HarmonyPostfix]
        public static void timeoutPatch()
        {
            if (GameNetworkManager.Instance.currentLobby != null && serverModList.Count == 0)
            {
                string newData = GameNetworkManager.Instance.currentLobby.Value.GetData("RyokuneCompatibilityChecker");
                if (newData.IsNullOrWhiteSpace())
                {
                    string oldData = GameNetworkManager.Instance.currentLobby.Value.GetData("mods");
                    serverModList = StringCompressionUtil.SetListTo(serverModList, oldData);
                    return;
                }
                ModNotifyBase.Logger.LogWarning("UH OH! NOT GOOD!"); // really bad if this happens.
            }
        }
    }
}
