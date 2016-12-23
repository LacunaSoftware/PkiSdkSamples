using Lacuna.Pki;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Lacuna.Pki.Stores;
using Microsoft.Win32;
using System.Windows;
using NLog;

namespace SampleWpfApp {

	class CertificateValidationViewModel : INotifyPropertyChanged {

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private string certificatePath;
		public string CertificatePath {
			get {
				return this.certificatePath;
			}
			set {
				if (value != certificatePath) {
					this.certificatePath = value;
					NotifyPropertyChanged();
				}
			}
		}

		public void BrowseForCertificate() {
			var openFileDialog = new OpenFileDialog() {
				DefaultExt = ".cer",
				Filter = "Certificates (.cer)|*.cer"
			};

			if (openFileDialog.ShowDialog() == true) {
				CertificatePath = openFileDialog.FileName;
			}
		}

		public void Validate() {
			try {
				var cert = PKCertificate.Decode(System.IO.File.ReadAllBytes(CertificatePath));
				var vr = cert.Validate(App.GetTrustArbitrator());
				var msg = (vr.IsValid ? "Certificate is valid!" : "Certificate is not valid!");
				new ValidationResultsDialog(msg, vr).ShowDialog();
			} catch (Exception ex) {
				logger.Error(ex, "Error validating certificate");
				MessageBox.Show(ex.Message);
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
