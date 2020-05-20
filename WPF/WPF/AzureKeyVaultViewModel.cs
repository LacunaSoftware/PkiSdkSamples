using Lacuna.Pki;
using Lacuna.Pki.AzureConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SampleWpfApp {

	class AzureKeyVaultViewModel : INotifyPropertyChanged {

		private string endpoint;
		public string Endpoint {
			get {
				return this.endpoint;
			}
			set {
				if (value != endpoint) {
					this.endpoint = value;
					NotifyPropertyChanged();
				}
			}
		}

		private string appId;
		public string AppId {
			get {
				return this.appId;
			}
			set {
				if (value != appId) {
					this.appId = value;
					NotifyPropertyChanged();
				}
			}
		}

		private string keyName;
		public string KeyName {
			get {
				return this.keyName;
			}
			set {
				if (value != keyName) {
					this.keyName = value;
					NotifyPropertyChanged();
				}
			}
		}

		private string log;
		public string Log {
			get {
				return this.log;
			}
			set {
				if (value != log) {
					this.log = value;
					NotifyPropertyChanged();
				}
			}
		}

		private Window owner;

		public void Initialize(Window owner) {
			this.owner = owner;
		}

		public async Task IssueCsrAsync(string appSecret) {

			if (!checkParameters(appSecret)) {
				return;
			}

			try {

				var key = await getKeyAsync(appSecret);
				var digestAlg = DigestAlgorithm.SHA256;

				var csrGenerator = new CsrGenerator() {
					PublicKey = key.PublicKey,
				};
				var toSignData = csrGenerator.GetToSignBytes();
				var csrSignature = key.GetSignatureCsp(digestAlg).SignData(toSignData);
				csrGenerator.SetPrecomputedSignature(key.PublicKey.Algorithm.GetSignatureAlgorithm(digestAlg), csrSignature);
				var csr = csrGenerator.Generate();

				writeLog(csr);

			} catch (Exception ex) {
				onError(ex);
			}
		}

		public async Task TestKeyAsync(string appSecret) {

			if (!checkParameters(appSecret)) {
				return;
			}

			try { 
				var key = await getKeyAsync(appSecret);
				var toSignData = BitConverter.GetBytes(DateTimeOffset.Now.Ticks);
				var signature = key.GetSignatureCsp(DigestAlgorithm.SHA256).SignData(toSignData);
				writeLog($"To sign data: {BitConverter.ToString(toSignData)}\r\nSignature: {BitConverter.ToString(signature)}");

			} catch (Exception ex) {
				onError(ex);
			}
		}

		private bool checkParameters(string appSecret) {
			if (string.IsNullOrEmpty(Endpoint)) {
				MessageBox.Show("Please fill the Endpoint field with the \"DNS Name\" of the key vault");
				return false;
			}
			if (string.IsNullOrEmpty(AppId)) {
				MessageBox.Show("Please provide the Application ID of an application registered on Azure Active Directory");
				return false;
			}
			if (string.IsNullOrEmpty(appSecret)) {
				MessageBox.Show("Please provide an authentication secret for the application, generated on Certificates & secrets");
				return false;
			}
			if (string.IsNullOrEmpty(KeyName)) {
				MessageBox.Show("Please provide the key name");
				return false;
			}
			return true;
		}

		private async Task<AzureKey> getKeyAsync(string appSecret) {
			var options = new AzureKeyVaultOptions() {
				Endpoint = Endpoint,
				AppId = AppId,
				AppSecret = appSecret,
			};
			var azureKeyProvider = new AzureKeyProvider(options);
			return await azureKeyProvider.GetKeyAsync(KeyName);
		}

		private void onError(Exception ex) {
			MessageBox.Show(ex.Message);
			writeLog(ex.ToString());
		}

		private void writeLog(string s) {
			Log += $"\r\n{s}\r\n";
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
