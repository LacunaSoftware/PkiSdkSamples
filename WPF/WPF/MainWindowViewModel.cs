using Lacuna.Pki;
using Lacuna.Pki.Stores;
using Lacuna.Pki.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SampleWpfApp {

	class MainWindowViewModel : INotifyPropertyChanged {

		private PredefinedTrustArbitrators _trustArbitrator;
		public PredefinedTrustArbitrators TrustArbitrator {
			get {
				return _trustArbitrator;
			}
			set {
				_trustArbitrator = value;
				App.TrustArbitrator = value;
				NotifyPropertyChanged();
			}
		}

		private bool _licenseLoaded;
		public bool LicenseLoaded {
			get {
				return _licenseLoaded;
			}
			set {
				_licenseLoaded = value;
				NotifyPropertyChanged();
			}
		}

		private Window owner;
		public void Initialize(Window owner) {
			this.owner = owner;
			TrustArbitrator = PredefinedTrustArbitrators.PkiBrazil;
			LicenseLoaded = (PkiInfo.License != null);
		}

		public void SignPdf() {
			if (checkLicenseLoaded()) {
				new PdfSignatureWindow().Show();
			}
		}

		public void SignCades() {
			if (checkLicenseLoaded()) {
				new CadesSignatureWindow().Show();
			}
		}

		public void ValidateCertificate() {
			if (checkLicenseLoaded()) {
				new CertificateValidationWindow().Show();
			}
		}

		public void ValidateSignature() {
			if (checkLicenseLoaded()) {
				new SignatureValidationWindow().Show();
			}
		}

		public void IssueAttributeCert() {
			if (checkLicenseLoaded()) {
				new IssueAttributeCertWindow().Show();
			}
		}

		public void ValidateAttributeCert() {

			if (!checkLicenseLoaded()) {
				return;
			}

			try {

				var certFileDialog = new OpenFileDialog() {
					DefaultExt = ".ac",
					Filter = "X.509 attribute certificate (.ac)|*.ac"
				};
				if (certFileDialog.ShowDialog() != true) {
					return;
				}

				// Read and decode the attribute certificate
				var certContent = File.ReadAllBytes(certFileDialog.FileName);
				var cert = AttributeCertificate.Decode(certContent);

				// If the certificate is issued without a link to its issuer (AIA extension), the validation will fail because the issuer will not be found. In this
				// case, have to provide the issuer certificate when decoding the attribute certificate.
				if (cert.IssuerNotFound) {

					MessageBox.Show("Could not find the issuer of the certificate. This usually happens with certificates that do not have a valid Authority Information Access (AIA) extension.\n\nTo continue, you will need to provide the .cer file of the issuer.", "Issuer not found");
					var issuerFileDialog = new OpenFileDialog() {
						DefaultExt = ".cer",
						Filter = "X.509 certificate|*.cer;*.crt"
					};
					if (issuerFileDialog.ShowDialog() != true) {
						return;
					}
					
					// Read and decode the issuer certificate
					var issuerContent = File.ReadAllBytes(issuerFileDialog.FileName);
					var issuerCert = PKCertificate.Decode(issuerContent);

					// Re-open the attribute certificate providing the issuer certificate
					cert = AttributeCertificate.Decode(certContent, new MemoryCertificateStore(new[] { issuerCert }));
				}

                CieStudentIdentity cieStudentIdentity = null;
                if (cert.Attributes.GetOids().Contains(CieStudentIdentity.Oid)) {
                    cieStudentIdentity = CieStudentIdentity.Decode(cert.Attributes);
                }

                CieStudentData cieStudentData = null;
                if (cert.Attributes.GetOids().Contains(CieStudentData.Oid)) {
                    cieStudentData = CieStudentData.Decode(cert.Attributes);
                }

				// Validate the certificate
				var vr = cert.Validate(App.GetTrustArbitrator());

				// Show the validation results
				new ValidationResultsDialog("Attribute certificate validation results", vr).ShowDialog();

			} catch (Exception ex) {

				MessageBox.Show(ex.ToString(), "An error has occurred");

			}
		}

		public void MergeCadesSignatures() {
			if (checkLicenseLoaded()) {
				new MergeCadesSignaturesWindow().Show();
			}
		}

		public void LoadLicense() {

			var openFileDialog = new OpenFileDialog() {
				DefaultExt = ".config",
				Filter = "License file (.config)|*.config"
			};

			if (openFileDialog.ShowDialog() == true) {
				try {
					Lacuna.Pki.PkiConfig.LoadLicense(openFileDialog.FileName);
					LicenseLoaded = true;
					MessageBox.Show("License loaded successfully!");
				} catch (Exception ex) {
					MessageBox.Show(ex.Message);
				}
			}
		}

		private bool checkLicenseLoaded() {
			if (LicenseLoaded) {
				return true;
			} else {
				MessageBox.Show("Please load the PKI SDK license file");
				return false;
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
