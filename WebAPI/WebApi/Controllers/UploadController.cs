using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WebApi.Classes;

namespace WebApi.Controllers {
	public class UploadController : ApiController {

		/**
		 * POST Api/Upload
		 * 
		 * Receives an POST request containg a HTML FormData class with the uploaded file and its
		 * information. The request's content-type should be 'multipart/form-data'.
		 */
		[HttpPost]
		public async Task<string> Post() {

			// Verify request's media type.
			if (!Request.Content.IsMimeMultipartContent()) {
				throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
			}
			var root = HttpContext.Current.Server.MapPath("~/App_Data");

			// Read the form data as a multipart upload request. We created the 
			// CustomMultipartFormDataStreamProvider class to change the default behavior of the
			// MultipartFormDataStreamProvider class, which stores the uploded file on a 
			// {root}/BodyPart_{Guid} path.
			var provider = new CustomMultipartFormDataStreamProvider(root);
			await Request.Content.ReadAsMultipartAsync(provider);

			// Verify if at least one file was uploaded and get the first uploaded file.
			if (provider.FileData == null || !provider.FileData.Any()) {
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			}
			var file = provider.FileData.First();

			// Return the path of the stored file.
			return Path.GetFileName(file.LocalFileName);
		}

		/**
		 * This class is necessary to change the default behavior of the 
		 * MultipartFormDataStreamProvider class, which stores the file on a {root}/BodyPart_{Guid}
		 * path. Instead we stores in the {root}/{originalFile}.{Guid}.{ext} file path. In this way,
		 * we can preserve the original filename and the extension. We use the GenerateFileId() method 
		 * to encapsulate this name generation.
		 */
		public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider {

			public CustomMultipartFormDataStreamProvider(string rootPath) : base(rootPath) { }

			public override string GetLocalFileName(HttpContentHeaders headers) {
				var originalFilename = headers.ContentDisposition.FileName.Trim('"');
				return Storage.GenerateFileId(originalFilename);
			}
		}
	}
}