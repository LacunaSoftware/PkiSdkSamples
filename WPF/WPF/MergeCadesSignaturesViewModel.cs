using Lacuna.Pki.Cades;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SampleWpfApp {

	public class MergeCadesSignaturesViewModel : INotifyPropertyChanged {

		public ObservableCollection<string> SignatureFiles { get; private set; } = new ObservableCollection<string>();

		private string encapsulatedContentFile;
		public string EncapsulatedContentFile {
			get {
				return this.encapsulatedContentFile;
			}
			set {
				if (value != encapsulatedContentFile) {
					this.encapsulatedContentFile = value;
					NotifyPropertyChanged();
				}
			}
		}

		private Window owner;

		public void Initialize(Window owner) {
			this.owner = owner;
		}

		public void MergeSignatures() {

			if (SignatureFiles.Count < 2) {
				MessageBox.Show("Please add at least two CAdES signature files");
				return;
			}

			if (!string.IsNullOrEmpty(EncapsulatedContentFile) && !File.Exists(EncapsulatedContentFile)) {
				MessageBox.Show($"Data file not found: {EncapsulatedContentFile}");
				return;
			}

			try {

				// Read encapsulated content file

				byte[] encapsulatedContent = null;
				if (!string.IsNullOrEmpty(EncapsulatedContentFile)) {
					encapsulatedContent = File.ReadAllBytes(EncapsulatedContentFile);
				}

				// read signature files

				var signatures = new List<CadesSignature>();
				foreach (var signatureFile in SignatureFiles) {
					signatures.Add(CadesSignature.Open(signatureFile));
				}

				// merge signatures

				var mergedSignature = CadesSignatureEditor.MergeSignatures(signatures, encapsulatedContent);

				// save file

				var saveFileDialog = new SaveFileDialog() {
					FileName = $"merged-signature.p7s",
				};

				if (saveFileDialog.ShowDialog() != true) {
					return;
				}

				File.WriteAllBytes(saveFileDialog.FileName, mergedSignature);
				MessageBox.Show($"Merged signature saved on {saveFileDialog.FileName}");

			} catch (Exception ex) {

				MessageBox.Show(ex.ToString(), "An error has occurred");

			}
		}

		public void ClearSignatureFiles() {
			SignatureFiles.Clear();
		}

		public void BrowseForEncapsulatedContentFile() {
			var openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true) {
				EncapsulatedContentFile = openFileDialog.FileName;
			}
		}

		public void BrowseForSignatureFile() {
			var openFileDialog = new OpenFileDialog() {
				DefaultExt = ".p7s",
				Filter = "CADES signature files (.p7s)|*.p7s",
				Multiselect = true
			};
			if (openFileDialog.ShowDialog() == true) {
				foreach (var file in openFileDialog.FileNames) {
					if (!SignatureFiles.Contains(file)) {
						SignatureFiles.Add(file);
					}
				}
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
