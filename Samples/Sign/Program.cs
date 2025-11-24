using Lacuna.Pki;
using Lacuna.Pki.Pades;
using Lacuna.Pki.Stores;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Lacuna.Sign;

internal class Program {
   private const string License = "AxAAd70BtFCUIUafM7AyGRYGXg8ATGFjdW5hIExhYiAyMDIyCACAPklY7SreCAgAAJi8INg03ggAAAAAAAAEAH8AAAAAAXYDqtgWS+eeOIgedaGiYYy05ffrz6wNQMNCQChqWmMbi83VHBgQfdjuaMsLMYSzJ/qk3jYvavWBnrOGhdAk7jWhI6JGV6z1GbIf/uDCUQSMqSuBUEPM62ha/A6wTTcyD+FUyNfoJCnsOBrHDi927pZoK4uxIUkJwueeMsKRJUFCQPLC6uG/gIKfsB+p2X4AsSblOnLVYP/zPIVA6qIC63HAdMEy6CiWhXJzZH7SmmsMs/X44OhhEnjNDmCEVgs8nHoHjCqOX1ywO7mNsOMZlAiiqoeknrAlLZrIHNsU5Lt0trecwRS/9tYwV2ztVymFEW5otA1rA4y1memKQ7ht1hc=\r\n\r\n";
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
             .WithExample("sign", "doc1.pdf", "CommonName", "-d 2025-01-03")
             .WithExample("sign", "doc1.pdf", "CommonName", "-d 2025-01-03", "-l");
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
      public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken) {
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

         [CommandOption("-l|--left")]
         public bool? Left { get; set; }
      }

      public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings, CancellationToken cancellationToken) {
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
         if (signDate != DateTimeOffset.MinValue) {
            var document = PdfReader.Open(settings.FileName);
            var info = document.Info;
            info.CreationDate = signDate.DateTime;
            info.ModificationDate = signDate.DateTime;
            document.Save(settings.FileName);
            Thread.Sleep(100);
         }
         var signedFile = Sign(settings.FileName, signingCert, settings.Left);
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


      public string Sign(string fileName, PKCertificateWithKey signingCert, bool? left) {
         var image = LoadEmbeddedImage("Lacuna.Sign.PdfStamp.png");

         var signer = new PadesSigner();
         var pdfBytes = File.ReadAllBytes(fileName);
         signer.SetCertificateValidationConfigurator(PkiUtil.OfflineSignerConfigurator);
         signer.SetSigningCertificate(signingCert);
         signer.SetPdfToSign(pdfBytes);
         var policy = PadesPolicySpec.GetBasic();
         policy.SignerSpecs.AttributeGeneration.EnableLtv = false;
         signer.SetPolicy(policy);
         PadesVisualRectangle signatureRectangle;
         if(left.HasValue && left.Value) {
            signatureRectangle = new PadesVisualRectangle() {
               Width = 6, // Largura = 7cm
               Height = 3, // Altura = 3cm
               Right = 2.50, // Distância da margem esquerda = 2.50cm
               Bottom = 2.50 // Distância da margem inferior = 2.50cm
            };
         } else  {
            signatureRectangle = new PadesVisualRectangle() {
               Width = 6, // Largura = 7cm
               Height = 3, // Altura = 3cm
               Left = 2.50, // Distância da margem esquerda = 2.50cm
               Bottom = 2.50 // Distância da margem inferior = 2.50cm
            };
         }

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


