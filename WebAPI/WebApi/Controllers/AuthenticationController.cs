using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using WebApi.Classes;
using WebApi.Models;

namespace WebApi.Controllers {

	public class AuthenticationController : ApiController {

		[HttpGet, Route("Api/Authentication")]
		public IHttpActionResult Get() {

			// The PKCertificateAuthentication class requires an implementation of INonceStore.
			// We'll use the FileSystemNonceStore class.
			var nonceStore = Util.GetNonceStore();

			// Instantiate the PKCertificateAuthentication class passing our EntityFrameworkNonceStore
			var certAuth = new PKCertificateAuthentication(nonceStore);

			// Call the Start() method, which is the first of the two server-side steps. This yields the nonce,
			// a 16-byte-array which we'll send to the view.
			var nonce = certAuth.Start();

			return Ok(nonce);
		}

		[HttpPost, Route("Api/Authentication")]
		public IHttpActionResult Post(AuthenticationPostRequest request) {

			// As before, we instantiate a FileSystemNonceStore class and use that to 
			// instantiate a PKCertificateAuthentication
			var nonceStore = Util.GetNonceStore();
			var certAuth = new PKCertificateAuthentication(nonceStore);

			// Call the Complete() method, which is the last of the two server-side steps. It receives:
			// - The nonce which was signed using the user's certificate
			// - The user's certificate encoding
			// - The nonce signature
			// - A TrustArbitrator to be used to determine trust in the certificate (for more information see http://pki.lacunasoftware.com/Help/html/e7724d78-9835-4f06-b58c-939b721f6e7b.htm)
			// The call yields:
			// - A ValidationResults which denotes whether the authentication was successful or not
			// - The user's decoded certificate
			PKCertificate certificate;
			var vr = certAuth.Complete(request.Nonce, request.Certificate, request.Signature, Util.GetTrustArbitrator(), out certificate);

			// NOTE: By changing the TrustArbitrator above, you can accept only certificates from a certain PKI,
			// for instance, ICP-Brasil (TrustArbitrators.PkiBrazil). For more information, see
			// http://pki.lacunasoftware.com/Help/html/e7724d78-9835-4f06-b58c-939b721f6e7b.htm
			//
			// The value above (TrustArbitrators.Windows) specifies that the root certification authorities in the
			// Windows certificate store are to be used as trust arbitrators.

			// Check the authentication result
			if (!vr.IsValid) {
				return new ResponseMessageResult(Request.CreateResponse(HttpStatusCode.BadRequest, new ValidationErrorModel(vr)));
			}

			var response = new AuthenticationPostResponse() {
				Certificate = new CertificateModel(certificate)
			};

			return Ok(response);
		}

	}
}
