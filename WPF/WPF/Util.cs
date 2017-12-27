using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SampleWpfApp {
	static class Util {

		public static DateTimeOffset GetMidnightOf(int year, int month, int day, TimeZoneInfo timeZone) {
			var date = new DateTime(year, month, day);
			var dateUtcOffset = timeZone.GetUtcOffset(date);
			return new DateTimeOffset(year, month, day, 23, 59, 59, dateUtcOffset);
		}

		public static string RemoveDiacritics(this string text) {
			// http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net
			var normalizedString = text.Normalize(NormalizationForm.FormD);
			var normalizedStringWithoutDcs = new String(normalizedString.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
			return normalizedStringWithoutDcs.Normalize(NormalizationForm.FormC);
		}

		public static string RemovePunctuation(this string s) {
			return new String(s.Where(c => !Char.IsPunctuation(c)).ToArray());
		}

		public static bool IsValidCpf(string cpf, bool ignorePunctuation = true) {

			if (String.IsNullOrWhiteSpace(cpf))
				return false;

			cpf = cpf.Trim();

			List<int> digits;

			if (ignorePunctuation) {

				cpf = cpf.RemovePunctuation();

				if (Regex.IsMatch(cpf, @"^\d{11}$")) {
					digits = cpf.Select(c => int.Parse(c.ToString())).ToList();
				} else {
					return false;
				}

			} else {

				if (Regex.IsMatch(cpf, @"^\d{11}$")) {
					digits = cpf.Select(c => int.Parse(c.ToString())).ToList();
				} else if (Regex.IsMatch(cpf, @"^\d{3}\.\d{3}\.\d{3}-\d{2}$")) {
					digits = cpf.Replace(".", "").Replace("-", "").Select(c => int.Parse(c.ToString())).ToList();
				} else {
					return false;
				}

			}

			var sum1 = Enumerable.Range(0, 9).Select(i => digits[i] * (10 - i)).Sum();
			var dv1 = 11 - (sum1 % 11);
			if (dv1 >= 10) {
				dv1 = 0;
			}

			if (digits[9] != dv1) {
				return false;
			}

			var sum2 = Enumerable.Range(0, 10).Select(i => digits[i] * (11 - i)).Sum();
			var dv2 = 11 - (sum2 % 11);
			if (dv2 >= 10) {
				dv2 = 0;
			}

			if (digits[10] != dv2) {
				return false;
			}

			return true;
		}
	}
}
