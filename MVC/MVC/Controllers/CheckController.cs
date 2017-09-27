using Lacuna.Pki.Pades;
using MVC.Classes;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace MVC.Controllers {
    public class CheckController : Controller {

        // GET: Check
        [HttpGet]
        public ActionResult Index(string code) {

            // We stored the unformatted version of the verification code (without hyphens) but used the formatted 
            // version (with hyphens) on the PDF. Now, we remove the hyphens before looking it up.
            var verificationCode = Util.ParseVerificationCode(code);

            // Get document associated with verification code
            var fileId = StorageMock.LookupVerificationCode(verificationCode);
            if (fileId == null) {
                // Invalid code given!
                // Small delay to slow down brute-force attacks (if you want to be extra careful you might want to add a CAPTCHA to the process)
                Thread.Sleep(TimeSpan.FromSeconds(2));
                return HttpNotFound();
            }

            // Read document form storage
            byte[] fileContent;
            if (!StorageMock.TryGetFile(fileId, out fileContent)) {
                return HttpNotFound();
            }

            // Open signature with PKI SDK
            var signature = PadesSignature.Open(fileContent);

            // Validate signatures
            var validationPolicy = PadesPolicySpec.GetBasic();
            var signers = new List<PadesSignatureModel>();
            foreach (var signer in signature.Signers) {
                signers.Add(new PadesSignatureModel() {
                    Signer = signer,
                    ValidationResults = signature.ValidateSignature(signer, validationPolicy)
                });
            }

            // Render validation page
            return View(new CheckModel() {
                Signers = signers,
                FileId = fileId
            });
        }
    }
}
