using Lacuna.Pki.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

		public void LoadLicense() {

			var openFileDialog = new OpenFileDialog() {
				DefaultExt = ".config",
				Filter = "License file (.conifg)|*.config"
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
