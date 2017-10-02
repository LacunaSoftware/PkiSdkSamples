using Lacuna.Pki.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

        [JsonConverter(typeof(StringEnumConverter))]
        public XmlSignedEntityTypes SignedEntityType { get; set; }

        public CertificateModel SigningCertificate { get; set; }
        public XmlElementModel SignedElement { get; set; }
        public DateTimeOffset? SigningTime { get; set; }
        public string SignaturePolicyId { get; set; }
        public ValidationErrorModel ValidationModel { get; set; }

        public XmlSignatureModel(XmlSignature signature) {

            var validationPolicy = XmlPolicySpec.GetXmlDSigBasic(Util.GetTrustArbitrator());
            ValidationModel = new ValidationErrorModel(signature.Validate(validationPolicy));
            SignedEntityType = signature.SignedEntityType;
            SigningTime = signature.SigningTime;

            if (signature.SigningCertificate != null) {
                SigningCertificate = new CertificateModel(signature.SigningCertificate);
            }
            if (signature.SignedElement != null) {
                SignedElement = new XmlElementModel(signature.SignedElement);
            }
            if (signature.PolicyIdentifier != null) {
                SignaturePolicyId = signature.PolicyIdentifier.SigPolicyId;
            }
        }
    }

    public class XmlElementModel {
        public string NamespaceURI { get; set; }
        public string LocalName { get; set; }

        public XmlElementModel(XmlElement element) {
            LocalName = element.LocalName;

            if (!string.IsNullOrEmpty(element.NamespaceURI)) {
                NamespaceURI = element.NamespaceURI;
            }
        }
    }
}