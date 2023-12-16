using BepInEx;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using System.Linq;
using System.Collections;
using CompatibilityChecker.MonoBehaviours;
using CompatibilityChecker.Utils;
using System;
using UnityEngine.UI;
using TMPro;

namespace CompatibilityChecker.Patches
{
    class SteamLobbyManagerPatch
    {
        [HarmonyPatch(typeof(SteamLobbyManager), nameof(SteamLobbyManager.LoadServerList))]
        [HarmonyPostfix]
        [HarmonyAfter("me.swipez.melonloader.morecompany")]
        public static void loadserverListPatch(ref SteamLobbyManager __instance, ref Lobby[] ___currentLobbyList)
        {
            CoroutineHandler.Instance.NewCoroutine(BetterCompatibility(__instance));
        }

        public static IEnumerator BetterCompatibility(SteamLobbyManager lobbyManager)
        {
            yield return new WaitUntil(() => (lobbyManager.levelListContainer.GetComponent<RectTransform>().childCount - 1 != 0) && (lobbyManager.levelListContainer.GetComponent<RectTransform>().childCount - 1) == UnityEngine.Object.FindObjectsOfType(typeof(LobbySlot)).Length && !GameNetworkManager.Instance.waitingForLobbyDataRefresh);
            int i = 0;
            float lobbySlotPositionOffset = 0f;
            try
            {
                foreach (LobbySlot slot in (LobbySlot[])UnityEngine.Object.FindObjectsOfType(typeof(LobbySlot)))
                {
                    i++;
                    GameObject JoinButton = slot.transform.Find("JoinButton")?.gameObject;
                    if (JoinButton != null)
                    {
                        GameObject CopyCodeButton = UnityEngine.Object.Instantiate(JoinButton, JoinButton.transform.parent);
                        CopyCodeButton.SetActive(true);
                        RectTransform rectTransform = CopyCodeButton.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition -= new Vector2(78f, 0f);
                            CopyCodeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Code";
                            Button ButtonComponent = CopyCodeButton.GetComponent<Button>();
                            if (ButtonComponent != null)
                            {
                                ButtonComponent.onClick = new Button.ButtonClickedEvent();
                                ButtonComponent.onClick.AddListener(() =>
                                {
                                    string lobbyCode = slot.lobbyId.ToString();
                                    GUIUtility.systemCopyBuffer = lobbyCode;
                                    ModNotifyBase.Logger.LogInfo("Lobby code copied to clipboard: " + lobbyCode);
                                });
                            }
                        }
                    }
                    Lobby lobby = slot.thisLobby;
                    bool lobbyModded = !lobby.GetData("mods").IsNullOrWhiteSpace();
                    if (lobbyModded && !slot.LobbyName.text.Contains("[Checker]"))
                    {
                        slot.LobbyName.text = $"{slot.LobbyName.text} [Checker]";
                    }
                    if (!ModNotifyBase.Text.IsNullOrWhiteSpace() && !slot.LobbyName.text.Contains(ModNotifyBase.Text, StringComparison.OrdinalIgnoreCase)) // TODO: Implement a better search system. Maybe.
                    {
                        i--;
                        //slot.gameObject.SetActive(false);
                        UnityEngine.Object.DestroyImmediate(slot.gameObject);
                        continue;
                    }
                    slot.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, lobbySlotPositionOffset);
                    lobbySlotPositionOffset -= 42f;
                }
                RectTransform rect = lobbyManager.levelListContainer.GetComponent<RectTransform>();
                float newWidth = rect.sizeDelta.x;
                float newHeight = Mathf.Max(0, (i) * 42f); //rect.childCount - 1
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
            }
            catch(Exception err)
            {
                ModNotifyBase.Logger.LogError(err);
            }
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
                    GameObject gameObject = UnityEngine.Object.Instantiate(__instance.LobbySlotPrefab, __instance.levelListContainer);
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
