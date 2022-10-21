using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Lacuna.Pki;
using Lacuna.Pki.Pades;

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

		Init();

		try {
			DocumentService.MoveFiles(configuration["RootPathTemp"]!, configuration["RootPathError"]!);
			var files = Directory.EnumerateFiles(configuration["RootPathInput"] ?? string.Empty, "*.*", SearchOption.AllDirectories).ToList();
			if (files.Any()) {
				logger.LogInformation("Found : {n} files in {RootPathInput}", files.Count(), configuration["RootPathInput"]);
			}
			foreach (var file in files) {
				try {
					documentService.Enqueue(file);
				} catch (Exception ex) {
					logger.LogError(ex, "File {file} error", file);
				}
			}
			while (!stoppingToken.IsCancellationRequested) {
				logger.LogDebug("DirectoryWatcher Running");
				await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
				while (documentService.TryNext(out var document)) {
					Debug.Assert(document != null, nameof(document) + " != null");
					if (!await sign(document)) {
						documentService.MoveFileToError(document);
					}
				}
			}
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
			var trustArbitrator = new LinkedTrustArbitrator(TrustArbitrators.PkiBrazil);
			var policy = PadesPoliciesForGeneration.GetPadesBasic(trustArbitrator);
			padesSigner.SetPolicy(policy);
			padesSigner.SetSigningCertificate(document.Certificate);
			if (configuration.GetSection("PadesVisualRepresentation").Exists()) {
				padesSigner.SetVisualRepresentation(Util.GetVisualRepresentation(document.Certificate.Certificate,configuration,logger));
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
			if (string.IsNullOrEmpty(configuration["RootPathLogs"])) {
				logger.LogError("RootPathLogs is null or empty");
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
			if (!Directory.Exists(configuration["RootPathLogs"])) {
				Directory.CreateDirectory(configuration["RootPathLogs"]!);
				logger.LogInformation("Log Directory {RootPathLogs} created", configuration["RootPathLogs"]);
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
