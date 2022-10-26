using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Text.RegularExpressions.Regex;

namespace Lacuna.SignerService;
public static class Util {
	public static IEnumerable<string> Split(this DirectoryInfo path) {
		if (path == null)
			throw new ArgumentNullException("path");
		if (path.Parent != null)
			foreach (var d in Split(path.Parent))
				yield return d;
		yield return path.Name;
	}

	public static string GetOnlyDigits(this string? str) {
		if (str == null) {
			return string.Empty;
		}
		return Match(str, @"\d+\.*\d*").Value;
	}

	public static int ToInt(this string value, int defaultValue = 0) {
		return int.TryParse(value, out var result) ? result : defaultValue;
	}
	public static double ToDouble(this string value, double defaultValue = 0) {
		return double.TryParse(value, out var result) ? result : defaultValue;
	}

	public static PadesVisualRepresentation2 GetVisualRepresentation(Lacuna.Pki.PKCertificate cert, IConfiguration configuration, ILogger logger) {
		var section = configuration.GetSection("PadesVisualRepresentation");

		// Create a visual representation.
		var visualRepresentation = new PadesVisualRepresentation2() {

			// Text of the visual representation.
			Text = new PadesVisualText() {
				CustomText = $"Assinado por {cert.SubjectDisplayName} ({cert.PkiBrazil.CPF})",
				FontSize = 9.0,
				IncludeSigningTime = true,
				HorizontalAlign = PadesTextHorizontalAlign.Left,
				Container = new PadesVisualRectangle() { Left = 0.2, Top = 0.2, Right = 0.2, Bottom = 0.2 }
			},
			Position = new PadesVisualManualPositioning() {
				PageNumber = (section["PageNumber"] ?? string.Empty).ToInt(-1),
				SignatureRectangle = new PadesVisualRectangle() {
					Width = (section["Width"] ?? string.Empty).ToDouble(6.5),
					Height = (section["Height"] ?? string.Empty).ToDouble(1.1),
					Right = (section["Right"] ?? string.Empty).ToDouble(0),
					Bottom = (section["Bottom"] ?? string.Empty).ToDouble(2),
				},
				MeasurementUnits = PadesMeasurementUnits.Centimeters,
			}
		};
		if (File.Exists(section["SignImagePath"] ?? string.Empty)) {
			visualRepresentation.Image = new PadesVisualImage() {
				// We'll use as background the image in Content/PdfStamp.png
				Content = File.ReadAllBytes(section["SignImagePath"]!),
				// Align image to the right horizontally.
				HorizontalAlign = PadesHorizontalAlign.Right,
				// Align image to center vertically.
				VerticalAlign = PadesVerticalAlign.Center
			};
		} else {
			logger.LogError("GetVisualRepresentationForPkiSdk Image {file} not found", section["SignImagePath"]);
		}

		// Position of the visual representation. We get the footnote position preset and customize it.
		//var visualPositioning = PadesVisualAutoPositioning.GetFootnote();
		//visualPositioning.Container.Height = 4.94;
		//visualPositioning.SignatureRectangleSize.Width = 8.0;
		//visualPositioning.SignatureRectangleSize.Height = 4.94;
		//visualRepresentation.Position = visualPositioning;
		return visualRepresentation;
	}
	public static string Sha256(this string value) {
		var sb = new StringBuilder();
		using (var hash = SHA256.Create()) {
			Encoding enc = Encoding.UTF8;
			var result = hash.ComputeHash(enc.GetBytes(value));
			foreach (var b in result)
				sb.Append(b.ToString("x2"));
		}
		return sb.ToString();
	}
}
