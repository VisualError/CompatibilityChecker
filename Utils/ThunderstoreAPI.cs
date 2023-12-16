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

		public long TotalDownloads;

		[JsonProperty("is_deprecated")]
		public bool IsDeprecated;
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

        public static async Task InitializeThunderstorePackagesAsync(Action onComplete)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    string jsonResponse = await httpClient.GetStringAsync(ApiBaseUrl);
                    packages = JsonConvert.DeserializeObject<List<Package>>(jsonResponse);
                    ModNotifyBase.Logger.LogInfo($"ThunderstoreAPI Initialized! Got packages: {packages.Count()}");
					foreach(Package pack in packages)
                    {
						foreach(Version ver in pack.Versions)
                        {
							pack.TotalDownloads += ver.Downloads;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
				ModNotifyBase.Logger.LogError($"Failed to initialize Thunderstore packages. Error: {ex.Message}");
            }

            onComplete?.Invoke();
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
			//string replaced = Regex.Replace(package.Name, @"[\s_-]+(\w)", m => m.Groups[0].Value.Substring(0, 0) + m.Groups[1].Value.ToUpper());
			string[] packageSplit = Regex.Split(package.Name, @"(?<=[a-z])(?=[A-Z])|[\s_\-]");
			string[] searchStringSplit = Regex.Split(searchString, @"(?<=[a-z])(?=[A-Z])|[\s_\-]");
			int minCommonKeywords = packageSplit.Length > searchStringSplit.Length ? searchStringSplit.Length/2 : packageSplit.Length/2; // Set your desired threshold here
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
				if (noSpace.Equals(searchString, StringComparison.OrdinalIgnoreCase) || searchString.Contains(noSpace, StringComparison.OrdinalIgnoreCase) || noSpace.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                {
					if(value == null)
                    {
						value = pkg;
                    }
                    if (pkg.TotalDownloads >= value?.TotalDownloads) 
					{
						value = pkg;
					}
				}
			}
			return value;
		}

		public static Package GetPackage(string searchString, BepInEx.PluginInfo info)
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
                if (info.Metadata.GUID.Split(".").Contains(pkg.Owner))
                {
					owned = true;
                }
				string noSpace = Regex.Replace(pkg.Name, @"[\s\-_]", "");
				if (noSpace.Equals(searchString, StringComparison.OrdinalIgnoreCase) || noSpace.Equals(info.Metadata.Name, StringComparison.OrdinalIgnoreCase) || noSpace.Equals(info.Metadata.GUID, StringComparison.OrdinalIgnoreCase))
				{
                    if (owned)
                    {
						return pkg;
                    }
					value = pkg;
				}
				if (searchString.Contains(noSpace, StringComparison.OrdinalIgnoreCase) || noSpace.Contains(searchString, StringComparison.OrdinalIgnoreCase) || noSpace.Contains(info.Metadata.Name, StringComparison.OrdinalIgnoreCase) || noSpace.Contains(info.Metadata.GUID, StringComparison.OrdinalIgnoreCase))
				{
					value = pkg;
				}
				if(IsSimilar(pkg.Name, searchString))
                {
					value = pkg;
                }
			}
			return value;
		}

		public static IEnumerator Initialize(Action onComplete = null)
		{
			yield return InitializeThunderstorePackagesAsync(onComplete);
		}
	}
}
