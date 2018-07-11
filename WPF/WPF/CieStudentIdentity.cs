using Lacuna.Pki.Asn1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SampleWpfApp {

	/**
	 * http://www.une.org.br/site/wp-content/uploads/2017/01/Padra%CC%83o-Nacional-2017.pdf
	 * 
	 * Pag 10
	 * 
	 * OID = 2.16.76.1.10.1 e conteúdo = nas primeiras 8 (oito) posições, a data de
	 * nascimento do titular, no formato ddmmaaaa; nas 11 (onze) posições subsequentes,
	 * o Cadastro de Pessoa Física (CPF) do titular; nas 15 (quinze) posições subsequentes,
	 * o número da matrícula do estudante; nas 15 (quinze) posições subsequentes, o 
	 * número do Registro Geral - RG do titular do atributo; nas 10 (dez) posições
	 * subsequentes, as siglas do órgão expedidor do RG e respectiva UF.
	 */
	class CieStudentIdentity {

		public const string Oid = "2.16.76.1.10.1";

		public DateTime DataNascimento { get; set; }
		public string Cpf { get; set; }
		public string Matricula { get; set; }
		public string RG { get; set; }
		public string RGEmissor { get; set; }
		public string RGEmissorUF { get; set; }

		public byte[] Encode() {

			// Normalize (trim, remove punctuations and diacritics (accents), pad or crop if needed)

			var normCpf = normalizeNumber(Cpf, 11);
			var normMatricula = normalizeNumber(Matricula, 15);
			var normRG = normalizeNumber(RG, 15);
			var normRGEmissor = normalizeText(RGEmissor, 8);
			var normRGEmissorUF = normalizeText(RGEmissorUF, 2);

			// Encode string

			var content = new StringBuilder();
			content.Append(DataNascimento.ToString("ddMMyyyy"));
			content.Append(normCpf);
			content.Append(normMatricula);
			content.Append(normRG);

			/**
			 * "Se o número do RG não estiver disponível, não se deve preencher o campo de
			 * órgão emissor e UF;"
			 * 
			 * "As 10 (dez) posições das informações sobre órgão emissor do RG e UF referem-se
			 * ao tamanho máximo, devendo ser utilizadas apenas as posições necessárias
			 * ao seu armazenamento, da esquerda para a direita. O mesmo se aplica às 22
			 * (vinte e duas) posições das informações sobre município e UF da instituição de
			 * ensino;
			 */
			if (normRG != "000000000000000") {
				content.Append(normRGEmissor.Trim());
				content.Append(normRGEmissorUF.Trim());
			}

			// Encode string as PrintableString

			return Asn1Util.DerEncodePrintableString(content.ToString());
		}

		public static CieStudentIdentity Decode(Lacuna.Pki.X509Attributes attributes) {
			return Decode(attributes.Get(Oid).EncodedValues[0]);
		}

		public static CieStudentIdentity Decode(byte[] encodedAttribute) {
			try {
				// decodifica o atributo como string
				var content = Asn1Util.DecodePrintableString(encodedAttribute);
				var cieId = new CieStudentIdentity();

				// nas primeiras 8 (oito) posições, a data de nascimento do titular, no formato ddmmaaaa;
				cieId.DataNascimento = DateTime.ParseExact(content.Substring(0, 8), "ddMMyyyy", null);

				// nas 11 (onze) posições subsequentes, o Cadastro de Pessoa Física (CPF) do titular;
				cieId.Cpf = content.Substring(8, 11);

				// nas 15 (quinze) posições subsequentes, o número da matrícula do estudante;
				cieId.Matricula = content.Substring(19, 15).TrimStart('0');

				// nas 15(quinze) posições subsequentes, o número do Registro Geral-RG do titular do atributo;
				// nas 10(dez) posições subsequentes, as siglas do órgão expedidor do RG e respectiva UF.
				cieId.RG = content.Substring(34, 15).TrimStart('0');
				if (!string.IsNullOrEmpty(cieId.RG)) {
					cieId.RGEmissor = content.Substring(49, content.Length - 49 - 2).Trim();
					cieId.RGEmissorUF = content.Substring(content.Length - 2, 2);
				}

				return cieId;

			} catch (Exception ex) {
				throw new FormatException("Error while decoding CIE student identity fields. Invalid format.", ex);
			}
		}

		private static string normalizeNumber(string s, int maxLen) {

			s = Regex.Replace((s ?? "").Trim().RemoveDiacritics().RemovePunctuation(), "[^0-9]", "");

			if (s.Length > maxLen) {
				return s.Substring(0, maxLen);
			} else if (s.Length < maxLen) {
				return s.PadLeft(maxLen, '0');
			} else {
				return s;
			}
		}

		private static string normalizeText(string s, int maxLen) {

			s = Regex.Replace((s ?? "").Trim().RemoveDiacritics().RemovePunctuation(), "[^A-Za-z0-9 ]", "");

			if (s.Length > maxLen) {
				return s.Substring(0, maxLen);
			} else if (s.Length < maxLen) {
				return s.PadRight(maxLen);
			} else {
				return s;
			}
		}
	}
}
