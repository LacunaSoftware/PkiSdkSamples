using Lacuna.Pki;
using Lacuna.Pki.Pades;
using Lacuna.Pki.Stores;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Lacuna.Sign;
internal class Program
{
	private const string License = "======  Ask for a license comercial@lacunasoftware.com ========";
	static int Main(string[] args) {
		if (License == "======  Ask for a license comercial@lacunasoftware.com ========") {
			AnsiConsole.Markup($"[red]License not configured![/][yellow]See source code![/]");
			return 0;
		}
		PkiConfig.LoadLicense(Convert.FromBase64String(License));
		var app = new CommandApp();
		app.Configure(config => {
			config.AddCommand<SignCommand>("sign")
				.WithDescription("Sign File Certificate")
				.WithExample("sign", "doc1.pdf", "CommonName")
				.WithExample("sign", "doc1.pdf", "CommonName", "-d 2025-01-03");
			config.AddCommand<ListCommand>("list")
				.WithDescription("List certificates with key")
				.WithExample("list");
			config.SetApplicationName("Sign");
			config.SetApplicationVersion("1.0.0");

		});
		return app.Run(args);

	}

	internal sealed class ListCommand : Command<ListCommand.Settings> {
		public class Settings : CommandSettings {
			[CommandArgument(0, "[Name]")]
			public string Name { get; set; }
		}
		public override int Execute(CommandContext context, Settings settings) {
			var table = new Table();
			table.AddColumn("#");
			table.AddColumn("Common Name");
			table.AddColumn(new TableColumn("CPF"));
			table.AddColumn(new TableColumn("Issuer"));


			var store = WindowsCertificateStore.LoadPersonalCurrentUser();
			var certsWithKey = store.GetCertificatesWithKey().Where(c => c.Certificate.PkiBrazil.CPF is not null).ToList();
			for (var index = 0; index < certsWithKey.Count; index++) {
				var c = certsWithKey[index];
				table.AddRow($"[green]{index}[/]", $"[green]{c.Certificate.SubjectName.CommonName}[/]", $"[green]{c.Certificate.PkiBrazil.CpfFormatted}[/]", $"[green]{c.Certificate.IssuerDisplayName}[/]");
			}

			AnsiConsole.Write(table);
			return 0;
		}
	}

	internal sealed class SignCommand : Command<SignCommand.Settings> {
		public sealed class Settings : CommandSettings {
			[Description("Sign Date.")]
			[CommandOption("-d|--SignDate")]
			public string? SignDate { get; init; }
			[Description("File to sign.")]
			[CommandArgument(0, "<FileName>")]
			public required string FileName { get; init; }

			[Description("Certificate Common Name")]
			[CommandArgument(1, "<Certificate>")]
			public required string Certificate { get; init; }
		}

		public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings) {
			if (!File.Exists(settings.FileName)) {
				AnsiConsole.Markup($"[red]File {settings.FileName} not found[/]");
				return 0;
			}
			var sw = Stopwatch.StartNew();
			AnsiConsole.Markup($"[yellow]Signing {settings.FileName} with certificate {settings.Certificate}[/]\n");

			var store = WindowsCertificateStore.LoadPersonalCurrentUser();
			var certsWithKey = store.GetCertificatesWithKey().Where(c => c.Certificate.PkiBrazil.CPF is not null).ToList();
			var signingCert = certsWithKey.FirstOrDefault(c => c.Certificate.SubjectName.CommonName == settings.Certificate);
			if (signingCert == null) {
				AnsiConsole.Markup($"[red]Certificate {settings.Certificate} not found![/]");
				return 0;
			}

			var signDate = DateTimeOffset.MinValue;
			if (settings.SignDate is not null) {
				if (!DateTimeOffset.TryParse(settings.SignDate, out signDate)) {
					AnsiConsole.Markup($"[red]Invalid date {settings.SignDate}[/]");
					return 0;
				}
				signDate = signDate.AddMilliseconds(new Random().NextInt64(10, 900));
				PkiConfig.TimeProvider = new TimeMachine(DateTimeOffset.Now - signDate);
			}
			var signedFile = Sign(settings.FileName, signingCert);
			if (signDate != DateTimeOffset.MinValue) {
				File.SetCreationTime(signedFile, signDate.DateTime);
				File.SetLastWriteTime(signedFile, signDate.DateTime);
				File.SetLastAccessTime(signedFile, signDate.DateTime);
			}

			AnsiConsole.Markup($"[yellow]{signedFile} Signed in {sw.Elapsed.TotalMilliseconds:N1} ms[/]\n");
			return 0;
		}

		public byte[] LoadEmbeddedImage(string resourceName) {
			var assembly = Assembly.GetExecutingAssembly();
			var allResources = assembly.GetManifestResourceNames();
			using var stream = assembly.GetManifestResourceStream(resourceName);
			using var memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}


		public string Sign(string fileName, PKCertificateWithKey signingCert) {
			var image = LoadEmbeddedImage("Lacuna.Sign.PdfStamp.png");

			var signer = new PadesSigner();
			var pdfBytes = File.ReadAllBytes(fileName);
			signer.SetCertificateValidationConfigurator(PkiUtil.OfflineSignerConfigurator);
			signer.SetSigningCertificate(signingCert);
			signer.SetPdfToSign(pdfBytes);
			var policy = PadesPolicySpec.GetBasic();
			policy.SignerSpecs.AttributeGeneration.EnableLtv = false;
			signer.SetPolicy(policy);
			var visual = new PadesVisualRepresentation2() {
				Position = new PadesVisualManualPositioning() {
					MeasurementUnits = PadesMeasurementUnits.Centimeters,
					PageNumber = -1,                                    // Define inserção na última página do documento
					SignatureRectangle = new PadesVisualRectangle() {
						Width = 6,                                      // Largura = 7cm
						Height = 3,                                     // Altura = 3cm
						Right = 2.50,                                    // Distância da margem esquerda = 2.50cm
						Bottom = 2.50                                   // Distância da margem inferior = 2.50cm
					}
				},
				Text = new PadesVisualText() {
					FontSize = 10,                                        // Tamanho da fonte = 10
					CustomText = $"Assinado digitalmente por\n{signingCert.Certificate.PkiBrazil.Responsavel}",
					IncludeSigningTime = true,
					Container = new PadesVisualRectangle() {                // Define container do texto
						Left = 0,
						Top = 0,
						Right = 1.5,
						Bottom = 0.5
					}
				},

				Image = new PadesVisualImage() {
					Content = image,
					HorizontalAlign = PadesHorizontalAlign.Right
				}
			};
			signer.SetVisualRepresentation(visual);

			signer.ComputeSignature();
			var signedPdf = signer.GetPadesSignature();
			var directory = Path.GetDirectoryName(fileName);
			var signedFileName = Path.Combine(directory ?? string.Empty, Path.GetFileNameWithoutExtension(fileName) + "-signed.pdf");
			File.WriteAllBytes(signedFileName, signedPdf);
			return signedFileName;
		}

	}

	internal class TimeMachine : IPkiTimeProvider {

		public DateTimeOffset Now => DateTimeOffset.Now - timeAgo;

		private readonly TimeSpan timeAgo;

		public TimeMachine(TimeSpan timeAgo) {
			this.timeAgo = timeAgo;
		}
	}

}


