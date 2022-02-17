using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace AmpliaBlazorExample.Model;
public class PjModel {
	[Required]
	[StringLength(50, ErrorMessage = "Email deve ter no máximo 50 caracteres")]
	public string? EmailAddress { get; set; }
	[Required]
	[StringLength(50, ErrorMessage = "CNPJ deve ter 18 caracteres")]
	public string? Cnpj { get; set; }
	[Required]
	[StringLength(50, ErrorMessage = "Razão Social deve ter no máximo 50 caracteres")]
	public string? CompanyName { get; set; }
	[Required]
	public string? Cpf { get; set; }
	[Required]
	[StringLength(50, ErrorMessage = "Nome deve ter no máximo 50 caracteres")]
	public string? Name { get; set; }
	[Required]
	[StringLength(2, ErrorMessage = "País deve ter no máximo 2 caracteres")]
	public string? Country { get; set; }
	[Required]
	[StringLength(50, ErrorMessage = "Organização deve ter no máximo 50 caracteres")]
	public string? Organization { get; set; }
	[Required]
	public string OrganizationalUnits { get; set; }

	public PjModel() {
		OrganizationalUnits = "TI";
	}


	public static string FormatCNPJ(string cnpj) {
		if (string.IsNullOrWhiteSpace(cnpj))
			return string.Empty;
		if(!Int64.TryParse(cnpj, out _)) {
			return cnpj;
		}
		return Convert.ToUInt64(cnpj).ToString(@"00\.000\.000\/0000\-00");
	}
}

public static class TextUtil {
	public static string RemoveAccents(this string? text) {
		if (string.IsNullOrWhiteSpace(text)) {
			return string.Empty;
		}
		var sbReturn = new StringBuilder();
		var arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
		foreach (char letter in arrayText) {
			if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
				sbReturn.Append(letter);
		}
		return sbReturn.ToString();
	}

	public static string FormatCPF(string cpf) {
		if (string.IsNullOrWhiteSpace(cpf))
			return string.Empty;
		if (!Int64.TryParse(cpf, out _)) {
			return cpf;
		}
		return Convert.ToUInt64(cpf).ToString(@"000\.000\.000\-00");
	}
	public static string RemoveFormat(string str) {
		return str.Replace(".", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty);
	}

}
