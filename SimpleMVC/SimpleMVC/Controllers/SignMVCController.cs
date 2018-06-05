using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lacuna.Pki;
using Lacuna.Pki.Pades;
using SimpleMVC.Models;
using System.IO;
using System.Web.Http.Results;

namespace SimpleMVC.Controllers {
	public class SignMVCController : Controller {

		public string TempPath {
			get { return System.Web.Hosting.HostingEnvironment.MapPath(@"~/App_Data"); }
		}

		public ActionResult SigOk() {
			return Content("OK");
		}

		[HttpPost]
		public ActionResult SignatureStart(SignatureStartMVCModel model) {
			var cert = PKCertificate.Decode(model.CertContent);
			var padesSigner = new PadesSigner();
			padesSigner.SetPdfToSign(Path.Combine(TempPath, "SampleDocument.pdf"));
			padesSigner.SetSigningCertificate(cert);
			padesSigner.SetPolicy(PadesPoliciesForGeneration.GetPadesBasic(GetTrustArbitrator()));
			var visual = new PadesVisualRepresentation2() {
				Text = new PadesVisualText() {
					CustomText = $"Assinado digitalmente por {cert.SubjectDisplayName}",
					IncludeSigningTime = true,
					HorizontalAlign = PadesTextHorizontalAlign.Left
				},
				// Background image of the visual representation
				Image = new PadesVisualImage() {
					Content = System.IO.File.ReadAllBytes(Path.Combine(TempPath, "stamp.png")),
					Opacity = 50,
					HorizontalAlign = PadesHorizontalAlign.Right
				},
				Position = PadesVisualAutoPositioning.GetFootnote()
			};
			padesSigner.SetVisualRepresentation(visual);
			padesSigner.SetVisualRepresentation(visual);
			// Generate the "to-sign-bytes". This method also yields the signature algorithm that must
			// be used on the client-side, based on the signature policy, as well as the "transfer data",
			// a byte-array that will be needed on the next step.
			var toSignBytes = padesSigner.GetToSignBytes(out var signatureAlg, out var transferData);
			var trasferDataId = $"{Guid.NewGuid()}.pdf";
			System.IO.File.WriteAllBytes(Path.Combine(TempPath, trasferDataId), transferData);
			var modelResult = new SignatureStartResponse() {
				TransferDataFileId = trasferDataId,
				ToSignBytes = toSignBytes,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			};
			return Json(modelResult);
		}

		[HttpPost]
		public ActionResult SignatureComplete(SignatureCompleteMVCModel request) {
			string fileSignedName;
			try {
				var tempFile = Path.Combine(TempPath, request.TransferDataFileId);
				var transferData = System.IO.File.ReadAllBytes(tempFile);
				System.IO.File.Delete(tempFile);
				var padesSigner = new PadesSigner();
				padesSigner.SetPolicy(PadesPoliciesForGeneration.GetPadesBasic(TrustArbitrators.Windows));
				padesSigner.SetPreComputedSignature(request.Signature, transferData);
				padesSigner.ComputeSignature();
				var signatureContent = padesSigner.GetPadesSignature();
				fileSignedName = $"SampleDocument{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.pdf";
				System.IO.File.WriteAllBytes(Path.Combine(TempPath, fileSignedName), signatureContent);
			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				return Json(new ValidationErrorModel(ex.ValidationResults));
			}

			// Pass the following fields to be used on signature-results template:
			// - The signature file will be stored on the folder "App_Data/". Its name will be passed by Filename field.
			// - The user's certificate
			var response = new SignatureCompleteResponse() {
				Filename = fileSignedName,
				Certificate = new CertificateModel(PKCertificate.Decode(request.CertContent))
			};
			return Json(response);
		}
		public static ITrustArbitrator GetTrustArbitrator() {
			// We start by trusting the ICP-Brasil roots and the roots registered as trusted on the host Windows Server
			var trustArbitrator = new LinkedTrustArbitrator(TrustArbitrators.PkiBrazil, TrustArbitrators.Windows);
#if DEBUG
			// For development purposes, we also trust in Lacuna Software's test certificates: https://github.com/LacunaSoftware/PkiSdkSamples#test-certificates
			var lacunaRoot = PKCertificate.Decode(Convert.FromBase64String("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"));
			trustArbitrator.Add(new TrustedRoots(lacunaRoot));
#endif
			return trustArbitrator;
		}

	}



}
