using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace MVC.Classes {

	public class Storage {
		public static string ContentPath {
			get {
				return HttpContext.Current.Server.MapPath("~/Content");
			}
		}

		// Função que simula o armazenamento de um arquivo, retornando um identificador que permite a sua recuperação
		public static string StoreFile(byte[] content, string extension) {
			var appDataPath = HttpContext.Current.Server.MapPath("~/App_Data");
			if (!Directory.Exists(appDataPath)) {
				Directory.CreateDirectory(appDataPath);
			}
			var id = Guid.NewGuid().ToString();
			var filename = id + extension;
			File.WriteAllBytes(Path.Combine(appDataPath, filename), content);

			return filename;
		}

		// Função que simula a recuperação de um arquivo previamente armazenado
		public static byte[] GetFile(string fileId, string extension) {
			var path = HttpContext.Current.Server.MapPath("~/App_Data/" + fileId + extension);
			return File.ReadAllBytes(path);
		}

		public static byte[] GetSampleNFeContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "SampleNFe.xml"));
		}
	}
}