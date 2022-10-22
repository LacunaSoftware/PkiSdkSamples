using Lacuna.Pki.Stores;
using Maoli;
using System.Collections.Concurrent;
using Lacuna.Pki;
using static System.Collections.Specialized.BitVector32;
using System.Linq;

namespace Lacuna.SignerService;

public class DocumentService {
	private readonly ILogger<DocumentService> logger;
	private readonly IConfiguration configuration;
	private readonly List<PKCertificateWithKey> certificates;


	public DocumentService(ILogger<DocumentService> logger, IConfiguration configuration) {
		this.logger = logger;
		this.configuration = configuration;
		PkiConfig.BinaryLicense = Convert.FromBase64String(configuration["PkiSDKLicense"] ?? string.Empty);
		WindowsCertificateStore certStore;
		if (configuration["CertificateStore"] == "LocalMachine") {
			certStore = WindowsCertificateStore.LoadPersonalLocalMachine();
		} else {
			certStore = WindowsCertificateStore.LoadPersonalCurrentUser();
		}
		certificates = certStore.GetCertificatesWithKey().Where(c => c.Certificate.PkiBrazil.CPF != null).ToList();
		logger.LogInformation("{certificates} certificates with CPF found", certificates.Count);
		foreach (var certificate in certificates) {
			logger.LogInformation("{cpf}:{SubjectDisplayName}:{Responsavel}:{ThumbprintSHA1}", certificate.Certificate.PkiBrazil.CpfFormatted,certificate.Certificate.SubjectDisplayName,certificate.Certificate.PkiBrazil.Responsavel,certificate.Certificate.ThumbprintSHA1);
		}
	}
	private ConcurrentQueue<DocumentModel?> documentQueue { get; } = new();

	public bool HasDocuments() {
		return !documentQueue.IsEmpty;
	}

	public void Enqueue(string fileName) {
		var document = new DocumentModel() {
			FileName = fileName,
			TempFileName = Path.Combine(configuration["RootPathTemp"] ?? string.Empty, Path.GetFileName(fileName)),
			SignedFileName = Path.Combine(configuration["RootPathSigned"] ?? string.Empty, Path.GetFileName(fileName)),
		};
		var parts = new DirectoryInfo(Path.GetDirectoryName(fileName) ?? string.Empty).Split().ToList();
		if (parts.Count < 2 || !Cpf.Validate(parts[^1])) {
			logger.LogError("Path {path} not contains cpf", Path.GetFullPath(fileName));
			return;
		}
		var cpf = parts[^1].Trim().GetOnlyDigits();
		var thumbprint = string.Empty;
		var section = configuration.GetSection("Certificates");
		var certificateConfigured = section.GetChildren().FirstOrDefault(c => c.Key == cpf);
		if (certificateConfigured != null) {
			thumbprint = certificateConfigured.Value??string.Empty;
		}
		PKCertificateWithKey? certificate;
		if (string.IsNullOrEmpty(thumbprint)) {
			certificate = certificates.FirstOrDefault(c => c.Certificate.PkiBrazil.CPF.GetOnlyDigits() == cpf);
		} else {
			certificate = certificates.FirstOrDefault(c => 
				c.Certificate.PkiBrazil.CPF.GetOnlyDigits() == cpf && 
				(PkiUtil.EncodeToHexString(c.Certificate.ThumbprintSHA1).Equals(thumbprint,StringComparison.OrdinalIgnoreCase) ||
				PkiUtil.EncodeToHexString(c.Certificate.ThumbprintSHA256).Equals(thumbprint, StringComparison.OrdinalIgnoreCase))
			);
		}
		if (certificate == null) {
			logger.LogError("Certificate for cpf:{cpf} and thumbprint:{thumbprint} not found", cpf, thumbprint);
			return;
		}
		document.Certificate = certificate;
		documentQueue.Enqueue(document);
	}

	public bool TryNext(out DocumentModel? document) {
		var result = documentQueue.TryDequeue(out document);
		return result;
	}

	public void MoveFileToError(DocumentModel document) {
		try {
			var destFileName = Path.Combine(configuration["RootPathTemp"] ?? string.Empty, $"{Path.GetFileNameWithoutExtension(document.TempFileName)}{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.{Path.GetExtension(document.TempFileName)}");
			File.Move(document.TempFileName, destFileName);
		} catch (Exception e) {
			logger.LogError(e,"document {document}", document.FileName);
		}
	}

	public static void MoveFiles(string inPath, string outPath) {
		var files = Directory.EnumerateFiles(inPath).ToList();
		foreach (var file in files) {
			var destFileName = Path.Combine(outPath, $"{Path.GetFileNameWithoutExtension(file)}{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.{Path.GetExtension(file)}");
			File.Move(file, destFileName);
		}
	}



}
