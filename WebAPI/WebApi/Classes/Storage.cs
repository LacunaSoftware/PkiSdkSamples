using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace WebApi.Classes {
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
		public static bool TryGetFile(string fileId, out byte[] content) {
			string extension;
			return TryGetFile(fileId, out content, out extension);
		}

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

		public static string GenerateFileId(string originalFilename) {
			var sb = new StringBuilder();
			sb.Append(Path.GetFileNameWithoutExtension(originalFilename));
			sb.Append('.');
			sb.Append(Guid.NewGuid());
			sb.Append(Path.GetExtension(originalFilename));
			return sb.ToString();
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
	}
}