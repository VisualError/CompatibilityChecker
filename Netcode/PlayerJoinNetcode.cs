using BepInEx;
using BepInEx.Bootstrap;
using CompatibilityChecker.MonoBehaviours;
using CompatibilityChecker.Utils;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEngine;

namespace CompatibilityChecker.Netcode
{
    [HarmonyPatch]
    class PlayerJoinNetcode
    {
        static string[] serverModList = null;
        public static Vector2 old = Vector2.zero;

        public static IEnumerator SetLobbyData(Lobby lobby)
        {
            /*yield return new WaitUntil(() => ModNotifyBase.thunderStoreList != null);
            if(ModNotifyBase.ModList.Count() == 0)
            {
                ModNotifyBase.InitializeModList();
            }
            yield return new WaitUntil(() => ModNotifyBase.ModList.Count() == Chainloader.PluginInfos.Count() );*/
            if(GameNetworkManager.Instance.currentLobby != null)
            {
                lobby.SetData("mods", ModNotifyBase.ModListString);
                ModNotifyBase.logger.LogInfo($"Set {lobby.GetData("name")} mods to: {ModNotifyBase.ModListString}");
            }
            yield break;
        }

        /*public static IEnumerator LoadMods()
        {
            yield return new WaitUntil(() => ModNotifyBase.thunderStoreList != null);
            if (ModNotifyBase.ModList.Count() == 0)
            {
                ModNotifyBase.InitializeModList();
            }
            yield return new WaitUntil(() => ModNotifyBase.ModList.Count() == Chainloader.PluginInfos.Count());
            yield break;
        }*/

        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
        [HarmonyPrefix]
        public static bool OnLobbyCreated(ref GameNetworkManager __instance, Result result, ref Lobby lobby)
        {
            if(result == Result.OK)
            {
                CoroutineHandler.Instance.NewCoroutine(SetLobbyData(lobby));
            }
            return true;
        }


        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartHost))]
        [HarmonyPrefix]
        public static bool StartHost(ref GameNetworkManager __instance) // This part of the code was revealed to me in a dream..
        {
            if (!ModNotifyBase.loadedMods)
            {
                CoroutineHandler.Instance.NewCoroutine(Connect());
                CoroutineHandler.Instance.NewCoroutine(ModNotifyBase.InitializeModsCoroutine());
                return false;
            }
            else
            {
                return true;
            }
        }

        public static IEnumerator Connect()
        {
            yield return new WaitUntil(() => ModNotifyBase.loadedMods);
            GameNetworkManager.Instance.StartHost();
            yield break;
        }

        [HarmonyPatch(typeof(GameNetworkManager), "LobbyDataIsJoinable")]
        [HarmonyPrefix]
        public static bool IsJoinable(ref Lobby lobby)
        {
            string mods = lobby.GetData("mods");
            if (!mods.IsNullOrWhiteSpace())
            {
                serverModList = mods.Split(ModNotifyBase.seperator);
                ModNotifyBase.logger.LogWarning($"{lobby.GetData("name")} returned:");
                try
                {
                    foreach (string mod in serverModList)
                    {
                        string versionNumber = Regex.Match(mod, @"\[([\d.]+)\]").Success ? $"v{Regex.Match(mod, @"\[([\d.]+)\]").Value}" : "No version number found";
                        string yourVersionNumber = "";
                        if (Chainloader.PluginInfos.ContainsKey(mod))
                        {
                            yourVersionNumber = Chainloader.PluginInfos[mod]?.Metadata?.Version?.ToString();
                        }
                        string newString = mod;
                        string incompatibilityString =  !yourVersionNumber.IsNullOrWhiteSpace() && !versionNumber.Equals(yourVersionNumber) ? $" (May be incompatible with your version v{yourVersionNumber})" : "";
                        Package package = ThunderstoreAPI.GetPackage(mod);
                        if (package != null)
                        {
                            newString = $"{mod} {versionNumber}{incompatibilityString}";
                        }
                        ModNotifyBase.logger.LogWarning(newString);
                    }
                }catch(Exception er)
                {
                    ModNotifyBase.logger.LogError(er);
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(GameNetworkManager), "ConnectionApproval")]
        [HarmonyPostfix]
        public static void JoinLobbyPostfix()
        {
            if(ModNotifyBase.ModList.Count == 0)
            {
                CoroutineHandler.Instance.NewCoroutine(ModNotifyBase.InitializeModsCoroutine());
            }
        }

        /*[HarmonyPatch(typeof(MenuManager), nameof(MenuManager.SetLoadingScreen))]
        [HarmonyPrefix]
        public static bool LoadingScreenPrefix(ref MenuManager __instance, ref RoomEnter result)
        {
            if (old == Vector2.zero)
            {
                old = __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta;
            }
            __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = old;
            return true;
        }*/

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.SetLoadingScreen))]
        [HarmonyPostfix]
        public static void SetLoadingScreenPatch(ref MenuManager __instance, ref RoomEnter result, ref bool isLoading, ref string overrideMessage)
        {
            if (old == Vector2.zero)
            {
                old = __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta;
            }
            if (result != RoomEnter.Error || !overrideMessage.IsNullOrWhiteSpace())
            {
                __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = old;
                return;
            }
            __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = old;
            Package CompatibilityCheckerPackage = ThunderstoreAPI.GetPackage(PluginInfo.PLUGIN_NAME);
            bool newVersion = CompatibilityCheckerPackage.Versions[0].VersionNumber != PluginInfo.PLUGIN_VERSION;
            string closeString = newVersion ? $"New CompatibilityChecker update is available! v{CompatibilityCheckerPackage.Versions[0].VersionNumber} != {PluginInfo.PLUGIN_VERSION}" : "[ Close ]";
            if (newVersion & !isLoading)
            {
                __instance.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(256.375f, 58.244f);
            }
            if (ModNotifyBase.ModList.Count == 0)
            {
                CoroutineHandler.Instance.NewCoroutine(ModNotifyBase.InitializeModsCoroutine());
            }
            if (overrideMessage.IsNullOrWhiteSpace() && GameNetworkManager.Instance.disconnectionReasonMessage.IsNullOrWhiteSpace() && !isLoading)
            {
                if (serverModList == null)
                {
                    __instance.DisplayMenuNotification($"Failed to join Modded Crew!\n Missing mods:\nCan't display: Host does not have CompatibilityChecker!", closeString);
                }
                else
                {
                    string lobbyName = GameNetworkManager.Instance?.currentLobby.Value.GetData("name");
                    string[] missingMods = serverModList.Except(ModNotifyBase.ModListArray).ToArray();
                    string[] couldBeIncompatible = ModNotifyBase.ModListArray.Except(serverModList).ToArray();
                    string list = missingMods == null || missingMods.Length == 0 ? "None..?" : string.Join("\n", missingMods);
                    string incompList = couldBeIncompatible == null || couldBeIncompatible.Length == 0 ? "None." : string.Join("\n\t\t", couldBeIncompatible);
                    __instance.DisplayMenuNotification($"Modded crew\n(Check logs/console for links)!\n Missing mods:\n{list}", closeString);
                    ModNotifyBase.logger.LogError($"\nMissing server-sided mods from lobby \"{lobbyName}\":");
                    foreach (string mod in missingMods)
                    {
                        string errorString = mod;
                        Package modPackage = ThunderstoreAPI.GetPackage(mod);
                        if (modPackage != null)
                        {
                            string versionNumber = Regex.Match(mod, @"\[([\d.]+)\]").Success ? $"v{Regex.Match(mod, @"\[([\d.]+)\]").Value}" : "No version number found";
                            errorString = $"\n\t--Name: {mod}\n\t--Link: {modPackage.PackageUrl}\n\t--Version: {versionNumber}\n\t--Downloads: {modPackage.Versions[0].Downloads}\n\t--Categories: [{string.Join(", ", modPackage.Categories)}]";
                        }
                        ModNotifyBase.logger.LogError(errorString);
                    }
                    ModNotifyBase.logger.LogWarning($"Mods \"{lobbyName}\" may not be compatible with:\n\t\t{incompList}");
                    serverModList = null;
                }
            }
            if (newVersion)
            {
                ModNotifyBase.logger.LogWarning($"NEW VERSION OF COMPATIBILITY CHECKER IS AVAILABE. PLEASE UPDATE TO {CompatibilityCheckerPackage.Versions[0].VersionNumber}");
            }
        }

        [HarmonyPatch(typeof(MenuManager), "connectionTimeOut")]
        [HarmonyPostfix]
        public static void timeoutPatch()
        {
            if (GameNetworkManager.Instance.currentLobby != null && serverModList == null)
            {
                serverModList = GameNetworkManager.Instance.currentLobby.Value.GetData("mods")?.Split(ModNotifyBase.seperator);
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Singleton_OnClientConnectedCallback")]
        [HarmonyPostfix]
        public static void ConnectCallbackPatch()
        {
            Lobby? currentLobby = GameNetworkManager.Instance.currentLobby;
            if (StartOfRound.Instance != null && currentLobby != null)
            {
                if (currentLobby.Value.GetData("mods").IsNullOrWhiteSpace())
                {
                    ModNotifyBase.logger.LogInfo("Setting lobbys mods");
                    CoroutineHandler.Instance.NewCoroutine(SetLobbyData(currentLobby.Value));
                }
            }
        }
    }
}
