using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Lacuna.Pki;
using Lacuna.Pki.Pades;
using System.Runtime.ConstrainedExecution;

namespace Lacuna.SignerService;

public class DirectoryWatcher : BackgroundService {
	private readonly IConfiguration configuration;
	private readonly ILogger<DirectoryWatcher> logger;
	private readonly DocumentService documentService;

	public DirectoryWatcher(IConfiguration configuration, ILogger<DirectoryWatcher> logger, DocumentService documentService) {
		this.configuration = configuration;
		this.logger = logger;
		this.documentService = documentService;
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		Init();

		using var watcher = new FileSystemWatcher(configuration["RootPathInput"] ?? string.Empty);
		watcher.NotifyFilter = NotifyFilters.Attributes
									  | NotifyFilters.CreationTime
									  | NotifyFilters.DirectoryName
									  | NotifyFilters.FileName
									  | NotifyFilters.LastAccess
									  | NotifyFilters.LastWrite
									  | NotifyFilters.Security
									  | NotifyFilters.Size;

		watcher.Created += OnChanged;
		watcher.Filter = "*.pdf";
		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;


		try {
			DocumentService.MoveFiles(configuration["RootPathTemp"]!, configuration["RootPathError"]!);
			var files = Directory
				.EnumerateFiles(configuration["RootPathInput"] ?? string.Empty, "*.*", SearchOption.AllDirectories)
				.ToList();
			if (files.Any()) {
				logger.LogInformation("Found : {n} files in {RootPathInput}", files.Count(),
					configuration["RootPathInput"]);
			}

			foreach (var file in files) {
				try {
					documentService.Enqueue(file);
				} catch (Exception ex) {
					logger.LogError(ex, "File {file} error", file);
				}
			}

			logger.LogInformation("Directory Watcher Started");
			while (!stoppingToken.IsCancellationRequested) {
				await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
				while (documentService.TryNext(out var document)) {
					Debug.Assert(document != null, nameof(document) + " != null");
					if (!await sign(document)) {
						documentService.MoveFileToError(document);
					}
				}
			}
		} catch (TaskCanceledException) {
			logger.LogInformation("Task Canceled");
			Environment.Exit(1);
		} catch (Exception ex) {
			logger.LogError(ex, "{Message}", ex.Message);
			Environment.Exit(1);
		}
	}

	private async Task<bool> sign(DocumentModel document) {
		try {
			var sw = Stopwatch.StartNew();
			File.Move(document.FileName, document.TempFileName);
			var padesSigner = new PadesSigner();
			padesSigner.SetPdfToSign(document.TempFileName);
			var policy = GetSignaturePolicy().GetPolicy(document.Certificate.Certificate);
			padesSigner.SetPolicy(policy);
			padesSigner.SetSigningCertificate(document.Certificate);
			if (configuration.GetSection("PadesVisualRepresentation").Exists()) {
				padesSigner.SetVisualRepresentation(Util.GetVisualRepresentation(document.Certificate.Certificate, configuration, logger));
			}
			padesSigner.ComputeSignature();
			var signatureContent = padesSigner.GetPadesSignature();
			await File.WriteAllBytesAsync(document.SignedFileName, signatureContent);
			File.Delete(document.TempFileName);
			logger.LogInformation("file {file} signed in {timespan} s", document.FileName, sw.Elapsed.TotalSeconds.ToString("N1"));
			return true;
		} catch (Exception e) {
			logger.LogError(e, "Error on signing document: {Document} message: {ErrorMessage} ", document.FileName, e.Message);
		}
		return false;
	}

	private IPadesPolicyMapper GetSignaturePolicy() {
		return PadesPoliciesForGeneration.GetPadesBasic(GetTrustArbitrator());
	}

	public ITrustArbitrator GetTrustArbitrator() {
		// We start by trusting the ICP-Brasil roots and the roots registered as trusted on the host
		// Windows Server.
		var trustArbitrator = new LinkedTrustArbitrator(TrustArbitrators.PkiBrazil, TrustArbitrators.Windows);
		if (configuration["AcceptLacunaTestCertificates"] == "true") {
			// For development purposes, we also trust in Lacuna Software's test certificates.
			var lacunaRoot = PKCertificate.Decode(Convert.FromBase64String(
				"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"));
			trustArbitrator.Add(new TrustedRoots(lacunaRoot));
		}
		return trustArbitrator;
	}



	private void OnChanged(object sender, FileSystemEventArgs e) {
		logger.LogInformation("file {file} {ChangeType}", e.FullPath, e.ChangeType);
		documentService.Enqueue(e.FullPath);
	}

	private void Init() {
		try {
			if (string.IsNullOrEmpty(configuration["RootPathTemp"])) {
				logger.LogError("RootPathTemp is null or empty");
				Environment.Exit(1);
			}
			if (string.IsNullOrEmpty(configuration["RootPathError"])) {
				logger.LogError("RootPathError is null or empty");
				Environment.Exit(1);
			}
			if (string.IsNullOrEmpty(configuration["RootPathInput"])) {
				logger.LogError("RootPathInput is null or empty");
				Environment.Exit(1);
			}
			if (string.IsNullOrEmpty(configuration["RootPathSigned"])) {
				logger.LogError("RootPathSigned is null or empty");
				Environment.Exit(1);
			}
			if (!Directory.Exists(configuration["RootPathTemp"])) {
				Directory.CreateDirectory(configuration["RootPathTemp"]!);
				logger.LogInformation("Temp Directory {RootPathTemp} created", configuration["RootPathTemp"]);
			}
			if (!Directory.Exists(configuration["RootPathError"])) {
				Directory.CreateDirectory(configuration["RootPathError"]!);
				logger.LogInformation("Error Directory {RootPathError} created", configuration["RootPathError"]);
			}
			if (!Directory.Exists(configuration["RootPathInput"])) {
				Directory.CreateDirectory(configuration["RootPathInput"]!);
				logger.LogInformation("Input Directory {RootPathInput} created", configuration["RootPathInput"]);
			}
			if (!Directory.Exists(configuration["RootPathSigned"])) {
				Directory.CreateDirectory(configuration["RootPathSigned"]!);
				logger.LogInformation("Signed Directory {RootPathLogs} created", configuration["RootPathSigned"]);
			}

		} catch (Exception ex) {
			logger.LogError(ex, "Error on directory create!");
			Environment.Exit(1);
		}
	}
}
