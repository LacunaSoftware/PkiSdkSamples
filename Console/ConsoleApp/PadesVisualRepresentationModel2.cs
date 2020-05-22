using Lacuna.Pki.Pades;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ConsoleApp {
	internal class PadesVisualRepresentationModel2 {
		#nullable enable
		public PadesVisualImageModel? Image { get; set; }
		public PadesVisualPositioningModel? Position { get; set; }

		public class PadesVisualPositioningModel {
			public int? PageNumber { get; set; }
			public PadesMeasurementUnits? MeasurementUnits { get; set; }
			public double? Height { get; set; }
			public double? Width { get; set; }
			public double? RightMargin { get; set; }
			public double? BottomMargin { get; set; }

		}

		public class PadesVisualImageModel {
			public string? Path { get; set; }
			public int? Opacity { get; set; }
		}

		private PadesVisualManualPositioning getPosition() {
			if (Position != null) {
				return new PadesVisualManualPositioning() {
					MeasurementUnits = Position.MeasurementUnits ?? PadesMeasurementUnits.Centimeters,
					PageNumber = Position.PageNumber ?? -1,
					SignatureRectangle = new PadesVisualRectangle() {
						Bottom = Position.BottomMargin ?? 2,
						Right = Position.RightMargin ?? 0,
						Width = Position.Width ?? 6.5,
						Height = Position.Height ?? 1.1,
					}
				};
			} else {
				return new PadesVisualManualPositioning() {
					MeasurementUnits = PadesMeasurementUnits.Centimeters,
					PageNumber = -1,
					SignatureRectangle = new PadesVisualRectangle() {
						Bottom = 2,
						Right = 0,
						Width = 6.5,
						Height = 1.1,
					}
				};
			}
		}

		internal PadesVisualRepresentation2 ToEntity(string text) {
			
			PadesVisualImage? backgroud = null;
			if (Image != null && !string.IsNullOrEmpty(Image.Path)) {
				backgroud = new PadesVisualImage() { 
					Content = Util.GetResourceContent(Image.Path), 
					Opacity = Image.Opacity 
				};
			}

			return new PadesVisualRepresentation2() {
				Text = new PadesVisualText {
					CustomText = text,
					FontSize = 7,
				},
				Image = backgroud,
				Position = getPosition()
			};
		}
	}
}
