using Lacuna.Pki.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using WebApi.Classes;

namespace WebApi.Models {
    public class OpenXmlSignatureResponse {
        public List<XmlSignatureModel> Signatures { get; set; }

        public OpenXmlSignatureResponse(List<XmlSignature> signatures) {
            Signatures = signatures.Select(s => new XmlSignatureModel(s)).ToList();
        }
    }

    public class XmlSignatureModel {
        public CertificateModel SigningCertificate { get; set; }
        public XmlElementModel SignedElement { get; set; }
        public XmlSignedEntityTypes SignedEntityType { get; set; }
        public DateTimeOffset? SigningTime { get; set; }
        public string SignaturePolicyId { get; set; }
        public ValidationErrorModel ValidationResults { get; set; }

        public XmlSignatureModel(XmlSignature signature) {
            SigningCertificate = new CertificateModel(signature.SigningCertificate);
            SignedElement = new XmlElementModel(signature.SignedElement);
            SignedEntityType = signature.SignedEntityType;
            SigningTime = signature.SigningTime;

            if (signature.PolicyIdentifier.SigPolicyId != null) {
                SignaturePolicyId = signature.PolicyIdentifier.SigPolicyId;
            }

            var validationPolicy = XmlPolicySpec.GetXmlDSigBasic(Util.GetTrustArbitrator());
            ValidationResults = new ValidationErrorModel(signature.Validate(validationPolicy));
        }
    }

    public class XmlElementModel {
        public string NamespaceURI { get; set; }
        public string LocalName { get; set; }

        public XmlElementModel(XmlElement element) {
            NamespaceURI = element.NamespaceURI;
            LocalName = element.LocalName;
        }
    }
}