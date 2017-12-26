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

			/*
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
			 *   CN = nome do titular do atributo			 *   			 * Na composição dos nomes, aplicam-se as restrições de nome conforme definido no
			 * item Restrição de nomes.			 */
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
