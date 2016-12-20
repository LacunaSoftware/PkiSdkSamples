using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models {

	public class CertificateModel {

		private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
			SerialNumber = c.SerialNumber.ToString();
			ValidityStart = c.ValidityStart;
			ValidityEnd = c.ValidityEnd;
			PkiBrazil = new PkiBrazilCertificateModel(c.PkiBrazil);
			if (!c.IsSelfSigned) {
				try {
					Issuer = new CertificateModel(c.Issuer);
				} catch (Exception ex) {
					logger.Warn(ex, "Error converting certificate issuer to model");
				}
			}
		}
	}

	public class NameModel {

		public string String {
			get; set;
		}

		public string Country {
			get; set;
		}

		public string Organization {
			get; set;
		}

		public string OrganizationUnit {
			get; set;
		}

		public string DNQualifier {
			get; set;
		}

		public string StateName {
			get; set;
		}

		public string CommonName {
			get; set;
		}

		public string SerialNumber {
			get; set;
		}

		public string Locality {
			get; set;
		}

		public string Title {
			get; set;
		}

		public string Surname {
			get; set;
		}

		public string GivenName {
			get; set;
		}

		public string Initials {
			get; set;
		}

		public string Pseudonym {
			get; set;
		}

		public string GenerationQualifier {
			get; set;
		}

		public string EmailAddress {
			get; set;
		}

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
			DateOfBirth = (c.DateOfBirth != null ? c.DateOfBirth.Value.ToString("yyyy-MM-dd") : null);
			Responsavel = c.Responsavel;
			OabUF = c.OabUF;
			OabNumero = c.OabNumero;
			RGEmissor = c.RGEmissor;
			RGEmissorUF = c.RGEmissorUF;
			RGNumero = c.RGNumero;
		}
	}
}
