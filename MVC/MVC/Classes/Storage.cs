﻿using System;
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
			var filename = Guid.NewGuid() + extension;
			File.WriteAllBytes(Path.Combine(appDataPath, filename), content);
			return filename.Replace('.', '_');
		}

		// Função que simula a recuperação de um arquivo previamente armazenado
		public static bool TryGetFile(string fileId, out byte[] content, out string extension) {
			content = null;
			extension = null;
			if (string.IsNullOrEmpty(fileId)) {
				return false;
			}
			var filename = fileId.Replace('_', '.');
			var path = HttpContext.Current.Server.MapPath("~/App_Data/" + filename);
			var fileInfo = new FileInfo(path);
			if (!fileInfo.Exists) {
				return false;
			}
			extension = fileInfo.Extension;
			content = File.ReadAllBytes(path);
			return true;
		}

		public static byte[] GetSampleNFeContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "SampleNFe.xml"));
		}

		public static byte[] GetSampleDocContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "SampleDocument.pdf"));
		}
        public static byte[] GetBatchDocContent(int id) {
            return File.ReadAllBytes(Path.Combine(ContentPath, string.Format("{0:D2}.pdf", ((id - 1) % 30) + 1)));
        }

        public static byte[] GetPdfStampContent() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "PdfStamp.png"));
		}

		public static byte[] GetSampleCodEnvelope() {
			return File.ReadAllBytes(Path.Combine(ContentPath, "SampleCodEnvelope.xml"));
		}
	}
}
