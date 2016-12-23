using System.Web;
using System.Web.Mvc;
using WebApi.Classes;

namespace WebApi.Controllers {
    public class DownloadController : Controller {
		// GET Download/File/{id}
		[HttpGet]
		public ActionResult File(string id) {

			if (string.IsNullOrEmpty(id)) {
				return HttpNotFound();
			}

			byte[] content;
			string extension;

			if (!Storage.TryGetFile(id, out content, out extension)) {
				return HttpNotFound();
			}

			var filename = "download" + extension;
			return File(content, MimeMapping.GetMimeMapping(filename), filename);
		}
	}
}
