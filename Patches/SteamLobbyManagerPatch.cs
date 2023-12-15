using BepInEx;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using System.Linq;
using System.Collections;
using CompatibilityChecker.MonoBehaviours;

namespace CompatibilityChecker.Patches
{
    class SteamLobbyManagerPatch
    {
        [HarmonyPatch(typeof(SteamLobbyManager), "LoadServerList")]
        [HarmonyPostfix]
        [HarmonyAfter("me.swipez.melonloader.morecompany")]
        public static void loadserverListPatch(ref SteamLobbyManager __instance, ref Lobby[] ___currentLobbyList)
        {
            CoroutineHandler.Instance.NewCoroutine(BetterCompatibility(__instance));
        }

        public static IEnumerator BetterCompatibility(SteamLobbyManager lobbyManager)
        {
            yield return new WaitUntil(() => (lobbyManager.levelListContainer.GetComponent<RectTransform>().childCount - 1 != 0) && (lobbyManager.levelListContainer.GetComponent<RectTransform>().childCount - 1) == UnityEngine.Object.FindObjectsOfType(typeof(LobbySlot)).Length && !GameNetworkManager.Instance.waitingForLobbyDataRefresh);
            foreach (LobbySlot slot in (LobbySlot[])UnityEngine.Object.FindObjectsOfType(typeof(LobbySlot)))
            {
                Lobby lobby = slot.thisLobby;
                bool lobbyModded = !lobby.GetData("mods").IsNullOrWhiteSpace();
                if (lobbyModded && !slot.LobbyName.text.Contains("[Checker]"))
                {
                    slot.LobbyName.text = $"{slot.LobbyName.text} [Checker]";
                }
            }
            RectTransform rect = lobbyManager.levelListContainer.GetComponent<RectTransform>();
            float newWidth = rect.sizeDelta.x;
            float newHeight = Mathf.Max(0, (rect.childCount - 1) * 42f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
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
                    componentInChildren.LobbyName.text = $"{lobbyName} {(lobbyModded ? "[Checker]" : "")}";
                    componentInChildren.playerCount.text = string.Format("{0} / 4", currentLobby.MemberCount);
                    componentInChildren.lobbyId = currentLobby.Id;
                    componentInChildren.thisLobby = currentLobby;
                }
            }
            yield break;
        }
    }
}
