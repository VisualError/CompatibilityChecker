using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CompatibilityChecker.Utils
{
    /// <summary>
    /// Static utilities class for StringCompression
    /// </summary>
    internal static class StringCompressionUtil
    {
        private static Dictionary<string, string> StringToShortened = new Dictionary<string, string>();


        public static Dictionary<string, string> SetListTo(Dictionary<string, string> dict ,string list)
        {
            var matches = Regex.Matches(list, @"(\w+(?:\.\w+)*)\[([\d\.]+)\]");
            foreach (Match match in matches)
            {
                string modName = match.Groups[1].Value;
                string modVersion = match.Groups[2].Value;
                if (!dict.ContainsKey(modName))
                {
                    dict.Add(modName, modVersion);
                }
            }
            return dict;
        }

        public static string ShortenAndCompressStringList(List<string> stringList)
        {
            // Replace mod names with shortened identifiers
            List<string> shortenedModList = new List<string>();
            foreach (var str in stringList)
            {
                // Generate a stable shortened identifier using the hash of the mod name
                string HashName = HashString(str);
                string shortenedIdentifier = HashName.Substring(0, 5) + HashName.Substring(HashName.Length - 5, 5);
                if (!StringToShortened.ContainsKey(str))
                {
                    StringToShortened[shortenedIdentifier] = str;
                }

                shortenedModList.Add(shortenedIdentifier);
            }

            // Concatenate the shortened mod names
            string concatenatedShortenedModList = string.Join(",", shortenedModList);
            return Compress(concatenatedShortenedModList);
        }

        public static string Compress(string str)
        {
            // Convert the concatenated string to bytes
            byte[] dataBytes = Encoding.UTF8.GetBytes(str);

            // Compress the data
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    gzipStream.Write(dataBytes, 0, dataBytes.Length);
                }

                // Convert the compressed bytes to a Base64-encoded string
                string compressedBase64 = Convert.ToBase64String(compressedStream.ToArray());
                return compressedBase64;
            }
        }

        private static string HashString(string modName)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(modName));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public static List<string> DecompressAndRecoverStringList(string compressedAndShortenedModList)
        {
            // Decompress the data using stream processing
            List<string> recoveredList = new List<string>();
            List<string> decompressedList = new List<string>(Decompress(compressedAndShortenedModList).Split(","));
            foreach(var shortenedString in decompressedList)
            {
                string originalString = ReverseLookup(shortenedString);
                recoveredList.Add(originalString);
            }

            return recoveredList;
        }

        public static string Decompress(string str)
        {
            byte[] compressedBytes = Convert.FromBase64String(str);
            using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
            using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(gzipStream, Encoding.UTF8))
            {
                StringBuilder decompressedText = new StringBuilder();
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    decompressedText.Append(line);
                }
                return decompressedText.ToString();
            }
        }

        private static string ReverseLookup(string shortenedIdentifier)
        {
            if (StringToShortened.ContainsKey(shortenedIdentifier))
            {
                return StringToShortened[shortenedIdentifier];
            }

            Console.WriteLine($"Warning: Unable to find original mod name for identifier {shortenedIdentifier}");
            return "Unknown"; // Handle the case where the identifier is not found
        }
    }
}
