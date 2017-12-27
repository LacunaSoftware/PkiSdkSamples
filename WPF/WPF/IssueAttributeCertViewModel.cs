using Lacuna.Pki;
using Lacuna.Pki.Stores;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SampleWpfApp {

	class IssueAttributeCertViewModel : INotifyPropertyChanged {

		private const string IssuerName = "Your Company Inc.";

		public ObservableCollection<CertificateItem> Certificates { get; private set; } = new ObservableCollection<CertificateItem>();

		private CertificateItem selectedCertificate;
		public CertificateItem SelectedCertificate {
			get {
				return this.selectedCertificate;
			}
			set {
				if (value != selectedCertificate) {
					this.selectedCertificate = value;
					NotifyPropertyChanged();
				}
			}
		}

		private string name;
		public string Name {
			get {
				return this.name;
			}
			set {
				if (value != name) {
					this.name = value;
					NotifyPropertyChanged();
				}
			}
		}

		private Window owner;

		public void Initialize(Window owner) {
			this.owner = owner;
			RefreshCertificates();
		}

		public void RefreshCertificates() {

			var originallySelected = this.SelectedCertificate;

			Certificates.Clear();
			var certStore = WindowsCertificateStore.LoadPersonalCurrentUser();
			foreach (var item in certStore.GetCertificatesWithKey().Select(c => new CertificateItem(c)).OrderBy(i => i.ToString())) {
				Certificates.Add(item);
			}
			if (originallySelected != null) {
				SelectedCertificate = Certificates.FirstOrDefault(c => c.CertificateWithKey.Certificate.Equals(originallySelected.CertificateWithKey.Certificate));
			}
		}

		public void Issue() {

			if (SelectedCertificate == null) {
				MessageBox.Show("Please choose a certificate");
				return;
			}
			if (string.IsNullOrWhiteSpace(Name)) {
				MessageBox.Show("Please fill the name");
				return;
			}

			try {

				/**
				 * HolderName
				 * 
				 * http://www.une.org.br/site/wp-content/uploads/2017/01/Padra%CC%83o-Nacional-2017.pdf
				 * 
				 * Pag 9
				 * 
				 * O nome do titular do certificado de atributo, pessoa física, constante no campo Holder,
				 * deverá adotar o Distinguished Name (DN) do padrão ITU X.500/ISO 9594, da seguinte forma:
				 * 
				 *   C = BR
				 *   O = ICP-Brasil
				 *   OU = nome fantasia ou sigla da Entidade Emissora de Atributo (EEA)
				 *   CN = nome do titular do atributo				 *   				 * Na composição dos nomes, aplicam-se as restrições de nome conforme definido no
				 * item Restrição de nomes.				 */
				var normalizedName = Name.Trim().RemoveDiacritics().RemovePunctuation();
				var holderName = string.Format("C=BR, O=ICP-Brasil, OU={0}, CN={1}", IssuerName, normalizedName);

				// Expiration (we're using midnight from March 31st to April 1st of next year)
				var expiration = Util.GetMidnightOf(DateTime.Today.Year + 1, 3, 31, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time") /* Brasília */);

				var certGen = new AttributeCertificateGenerator();
				certGen.SetIssuer(SelectedCertificate.CertificateWithKey);
				certGen.SetHolderName(NameGenerator.GenerateFromDNString(holderName, NameGeneratorTypePolicies.PrintableStringsOnly));
				certGen.SetValidity(DateTimeOffset.Now, expiration);
				certGen.GenerateUniqueSerialNumber();
				certGen.SetSignatureAlgorithm(SignatureAlgorithm.SHA256WithRSA);
				certGen.SetExtensionNoRevocationAvailable();

				// Authority Information Access extension (optional)
				//certGen.SetCAIssuersUri(new Uri("http://ca.yourcompany.com/issuer.cer"));

				// Attribute #1
				var cieStudentIdentity = new CieStudentIdentity() {
					Cpf = "374.353.901-27",
					DataNascimento = new DateTime(2001, 9, 11),
					Matricula = "555.555",
					RG = "12.345.678",
					RGEmissor = "SSP",
					RGEmissorUF = "SP",
				};
				certGen.AddRawAttribute(CieStudentIdentity.Oid, cieStudentIdentity.Encode());

				// Attribute #2
				var cieStudentData = new CieStudentData() {
					Curso = "Engenharia da Computação",
					GrauEscolaridade = "Superior",
					InstituicaoEnsino = "Universidade de São Paulo",
					InstituicaoEnsinoCidade = "São Paulo",
					InstituicaoEnsinoUF = "SP",
				};
				certGen.AddRawAttribute(CieStudentData.Oid, cieStudentData.Encode());

				// Photo attribute (optional)
				var holderPhotoAttribute = new LacunaHolderPhotoAttribute() {
					MimeType = "image/jpeg",
					Content = Convert.FromBase64String("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"),
				};
				certGen.AddAttribute(LacunaHolderPhotoAttribute.Oid, holderPhotoAttribute);

				// Issue
				var cert = certGen.Generate();

				var saveFileDialog = new SaveFileDialog() {
					FileName = $"{normalizedName}.ac",
				};

				if (saveFileDialog.ShowDialog() != true) {
					return;
				}

				File.WriteAllBytes(saveFileDialog.FileName, cert.EncodedValue);
				MessageBox.Show($"Certificate saved on {saveFileDialog.FileName}");

			} catch (Exception ex) {

				MessageBox.Show($"An error has occurred: {ex}");

			}
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		// https://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged(v=vs.110).aspx
		// This method is called by the Set accessor of each property.
		// The CallerMemberName attribute that is applied to the optional propertyName
		// parameter causes the property name of the caller to be substituted as an argument.
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion
	}
}
