using BepInEx;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompatibilityChecker.Utils
{
    class ModDataUtil
    {

        public static IEnumerator SetLobbyData(Lobby lobby)
        {
            if (GameNetworkManager.Instance.currentLobby != null)
            {
                lobby.SetData("RyokuneCompatibilityChecker", ModNotifyBase.ModListString);
                lobby.SetData("mods", ModNotifyBase.ModListStringOld);
                ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"Set {lobby.GetData("name")} new mod data to: {string.Join(",", ModNotifyBase.ModListArray)} ({ModNotifyBase.ModListArray.Count})");
            }
            yield break;
        }

        public static Dictionary<string, string> ProcessModData(string oldModData, string mods, Lobby lobby, Dictionary<string, string> serverModList)
        {
            ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"{lobby.GetData("name")} returned:");
            if (!mods.IsNullOrWhiteSpace())
            {
                string decompressedData = StringCompressionUtil.Decompress(mods);
                if (!decompressedData.IsNullOrWhiteSpace())
                {
                    var modMatches = Regex.Matches(decompressedData, @"(\w+(?:\.\w+)*)\[([\d\.]+)\]").Cast<Match>().Select(match => new KeyValuePair<string, string>(match.Groups[1].Value, match.Groups[2].Value)).ToList();
                    foreach(KeyValuePair<string, string> mod in modMatches)
                    {
                        Package modPackage = ThunderstoreAPI.GetPackageUsingID(mod.Key) ?? ThunderstoreAPI.GetPackage(mod.Key);
                        string modVersion = mod.Value ?? "No version number found";
                        if(modPackage != null)
                        {
                            if (!serverModList.ContainsKey(modPackage.Name))
                            {
                                serverModList.Add(modPackage.Name, modVersion);
                            }
                        }
                    }
                    ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, string.Join("\n", serverModList));
                    return serverModList;
                }
            }
            else if (!oldModData.IsNullOrWhiteSpace())
            {
                string seperated = oldModData.Replace(ModNotifyBase.seperator, "");
                serverModList = StringCompressionUtil.SetListTo(serverModList, seperated);
                ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, string.Join("\n", serverModList));
                return serverModList;
            }
            return serverModList;
        }

        public static void LogModList(List<KeyValuePair<string, string>> modList)
        {
            StringBuilder ServerSide = new StringBuilder();
            StringBuilder ClientSide = new StringBuilder();
            StringBuilder ServerSideAndClientSide = new StringBuilder();
            StringBuilder others = new StringBuilder();
            StringBuilder failedToLoad = new StringBuilder();
            ServerSide.AppendLine("Server-sided mods:");
            ClientSide.AppendLine("Client-sided mods:");
            ServerSideAndClientSide.AppendLine("Server-side & Client-sided mods:");
            others.AppendLine("Others:");
            failedToLoad.AppendLine("Failed to load package:");
            foreach (KeyValuePair<string,string> mod in modList)
            {
                Package modPackage = ThunderstoreAPI.GetPackageUsingID(mod.Key) ?? ThunderstoreAPI.GetPackage(mod.Key);
                string modVersion = mod.Value ?? "No version number found";
                string incompatibilityString = "";
                if (modPackage != null)
                {
                    if (ModNotifyBase.ModListArray.ContainsKey(modPackage.Name))
                    {
                        if (VersionUtil.ConvertToNumber(modVersion) != VersionUtil.ConvertToNumber(ModNotifyBase.ModListArray[modPackage.Name]))
                        {
                            incompatibilityString = $"(May be incompatible with your version v{ModNotifyBase.ModListArray[modPackage.Name]})";
                        }
                    }
                    string modString = $"\n\t--Name: {mod} {incompatibilityString}\n\t--Link: {modPackage.PackageUrl}\n\t--Version: {modVersion}\n\t--Downloads: {modPackage.Versions[0].Downloads}\n\t--Categories: [{string.Join(", ", modPackage.Categories)}]";
                    if (modPackage.Categories.Contains("Client-side", StringComparer.OrdinalIgnoreCase) && modPackage.Categories.Contains("Server-side", StringComparer.OrdinalIgnoreCase))
                    {
                        ServerSideAndClientSide.AppendLine(modString);
                        continue;
                    }
                    if (modPackage.Categories.Contains("Client-side", StringComparer.OrdinalIgnoreCase) && !modPackage.Categories.Contains("Server-side", StringComparer.OrdinalIgnoreCase))
                    {
                        ClientSide.AppendLine(modString);
                        continue;
                    }
                    if (!modPackage.Categories.Contains("Client-side", StringComparer.OrdinalIgnoreCase) && modPackage.Categories.Contains("Server-side", StringComparer.OrdinalIgnoreCase))
                    {
                        others.AppendLine(modString);
                        continue;
                    }
                    others.AppendLine(modString);
                }
                else
                {
                    failedToLoad.AppendLine($"----{mod.Key} {mod.Value}");
                }
            }
            ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"\n{others}");
            ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"\n{ClientSide}");
            ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"\n{ServerSideAndClientSide}");
            ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"\n{ServerSide}");
            ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"\n{failedToLoad}");
        }
    }
}
