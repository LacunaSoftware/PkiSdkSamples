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
	 * Pag 11
	 * 
	 * OID = 2.16.76.1.10.2 e conteúdo = nas primeiras 40 (quarenta) posições, o nome
	 * da instituição de ensino; nas 15 (quinze) posições subsequentes, o grau de
	 * escolaridade; nas 30 (trinta) posições subsequentes, o nome do curso, nas 20 (vinte)
	 * posições subsequentes, o município da instituição e nas 2 (duas) posições
	 * subsequentes, a UF domunicípio.
	 */
	class CieStudentData {

		public const string Oid = "2.16.76.1.10.2";

		public string InstituicaoEnsino { get; set; }
		public string GrauEscolaridade { get; set; }
		public string Curso { get; set; }
		public string InstituicaoEnsinoCidade { get; set; }
		public string InstituicaoEnsinoUF { get; set; }


		public byte[] Encode() {

			// Normalize (trim, remove punctuations and diacritics (accents), pad or crop if needed)

			var normInstituicaoEnsino = normalizeText(InstituicaoEnsino, 40);
			var normGrauEscolaridade = normalizeText(GrauEscolaridade, 15);
			var normCurso = normalizeText(Curso, 30);
			var normInstituicaoEnsinoCidade = normalizeText(InstituicaoEnsinoCidade, 20);
			var normInstituicaoEnsinoUF = normalizeText(InstituicaoEnsinoUF, 2);

			// Encode string

			var content = new StringBuilder();
			content.Append(normInstituicaoEnsino);
			content.Append(normGrauEscolaridade);
			content.Append(normCurso);

			/*
			 * "As 10 (dez) posições das informações sobre órgão emissor do RG e UF referem-se
			 * ao tamanho máximo, devendo ser utilizadas apenas as posições necessárias
			 * ao seu armazenamento, da esquerda para a direita. O mesmo se aplica às 22
			 * (vinte e duas) posições das informações sobre município e UF da instituição de
			 * ensino;"
			 */
			content.Append(normInstituicaoEnsinoCidade.Trim());
			content.Append(normInstituicaoEnsinoUF.Trim());

			// Encode string as PrintableString

			return Asn1Util.DerEncodePrintableString(content.ToString());
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
