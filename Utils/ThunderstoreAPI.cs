using CompatibilityChecker.MonoBehaviours;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompatibilityChecker.Utils
{
	[Serializable]
	public class Package
	{
		[JsonProperty("name")]
		public string Name;

		[JsonProperty("full_name")]
		public string FullName;

		[JsonProperty("owner")]
		public string Owner;

		[JsonProperty("package_url")]
		public string PackageUrl;

		[JsonProperty("categories")]
		public string[] Categories;

		[JsonProperty("versions")]
		public Version[] Versions;

		[JsonProperty("uuid4")]
		public string Uuid4;

		[JsonProperty("is_deprecated")]
		public bool IsDeprecated;

		public string ShorterId;

		public long TotalDownloads;
	}
	[Serializable]
	public class Version
	{
		[JsonProperty("name")]
		public string Name;

		[JsonProperty("full_name")]
		public string FullName;

		[JsonProperty("version_number")]
		public string VersionNumber;

		[JsonProperty("download_url")]
		public Uri DownloadUrl;

		[JsonProperty("downloads")]
		public long Downloads;

		[JsonProperty("website_url")]
		public Uri WebsiteUrl;
	}
	public static class ThunderstoreAPI
	{
		private const string ApiBaseUrl = "https://thunderstore.io/c/lethal-company/api/v1/package/";
		private static List<Package> packages;

		public static List<Package> Packages => packages;

		public static bool Failed { get; private set; }


		private static async Task RetryOnExceptionAsync(Func<Task> action, int retries)
		{
			for (int i = 1; i <= retries; i++)
			{
				try
				{
					ModNotifyBase.Logger.Log(BepInEx.Logging.LogLevel.All, $"Loading Thunderstore packages try {i}/{retries}");
					await action();
					break;
				}
				catch (Exception ex)
				{
					if (i == retries)
					{
						Failed = true;
						ModNotifyBase.Logger.LogError($"Failed to initialize Thunderstore packages. Error: {ex.Message}");
					}
				}
			}
		}

		public static async Task InitializeThunderstorePackagesAsync()
        {
			Failed = false;
			await RetryOnExceptionAsync(async () => 
			{
				using (HttpClient httpClient = new HttpClient())
				{
					httpClient.Timeout = TimeSpan.FromSeconds(ModNotifyBase.ThunderstoreTimeout.Value);
					string jsonResponse = await httpClient.GetStringAsync(ApiBaseUrl);
					ModNotifyBase.Logger.LogInfo($"Thunderstore API responded! Deserializing..");
					packages = JsonConvert.DeserializeObject<List<Package>>(jsonResponse);
					ModNotifyBase.Logger.LogInfo($"ThunderstoreAPI Initialized! Got packages: {packages.Count}");
					foreach (Package pack in packages)
					{
						foreach (Version ver in pack.Versions)
						{
							pack.TotalDownloads += ver.Downloads;
						}
						pack.ShorterId = pack.Uuid4.Substring(0, 5) + pack.Uuid4.Substring(pack.Uuid4.Length - 5, 5);
					}
				}
			}, ModNotifyBase.ThunderstoreRetries.Value);
        }

		public static bool IsSimilar(string s1, string s2)
		{
			string pattern = string.Join(".*?", s2.ToCharArray());
			Match match = Regex.Match(s1, pattern);
            if (match.Success)
            {
				return match.Success;
            }

			int[,] dp = new int[s1.Length + 1, s2.Length + 1];

			for (int i = 0; i <= s1.Length; i++)
			{
				for (int j = 0; j <= s2.Length; j++)
				{
					if (i == 0)
					{
						dp[i, j] = j;
					}
					else if (j == 0)
					{
						dp[i, j] = i;
					}
					else
					{
						dp[i, j] = Math.Min(
							Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
							dp[i - 1, j - 1] + (s1[i - 1] == s2[j - 1] ? 0 : 1)
						);
					}
				}
			}

			int maxLen = Math.Max(s1.Length, s2.Length);
			int distance = dp[s1.Length, s2.Length];

			// Adjust the threshold based on your requirements
			double similarity = 1.0 - (double)distance / maxLen;

			// Adjust the similarity threshold based on your needs
			return similarity > 0.54; // You can adjust this threshold
		}

		private static bool IsMatchingName(Package package, string searchString)
        {
			string[] packageSplit = Regex.Split(package.Name.Replace("lethalcompany","",StringComparison.OrdinalIgnoreCase), @"(?<=[a-z])(?=[A-Z])|[\s_\-]");
			string[] searchStringSplit = Regex.Split(searchString, @"(?<=[a-z])(?=[A-Z])|[\s_\-]");
			int minCommonKeywords = 2; // Set your desired threshold here
			int commonKeywordsCount = packageSplit.Count(token => searchStringSplit.Contains(token, StringComparer.OrdinalIgnoreCase));
			bool keywordCheck = commonKeywordsCount >= minCommonKeywords;
			return keywordCheck;
		}
		private static bool IsMatchingName(Package package, string searchString, string owner)
		{
			string[] nameTokens = Regex.Split(Regex.Replace(package.Name, @"[\s_]+(\w)", m => m.Groups[1].Value.ToUpper()).Replace(" ", ""), @"(?<=[a-z])(?=[A-Z])");
			string[] searchStringNameTokens = Regex.Split(searchString, @"(?<=[a-z])(?=[A-Z])");
			int minCommonKeywords = 2; // Set your desired threshold here
			int commonKeywordsCount = nameTokens.Count(token => searchStringNameTokens.Contains(token, StringComparer.OrdinalIgnoreCase));
			bool keywordCheck = commonKeywordsCount >= minCommonKeywords;
			return keywordCheck && owner.Contains(package.Owner, StringComparison.OrdinalIgnoreCase);
		}

		public static List<Package> GetPackages(string searchString)
		{
			return Packages?.Where(package => IsMatchingName(package, searchString)).ToList();
		}

		public static List<Package> GetPackages(string searchString, string owner)
		{
			return Packages?.Where(package => IsMatchingName(package, searchString, owner)).ToList();
		}

		public static Package GetPackage(string searchString)
		{
			Package value = null;
			foreach (Package pkg in Packages)
			{
				if (pkg.IsDeprecated)
				{
					continue;
				}
				string noSpace = Regex.Replace(pkg.Name, @"[\s\-_]", "");
				string noSpaceSearch = Regex.Replace(searchString, @"[\s\-_]", "");
				noSpace = noSpace.Replace("lethalcompany", "", StringComparison.OrdinalIgnoreCase);
				noSpaceSearch = noSpaceSearch.Replace("lethalcompany", "", StringComparison.OrdinalIgnoreCase);
				if (noSpace.Equals(noSpaceSearch, StringComparison.OrdinalIgnoreCase))
                {
					if(value == null)
                    {
						value = pkg;
                    }
                    if (pkg.TotalDownloads > value?.TotalDownloads) 
					{
						value = pkg;
					}
				}
			}
			return value;
		}

		public static Package GetPackageUsingID(string id)
        {
			return Packages?.FirstOrDefault(package => package.ShorterId.Equals(id));
        }

		public static Package GetPackage(string searchString, BepInEx.PluginInfo info) // If anyone can figure out a better search algorithm, please PR..
		{
			//return Packages?.FirstOrDefault(package => IsMatchingPackage(info, package, searchString) );
			Package value = null;
			bool owned = false;
			foreach(Package pkg in Packages)
            {
                if (pkg.IsDeprecated)
                {
					continue;
                }
                if (info.Metadata.GUID.Split(".").Contains(pkg.Owner, StringComparer.OrdinalIgnoreCase))
                {
					owned = true;
                }
				string noSpace = Regex.Replace(pkg.Name, @"[\s\-_]", "");
				string noSpaceSearch = Regex.Replace(searchString, @"[\s\-_]", "");
				noSpace = noSpace.Replace("lethalcompany", "", StringComparison.OrdinalIgnoreCase);
				noSpaceSearch = noSpaceSearch.Replace("lethalcompany", "", StringComparison.OrdinalIgnoreCase);
				if (noSpace.Equals(noSpaceSearch, StringComparison.OrdinalIgnoreCase) || noSpace.Equals(info.Metadata.Name, StringComparison.OrdinalIgnoreCase) || noSpace.Equals(info.Metadata.GUID, StringComparison.OrdinalIgnoreCase))
				{
                    if (owned)
                    {
						return pkg;
                    }
					if (value == null)
					{
						value = pkg;
					}
					if (pkg.TotalDownloads > value.TotalDownloads)
					{
						value = pkg;
					}
					continue;
				}
				if (noSpaceSearch.Contains(noSpace, StringComparison.OrdinalIgnoreCase) || noSpace.Contains(noSpaceSearch, StringComparison.OrdinalIgnoreCase) || noSpace.Contains(info.Metadata.Name, StringComparison.OrdinalIgnoreCase) || noSpace.Contains(info.Metadata.GUID, StringComparison.OrdinalIgnoreCase))
				{
					if (value == null)
					{
						value = pkg;
					}
					if (pkg.TotalDownloads > value.TotalDownloads)
					{
						value = pkg;
					}
					continue;
				}
				if(IsSimilar(noSpace, noSpaceSearch))
                {
					if (owned)
					{
						return pkg;
					}
					if (value == null)
                    {
						value = pkg;
                    }
                    if (pkg.TotalDownloads > value.TotalDownloads) 
					{
						value = pkg;
					}
				}
			}
			return value;
		}

		public static IEnumerator Initialize()
		{
			yield return InitializeThunderstorePackagesAsync();
		}
	}
}
