using Lacuna.Pki;
using Lacuna.Pki.Pades;
using Lacuna.Pki.Pdf;
using MVC.Api.Models;
using MVC.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MVC.Api {
    public class BatchSignatureController : ApiController {
        
        /**
		* This method defines the signature policy that will be used on the signature.
		*/
        private IPadesPolicyMapper getSignaturePolicy() {
            var policy = PadesPoliciesForGeneration.GetPadesBasic(TrustArbitrators.PkiBrazil);

#if DEBUG
            // During debug only, we return a wrapper which will overwrite the policy's default trust arbitrator (which in this case
            // corresponds to the ICP-Brasil roots only), with our custom trust arbitrator which accepts test certificates
            // (see Util.GetTrustArbitrator())
            return new PadesPolicyMapperWrapper(policy, Util.GetTrustArbitrator());
#else
			return policy;
#endif
        }

        /**
        * This action is called asynchronously from the batch signature page in order to initiate the signature of each document
        * in the batch.
        */
        [HttpPost]
        public IHttpActionResult Start(BatchSignatureStartRequest request) {

            byte[] toSignBytes, transferData;
            SignatureAlgorithm signatureAlg;
            string formattedCode;

            try {

                // Decode the user's certificate
                var cert = PKCertificate.Decode(request.CertContent);

                // Generate verification code and format it
                var code = Util.GenerateVerificationCode();
                formattedCode = Util.FormatVerificationCode(code);

                // Instantiate PdfMarker class
                var pdfMarker = new PdfMarker();

                // Create pdf mark
                var pdfMark = new PdfMark() {
                    MeasurementUnits = PadesMeasurementUnits.Centimeters,
                    Container = new PadesVisualRectangle() {
                        Height = 0.7,
                        Bottom = 1.75,
                        Left = 0.6,
                        Right = 3.5
                    }
                };

                // Add text element to pdf mark
                var markText = new PdfMarkText() {
                    Opacity = 75
                };
                markText.Texts.Add(new PdfTextSection() {
                    Text = $"Este documento foi assinado digitalmente por { cert.SubjectDisplayName }.\r\nPara verificar a validade das assinaturas acesse a Minha Central de Verificação em http://localhost:49537/ e informe o código { formattedCode }",
                });
                pdfMark.Elements.Add(markText);

                // Add pdf mark to PdfMaker
                pdfMarker.AddMark(pdfMark);

                // Write marks on PDF before the signature
                var pdfContent = StorageMock.GetBatchDocContent(request.Id);
                var pdfWithMarks = pdfMarker.WriteMarks(pdfContent);

                // Instantiate a PadesSigner class
                var padesSigner = new PadesSigner();

                // Set the PDF to sign, which in the case of this example is one of the batch documents
                padesSigner.SetPdfToSign(pdfWithMarks);

                // Set the signer certificate
                padesSigner.SetSigningCertificate(cert);

                // Set the signature policy
                padesSigner.SetPolicy(getSignaturePolicy());

                // Generate the "to-sign-bytes". This method also yields the signature algorithm that must
                // be used on the client-side, based on the signature policy, as well as the "transfer data",
                // a byte-array that will be needed on the next step.
                toSignBytes = padesSigner.GetToSignBytes(out signatureAlg, out transferData);

            } catch (ValidationException ex) {
                // Some of the operations above may throw a ValidationException, for instance if the certificate
                // encoding cannot be read or if the certificate is expired.
                var message = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.ValidationResults.ToString());
                return ResponseMessage(message);
            }

            // For the next steps, we'll need once again some information:
            // - The "transfer data" filename. Its content is stored in a temporary file (with extension .bin) to
            // be shared with the Complete action.
            // - The "to-sign-hash" (digest of the "to-sign-bytes"). And the OID of the digest algorithm to be 
            // used during the signature operation. this information is need in the signature computation with
            // Web PKI component. (see batch-signature-form.js)
            return Ok(new BatchSignatureStartResponse() {
                TransferDataFileId = StorageMock.StoreFile(transferData, ".bin"),
                ToSignHash = signatureAlg.DigestAlgorithm.ComputeHash(toSignBytes),
                DigestAlgorithmOid = signatureAlg.DigestAlgorithm.Oid,
                VerificationCode = formattedCode
            });
        }

        /**
		* POST: Api/BatchSignature/Complete
		* 
		* This action is called once the "to-sign-hash" are signed using the user's certificate. After signature,
		* it'll be redirect to SignatureInfo action to show the signature file.
		*/
        [HttpPost]
        public IHttpActionResult Complete(BatchSignatureCompleteRequest request) {

            byte[] signatureContent;
            string fileId;

            try {

                // Recover the "transfer data" content stored in a temporary file
                string extension;
                byte[] transferDataContent;
                if (!StorageMock.TryGetFile(request.TransferDataFileId, out transferDataContent, out extension)) {
                    return NotFound();
                }

                // Instantiate a PadesSigner class
                var padesSigner = new PadesSigner();

                // Set the signature policy, exactly like in the Start method
                padesSigner.SetPolicy(getSignaturePolicy());

                // Set the signature computed on the client-side, along with the "transfer data" recovered from a temporary file
                padesSigner.SetPreComputedSignature(request.Signature, transferDataContent);

                // Call ComputeSignature(), which does all the work, including validation of the signer's certificate and of the 
                // resulting signature
                padesSigner.ComputeSignature();

                // Get the signed PDF as an array of bytes
                signatureContent = padesSigner.GetPadesSignature();

                // Store file
                fileId = StorageMock.StoreFile(signatureContent, ".pdf");

                // Register fileId using a unformatted version of the verification code (without hyphens). However we used the 
                // formatted version (with hyphens) on the PDF. Now, we remove the hyphens before storing it.
                var code = Util.ParseVerificationCode(request.VerificationCode);
                StorageMock.SetVerificationCode(fileId, code);

            } catch (ValidationException ex) {
                // Some of the operations above may throw a ValidationException, for instance if the certificate is revoked.
                var message = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.ValidationResults.ToString());
                return ResponseMessage(message);
            }

            return Ok(new BatchSignatureCompleteResponse() {
                SignedFileId = fileId
            });
        }
    }
}
