using Lacuna.Pki;
using Lacuna.Pki.Cades;
using Lacuna.Pki.Stores;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SampleWpfApp {

	class CadesSignatureViewModel : INotifyPropertyChanged {

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private bool coSignValue;
		public bool CoSign {
			get {
				return coSignValue;
			}
			set {
				if (value != coSignValue) {
					coSignValue = value;
					NotifyPropertyChanged();
				}
			}
		}

		private string filePathValue;
		public string FilePath {
			get {
				return this.filePathValue;
			}
			set {
				if (value != filePathValue) {
					this.filePathValue = value;
					NotifyPropertyChanged();
				}
			}
		}

		private string cmsPathValue;
		public string CmsPath {
			get {
				return this.cmsPathValue;
			}
			set {
				if (value != cmsPathValue) {
					this.cmsPathValue = value;
					NotifyPropertyChanged();
				}
			}
		}

		public ObservableCollection<CertificateItem> Certificates { get; private set; }

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

		public CadesSignatureViewModel() {
			Certificates = new ObservableCollection<CertificateItem>();
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
			certStore.GetCertificatesWithKey().ForEach(c => Certificates.Add(new CertificateItem(c)));

			if (originallySelected != null) {
				SelectedCertificate = Certificates.FirstOrDefault(c => c.CertificateWithKey.Certificate.Equals(originallySelected.CertificateWithKey.Certificate));
			}
		}

		public void BrowseForFile() {

			var openFileDialog = new OpenFileDialog();

			if (openFileDialog.ShowDialog() == true) {
				FilePath = openFileDialog.FileName;
			}
		}

		public void BrowseForCms() {

			var openFileDialog = new OpenFileDialog() {
				DefaultExt = ".p7s",
				Filter = "CADES signature files (.p7s)|*.p7s"
			};

			if (openFileDialog.ShowDialog() == true) {
				CmsPath = openFileDialog.FileName;
			}
		}

		public void Sign() {

			if (!checkParameters()) {
				return;
			}

			try {
				var success = TaskProgressDialog.Run(d => sign(d), owner);
				if (success) {
					MessageBox.Show("Process completed successfully", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			} catch (TaskCanceledException) {
				MessageBox.Show("Operation cancelled", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private async Task<bool> sign(TaskProgressDialog progressDialog) {

			try { 

				var signer = new CadesSigner();

				if (CoSign) {
					progressDialog.Message = "Reading existing CAdES signature ...";
				} else {
					progressDialog.Message = "Reading file ...";
				}
				await Task.Delay(TimeSpan.FromMilliseconds(100));

				if (CoSign) {
					var cmsBytes = await readAllBytesAsync(CmsPath, progressDialog.CancellationToken);
					signer.SetSignatureToCoSign(cmsBytes);
				} else {
					var fileBytes = await readAllBytesAsync(FilePath, progressDialog.CancellationToken);
					signer.SetDataToSign(fileBytes);
				}

				if (progressDialog.CancellationToken.IsCancellationRequested)
					return false;

				progressDialog.Progress = 33;
				progressDialog.Message = "Signing ...";
				await Task.Delay(TimeSpan.FromMilliseconds(100));

				signer.SetSigningCertificate(SelectedCertificate.CertificateWithKey);
				signer.SetPolicy(CadesPoliciesForGeneration.GetCadesBasic(App.GetTrustArbitrator()));
				signer.ComputeSignature();
				var signature = signer.GetSignature();

				if (progressDialog.CancellationToken.IsCancellationRequested)
					return false;

				progressDialog.Progress = 66;
				progressDialog.Message = "Saving signature ...";
				await Task.Delay(TimeSpan.FromMilliseconds(100));

				var saveFileDialog = new SaveFileDialog() {
					Filter = "CAdES signature files (.p7s)|*.p7s",
					FilterIndex = 1,
					FileName = CoSign ? string.Format("{0}-{1:yyyy-MM-dd-HHmmss}.p7s", Path.GetFileNameWithoutExtension(CmsPath), DateTime.Now) : FilePath + ".p7s"
				};
				if (saveFileDialog.ShowDialog() != true) {
					return false;
				}

				var outFilePath = saveFileDialog.FileName;
				await writeAllBytesAsync(outFilePath, signature, progressDialog.CancellationToken);

				if (progressDialog.CancellationToken.IsCancellationRequested)
					return false;

				progressDialog.Progress = 100;
				progressDialog.Message = "Completed!";
				return true;

			} catch (ValidationException ex) {

				new ValidationResultsDialog("Validation failed", ex.ValidationResults).ShowDialog();
				return false;

			} catch (Exception ex) {

				logger.Error(ex, "Error while performing CAdES signature");
				MessageBox.Show(ex.Message);
				return false;

			}
		}

		private async Task<byte[]> readAllBytesAsync(string path, CancellationToken ct) {
			using (var fileStream = File.OpenRead(path)) {
				using (var buffer = new MemoryStream()) {
					await fileStream.CopyToAsync(buffer, 1024 * 1024, ct);
					return buffer.ToArray();
				}
			}
		}

		private async Task writeAllBytesAsync(string path, byte[] content, CancellationToken ct) {
			using (var fileStream = File.OpenWrite(path)) {
				using (var buffer = new MemoryStream(content)) {
					await buffer.CopyToAsync(fileStream, 1024 * 1024, ct);
				}
			}
		}

		private bool checkParameters() {
			if (CoSign) {
				if (string.IsNullOrEmpty(CmsPath)) {
					MessageBox.Show("Please choose the CMS file to co-sign");
					return false;
				}
				if (!File.Exists(CmsPath)) {
					MessageBox.Show("File not found: " + CmsPath);
					return false;
				}
			} else {
				if (string.IsNullOrEmpty(FilePath)) {
					MessageBox.Show("Please choose the file sign");
					return false;
				}
				if (!File.Exists(FilePath)) {
					MessageBox.Show("File not found: " + FilePath);
					return false;
				}
			}
			if (SelectedCertificate == null) {
				MessageBox.Show("Please choose a certificate");
				return false;
			}
			return true;
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
