using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {

	public partial class Download : System.Web.UI.Page {

		protected void Page_Load(object sender, EventArgs e) {

			var fileId = Request.QueryString["file"];
			if (string.IsNullOrEmpty(fileId)) {
				Response.Redirect("~/");
			}

			byte[] content;
			string extension;

			if (!Storage.TryGetFile(fileId, out content, out extension)) {
				Response.Redirect("~/");
			}

			var filename = "download" + extension;
			Response.ContentType = MimeMapping.GetMimeMapping(filename);
			Response.AddHeader("Content-Disposition", "attachment; filename=" + filename);
			Response.BinaryWrite(content);
			Response.End();
		}
	}
}