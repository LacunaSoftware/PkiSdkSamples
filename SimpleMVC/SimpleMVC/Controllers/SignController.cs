using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Lacuna.Pki;
using Lacuna.Pki.Pades;
using SimpleMVC.Models;

namespace SimpleMVC.Controllers {
	public class SignController : ApiController {

		public string TempPath {
			get { return System.Web.Hosting.HostingEnvironment.MapPath(@"~/App_Data"); }
		}

		[System.Web.Http.Route("api/sign/SigOk")]
		[HttpGet]
		public IHttpActionResult SigOk() {
			return Ok("Serviço OK");
		}

		// POST: api/Sign
		[Route("api/sign/SignatureStart")]
		[System.Web.Http.HttpPost]
		public IHttpActionResult SignatureStart([FromBody]SignatureStartModel model) {
			var cert = PKCertificate.Decode(model.CertContent);
			var padesSigner = new PadesSigner();
			padesSigner.SetPdfToSign(Path.Combine(TempPath, "SampleDocument.pdf"));
			padesSigner.SetSigningCertificate(cert);
			padesSigner.SetPolicy(PadesPoliciesForGeneration.GetPadesBasic(TrustArbitrators.PkiBrazil));
			var visual = new PadesVisualRepresentation2() {
				Text = new PadesVisualText() {
					CustomText = $"Assinado digitalmente por {cert.SubjectDisplayName}",
					IncludeSigningTime = true,
					HorizontalAlign = PadesTextHorizontalAlign.Left
				},
				// Background image of the visual representation
				Image = new PadesVisualImage() {
					Content = File.ReadAllBytes(Path.Combine(TempPath, "stamp.png")),
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
			File.WriteAllBytes(Path.Combine(TempPath, trasferDataId), transferData);
			var modelResult = new SignatureStartResponse() {
				TransferDataFileId = trasferDataId,
				ToSignBytes = toSignBytes,
				DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid
			};
			return Ok(modelResult);
		}

		[HttpPost, Route("api/sign/SignatureComplete")]
		public IHttpActionResult SignatureComplete(SignatureCompleteRequest request) {
			string fileSignedName;
			try {
				var tempFile = Path.Combine(TempPath, request.TransferDataFileId);
				var transferData = File.ReadAllBytes(tempFile);
				File.Delete(tempFile);
				var padesSigner = new PadesSigner();
				padesSigner.SetPolicy(PadesPoliciesForGeneration.GetPadesBasic(TrustArbitrators.PkiBrazil));
				padesSigner.SetPreComputedSignature(request.Signature, transferData);
				padesSigner.ComputeSignature();
				var signatureContent = padesSigner.GetPadesSignature();
				fileSignedName = $"SampleDocument{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.pdf";
				File.WriteAllBytes(Path.Combine(TempPath, fileSignedName), signatureContent);
			} catch (ValidationException ex) {
				// Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(ex.ValidationResults)));
			}

			// Pass the following fields to be used on signature-results template:
			// - The signature file will be stored on the folder "App_Data/". Its name will be passed by Filename field.
			// - The user's certificate
			var response = new SignatureCompleteResponse() {
				Filename = fileSignedName,
				Certificate = new CertificateModel(PKCertificate.Decode(request.Certificate))
			};
			return Ok(response);
		}
	}
}
