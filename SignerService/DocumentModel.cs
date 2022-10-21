using Lacuna.Pki;

namespace Lacuna.SignerService;

public class DocumentModel {
	public string FileName { get; set; } = null!;

	public string Name => Path.GetFileName(FileName);

	public string Date {
		get {
			var fileInfo = new FileInfo(FileName);
			return fileInfo.Exists ? fileInfo.LastWriteTime.ToString("g") : string.Empty;
		}
	}

	public string TempFileName { get; set; }
	public string SignedFileName { get; set; }
	public PKCertificateWithKey Certificate { get; set; }

}
