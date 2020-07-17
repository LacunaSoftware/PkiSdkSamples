using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Lacuna.Pki;
using Lacuna.Pki.Pades;
using Lacuna.Pki.Pkcs11;
using Lacuna.Pki.Stores;
using Newtonsoft.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using System.Security.Cryptography;
using iText.Pdfa;
using iText.IO.Colors;

namespace ConsoleApp {

	class Program {
		// ---------------------------------------------------------------------------------------
		// SET YOUR BINARY LICENSE BELOW
		private const string LicenseBase64 = "PASTE YOUR BASE64-ENCODED BINARY LICENSE HERE";
		//                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
		// ---------------------------------------------------------------------------------------
		static int filesSigned = 0;
		private static int fileErrors = 0;
		private static readonly string orginalTestPdf = "2MBytes.pdf";
		private static readonly string logFileName = "Console_Signer_Log";
		private static readonly string logFile = logFileName + ".txt";
		private static readonly string logOk = logFileName + ".ok";
		private static readonly string logErro = logFileName + ".erros";
		private static object okLock = new object();
		private static object logLock = new object();
		private static object errorLock = new object();

		public class Options {
			[Option('t', "test", Required = false, HelpText = "Generate and sign file")]
			public int Test { get; set; }

			[Option('r', "reprocess", Required = false, HelpText = "Sign remaing unsigned files")]
			public bool Reprocess { get; set; }

			[Option('s', "source", Required = false, HelpText = "Source directory")]
			public string SourceDir { get; set; }

			[Option('d', "destination", Required = false, HelpText = "Destination directory")]
			public string DestinationDir { get; set; }

			[Option('c', "certificate", Required = false, HelpText = "Signer certificate thumbprint")]
			public string CertThumbprint { get; set; }

			// 0,0 is the right bottom corner
			[Option('v', "visual-representation", Required = false, HelpText = "JSON with the signature visual representation configuration")]
			public string VisualRep { get; set; }

			[Option('f', "file", Required = false, HelpText = "Sign only this file")]
			public string File { get; set; }

			[Option('p', "token-pin", Required = false, HelpText = "Token's pin")]
			public string Pin { get; set; }

			[Option('m', "metadata", Required = false, HelpText = "JSON with the pdf's to be added metadata")]
			public string Metadata { get; set; }
		}


		static void Main(string[] args) {
			Console.WriteLine("===========================");
			Console.WriteLine(" CONSOLE SIGNER       0.0.1");
			Console.WriteLine("===========================");
			Console.WriteLine();

			if (File.Exists(logFile)) {
				File.Move(logFile, $"{logFileName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt");
			}
			Log("Begin");

			var parser = new Parser(with => {
				with.EnableDashDash = true;
				with.AutoHelp = true;
				with.HelpWriter = Console.Out;
			});
			var result = parser.ParseArguments<Options>(args);
			result.WithParsed(options => {
				Console.WriteLine("Parser Success- Creating Options with values:");
				Console.WriteLine("options.Source= {0}", options.SourceDir);
				Console.WriteLine("options.Destination= {0}", options.DestinationDir);
				Console.WriteLine("options.Test= {0}", options.Test);
				Console.WriteLine("options.Thumbprint= {0}", options.CertThumbprint);
				process(options);
			}).WithNotParsed(errs => {
				//if (errs.Any(e => e.Tag == ErrorType.MissingRequiredOptionError)) {
				//	Console.WriteLine($"Paramêtro ");
				//}
			});
		}

		static void process(Options options) {
			PkiConfig.LoadLicense(Convert.FromBase64String(LicenseBase64));
			var isTest = options.Test > 0;
			var testCount = options.Test;


			var documentsInputDir = options.SourceDir;
			var signedDocumentsOutputDir = options.DestinationDir;

			if (isTest) {
				Util.CheckTestDirectories(documentsInputDir, signedDocumentsOutputDir);
				DeleteFiles(documentsInputDir, signedDocumentsOutputDir);
				PdfGenerate(testCount, documentsInputDir);
			} else {
				if (!Directory.Exists(documentsInputDir) && string.IsNullOrWhiteSpace(options.File)) {
					Console.WriteLine($"Error! The directory was not found: {documentsInputDir}");
					return;
				}
				if (!Directory.Exists(signedDocumentsOutputDir) && string.IsNullOrWhiteSpace(options.File)) {
					Directory.CreateDirectory(signedDocumentsOutputDir);
				}

				Console.WriteLine();
			}

			// Signer certificate

			PKCertificateWithKey cert = null;
			var store = Pkcs11CertificateStore.Load("eTPKCS11.dll", new StaticLoginProvider(options.Pin));
			if (string.IsNullOrEmpty(options.CertThumbprint)) {
				List<PKCertificateWithKey> certificates;
				Console.WriteLine();
				Console.WriteLine("Listing Certificates...");
				if (string.IsNullOrEmpty(options.Pin)) {
					certificates = WindowsCertificateStore.LoadPersonalCurrentUser().GetCertificatesWithKey();
				} else {
					certificates = store.GetCertificatesWithKey();
				}
				for (var i = 0; i < certificates.Count; i++) {
					Console.WriteLine($"[{i}] {certificates[i].Certificate.SubjectDisplayName} (Issued by {certificates[i].Certificate.IssuerDisplayName})");
				}

				Console.WriteLine();
				Console.Write("Select the signer certificate: ");
				var indexstring = Console.ReadLine();
				if (!int.TryParse(indexstring, out var index)) {
					Console.WriteLine($"Error! Invalid index: {indexstring}");
					return;
				}

				cert = certificates[index];

			} else {
				var thumbprint = PkiUtil.DecodeHexString(options.CertThumbprint);
				if (string.IsNullOrEmpty(options.Pin)) {
					cert = WindowsCertificateStore.LoadPersonalCurrentUser().GetCertificatesWithKey().FirstOrDefault(c => c.Certificate.ThumbprintSHA1.SequenceEqual(thumbprint));
				} else {
					cert = store.GetCertificatesWithKey().FirstOrDefault(c => c.Certificate.ThumbprintSHA1.SequenceEqual(thumbprint));
				}
				if (cert == null) {
					Console.WriteLine($"Error! No certificate was found with thumbprint: {options.CertThumbprint}");
					return;
				}
			}

			Console.WriteLine($"Signer: {cert.Certificate.SubjectDisplayName} (thumbprint: {Util.ToHex(cert.Certificate.ThumbprintSHA1)})");

			Metadata metadata = null;
			if (!string.IsNullOrEmpty(options.Metadata) && Util.FileExists(options.Metadata))
			{
				try
				{
					var metadataContent = File.ReadAllBytes(options.Metadata);
					var metadataJson = Encoding.UTF8.GetString(metadataContent);
					metadata = JsonConvert.DeserializeObject<MetadataModel>(metadataJson).ToEntity();
				}
				catch (Exception ex)
				{
					Log(ex.ToString());
					Console.WriteLine($"Error parsing metadata file: {ex}");
				}
			}

			if (string.IsNullOrWhiteSpace(options.File)) {
				Console.WriteLine("Getting things ready.");
				Sign(cert, documentsInputDir, signedDocumentsOutputDir, options.Reprocess, options.VisualRep, metadata);
			} else {
				var visual = CreateVisualRepresentation(cert.Certificate, options.VisualRep, (metadata != null));
				var policy = GetSignaturePolicy().GetPolicy(cert.Certificate);
				policy.SignerSpecs.AttributeGeneration.EnableLtv = false;

				if (!SignFile(options.File, cert, policy, visual, metadata, "", "Signed_"+options.File)) {
					Console.WriteLine($"Error signing file");
					return;
				} else {
					Console.WriteLine($"File successfully signed.");
				}

			}
			store.Dispose();
		}

		static void Sign(PKCertificateWithKey certWithKey, string inputDir, string outputDir, bool reprocess, string visualRep, Metadata metadata) {
			var sw = Stopwatch.StartNew();
			Console.WriteLine("Listing files to be signed.");
			var files = Directory.EnumerateFiles(inputDir,"*.pdf").ToList();
			if (reprocess) {
				Console.WriteLine("Preparing to reprocess not signed files");
				var signerFiles = Directory.EnumerateFiles(outputDir, "*.pdf").ToList();
				files = files.Except(signerFiles).ToList();
			}
			Console.WriteLine($"{files.Count().ToString("N0")} files to be signed in directory {outputDir}");
			Console.WriteLine("Started signing process");
			//Console.WriteLine($"----------------------------------------------------------".Pastel(Color.ForestGreen));
			var errorFiles = new ConcurrentBag<string>();
			try {
				var visual = CreateVisualRepresentation(certWithKey.Certificate, visualRep, (metadata != null));
				var policy = GetSignaturePolicy().GetPolicy(certWithKey.Certificate);
				policy.SignerSpecs.AttributeGeneration.EnableLtv = false;
				// --------------
				Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, (file) => {

					if (!SignFile(file, certWithKey, policy, visual, metadata, outputDir)) {
						errorFiles.Add(file);
						var e = Interlocked.Increment(ref fileErrors);
						Console.SetCursorPosition(0, Console.CursorTop);
						Console.Write($"Files to be reprocessed {e.ToString("N0")}");
						LogAssinados(file, false);
						return;
					} else {
						LogAssinados(file,true);
					}

					var y = Interlocked.Increment(ref filesSigned);
					if (y % 100 == 0) {
						Console.SetCursorPosition(0, Console.CursorTop);
						Console.Write($"Files {y.ToString("N0")} signed in {sw.Elapsed.TotalSeconds.ToString("N1")}s ({(y / sw.Elapsed.TotalSeconds).ToString("N1")} signatures/s)");
					}
				});
				Console.WriteLine();
				Console.WriteLine($"Reprocessing {errorFiles.Count:N0} files");
				foreach (var errorFile in errorFiles) {
					SignFile(errorFile, certWithKey, policy, visual, metadata, outputDir);
				}
			} catch (Exception ex) {
				Log(ex.ToString());
				Console.WriteLine($"Error: {ex}");
			}

			Console.WriteLine();
			Console.WriteLine($"Finish signing process");
			Console.WriteLine();
			Console.WriteLine($"----------------------------------------------------");
			Console.WriteLine($"Files:{files.Count():N0} {sw.Elapsed.TotalSeconds:N1}s ({files.Count() / sw.Elapsed.TotalSeconds:N1}) signatures/s");
			Console.ReadLine();
		}

		private static bool SignFile(string file, PKCertificateWithKey certWithKey, PadesPolicySpec policy, PadesVisualRepresentation2 visual, Metadata metadata, string outputDir, string outputName=null) {
			var documentoToSign = File.ReadAllBytes(file);
			if (metadata != null)
			{
				using(var buffer = new MemoryStream())
				{
					using (var stream = new MemoryStream(documentoToSign))
					{
						DoConvertToPdfA(stream, metadata, buffer);
					}
					documentoToSign = buffer.ToArray();
				}
			}
			
			var signer = new PadesSigner();
			signer.SetSigningCertificate(certWithKey);
			signer.SetPdfToSign(documentoToSign);
			signer.SetPolicy(policy);
			signer.SetVisualRepresentation(visual);
			signer.SetCertificateValidationConfigurator(ConfigureNoValidation);
			if (string.IsNullOrWhiteSpace(outputName)) {
				outputName = Path.GetFileName(file);
			} 
			try {
				signer.ComputeSignature();
				var signed = signer.GetPadesSignature();
				File.WriteAllBytes(Path.Combine(outputDir, outputName), signed);
			} catch (Exception exception) {
				Log(exception.ToString(), file);
				return false;
			}
			return true;
		}

		private static void PdfGenerate(int n, string documentsInputDir) {
			Console.WriteLine($"Generating {n.ToString("N0")} PDFs for TEST");
			for (int i = 0; i < n; i++) {
				File.Copy(Util.GetResourcePath(orginalTestPdf), Path.Combine(documentsInputDir, $"{Guid.NewGuid()}.pdf"));
			}

			Console.WriteLine($"{n.ToString("N0")} generated files.");
		}

		private static void DeleteFiles(string documentsOutputDir, string signedDocumentsOutputDir) {
			var files = Directory.EnumerateFiles(signedDocumentsOutputDir).ToList();
			if (files.Count > 0) {
				Console.WriteLine("Deleting signed files.");
				foreach (var file in files) {
					File.Delete(file);
				}

				Console.WriteLine($"{files.Count.ToString("N0")} signed files deleted.");
			}

			files = Directory.EnumerateFiles(documentsOutputDir).ToList();
			if (files.Count > 0) {
				Console.WriteLine("Deleting original files.");
				foreach (var file in files) {
					File.Delete(file);
				}

				Console.WriteLine($"{files.Count.ToString("N0")} original files deleted.");
			}
		}

		private static PadesVisualRepresentation2 CreateVisualRepresentation(PKCertificate signerCertificate, string visualRep, bool metadata= false) {
			var name = signerCertificate.PkiBrazil.CompanyName ?? signerCertificate.PkiBrazil.Responsavel ?? signerCertificate.SubjectDisplayName;
			var sb = new StringBuilder();
			if (metadata)
			{
				sb.AppendLine($"Document scanned in accordance with Decree no. 10.278/2020 by {name}");
			}
			else
			{
				sb.AppendLine($"Digitally signed by:\n{name.ToUpper()}");
			}

			var position = PadesVisualAutoPositioning.GetFootnote();
			position.HorizontalDirection = AutoPositioningHorizontalDirections.RightToLeft;

			PadesVisualRepresentationModel2 vrModel;

			if (!string.IsNullOrEmpty(visualRep) && Util.FileExists(visualRep)) {
				try {
					var vrContent = File.ReadAllBytes(visualRep);
					var visualRepJson = Encoding.UTF8.GetString(vrContent);
					vrModel = JsonConvert.DeserializeObject<PadesVisualRepresentationModel2>(visualRepJson);
				} catch (Exception ex) {
					Log(ex.ToString());
					Console.WriteLine($"Error parsing visual representation parameters file: {ex}");
					Console.WriteLine($"Using default visual representation parameters.");
					vrModel = new PadesVisualRepresentationModel2();
				}
			} else {
				vrModel = new PadesVisualRepresentationModel2();
			}
			return vrModel.ToEntity(sb.ToString());
		}

		private static IPadesPolicyMapper GetSignaturePolicy() {
			return PadesPoliciesForGeneration.GetPadesBasic(GetTrustArbitrator());
		}

		public static void ConfigureNoValidation(CertificateValidationOptions validationOptions) {
			validationOptions.ValidateRevocationStatus = false;
			validationOptions.ValidateIssuerSignature = false;
			validationOptions.ValidateValidity = false;
			validationOptions.ValidateRootTrust = false;
			validationOptions.ValidateIssuer = false;
		}

		public static ITrustArbitrator GetTrustArbitrator() {
			// We start by trusting the ICP-Brasil roots and the roots registered as trusted on the host
			// Windows Server.
			var trustArbitrator = new LinkedTrustArbitrator(TrustArbitrators.PkiBrazil, TrustArbitrators.Windows);
			// For development purposes, we also trust in Lacuna Software's test certificates.
			var lacunaRoot = PKCertificate.Decode(Convert.FromBase64String("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"));
			trustArbitrator.Add(new TrustedRoots(lacunaRoot));
			return trustArbitrator;
		}

		public static void Log(string log,string file = null) {
			lock (logLock) {
				if (string.IsNullOrWhiteSpace(file)) {
					File.AppendAllText(logFile, $"{DateTime.Now:O}|{log}\n");
				} else {
					File.AppendAllText(logFile, $"{DateTime.Now:O}|{file}|{log}\n");
				}
			}
		}

		public static void LogAssinados(string file, bool ok) {
			if (ok) {
				lock (okLock) {
					File.AppendAllText(logOk, $"{file}\n");
				}
			} else {
				lock (errorLock) {
					File.AppendAllText(logErro, $"{file}\n");
				}
			}
		}

		private static void DoConvertToPdfA(Stream pdfInput, Metadata metadata, Stream pdfAOutput)
		{

			var conformance = PdfAConformanceLevel.PDF_A_2B;

			using (var originalPdf = new PdfDocument(new PdfReader(pdfInput)))
			{

				var IccProfile = Convert.FromBase64String("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");
				var pdfADocument = new PdfADocument(new PdfWriter(pdfAOutput), conformance, new PdfOutputIntent("Custom", "", "https://www.color.org", "sRGB IEC61966-2.1", new MemoryStream(IccProfile)));
				using (var newDocument = new Document(pdfADocument))
				{
					newDocument.SetMargins(0, 0, 0, 0);

					// copy pages
					originalPdf.CopyPagesTo(1, originalPdf.GetNumberOfPages(), newDocument.GetPdfDocument());

					// add metadata
					AddMetadata(originalPdf, newDocument, metadata);

					// save on close
				}
			}
		}

		private static void AddMetadata(PdfDocument originalDocument, Document document, Metadata metadata)
		{

			var documentInfo = document.GetPdfDocument().GetDocumentInfo();

			// desc
			documentInfo.SetAuthor(metadata.Dict[MetadataKeys.Creator]);
			documentInfo.SetTitle(metadata.Dict[MetadataKeys.Title]);

			if (metadata.Keywords?.Any() == true)
			{
				documentInfo.SetSubject(Util.GetSubjectFromKeywords(metadata.Keywords));
			}

			// adm
			// itext default add methods insert dates with offset info, so no need for timezoneinfo config
			documentInfo.AddCreationDate();
			documentInfo.AddModDate();

			// custom
			foreach (var m in metadata.Dict)
			{
				documentInfo.SetMoreInfo(m.Key, m.Value);
			}

			// images hashes
			var digestAlgorithm = DigestAlgorithm.GetInstanceByHashAlgorithmName(new HashAlgorithmName("SHA256"));
			var formattedHashes = new List<string>();

			foreach (var pageIndex in Enumerable.Range(1, originalDocument.GetNumberOfPages()))
			{
				var pageImage = GetPageImage(originalDocument, pageIndex);
				formattedHashes.Add(FormatImageHash(pageIndex, digestAlgorithm, digestAlgorithm.ComputeHash(pageImage)));
			}

			documentInfo.SetKeywords(string.Join(" ", formattedHashes));

			// creator tool
			var creatorToolPropName = "CreatorTool";
			var creatorToolPropValue = "ConsoleAppSample";
			var meta = iText.Kernel.XMP.XMPMetaFactory.ParseFromBuffer(document.GetPdfDocument().GetXmpMetadata(true));
			meta.SetProperty(iText.Kernel.XMP.XMPConst.NS_XMP, creatorToolPropName, creatorToolPropValue);
			document.GetPdfDocument().SetXmpMetadata(meta);
		}
		
		private static byte[] GetPageImage(PdfDocument originalDocument, int pageIndex)
		{

			var pageImages = new List<byte[]>();

			var page = originalDocument.GetPage(pageIndex);
			var pageDic = page.GetPdfObject();
			var resources = pageDic.GetAsDictionary(PdfName.Resources);
			var xObjects = resources.GetAsDictionary(PdfName.XObject);
			var xNames = xObjects.KeySet().GetEnumerator();

			while (xNames.MoveNext())
			{
				var imgRef = xNames.Current;
				if (xObjects.Get(imgRef) is PdfDictionary imgDict && imgDict.Get(PdfName.Subtype) == PdfName.Image)
				{
					var imageSource = xObjects.GetAsStream(imgRef);
					pageImages.Add(imageSource.GetBytes(false));
				}
			}

			if (pageImages.Count == 0)
			{
				throw new Exception($"Could not get image of page {pageIndex}");
			}
			else if (pageImages.Count == 1)
			{
				return pageImages.First();
			}
			else
			{
				// If more than one image is found, return the largest
				var maxLength = pageImages.Max(i => i.Length);
				return pageImages.First(i => i.Length == maxLength);
			}
		}

		private static string FormatImageHash(int index, DigestAlgorithm digestAlgorithm, byte[] digestValue)
		{
			return $"{index}:{digestAlgorithm.Name}:{BitConverter.ToString(digestValue).ToLower().Replace("-", string.Empty)}";
		}

	}
}
