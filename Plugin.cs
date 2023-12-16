using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using CompatibilityChecker.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System.Reflection;
using CompatibilityChecker.Patches;
using System.Text.RegularExpressions;
using System.Text;
using System;
using System.Globalization;
using System.Collections;
using CompatibilityChecker.Utils;
using CompatibilityChecker.MonoBehaviours;
using UnityEngine.SceneManagement;

namespace CompatibilityChecker
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Lethal Company.exe")]
    public class ModNotifyBase : BaseUnityPlugin
    {
        public static ModNotifyBase instance;
        public static new ManualLogSource Logger;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        public static Dictionary<string, BepInEx.PluginInfo> ModList = new Dictionary<string, BepInEx.PluginInfo>();
        public static string ModListString;
        public static string[] ModListArray;
        public static bool loadedMods;
        public static string seperator = "/@/";
        public static SearchBox ServerSearchBox;
        public static string Text;
        private static void OnEndEdit(string newValue)
        {
            // Handle the search value change here
            Text = newValue;
            SteamLobbyManager lobbyManager = FindObjectOfType<SteamLobbyManager>();
            lobbyManager.LoadServerList();
        }
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
                        TMPro.TMP_InputField inputField = searchBoxObject.GetComponent<TMPro.TMP_InputField>();
                        if(inputField != null)
                        {
                            inputField.interactable = true;
                            inputField.placeholder.gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Search or Enter a room code...";
                            inputField.onSubmit = new TMPro.TMP_InputField.SubmitEvent();
                            inputField.onSubmit.AddListener(OnEndEdit);
                        }
                    }catch(Exception err)
                    {
                        Logger.LogError(err);
                    }
                }
            }
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += sceneLoad;
            if (instance == null)
            {
                instance = this;
                Logger = base.Logger;
            }
            CoroutineHandler.Instance.NewCoroutine(ThunderstoreAPI.Initialize());
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogInfo("Modded servers with CompatibilityChecker will now notify you what mods are needed.");
            harmony.PatchAll(typeof(ModNotifyBase));
            harmony.PatchAll(typeof(PlayerJoinNetcode));
            harmony.PatchAll(typeof(SteamLobbyManagerPatch));
        }

        public static IEnumerator InitializeModsCoroutine()
        {
            yield return new WaitUntil(() => ThunderstoreAPI.Packages != null);
            ModList = Chainloader.PluginInfos;
            StringBuilder messageBuilder = new StringBuilder();
            MenuManager menuManager = FindObjectOfType<MenuManager>();
            PlayerJoinNetcode.old = menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta != Vector2.zero ? menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta : PlayerJoinNetcode.old;
            int i = 0;
            int count = ModList.Count();
            menuManager.menuNotification.SetActive(true);
            foreach (BepInEx.PluginInfo info in ModList.Values)
            {
                i++;
                menuManager.menuNotificationText.text = $"Loading mods {i}/{count}";
                menuManager.menuNotificationButtonText.text = null;
                menuManager.menuNotificationButtonText.transform.parent.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                string noSpace = Regex.Replace(info.Metadata.Name, @"[\s\-_]", "");
                Package package = ThunderstoreAPI.GetPackage(noSpace, info);
                if (package != null)
                {
                    Logger.LogInfo($"[{info.Metadata.GUID}] Found package: {info.Metadata.Name}[{info.Metadata.Version}]\n\t\tDATA:\n\t\t--NAME: {package.FullName}\n\t\t--LINK: {package.PackageUrl}");
                    if (VersionUtil.ConvertToNumber(package.Versions[0].VersionNumber) > VersionUtil.ConvertToNumber(info.Metadata.Version.ToString()))
                    {
                        messageBuilder.AppendLine($"\n\t\t--Mod {info.Metadata.Name} [{info.Metadata.GUID}] v{info.Metadata.Version} does not equal the latest release!");
                        messageBuilder.AppendLine($"\t\t--Latest version: v{package.Versions[0].VersionNumber}");
                        messageBuilder.AppendLine($"\t\t--Link: {package.PackageUrl}");
                        messageBuilder.AppendLine($"\t\t--Full mod name: {package.FullName}");
                        messageBuilder.AppendLine($"\t\t--(If this is wrong, please ignore this.)");
                    }
                }
                if (package != null && package.Categories.Contains("Server-side"))
                {
                    if (package.Name == PluginInfo.PLUGIN_NAME && package.Versions[0].VersionNumber != PluginInfo.PLUGIN_VERSION)
                    {
                        string warning = $"Current {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} does not equal latest release v{package.Versions[0].VersionNumber}!\nPlease update to the latest version of {PluginInfo.PLUGIN_NAME}!!!";
                        Logger.LogWarning(warning);
                        FindObjectOfType<MenuManager>().DisplayMenuNotification(warning, null);
                    }
                    ModListString += $"{info.Metadata.Name}[{info.Metadata.Version}]{seperator}";
                }
                else if (package == null)
                {
                    Logger.LogWarning($"Couldn't find package: {info.Metadata.Name}[{info.Metadata.Version}]\n\t\t[{info.Metadata.GUID}]");
                    ModListString += $"{info.Metadata.GUID}[{info.Metadata.Version}]{seperator}";
                }
                yield return null;
            }
            if (messageBuilder.Length != 0)
            {
                Logger.LogWarning(messageBuilder.ToString());
            }
            ModListString = ModListString.Remove(ModListString.Length - 3, 3); // :3
            ModListArray = ModListString.Split(seperator);
            Logger.LogWarning($"Server-sided Mod List Count: {ModListArray.Count()}");
            menuManager.menuNotification.SetActive(false);
            loadedMods = true;

            yield break;
        }
    }
}
