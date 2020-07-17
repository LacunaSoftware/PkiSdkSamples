using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ConsoleApp {
	class Util {
		public static string GetResourcePath(string filename) {
			return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), $"Resources\\{filename}");
		}

		public static byte[] GetResourceContent(string filename) {
			return File.ReadAllBytes(GetResourcePath(filename));
		}

		public static string ToHex(byte[] value) {
			return BitConverter.ToString(value).ToLower().Replace("-", string.Empty);
		}
		public static Boolean FileExists(string path) {
			if (!File.Exists(path)) {
				Console.WriteLine($"File not found: {path}");
				return false;
			}
			return true;
		}

		public static void CheckTestDirectories(string documentsOutputDir, string signedDocumentsOutputDir) {
			if (!Directory.Exists(documentsOutputDir)) {
				Directory.CreateDirectory(documentsOutputDir);
			}

			if (!Directory.Exists(signedDocumentsOutputDir)) {
				Directory.CreateDirectory(signedDocumentsOutputDir);
			}
		}

		public static string GetSubjectFromKeywords(IEnumerable<string> keywords)
			=> string.Join(", ", keywords.Select(w => JsonConvert.SerializeObject(w)));

		public static string FormatForDocument(DateTimeOffset date, string timezone, string culture)
			=> TimeZoneInfo.ConvertTime(date, TimeZoneConverter.TZConvert.GetTimeZoneInfo(timezone))
				.ToString(CultureInfo.GetCultureInfo(culture));
	}
}
