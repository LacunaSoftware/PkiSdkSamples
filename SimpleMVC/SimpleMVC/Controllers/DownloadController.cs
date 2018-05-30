using System.IO;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace SimpleMVC.Controllers {
    public class DownloadController : Controller {
		// GET Download/File/{id}
		[HttpGet]
		public ActionResult File(string name) {
			if (string.IsNullOrEmpty(name)) {
				return HttpNotFound("Name is required");
			}
			var file = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath(@"~/App_Data"), name);
			if (System.IO.File.Exists(file)) {
				return HttpNotFound($"File {name} not found");
			}
			var content = System.IO.File.ReadAllBytes(file);
			return File(content, MimeMapping.GetMimeMapping(name), name);
		}
	}
}
