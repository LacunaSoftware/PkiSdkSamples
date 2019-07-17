using Lacuna.Pki;
using Lacuna.Pki.Cades;
using Lacuna.Pki.Pades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace WebForms.Classes {

	public class PadesSignatureModel {
		public List<PadesSignerInfoModel> Signers { get; set; }

		public PadesSignatureModel(Lacuna.Pki.Pades.PadesSignature signature) {
			Signers = signature.Signers.Select(s => new PadesSignerInfoModel(s)).ToList();
		}

		public PadesSignatureModel(Lacuna.Pki.Pades.PadesSignature signature, IPadesPolicyMapper policyMapper) {
			Signers = signature.Signers.Select(s => new PadesSignerInfoModel(s, policyMapper, signature)).ToList();
		}
	}

	public enum CmsContentTypes {
		Data,
		SignedData,
		EnvelopedData,
		DigestedData,
		EncryptedData,
		AuthenticatedData,
		TstInfo
	}

	public class CadesSignatureModel {

		public CmsContentTypes EncapsulatedContentType { get; set; }
		public bool HasEncapsulatedContent { get; set; }
		public List<CadesSignerInfoModel> Signers { get; set; }
		public byte[] EncapsulatedContent { get; set; }

		public CadesSignatureModel(Lacuna.Pki.Cades.CadesSignature signature) {
			if (signature.EncapsulatedContentType == CmsContentType.Data) {
				EncapsulatedContentType = CmsContentTypes.Data;
			} else if (signature.EncapsulatedContentType == CmsContentType.SignedData) {
				EncapsulatedContentType = CmsContentTypes.SignedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.EnvelopedData) {
				EncapsulatedContentType = CmsContentTypes.EnvelopedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.DigestedData) {
				EncapsulatedContentType = CmsContentTypes.DigestedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.EncryptedData) {
				EncapsulatedContentType = CmsContentTypes.EncryptedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.AuthenticatedData) {
				EncapsulatedContentType = CmsContentTypes.AuthenticatedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.TstInfo) {
				EncapsulatedContentType = CmsContentTypes.TstInfo;
			}
			HasEncapsulatedContent = signature.HasEncapsulatedContent;
			EncapsulatedContent = signature.GetEncapsulatedContent();
			Signers = signature.Signers.Select(s => new CadesSignerInfoModel(s)).ToList();
		}

		public CadesSignatureModel(Lacuna.Pki.Cades.CadesSignature signature, ICadesPolicyMapper policyMapper) {
			if (signature.EncapsulatedContentType == CmsContentType.Data) {
				EncapsulatedContentType = CmsContentTypes.Data;
			} else if (signature.EncapsulatedContentType == CmsContentType.SignedData) {
				EncapsulatedContentType = CmsContentTypes.SignedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.EnvelopedData) {
				EncapsulatedContentType = CmsContentTypes.EnvelopedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.DigestedData) {
				EncapsulatedContentType = CmsContentTypes.DigestedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.EncryptedData) {
				EncapsulatedContentType = CmsContentTypes.EncryptedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.AuthenticatedData) {
				EncapsulatedContentType = CmsContentTypes.AuthenticatedData;
			} else if (signature.EncapsulatedContentType == CmsContentType.TstInfo) {
				EncapsulatedContentType = CmsContentTypes.TstInfo;
			}
			HasEncapsulatedContent = signature.HasEncapsulatedContent;
			EncapsulatedContent = signature.GetEncapsulatedContent();
			Signers = signature.Signers.Select(s => new CadesSignerInfoModel(s, policyMapper, signature)).ToList();
		}
	}

	public class CadesSignerInfoModel {
		public DigestAlgorithmAndValueModel MessageDigest { get; set; }
		public SignatureAlgorithmAndValueModel Signature { get; set; }
		public SignaturePolicyIdentifierModel SignaturePolicy { get; set; }
		public CertificateModel Certificate { get; set; }
		public DateTimeOffset? SigningTime { get; set; }
		public DateTimeOffset? CertifiedDateReference { get; set; }
		public List<CadesTimestampModel> Timestamps { get; set; }
		public ValidationResultsModel ValidationResults { get; set; }

		public CadesSignerInfoModel(CadesSignerInfo signerInfo, ICadesPolicyMapper policyMapper, Lacuna.Pki.Cades.CadesSignature signature) : this(signerInfo) {
			ValidationResults = new ValidationResultsModel(signerInfo, policyMapper, signature);
		}

		public CadesSignerInfoModel(CadesSignerInfo signerInfo) {
			MessageDigest = new DigestAlgorithmAndValueModel(signerInfo.DigestAlgorithm, signerInfo.MessageDigest);
			Signature = new SignatureAlgorithmAndValueModel(signerInfo.SignatureAlgorithm, signerInfo.SignatureValue);
			if (signerInfo.SignaturePolicy != null) {
				SignaturePolicy = new SignaturePolicyIdentifierModel(signerInfo.SignaturePolicy);
			}
			Certificate = new CertificateModel(signerInfo.SigningCertificate);
			Timestamps = signerInfo.SignatureTimeStamps.Select(s => new CadesTimestampModel(s)).ToList();
			try {

				bool isCertified;
				var dateReference = signerInfo.GetDateReference(out isCertified);
				if (isCertified) {
					CertifiedDateReference = dateReference;
				}
			} catch {

			}
		}
	}

	public class PadesSignerInfoModel : CadesSignerInfoModel {
		public bool IsDocumentTimestamp { get; set; }
		public string SignatureFieldName { get; set; }

		public PadesSignerInfoModel(PadesSignerInfo signerInfo, IPadesPolicyMapper policyMapper, Lacuna.Pki.Pades.PadesSignature signature) : this(signerInfo) {
			ValidationResults = new ValidationResultsModel(signerInfo, policyMapper, signature);
		}

		public PadesSignerInfoModel(PadesSignerInfo signerInfo) : base(signerInfo.Signer) {
			IsDocumentTimestamp = signerInfo.IsTsp;
			SignatureFieldName = signerInfo.SignatureFieldName;
			SigningTime = signerInfo.SigningTime;
		}
	}

	public class SignaturePolicyIdentifierModel {

		public DigestAlgorithmAndValueModel Digest { get; set; }
		public string Oid { get; set; }
		public string Uri { get; set; }

		public SignaturePolicyIdentifierModel(CadesSignaturePolicyInfo policy) {
			Oid = policy.Oid;
			Digest = new DigestAlgorithmAndValueModel(policy.Digest.Algorithm, policy.Digest.Value);
			Uri = policy.Uri.ToString();
		}
	}

	public enum DigestAlgorithms {
		MD5 = 1,
		SHA1,
		SHA256,
		SHA384,
		SHA512
	}

	public class DigestAlgorithmAndValueModel {

		public DigestAlgorithms Algorithm { get; set; }
		public byte[] Value { get; set; }
		public string HexValue { get; set; }

		public DigestAlgorithmAndValueModel(DigestAlgorithm digest, byte[] value) {
			if (digest == DigestAlgorithm.MD5) {
				Algorithm = DigestAlgorithms.MD5;
			} else if (digest == DigestAlgorithm.SHA1) {
				Algorithm = DigestAlgorithms.SHA1;
			} else if (digest == DigestAlgorithm.SHA256) {
				Algorithm = DigestAlgorithms.SHA256;
			} else if (digest == DigestAlgorithm.SHA384) {
				Algorithm = DigestAlgorithms.SHA384;
			} else if (digest == DigestAlgorithm.SHA512) {
				Algorithm = DigestAlgorithms.SHA512;
			}
			Value = value;
			HexValue = string.Join("", value.Select(b => b.ToString("X2")));
		}
	}

	public enum SignatureAlgorithms {
		MD5WithRSA = 1,
		SHA1WithRSA,
		SHA256WithRSA,
		SHA384WithRSA,
		SHA512WithRSA
	}

	public class SignatureAlgorithmAndValueModel {

		public SignatureAlgorithms AlgorithmIdentifier;
		public byte[] Value { get; set; }
		public string HexValue { get; set; }

		public SignatureAlgorithmAndValueModel(SignatureAlgorithm alg, byte[] value) {
			if (alg == SignatureAlgorithm.MD5WithRSA) {
				AlgorithmIdentifier = SignatureAlgorithms.MD5WithRSA;
			} else if (alg == SignatureAlgorithm.SHA1WithRSA) {
				AlgorithmIdentifier = SignatureAlgorithms.SHA1WithRSA;
			} else if (alg == SignatureAlgorithm.SHA256WithRSA) {
				AlgorithmIdentifier = SignatureAlgorithms.SHA256WithRSA;
			} else if (alg == SignatureAlgorithm.SHA384WithRSA) {
				AlgorithmIdentifier = SignatureAlgorithms.SHA384WithRSA;
			} else if (alg == SignatureAlgorithm.SHA512WithRSA) {
				AlgorithmIdentifier = SignatureAlgorithms.SHA512WithRSA;
			}
			Value = value;
			HexValue = string.Join("", value.Select(b => b.ToString("X2")));
		}
	}

	public class CadesTimestampModel : CadesSignatureModel {

		public DateTimeOffset GenTime { get; set;  }
		public string SerialNumber { get; set; }
		public DigestAlgorithmAndValueModel MessageImprint { get; set; }

		public CadesTimestampModel(CadesTimestamp timestamp) : base(timestamp)  {
			GenTime = timestamp.GenTime;
			MessageImprint = new DigestAlgorithmAndValueModel(timestamp.TstInfo.MessageImprint.Algorithm, timestamp.TstInfo.MessageImprint.Value);
		}
	}

	public class CertificateModel {

		public NameModel SubjectName { get; set; }
		public string EmailAddress { get; set; }
		public NameModel IssuerName { get; set; }
		public string SerialNumber { get; set; }
		public DateTimeOffset ValidityStart { get; set; }
		public DateTimeOffset ValidityEnd { get; set; }
		public CertificateModel Issuer { get; set; }
		public PkiBrazilCertificateModel PkiBrazil { get; set; }

		public CertificateModel(PKCertificate c) {
			SubjectName = new NameModel(c.SubjectName);
			EmailAddress = c.EmailAddress;
			IssuerName = new NameModel(c.IssuerName);
			ValidityStart = c.ValidityStart;
			ValidityEnd = c.ValidityEnd;
			PkiBrazil = new PkiBrazilCertificateModel(c.PkiBrazil);
			if (!c.IsSelfSigned) {
				Issuer = new CertificateModel(c.Issuer);
			}
		}
	}

	public class NameModel {

		public string String { get; set; }
		public string Country { get; set; }
		public string Organization { get; set; }
		public string OrganizationUnit { get; set; }
		public string DNQualifier { get; set; }
		public string StateName { get; set; }
		public string CommonName { get; set; }
		public string SerialNumber { get; set; }
		public string Locality { get; set; }
		public string Title { get; set; }
		public string Surname { get; set; }
		public string GivenName { get; set; }
		public string Initials { get; set; }
		public string Pseudonym { get; set; }
		public string GenerationQualifier { get; set; }
		public string EmailAddress { get; set; }

		public NameModel(Name n) {
			String = n.ToString();
			Country = n.Country;
			Organization = n.Organization;
			DNQualifier = n.DNQualifier;
			StateName = n.StateName;
			CommonName = n.CommonName;
			SerialNumber = n.SerialNumber;
			Locality = n.Locality;
			Title = n.Title;
			Surname = n.Surname;
			Initials = n.Initials;
			Pseudonym = n.Pseudonym;
			GenerationQualifier = n.GenerationQualifier;
			EmailAddress = n.EmailAddress;
		}
	}

	public class PkiBrazilCertificateModel {

		public string CertificateType { get; set; }
		public string Cpf { get; set; }
		public string Cnpj { get; set; }
		public string Responsavel { get; set; }
		public string DateOfBirth { get; set; }
		public string CompanyName { get; set; }
		public string OabUF { get; set; }
		public string OabNumero { get; set; }
		public string RGEmissor { get; set; }
		public string RGEmissorUF { get; set; }
		public string RGNumero { get; set; }

		public PkiBrazilCertificateModel(IcpBrasilCertificateFields c) {
			CertificateType = c.CertificateType.ToString();
			Cpf = c.CPF;
			Cnpj = c.Cnpj;
			Responsavel = c.Responsavel;
			DateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd");
			Responsavel = c.Responsavel;
			OabUF = c.OabUF;
			OabNumero = c.OabNumero;
			RGEmissor = c.RGEmissor;
			RGEmissorUF = c.RGEmissorUF;
			RGNumero = c.RGNumero;
		}
	}

	public class ValidationResultsModel {
		public List<ValidationItemModel> PassedChecks { get; set; }
		public List<ValidationItemModel> Errors { get; set; }
		public List<ValidationItemModel> Warnings { get; set; }
		public int ChecksPerformed {
			get {
				return PassedChecks.Count + Errors.Count + Warnings.Count;
			}
		}
		public bool IsValid {
			get {
				return Errors.Count == 0;
			}
		}
		public bool HasErrors {
			get {
				return this.Errors.Count > 0;
			}
		}
		public bool HasWarnings {
			get {
				return this.Warnings.Count > 0;
			}
		}

		public ValidationResultsModel(PadesSignerInfo signerInfo, IPadesPolicyMapper policyMapper, Lacuna.Pki.Pades.PadesSignature signature) : this(signature.ValidateSignature(signerInfo, policyMapper)) {

		}

		public ValidationResultsModel(CadesSignerInfo signerInfo, ICadesPolicyMapper policyMapper, Lacuna.Pki.Cades.CadesSignature signature) :
			this(signature.ValidateSignature(signerInfo, policyMapper)) {

		}

		public ValidationResultsModel(ValidationResults vr) {
			PassedChecks = vr.PassedChecks.ConvertAll(i => new ValidationItemModel(i));
			Warnings = vr.Warnings.ConvertAll(i => new ValidationItemModel(i));
			Errors = vr.Errors.ConvertAll(i => new ValidationItemModel(i));
		}

		public override string ToString() {
			return ToString(0);
		}

		internal string ToString(int identationLevel) {
			var tab = new String('\t', identationLevel);
			var text = new StringBuilder();
			text.Append(getSummary(identationLevel));
			if (Errors.Count > 0) {
				text.AppendLine();
				text.AppendLine(tab + "Errors:");
				text.Append(String.Join(Environment.NewLine, Errors.Select(e => tab + "- " + e.ToString(identationLevel)).ToArray()));
			}
			if (Warnings.Count > 0) {
				text.AppendLine();
				text.AppendLine(tab + "Warnings:");
				text.Append(String.Join(Environment.NewLine, Warnings.Select(w => tab + "- " + w.ToString(identationLevel)).ToArray()));
			}
			if (PassedChecks.Count > 0) {
				text.AppendLine();
				text.AppendLine(tab + "Passed checks:");
				text.Append(String.Join(Environment.NewLine, PassedChecks.Select(e => tab + "- " + e.ToString(identationLevel)).ToArray()));
			}
			return text.ToString();
		}

		private string getSummary(int identationLevel) {
			var tab = new String('\t', identationLevel);
			var text = new StringBuilder();
			text.Append(tab + "Validation results:");
			if (ChecksPerformed == 0) {
				text.Append("no checks performed");
			} else {
				text.AppendFormat(String.Format("{0} checks performed", ChecksPerformed));
				if (Errors.Count > 0) {
					text.AppendFormat(String.Format(", {0} errors", Errors.Count));
				}
				if (Warnings.Count > 0) {
					text.AppendFormat(String.Format(", {0} warnings", Warnings.Count));
				}
				if (PassedChecks.Count > 0) {
					if (Errors.Count == 0 && Warnings.Count == 0) {
						text.Append(", all passed");
					} else {
						text.AppendFormat(String.Format(", {0} passed", PassedChecks.Count));
					}
				}
			}
			return text.ToString();
		}
	}

	public class ValidationItemModel {
		public ValidationItemTypes Type { get; set; }
		public string Message { get; set; }
		public string Detail { get; set; }
		public ValidationResultsModel InnerValidationResults { get; set; }

		public ValidationItemModel(ValidationItem vi) {
			Type = vi.Type;
			Message = vi.Message;
			Detail = vi.Detail;
			InnerValidationResults = vi.InnerValidationResults != null ? new ValidationResultsModel(vi.InnerValidationResults) : null;
		}

		public override string ToString() {
			return ToString(0);
		}

		internal string ToString(int identationLevel) {
			var text = new StringBuilder();
			text.Append(Message);
			if (!String.IsNullOrEmpty(Detail)) {
				text.AppendFormat(" ({0})", Detail);
			}
			if (InnerValidationResults != null) {
				text.AppendLine();
				text.Append(InnerValidationResults.ToString(identationLevel + 1));
			}
			return text.ToString();
		}
	}
}