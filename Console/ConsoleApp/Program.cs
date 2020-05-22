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

			[Option('S', "source", Required = false, HelpText = "Source directory")]
			public string SourceDir { get; set; }

			[Option('D', "destination", Required = false, HelpText = "Destination directory")]
			public string DestinationDir { get; set; }

			[Option('C', "certificate", Required = false, HelpText = "Signer certificate thumbprint")]
			public string CertThumbprint { get; set; }

			// 0,0 is the right bottom corner
			[Option('v', "visual-representation", Required = false, HelpText = "JSON with the signature visual representation configuration")]
			public string VisualRep { get; set; }

			[Option('f', "file", Required = false, HelpText = "Sign only this file")]
			public string File { get; set; }

			[Option('p', "token-pin", Required = false, HelpText = "Token's pin")]
			public string Pin { get; set; }
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
				deleteFiles(documentsInputDir, signedDocumentsOutputDir);
				pdfGenerate(testCount, documentsInputDir);
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
			if (string.IsNullOrWhiteSpace(options.File)) {
				Console.WriteLine("Getting things ready.");
				sign(cert, documentsInputDir, signedDocumentsOutputDir, options.Reprocess, options.VisualRep);
			} else {
				var visual = createVisualRepresentation(cert.Certificate, options.VisualRep);
				var policy = getSignaturePolicy().GetPolicy(cert.Certificate);
				policy.SignerSpecs.AttributeGeneration.EnableLtv = false;

				if (!signFile(options.File, cert, policy, visual, "", "Signed_"+options.File)) {
					Console.WriteLine($"Error signing file");
					return;
				} else {
					Console.WriteLine($"File successfully signed.");
				}

			}
			store.Dispose();
		}

		static void sign(PKCertificateWithKey certWithKey, string inputDir, string outputDir, bool reprocess, string visualRep) {
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
				var visual = createVisualRepresentation(certWithKey.Certificate, visualRep);
				var policy = getSignaturePolicy().GetPolicy(certWithKey.Certificate);
				policy.SignerSpecs.AttributeGeneration.EnableLtv = false;
				// --------------
				Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, (file) => {
					if (!signFile(file, certWithKey, policy, visual, outputDir)) {
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
					signFile(errorFile, certWithKey, policy, visual, outputDir);
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

		private static bool signFile(string file, PKCertificateWithKey certWithKey, PadesPolicySpec policy, PadesVisualRepresentation2 visual, string outputDir, string outputName=null) {
			var signer = new PadesSigner();
			signer.SetSigningCertificate(certWithKey);
			signer.SetPdfToSign(File.ReadAllBytes(file));
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

		private static void pdfGenerate(int n, string documentsInputDir) {
			Console.WriteLine($"Generating {n.ToString("N0")} PDFs for TEST");
			for (int i = 0; i < n; i++) {
				File.Copy(Util.GetResourcePath(orginalTestPdf), Path.Combine(documentsInputDir, $"{Guid.NewGuid()}.pdf"));
			}

			Console.WriteLine($"{n.ToString("N0")} generated files.");
		}

		private static void deleteFiles(string documentsOutputDir, string signedDocumentsOutputDir) {
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

		private static PadesVisualRepresentation2 createVisualRepresentation(PKCertificate signerCertificate, string visualRep) {
			var name = signerCertificate.PkiBrazil.CompanyName ?? signerCertificate.PkiBrazil.Responsavel ?? signerCertificate.SubjectDisplayName;
			var sb = new StringBuilder();
			sb.AppendLine($"Digitally signed by:\n{name.ToUpper()}");

			var position = PadesVisualAutoPositioning.GetFootnote();
			position.HorizontalDirection = AutoPositioningHorizontalDirections.RightToLeft;

			PadesVisualRepresentationModel2 vrModel;

			if (!string.IsNullOrEmpty(visualRep) && Util.FileExists(visualRep)) {
				byte[] vrContent;
				try {
					vrContent = File.ReadAllBytes(visualRep);
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

		private static IPadesPolicyMapper getSignaturePolicy() {
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

	}
}
