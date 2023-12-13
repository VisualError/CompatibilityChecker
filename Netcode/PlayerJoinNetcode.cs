using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections;

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

        [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
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
                    componentInChildren.LobbyName.text = $"{(lobbyModded ? "[Compatibility Checker] " : "")}{lobbyName.Substring(0, Mathf.Min(lobbyName.Length, 40))}";
                    componentInChildren.playerCount.text = string.Format("{0} / 4", currentLobby.MemberCount);
                    componentInChildren.lobbyId = currentLobby.Id;
                    componentInChildren.thisLobby = currentLobby;
                }
            }
            RectTransform rect = __instance.levelListContainer.GetComponent<RectTransform>();
            float newWidth = rect.sizeDelta.x;
            float newHeight = Mathf.Max(0, (rect.childCount-1) * 42f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
            yield break;
        }
    }
}
