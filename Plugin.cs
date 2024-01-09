﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using CompatibilityChecker.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using CompatibilityChecker.Patches;
using System.Text;
using System;
using System.Collections;
using CompatibilityChecker.Utils;
using CompatibilityChecker.MonoBehaviours;
using UnityEngine.SceneManagement;
using Steamworks.Data;
using Steamworks;
using UnityEngine.UI;
using BepInEx.Configuration;
using TMPro;

namespace CompatibilityChecker
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)] // TODO: REFACTOR.
    [BepInProcess("Lethal Company.exe")]
    public class ModNotifyBase : BaseUnityPlugin
    {
        // Configs
        public static ConfigEntry<int> ThunderstoreTimeout;
        public static ConfigEntry<int> ThunderstoreRetries;


        public static ModNotifyBase instance;
        public static new ManualLogSource Logger;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        public static Dictionary<string, BepInEx.PluginInfo> ModList = new Dictionary<string, BepInEx.PluginInfo>();
        public static string ModListString;
        public static string ModListStringOld;
        public static Dictionary<string, string> ModListArray = new Dictionary<string, string>();
        public static bool loadedMods;
        public static string seperator = "/@/";
        //public static SearchBox ServerSearchBox;
        public static string Text;
        public static TMP_InputField searchInputField;

        private void Awake()
        {
            // Set configs.
            ThunderstoreTimeout = Config.Bind("Thunderstore API", "Timeout", 300, "How long it'll take before the API times out in seconds.");
            ThunderstoreRetries = Config.Bind("Thunderstore API", "Retries", 10, "How many retries to access API before failing.");


            // Set instance.
            if (instance == null)
            {
                instance = this;
                Logger = base.Logger;
            }
            // Load Thunderstore API.
            CoroutineHandler.Instance.NewCoroutine(ThunderstoreAPI.Initialize());

            // Debugging purposes
            Application.logMessageReceived += (string log, string trace, LogType type) =>
            {
                if (type == LogType.Error)
                {
                    Logger.Log(LogLevel.All, $"[Unity Error Debugging] {log}\n{trace}");
                }
            };

            // TODO: Use harmony for this
            SceneManager.sceneLoaded += sceneLoad;

            // Plugin startup logic.
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogInfo("Modded servers with CompatibilityChecker will now notify you what mods are needed.");
            harmony.PatchAll(typeof(ModNotifyBase));
            harmony.PatchAll(typeof(PlayerJoinNetcode));
            harmony.PatchAll(typeof(SteamLobbyManagerPatch));
        }

        private static IEnumerator JoinLobby(ulong lobbyId, SteamLobbyManager lobbyManager)
        {
            bool found = false;
            Logger.LogWarning("Getting Lobby");
            Task<Lobby?> joinTask = SteamMatchmaking.JoinLobbyAsync(lobbyId);
            yield return new WaitUntil(() => joinTask.IsCompleted);
            if (joinTask.Result.HasValue)
            {
                Logger.LogWarning("Getting Lobby Value");
                Lobby lobby = joinTask.Result.Value;
                if (!lobby.GetData("vers").IsNullOrWhiteSpace())
                {
                    LobbySlot.JoinLobbyAfterVerifying(lobby, lobby.Id);
                    found = true;
                    Logger.LogWarning("Success!");
                }
            }
            else
            {
                Logger.LogWarning("Failed to join lobby.");
            }
            if (!found)
            {
                lobbyManager.LoadServerList();
            }else if (searchInputField != null)
            {
                searchInputField.text = "";
                Text = "";
            }
        }
        private static void OnEndEdit(string newValue)
        {
            // Handle the search value change here
            Text = newValue;
            SteamLobbyManager lobbyManager = FindObjectOfType<SteamLobbyManager>();
            if (ulong.TryParse(newValue, out ulong result))
            {
                CoroutineHandler.Instance.NewCoroutine(JoinLobby(result, lobbyManager));
                return;
            }
            lobbyManager.LoadServerList();
        }

        private static IEnumerator displayCopied(TextMeshProUGUI textMesh)
        {
            string oldtext = textMesh.text;
            if (GameNetworkManager.Instance.currentLobby.HasValue)
            {
                textMesh.text = "(Copied to clipboard!)";
                string id = GameNetworkManager.Instance.currentLobby.Value.Id.ToString();
                GUIUtility.systemCopyBuffer = id;
                Logger.LogWarning("Lobby code copied to clipboard: " + id);
            }
            else
            {
                textMesh.text = "Can't get Lobby code!";
            }
            yield return new WaitForSeconds(1.2f);
            textMesh.text = oldtext;
            yield break;
        }

        // TODO: Use harmony for this.
        static void sceneLoad(Scene sceneName, LoadSceneMode load)
        {
            if (sceneName.name == "MainMenu")
            {
                //GameObject obj = new GameObject("SearchBox");
                //ServerSearchBox = obj.AddComponent<SearchBox>();
                GameObject obj = GameObject.Find("/Canvas/MenuContainer/LobbyList/JoinCode");
                if (obj != null)
                {
                    try
                    {
                        GameObject searchBoxObject = Instantiate(obj.gameObject, obj.transform.parent);
                        searchBoxObject.SetActive(true);
                        searchInputField = searchBoxObject.GetComponent<TMP_InputField>();
                        searchInputField.interactable = true;
                        searchInputField.placeholder.gameObject.GetComponent<TextMeshProUGUI>().text = "Search or Enter a room code...";
                        searchInputField!.onEndEdit = new TMP_InputField.SubmitEvent();
                        searchInputField!.onEndTextSelection = new TMP_InputField.TextSelectionEvent();
                        searchInputField!.onSubmit = new TMP_InputField.SubmitEvent();
                        searchInputField!.onSubmit.AddListener(OnEndEdit);
                    }
                    catch(Exception err)
                    {
                        Logger.LogError(err);
                    }
                }
            }else if(sceneName.name == "SampleSceneRelay")
            {
                GameObject ResumeObj = GameObject.Find("/Systems/UI/Canvas/QuickMenu/MainButtons/Resume");
                if (ResumeObj != null)
                {
                    GameObject LobbyCodeObj = Instantiate(ResumeObj.gameObject, ResumeObj.transform.parent);
                    LobbyCodeObj.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, 182f);
                    TextMeshProUGUI LobbyCodeTextMesh = LobbyCodeObj.GetComponentInChildren<TextMeshProUGUI>();
                    LobbyCodeTextMesh.text = "> Lobby Code";
                    Button LobbyCodeButton = LobbyCodeObj.GetComponent<Button>();
                    LobbyCodeButton!.onClick = new Button.ButtonClickedEvent();
                    LobbyCodeButton!.onClick.AddListener(() =>
                    {
                        CoroutineHandler.Instance.NewCoroutine(displayCopied(LobbyCodeTextMesh));
                    });
                }
            }
        }


        // TODO: what the hell is this
        public static IEnumerator InitializeModsCoroutine()
        {
            MenuManager menuManager = FindObjectOfType<MenuManager>();
            PlayerJoinNetcode.old = menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta != Vector2.zero ? menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta : PlayerJoinNetcode.old;
            menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            menuManager.menuNotificationText.text = $"Waiting for Thunderstore list to be initialized.";
            menuManager.menuNotificationButtonText.text = null;
            menuManager.menuNotification.SetActive(true);
            if (ThunderstoreAPI.Failed)
            {
                CoroutineHandler.Instance.NewCoroutine(ThunderstoreAPI.Initialize());
            }
            yield return new WaitUntil(() => ThunderstoreAPI.Packages != null || ThunderstoreAPI.Failed);
            if (ThunderstoreAPI.Failed)
            {
                menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = PlayerJoinNetcode.old;
                menuManager.DisplayMenuNotification("Failed to initialize Thunderstore list. Check your internet connection or change the Configs!", "[ Back ]");
                CoroutineHandler.Instance.StopAllCoroutines();
                yield break;
            }
            ModList = Chainloader.PluginInfos;
            StringBuilder messageBuilder = new StringBuilder();
            int i = 0;
            int count = Chainloader.PluginInfos.Count;
            string warning = "";
            foreach (BepInEx.PluginInfo info in ModList.Values)
            {
                i++;
                menuManager.menuNotificationText.text = $"Loading mods {i}/{count}";
                menuManager.menuNotificationButtonText.text = null;
                menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                Package package = ThunderstoreAPI.GetPackage(info.Metadata.Name, info);
                if (package != null)
                {
                    Logger.LogInfo($"[{info.Metadata.GUID}] Found package ({i}/{count}): {info.Metadata.Name}[{info.Metadata.Version}]\n\t\tDATA:\n\t\t--NAME: {package.FullName}\n\t\t--LINK: {package.PackageUrl}");
                    if (VersionUtil.ConvertToNumber(package.Versions[0].VersionNumber) > VersionUtil.ConvertToNumber(info.Metadata.Version.ToString()))
                    {
                        messageBuilder.AppendLine($"\n\t\t--Mod {info.Metadata.Name} [{info.Metadata.GUID}] v{info.Metadata.Version} does not equal the latest release!");
                        messageBuilder.AppendLine($"\t\t--Latest version: v{package.Versions[0].VersionNumber}");
                        messageBuilder.AppendLine($"\t\t--Link: {package.PackageUrl}");
                        messageBuilder.AppendLine($"\t\t--Full mod name: {package.FullName}");
                        messageBuilder.AppendLine($"\t\t--(If this is wrong, please ignore this.)");
                    }
                    ModListString += $"{package.ShorterId}[{info.Metadata.Version}]";
                }
                if (package != null && package.Categories.Contains("Server-side", StringComparer.OrdinalIgnoreCase))
                {
                    if (package.Name == PluginInfo.PLUGIN_NAME && package.Versions[0].VersionNumber != PluginInfo.PLUGIN_VERSION)
                    {
                        warning = $"Current {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} does not equal latest release v{package.Versions[0].VersionNumber}!\nPlease update to the latest version of {PluginInfo.PLUGIN_NAME}!!!";
                    }
                    ModListStringOld += $"{info.Metadata.Name}[{info.Metadata.Version}]";
                }
                else if (package == null)
                {
                    Logger.LogWarning($"Couldn't find package: {info.Metadata.Name}[{info.Metadata.Version}]\n\t\t[{info.Metadata.GUID}]");
                }
                yield return null;
            }
            if (messageBuilder.Length != 0)
            {
                Logger.LogWarning(messageBuilder.ToString());
            }
            if (!warning.IsNullOrWhiteSpace())
            {
                Logger.LogWarning(warning);
                FindObjectOfType<MenuManager>().DisplayMenuNotification(warning, null);
            }
            ModListArray = StringCompressionUtil.SetListTo(ModListArray, ModListStringOld);
            ModListString = StringCompressionUtil.Compress(ModListString);
            Logger.LogWarning($"Server-sided Mod List Count: {ModListArray.Count}");
            menuManager.menuNotification.SetActive(false);
            loadedMods = true;
            //GUIUtility.systemCopyBuffer = string.Join(",", Chainloader.PluginInfos.Keys);
            yield break;
        }
    }
}
