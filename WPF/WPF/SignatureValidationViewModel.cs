using Lacuna.Pki;
using Lacuna.Pki.Cades;
using Lacuna.Pki.Pades;
using Lacuna.Pki.Xml;
using Microsoft.Win32;
using NLog;
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

	class SignatureValidationViewModel : INotifyPropertyChanged {



		private static Logger logger = LogManager.GetCurrentClassLogger();

		private string _filePath;
		public string FilePath {
			get {
				return _filePath;
			}
			set {
				if (value != _filePath) {
					_filePath = value;
					NotifyPropertyChanged();
				}
			}
		}

		public ObservableCollection<SignerItem> Signers { get; private set; }

		private SignerItem _selectedSigner;
		public SignerItem SelectedSigner {
			get {
				return _selectedSigner;
			}
			set {
				if (value != _selectedSigner) {
					_selectedSigner = value;
					NotifyPropertyChanged();
				}
			}
		}

		private Window owner;

		public SignatureValidationViewModel() {
			Signers = new ObservableCollection<SignerItem>();
		}

		public void Initialize(Window owner) {
			this.owner = owner;
		}

		public void BrowseForFile() {
			var openFileDialog = new OpenFileDialog() {
				DefaultExt = ".cer",
				Filter = "PDF, XML or P7S files|*.pdf;*.xml;*.p7s"
			};

			if (openFileDialog.ShowDialog() == true) {
				FilePath = openFileDialog.FileName;
			}
		}

		public void Validate() {

			try {

				var fileInfo = new FileInfo(FilePath);
				if (!fileInfo.Exists) {
					MessageBox.Show("File not found: " + FilePath);
					return;
				}

				switch (fileInfo.Extension.ToLower()) {
					case ".pdf":
						validatePdfSignature(FilePath);
						break;
					case ".p7s":
						validateCadesSignature(FilePath);
						break;
					case ".xml":
						validateXmlSignature(FilePath);
						break;
					default:
						MessageBox.Show("Invalid file selected");
						break;
				}

			} catch (Exception ex) {
				logger.Error(ex, "Error validating signature");
				MessageBox.Show(ex.Message);
			}
		}

		public void ShowSignerValidationResults() {

			if (SelectedSigner == null) {
				return;
			}

			new ValidationResultsDialog(this.owner, "Signer: " + SelectedSigner.Description, SelectedSigner.ValidationResults).ShowDialog();
		}

		private void validatePdfSignature(string filePath) {
			var policy = PadesPoliciesForValidation.GetPadesBasic(App.GetTrustArbitrator());
			var padesSignature = PadesSignature.Open(filePath);
			Signers.Clear();
			foreach (var signer in padesSignature.Signers) {
				var vr = padesSignature.ValidateSignature(signer, policy);
				Signers.Add(new SignerItem(getSignerDescription(signer, vr), vr));
			}
		}

		private string getSignerDescription(PadesSignerInfo signer, ValidationResults vr) {
			var text = new StringBuilder();
			text.Append(getCertificateDescription(signer.Signer.SigningCertificate));
			if (signer.SigningTime != null) {
				text.AppendFormat(" at {0:g}", signer.SigningTime.Value.LocalDateTime);
			}
			if (vr.IsValid) {
				text.AppendFormat(" - Valid");
			} else {
				text.AppendFormat(" - INVALID");
			}
			return text.ToString();
		}

		private void validateCadesSignature(string filePath) {
			var policy = CadesPoliciesForValidation.GetCadesBasic(App.GetTrustArbitrator());
			var cadesSignature = CadesSignature.Open(filePath);
			if (!cadesSignature.HasEncapsulatedContent) {
				MessageBox.Show("This CAdES signature does not have an encapsulated content (\"detached signature\"). Please provide the data file to continue with the validation.", "Data file needed");
				var dataFileDialog = new OpenFileDialog() {
				};
				if (dataFileDialog.ShowDialog() != true) {
					return;
				}
				cadesSignature.SetExternalData(File.ReadAllBytes(dataFileDialog.FileName));
			}
			Signers.Clear();
			foreach (var signer in cadesSignature.Signers) {
				var vr = cadesSignature.ValidateSignature(signer, policy);
				Signers.Add(new SignerItem(getSignerDescription(signer, vr), vr));
			}
		}

		private string getSignerDescription(CadesSignerInfo signer, ValidationResults vr) {
			var text = new StringBuilder();
			text.Append(getCertificateDescription(signer.SigningCertificate));
			if (signer.SigningTime != null) {
				text.AppendFormat(" at {0:g}", signer.SigningTime.Value.LocalDateTime);
			}
			if (vr.IsValid) {
				text.AppendFormat(" - Valid");
			} else {
				text.AppendFormat(" - INVALID");
			}
			return text.ToString();
		}

		private void validateXmlSignature(string filePath) {
			var policy = XmlPolicySpec.GetXmlDSigBasic(App.GetTrustArbitrator());
			var xmlSigLocator = new XmlSignatureLocator(File.ReadAllBytes(filePath));
			Signers.Clear();
			foreach (var signature in xmlSigLocator.GetSignatures()) {
				var vr = signature.Validate(policy);
				Signers.Add(new SignerItem(getSignerDescription(signature, vr), vr));
			}
		}

		private string getSignerDescription(XmlSignature signature, ValidationResults vr) {
			var text = new StringBuilder();
			text.Append(getCertificateDescription(signature.SigningCertificate));
			if (signature.SigningTime != null) {
				text.AppendFormat(" at {0:g}", signature.SigningTime.Value.LocalDateTime);
			}
			text.AppendFormat(" of {0}", signature.SignedEntityType);
			if (signature.SignedEntityType == XmlSignedEntityTypes.XmlElement) {
				text.AppendFormat(" {0}", signature.SignedElement.Name);
			}
			if (vr.IsValid) {
				text.AppendFormat(" - Valid");
			} else {
				text.AppendFormat(" - INVALID");
			}
			return text.ToString();
		}

		private string getCertificateDescription(PKCertificate certificate) {
			var text = new StringBuilder();
			if (!string.IsNullOrEmpty(certificate.PkiBrazil.Responsavel)) {
				text.Append(certificate.PkiBrazil.Responsavel);
			} else {
				text.Append(certificate.SubjectName.CommonName);
			}
			if (!string.IsNullOrEmpty(certificate.PkiBrazil.CPF)) {
				text.AppendFormat(" (CPF: {0})", formatCpf(certificate.PkiBrazil.CPF));
			}
			return text.ToString();
		}

		private string formatCpf(string cpf) {
			if (string.IsNullOrEmpty(cpf) || cpf.Length != 11) {
				return cpf;
			}
			return string.Format("{0}.{1}.{2}-{3}", cpf.Substring(0, 3), cpf.Substring(3, 3), cpf.Substring(6, 3), cpf.Substring(9));
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
