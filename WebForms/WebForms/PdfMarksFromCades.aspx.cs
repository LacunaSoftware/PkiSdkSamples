using Lacuna.Pki;
using Lacuna.Pki.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {

	public partial class PdfMarksFromCades : System.Web.UI.Page {

		protected void Page_Load(object sender, EventArgs e) {

			var cadesSignatureContent = Storage.GetSampleCadesSignatureOfPdf();
			var cadesSignature = Lacuna.Pki.Cades.CadesSignature.Open(cadesSignatureContent);

			var fontSize = 10;
			var markTexts = new List<PdfTextSection>() {
				new PdfTextSection() {
					Text = "Document digitally signed by ",
					FontSize = fontSize
				}
			};
			for (int i = 0; i < cadesSignature.Signers.Count; i++) {
				var signer = cadesSignature.Signers[i];
				if (i > 0) {
					markTexts.Add(new PdfTextSection() {
						Text = i < cadesSignature.Signers.Count - 1 ? ", " : " and ",
						FontSize = fontSize
					});
				}
				markTexts.Add(new PdfTextSection() {
					Text = getDisplayName(signer.SigningCertificate),
					FontSize = fontSize,
					Style = PdfTextStyle.Bold					
				});
			}

			var mark = new PdfMark() {
				MeasurementUnits = Lacuna.Pki.Pades.PadesMeasurementUnits.Centimeters,
				Container = new Lacuna.Pki.Pades.PadesVisualRectangle() {
					Right = 1,
					Top = 1,
					Bottom = 1,
					Width = 2
				},
				BorderColor = Color.Black,
				BorderWidth = 0.01,
				BackgroundColor = Color.FromArgb(80, Color.LightGreen), // Yellow with 10% opacity
				Elements = new List<PdfMarkElement>() {
					new PdfMarkText() {
						Rotation = PdfMarkRotation.D90,
						Texts = markTexts,
						RelativeContainer = new Lacuna.Pki.Pades.PadesVisualRectangle() {
							Left = 0.3,
							Top = 0.3,
							Bottom = 0.3,
							Right = 0.3
						}
					}
				}
			};

			var pdfMarker = new PdfMarker();
			pdfMarker.AddMark(mark);
			var pdfWithMarks = pdfMarker.WriteMarks(cadesSignature.GetEncapsulatedContent());

			Response.ContentType = "application/pdf";
			Response.AddHeader("Content-Disposition", "attachment; filename=printable-version.pdf");
			Response.BinaryWrite(pdfWithMarks);
			Response.End();
		}

		private string getDisplayName(PKCertificate certificate) {
			var text = new StringBuilder();
			if (!string.IsNullOrEmpty(certificate.PkiBrazil.Responsavel)) {
				text.Append(certificate.PkiBrazil.Responsavel);
			} else {
				text.Append(certificate.SubjectName.CommonName);
			}
			if (!string.IsNullOrEmpty(certificate.PkiBrazil.CPF)) {
				text.AppendFormat(" (CPF {0})", formatCpf(certificate.PkiBrazil.CPF));
			}
			return text.ToString();
		}

		private string formatCpf(string cpf) {
			if (string.IsNullOrEmpty(cpf) || cpf.Length != 11) {
				return cpf;
			}
			return string.Format("{0}.{1}.{2}-{3}", cpf.Substring(0, 3), cpf.Substring(3, 3), cpf.Substring(6, 3), cpf.Substring(9));
		}
	}
}
