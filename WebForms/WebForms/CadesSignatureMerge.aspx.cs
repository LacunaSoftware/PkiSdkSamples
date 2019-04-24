using Lacuna.Pki.Cades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebForms.Classes;

namespace WebForms {
	public partial class CadesSignatureMerge : System.Web.UI.Page {
		protected void Page_Load(object sender, EventArgs e) {

			var detachedSignatures = Storage.GetSampleCadesDetachedSignaturesToMerge();
			var encapsulatedContent = Storage.GetSampleDocContent();

			// Open each detached signature.
			var signatures = detachedSignatures.Select(ds => Lacuna.Pki.Cades.CadesSignature.Open(ds));

			// Merge signatures using the MergeSignatures() method by passing the list of detached signatures to
			// be merged and the encapsulated content.
			var mergedSignature = CadesSignatureEditor.MergeSignatures(signatures, encapsulatedContent);

			Response.ContentType = "application/pdf";
			Response.AddHeader("Content-Disposition", "attachment; filename=merged-signature.p7s");
			Response.BinaryWrite(mergedSignature);
			Response.End();
		}
	}
}
