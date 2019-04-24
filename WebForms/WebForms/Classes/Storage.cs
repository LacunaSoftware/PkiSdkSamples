using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WebForms.Classes {

	public class Storage {

		public static string ContentPath {
			get {
				return HttpContext.Current.Server.MapPath("~/Content");
			}
		}

		// Função que simula o armazenamento de um arquivo, retornando um identificador que permite a sua recuperação
		public static string StoreFile(byte[] content, string extension = "") {
			var appDataPath = HttpContext.Current.Server.MapPath("~/App_Data");
			if (!Directory.Exists(appDataPath)) {
				Directory.CreateDirectory(appDataPath);
			}
			var filename = Guid.NewGuid() + extension;
			File.WriteAllBytes(Path.Combine(appDataPath, filename), content);
			return filename.Replace('.', '_');
		}

		// Função que simula a recuperação de um arquivo previamente armazenado
		public static bool TryGetFile(string fileId, out byte[] content, out string extension) {
			var filename = fileId.Replace('_', '.');
			var path = HttpContext.Current.Server.MapPath("~/App_Data/" + filename);
			var fileInfo = new FileInfo(path);
			if (!fileInfo.Exists) {
				content = null;
				extension = null;
				return false;
			}
			extension = fileInfo.Extension;
			content = File.ReadAllBytes(path);
			return true;
		}

		public static byte[] GetFile(string fileId, out string extension) {
			byte[] content;
			if (!TryGetFile(fileId, out content, out extension)) {
				throw new Exception("File not found on database: " + fileId);
			}
			return content;
		}

		public static byte[] GetFile(string fileId) {
			string extension;
			return GetFile(fileId, out extension);
		}

		public static void DeleteFile(string fileId) {
			var filename = fileId.Replace('_', '.');
			var path = HttpContext.Current.Server.MapPath("~/App_Data/" + filename);
			var fileInfo = new FileInfo(path);
			if (fileInfo.Exists) {
				fileInfo.Delete();
			}
		}

		public static byte[] GetSampleNFeContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "SampleNFe.xml"));
		}

		public static byte[] GetSampleDocContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "SampleDocument.pdf"));
		}

		public static byte[] GetPdfStampContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "PdfStamp.png"));
		}

		public static byte[] GetIcpBrasilLogoContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "icp-brasil.png"));
		}

		public static byte[] GetValidationResultIcon(bool isValid) {
			var filename = isValid ? "ok.png" : "not-ok.png";
			return File.ReadAllBytes(Path.Combine(ContentPath, filename));
		}

		public static byte[] GetBatchDocContent(int id) {
			return File.ReadAllBytes(Path.Combine(ContentPath, string.Format("{0:D2}.pdf", (id % 10))));
		}

		public static byte[] GetSampleCadesSignatureOfPdf() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "SamplePdfSigned.p7s"));
		}

		public static List<byte[]> GetSampleCadesDetachedSignaturesToMerge() {
			List<byte[]> detachedSignatures = new List<byte[]>() {
					File.ReadAllBytes(Path.Combine(ContentPath, "SampleDetachedSignature1.p7s")),
					File.ReadAllBytes(Path.Combine(ContentPath, "SampleDetachedSignature2.p7s"))
				};
			return detachedSignatures;
		}

		/// <summary>
		/// Returns the verification code associated with the given document, or null if no verification code has been associated with it
		/// </summary>
		public static string GetVerificationCode(string fileId) {
			// >>>>> NOTICE <<<<<
			// This should be implemented on your application as a SELECT on your "document table" by the
			// ID of the document, returning the value of the verification code column
			return HttpContext.Current.Session[string.Format("Files/{0}/Code", fileId)] as string;
		}

		/// <summary>
		/// Registers the verification code for a given document.
		/// </summary>
		public static void SetVerificationCode(string fileId, string code) {
			// >>>>> NOTICE <<<<<
			// This should be implemented on your application as an UPDATE on your "document table" filling
			// the verification code column, which should be an indexed column
			HttpContext.Current.Session[string.Format("Files/{0}/Code", fileId)] = code;
			HttpContext.Current.Session[string.Format("Codes/{0}", code)] = fileId;
		}

		/// <summary>
		/// Returns the ID of the document associated with a given verification code, or null if no document matches the given code
		/// </summary>
		public static string LookupVerificationCode(string code) {
			if (string.IsNullOrEmpty(code)) {
				return null;
			}
			// >>>>> NOTICE <<<<<
			// This should be implemented on your application as a SELECT on your "document table" by the
			// verification code column, which should be an indexed column
			return HttpContext.Current.Session[string.Format("Codes/{0}", code)] as string;
		}
	}
}
