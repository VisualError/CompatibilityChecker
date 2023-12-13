using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CompatibilityChecker
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

		private static readonly HttpClient httpClient = new HttpClient();

		public static async Task<Package[]> GetThunderstorePackages()
		{
			string jsonResponse = await httpClient.GetStringAsync(ApiBaseUrl);
			return JsonConvert.DeserializeObject<Package[]>(jsonResponse);
		}
	}
}
