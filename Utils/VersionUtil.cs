using UnityEngine;

namespace CompatibilityChecker.Utils
{
    internal static class VersionUtil
    {
        public static int ConvertToNumber(string version)
        {
            string versionWithoutDots = version.Replace(".", "");
            if (int.TryParse(versionWithoutDots, out int result))
            {
                return result;
            }
            else
            {
                ModNotifyBase.logger.LogWarning($"Error parsing version: {version}");
                return 0;
            }
        }
    }
}
