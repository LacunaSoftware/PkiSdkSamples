using Lacuna.Pki;
using Lacuna.Pki.Pades;
using Lacuna.Pki.Pdf;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {

	public partial class PrinterFriendlyVersion : System.Web.UI.Page {

		// ####################################################################################################
		// Configuration of the Printer-Friendly version.
		// ####################################################################################################

		// Name of your website, with preceding article (article in lowercase).
		private const string VerificationSiteNameWithArticle = "my Verification Center";

		// Publicly accessible URL of your website. Preferably HTTPS.
		private const string VerificationSite = "http://localhost:57942/";

		// Format of the verification link, with "{0}" as the verification code placeholder.
		private const string VerificationLinkFormat = "http://localhost:57942/Check?c={0}";

		// "Normal" font size. Sizes of header fonts are defined based on this size.
		private const int NormalFontSize = 12;

		// CultureInfo to be used when converting dates to string.
		public static readonly CultureInfo CultureInfo = new CultureInfo("pt-BR");

		// Date format to be used when converting dates to string (for other formats, see:
		// https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings)
		public const string DateFormat = "g"; // Short date with short time.

		// Time zone to be used when converting dates to string (for other time zones, see:
		// https://docs.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones)
		public static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

		// Display name of the time zone chosen above.
		public const string TimeZoneDisplayName = "Brasilia timezone";

		// You may also change texts, positions and more by editing directly the method 
		// generatePrinterFriendlyVersion() below.
		// ####################################################################################################

		protected void Page_Load(object sender, EventArgs e) {

			// Get document ID from query string.
			var fileId = Request.QueryString["file"];
			
			// Our action only works if a fileId is given to work with.
			if (string.IsNullOrEmpty(fileId)) {
				// Return "Not Found" HTTP response.
				Response.StatusCode = 404;
				Response.End();
				return;
			}

			// Locate document and read content from storage.
			var fileContent = Storage.GetFile(fileId);

			// Check if doc already has a verification code registered on storage.
			var verificationCode = Storage.GetVerificationCode(fileId);
			if (verificationCode == null) {
				// If not, generate a code and register it.
				verificationCode = AlphaCode.Generate();
				Storage.SetVerificationCode(fileId, verificationCode);
			}

			// Generate the printer-friendly version.
			var pfvContent = generatePrinterFriendlyVersion(fileContent, verificationCode);

			// Return printer-friendly version as a downloadable file.
			Response.ContentType = "application/pdf";
			Response.AddHeader("Content-Disposition", "attachment; filename=printer-friendly.pdf");
			Response.BinaryWrite(pfvContent);
			Response.End();
		}

		private byte[] generatePrinterFriendlyVersion(byte[] pdfContent, string verificationCode) {

			// The verification code is generated without hyphens to save storage space and avoid copy-and-paste
			// problems. On the PDF generation, we use the "formatted" version, with hyphens (which will later
			// be discarded on the verification page).
			var formattedVerificationCode = AlphaCode.Format(verificationCode);

			// Build the verification link from the constant "VerificationLinkFormat" (see above) and the
			// formatted verification code.
			var verificationLink = string.Format(VerificationLinkFormat, formattedVerificationCode);

			// 1. Inspect signatures on the PDF.
			var signature = Lacuna.Pki.Pades.PadesSignature.Open(pdfContent);

			// 2. Create PDF with verification information from the signed PDF.
			var pdfMarker = new PdfMarker();

			// Build string with joined names of signers (see method getDisplayName() below).
			var signerNames = Util.JoinStringsPt(signature.Signers.Select(s => getDisplayName(s.Signer.SigningCertificate)));
			var allPagesMessage = string.Format("This document was digitally signed by {0}.\nTo verify the signatures go to {1} on {2} and inform the code {3}", signerNames, VerificationSiteNameWithArticle, VerificationSite, formattedVerificationCode);

			// ICP-Brasil logo on bottom-right corner of every page (except on the page which will be created at
			// the end of the document).
			pdfMarker.AddMark(new PdfMark() {
				PageOption = PdfMarkPageOptions.AllPages,
				Container = new PadesVisualRectangle() {
					Width = 1,
					Right = 1,
					Height = 1,
					Bottom = 1
				},
				Elements = new List<PdfMarkElement>() {
					new PdfMarkImage() {
						ImageContent = Storage.GetIcpBrasilLogoContent(),
						Opacity = 75
					}
				}
			});

			// Summary on bottom margin of every page (except on the page which will be created at the end of
			// the document).
			pdfMarker.AddMark(new PdfMark() {
				PageOption = PdfMarkPageOptions.AllPages,
				Container = new PadesVisualRectangle() {
					Height = 2,
					Bottom = 0,
					Left = 1.5,
					Right = 3.5
				},
				Elements = new List<PdfMarkElement>() {
					new PdfMarkText() {
						Texts = new List<PdfTextSection>() {
							new PdfTextSection() {
								Style = PdfTextStyle.Normal,
								Text = allPagesMessage
							}
						}
					}
				}
			});

			// Summary on right margin of every page (except on the page which will be created at the end of the
			// document), rotated 90 degrees counterclockwise (text goes up).
			pdfMarker.AddMark(new PdfMark() {
				PageOption = PdfMarkPageOptions.AllPages,
				Container = new PadesVisualRectangle() {
					Width = 2,
					Right = 0,
					Top = 1.5,
					Bottom = 3.5
				},
				Elements = new List<PdfMarkElement>() {
					new PdfMarkText() {
						Rotation = PdfMarkRotation.D90,
						Texts = new List<PdfTextSection>() {
							new PdfTextSection() {
								Style = PdfTextStyle.Normal,
								Text = allPagesMessage
							}
						}
					}
				}
			});

			// Create a "manifest" mark on a new page added on the end of the document. We'll add several
			// elements to this marks.
			var manifestMark = new PdfMark() {
				PageOption = PdfMarkPageOptions.NewPage,
				// This mark's container is the whole page with 1-inch margins.
				Container = new PadesVisualRectangle() {
					Top = 2.54,
					Bottom = 2.54,
					Right = 2.54,
					Left = 2.54
				}
			};

			// We'll keep track of our "vertical offset" as we add elements to the mark.
			double verticalOffset = 0;
			double elementHeight;

			elementHeight = 3;
			// ICP-Brasil logo on the upper-left corner.
			manifestMark.Elements.Add(new PdfMarkImage() {
				RelativeContainer = new PadesVisualRectangle() {
					Height = elementHeight,
					Top = verticalOffset,
					Width = elementHeight, /* Using elemengHeight as width because the image is square. */
					Left = 0
				},
				ImageContent = Storage.GetIcpBrasilLogoContent()
			});
			// QR Code with the verification link on the upper-right corner. We will generate a PdfMarkImage from
			// a QR Code generated using the QRCoder library.
			byte[] qrCodeImageContent;
			using (var qrGenerator = new QRCodeGenerator()) {
				using (var qrCodeData = qrGenerator.CreateQrCode(verificationLink, QRCodeGenerator.ECCLevel.M)) {
					using (var qrCode = new QRCode(qrCodeData)) {
						var qrCodeBitmap = qrCode.GetGraphic(10, Color.Black, Color.White, false);
						using (var buffer = new MemoryStream()) {
							qrCodeBitmap.Save(buffer, ImageFormat.Png);
							qrCodeImageContent = buffer.ToArray();
						};
					}
				}
			}
			manifestMark.Elements.Add(new PdfMarkImage() {
				RelativeContainer = new PadesVisualRectangle() {
					Height = elementHeight,
					Top = verticalOffset,
					Width = elementHeight, /* Using elemengHeight as width because the image is square. */
					Right = 0
				},
				ImageContent = qrCodeImageContent
			});
			// Header "SIGNATURES VERIFICATION" centered between ICP-Brasil logo and QR Code.
			manifestMark.Elements.Add(new PdfMarkText() {
				RelativeContainer = new PadesVisualRectangle() {
					Height = elementHeight,
					Top = verticalOffset + 0.2,
					Right = 0,
					Left = 0
				},
				Align = PadesHorizontalAlign.Center,
				Texts = new List<PdfTextSection>() {
					new PdfTextSection() {
						Style = PdfTextStyle.Normal,
						FontSize = NormalFontSize * 1.6,
						Text = "SIGNATURES\nVERIFICATION"
					}
				}
			});
			verticalOffset += elementHeight;

			// Verifical padding.
			verticalOffset += 1.7;

			// Header with verification code.
			elementHeight = 2;
			manifestMark.Elements.Add(new PdfMarkText() {
				RelativeContainer = new PadesVisualRectangle() {
					Height = elementHeight,
					Top = verticalOffset,
					Right = 0,
					Left = 0
				},
				Align = PadesHorizontalAlign.Center,
				Texts = new List<PdfTextSection>() {
					new PdfTextSection() {
						Style = PdfTextStyle.Normal,
						FontSize = NormalFontSize * 1.2,
						Text = string.Format("Verification Code: {0}", formattedVerificationCode)
					}
				}
			});
			verticalOffset += elementHeight;

			// Paragraph saying "this document was signed by the following signer etc" and mentioning the time zone of the
			// date/times below.
			elementHeight = 2.5;
			manifestMark.Elements.Add(new PdfMarkText() {
				RelativeContainer = new PadesVisualRectangle() {
					Height = elementHeight,
					Top = verticalOffset,
					Left = 0,
					Right = 0
				},
				Texts = new List<PdfTextSection>() {
					new PdfTextSection() {
						Style = PdfTextStyle.Normal,
						FontSize = NormalFontSize,
						Text = string.Format("This document was digitally signed by the following signers on the indicated dates ({0}):", TimeZoneDisplayName)
					}
				}
			});
			verticalOffset += elementHeight;

			// Iterate signers.
			foreach (var signer in signature.Signers) {

				elementHeight = 1.5;

				// Validate signature based on the PAdES Basic policy.
				var policyMapper = PadesPoliciesForGeneration.GetPadesBasic(Util.GetTrustArbitrator());
				var validationResults = signature.ValidateSignature(signer, policyMapper);

				// Green "check" or red "X" icon depending on result of validation for this signer.
				manifestMark.Elements.Add(new PdfMarkImage() {
					RelativeContainer = new PadesVisualRectangle() {
						Height = 0.5,
						Top = verticalOffset + 0.2,
						Width = 0.5,
						Left = 0
					},
					ImageContent = Storage.GetValidationResultIcon(validationResults.IsValid)
				});
				// Description of signer (see method getSignerDescription() below).
				manifestMark.Elements.Add(new PdfMarkText() {
					RelativeContainer = new PadesVisualRectangle() {
						Height = elementHeight,
						Top = verticalOffset,
						Left = 0.8,
						Right = 0
					},
					Texts = new List<PdfTextSection>() {
						new PdfTextSection() {
							Style = PdfTextStyle.Normal,
							FontSize = NormalFontSize,
							Text = getSignerDescription(signer)
						}
					}
				});

				verticalOffset += elementHeight;
			}

			// Some vertical padding from last signer.
			verticalOffset += 1;

			// Paragraph with link to veritifcation site and citing both the verification code above and the
			// verification link below.
			elementHeight = 2.5;
			manifestMark.Elements.Add(new PdfMarkText() {
				RelativeContainer = new PadesVisualRectangle() {
					Height = elementHeight,
					Top = verticalOffset,
					Right = 0,
					Left = 0
				},
				Texts = new List<PdfTextSection>() {
					new PdfTextSection() {
						Style = PdfTextStyle.Normal,
						FontSize = NormalFontSize,
						Text = string.Format("To verify the signatures, go to {0} on ", VerificationSiteNameWithArticle)
					},
					new PdfTextSection() {
						Color = Color.Blue,
						Style = PdfTextStyle.Normal,
						FontSize = NormalFontSize,
						Text = VerificationSite
					},
					new PdfTextSection() {
						Style = PdfTextStyle.Normal,
						FontSize = NormalFontSize,
						Text = " and inform the code above or follow the link below:"
					}
				}
			});
			verticalOffset += elementHeight;

			// Verification link.
			elementHeight = 1.5;
			manifestMark.Elements.Add(new PdfMarkText() {
				RelativeContainer = new PadesVisualRectangle() {
					Height = elementHeight,
					Top = verticalOffset,
					Right = 0,
					Left = 0
				},
				Align = PadesHorizontalAlign.Center,
				Texts = new List<PdfTextSection>() {
					new PdfTextSection() {
						Color = Color.Blue,
						Style = PdfTextStyle.Normal,
						FontSize = NormalFontSize,
						Text = verificationLink
					}
				}
			});
			pdfMarker.AddMark(manifestMark);

			// Prevent from throwing exception when the file to be marked already have a signature (default: true).
			pdfMarker.ThrowIfSignedPdf = false;
			// Note: Before applying the marks, all signature from the signed file will be removed.

			// Apply marks and return the printer-friendly PDF's content.
			byte[] pfvContent = pdfMarker.WriteMarks(pdfContent);
			return pfvContent;
		}

		private static string getDisplayName(PKCertificate c) {
			if (!string.IsNullOrEmpty(c.PkiBrazil.Responsavel)) {
				return c.PkiBrazil.Responsavel;
			}
			return c.SubjectName.CommonName;
		}

		private static string getDescription(PKCertificate c) {
			var text = new StringBuilder();
			text.Append(getDisplayName(c));
			if (!string.IsNullOrEmpty(c.PkiBrazil.CPF)) {
				text.AppendFormat(" (CPF {0})", c.PkiBrazil.CPF);
			}
			if (!string.IsNullOrEmpty(c.PkiBrazil.Cnpj)) {
				text.AppendFormat(", company {0} (CNPJ {1})", c.PkiBrazil.CompanyName, c.PkiBrazil.Cnpj);
			}
			return text.ToString();
		}

		private static string getSignerDescription(PadesSignerInfo signer) {
			var text = new StringBuilder();
			text.Append(getDescription(signer.Signer.SigningCertificate));
			if (signer.SigningTime != null) {
				var dateStr = TimeZoneInfo.ConvertTime(signer.SigningTime.Value, TimeZone).ToString(DateFormat, CultureInfo);
				text.AppendFormat(" on {0}", dateStr);
			}
			return text.ToString();
		}
	}
}