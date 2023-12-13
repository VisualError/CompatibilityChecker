using BepInEx;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;


namespace CompatibilityChecker.Netcode
{
    [HarmonyPatch]
    class PlayerJoinNetcode
    {
        static string[] serverModList = null;
        static Coroutine currentCoroutine = null;

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
                serverModList = mods.Split("/@/");
                ModNotifyBase.logger.LogWarning($"{lobby.GetData("name")} returned:");
                foreach (string mod in serverModList)
                {
                    ModNotifyBase.logger.LogWarning(mod);
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
                ModNotifyBase.InitializeModList();
            }
        }

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.SetLoadingScreen))]
        [HarmonyPostfix]
        public static void SetLoadingScreenPatch(ref MenuManager __instance, ref RoomEnter result, ref bool isLoading)
        {
            if(ModNotifyBase.ModList.Count == 0)
            {
                ModNotifyBase.InitializeModList();
            }
            if (result == RoomEnter.Error && serverModList != null && !isLoading)
            {
                string[] missingMods = serverModList.Except(ModNotifyBase.ModListArray).ToArray();
                string list = missingMods == null || missingMods.Length == 0 ? "None..?" : string.Join("\n", missingMods);
                __instance.DisplayMenuNotification($"Failed to join modded crew (Check logs/console for links)!\n Missing mods:\n{list}", "[ Close ]");
                ModNotifyBase.logger.LogError($"\nMissing server-sided mods from lobby {GameNetworkManager.Instance?.currentLobby.Value.GetData("name")}:");
                foreach(string mod in missingMods)
                {
                    string errorString = mod;
                    Package package = ModNotifyBase.thunderStoreList?.FirstOrDefault(package => package.Name == mod);
                    if (package != null)
                    {
                        errorString = $"{mod} Link: ({package.PackageUrl}) (D: {package.Versions[0].Downloads})";
                    }
                    ModNotifyBase.logger.LogError(errorString);
                }
                serverModList = null;
            }
        }

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.DisplayMenuNotification))]
        [HarmonyPostfix]
        public static void DisplayMenuNotificationPostfix(ref MenuManager __instance)
        {
            ContentSizeFitter fitter = __instance.menuNotification.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = __instance.menuNotification.gameObject.AddComponent<ContentSizeFitter>();   
            }
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        [HarmonyPatch(typeof(MenuManager), "connectionTimeOut")]
        [HarmonyPostfix]
        public static void timeoutPatch()
        {
            if (GameNetworkManager.Instance.currentLobby != null && serverModList == null)
            {
                serverModList = GameNetworkManager.Instance.currentLobby.Value.GetData("mods")?.Split("/@/");
            }
        }

        [HarmonyPatch(typeof(SteamLobbyManager), "LoadServerList")]
        [HarmonyPostfix]
        [HarmonyAfter("me.swipez.melonloader.morecompany")]
        public static void loadserverListPatch(ref SteamLobbyManager __instance, ref Lobby[] ___currentLobbyList)
        {
            currentCoroutine = __instance.StartCoroutine(Thing(__instance));
        }

        public static IEnumerator Thing(SteamLobbyManager lobbyManager)
        {
            if(currentCoroutine != null)
            {
                yield break;
            }
            yield return new WaitUntil(() => (lobbyManager.levelListContainer.GetComponent<RectTransform>().childCount - 1 != 0) && (lobbyManager.levelListContainer.GetComponent<RectTransform>().childCount-1) == UnityEngine.Object.FindObjectsOfType(typeof(LobbySlot)).Length && !GameNetworkManager.Instance.waitingForLobbyDataRefresh);
            foreach(LobbySlot slot in (LobbySlot[])UnityEngine.Object.FindObjectsOfType(typeof(LobbySlot)))
            {
                Lobby lobby = slot.thisLobby;
                bool lobbyModded = !lobby.GetData("mods").IsNullOrWhiteSpace();
                if (lobbyModded && !slot.LobbyName.text.Contains("[Checker]"))
                {
                    slot.LobbyName.text = $"[Checker]{slot.LobbyName.text}";
                }
            }
            RectTransform rect = lobbyManager.levelListContainer.GetComponent<RectTransform>();
            float newWidth = rect.sizeDelta.x;
            float newHeight = Mathf.Max(0, (rect.childCount - 1) * 42f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
            currentCoroutine = null;
            yield break;
        }

        [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
        [HarmonyAfter("me.swipez.melonloader.morecompany")]
        [HarmonyPrefix]
        public static bool loadLobbyPrefixPatch(ref SteamLobbyManager __instance, ref Lobby[] ___currentLobbyList, ref float ___lobbySlotPositionOffset, ref IEnumerator __result)
        {
            __result = modifiedLoadLobbyIEnumerator(__instance, ___currentLobbyList, ___lobbySlotPositionOffset);
            return false;
        }

        public static IEnumerator modifiedLoadLobbyIEnumerator(SteamLobbyManager __instance, Lobby[] ___currentLobbyList, float ___lobbySlotPositionOffset)
        {
            string[] offensiveWords = new string[]
            {
            "nigger",
            "faggot",
            "n1g",
            "nigers",
            "cunt",
            "pussies",
            "pussy",
            "minors",
            "chink",
            "buttrape",
            "molest",
            "rape",
            "coon",
            "negro",
            "beastiality",
            "cocks",
            "cumshot",
            "ejaculate",
            "pedophile",
            "furfag",
            "necrophilia"
            };
            for (int i = 0; i < ___currentLobbyList.Length; i++)
            {
                Lobby currentLobby = ___currentLobbyList[i];
                Friend[] blockedUsers = SteamFriends.GetBlocked().ToArray();
                if (blockedUsers != null)
                {
                    foreach (Friend blockedUser in blockedUsers)
                    {
                        if (currentLobby.IsOwnedBy(blockedUser.Id))
                        {
                            continue;
                        }
                    }
                }
                string lobbyName = currentLobby.GetData("name");
                bool lobbyModded = !currentLobby.GetData("mods").IsNullOrWhiteSpace();
                if (!lobbyName.IsNullOrWhiteSpace())
                {
                    string lobbyNameLowercase = lobbyName.ToLower();
                    if (__instance.censorOffensiveLobbyNames)
                    {
                        foreach (string word in offensiveWords)
                        {
                            if (lobbyNameLowercase.Contains(word))
                            {
                                continue;
                            }
                        }
                    }
                    GameObject gameObject = Object.Instantiate(__instance.LobbySlotPrefab, __instance.levelListContainer);
                    gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, ___lobbySlotPositionOffset);
                    ___lobbySlotPositionOffset -= 42f;
                    LobbySlot componentInChildren = gameObject.GetComponentInChildren<LobbySlot>();
                    componentInChildren.LobbyName.text = $"{(lobbyModded ? "[Checker]" : "")}{lobbyName.Substring(0, Mathf.Min(lobbyName.Length, 40))}";
                    componentInChildren.playerCount.text = string.Format("{0} / 4", currentLobby.MemberCount);
                    componentInChildren.lobbyId = currentLobby.Id;
                    componentInChildren.thisLobby = currentLobby;
                }
            }
            yield break;
        }
    }
}
