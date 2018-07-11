using Lacuna.Pki;
using Lacuna.Pki.Pades;
using Lacuna.Pki.Stores;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SampleWpfApp {

	class PdfSignatureViewModel : INotifyPropertyChanged {

		private static Logger logger = LogManager.GetCurrentClassLogger();

		private string pdfPathValue;
		public string PdfPath {
			get {
				return this.pdfPathValue;
			}
			set {
				if (value != pdfPathValue) {
					this.pdfPathValue = value;
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

		public PdfSignatureViewModel() {
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

		public void Sign() {

			if (string.IsNullOrEmpty(PdfPath)) {
				MessageBox.Show("Please choose a PDF file sign");
				return;
			}
			if (!File.Exists(PdfPath)) {
				MessageBox.Show("File not found: " + PdfPath);
				return;
			}
			if (SelectedCertificate == null) {
				MessageBox.Show("Please choose a certificate to sign the PDF");
				return;
			}

			try {

				var signer = new PadesSigner();                     // Instantiate the PDF signer
				signer.SetSigningCertificate(selectedCertificate.CertificateWithKey);  // certificate with private key associated
				signer.SetPdfToSign(PdfPath);                       // PDF file path
				signer.SetPolicy(PadesPoliciesForGeneration.GetPadesBasic(App.GetTrustArbitrator())); // Basic signature policy with the selected trust arbitrator
				signer.SetVisualRepresentation(getVisualRepresentation(selectedCertificate.CertificateWithKey.Certificate)); // Signature visual representation
				signer.ComputeSignature();                          // computes the signature
				byte[] signedPdf = signer.GetPadesSignature();      // return the signed PDF bytes

				// saving signed PDF file
				var savePath = getSaveFilePath();
				if (!string.IsNullOrEmpty(savePath)) {
					File.WriteAllBytes(savePath, signedPdf);
					Process.Start(savePath);
				}

			} catch (ValidationException ex) {

				new ValidationResultsDialog("Validation failed", ex.ValidationResults).ShowDialog();

			} catch (Exception ex) {

				logger.Error(ex, "Error while signing PDF");
				MessageBox.Show(ex.Message);

			}
		}

		private PadesVisualRepresentation2 getVisualRepresentation(PKCertificate signerCert) {
			// Creating Visual Representation
			return new PadesVisualRepresentation2() {

				Position = new PadesVisualAutoPositioning() {              // Setting custom container for visual representation auto positioning
					MeasurementUnits = PadesMeasurementUnits.Centimeters,
					PageNumber = -1,                                       // Apply container in document last page
					SignatureRectangleSize = new PadesSize(7, 3),          // Signature visual representation rectangle dimensions
					Container = new PadesVisualRectangle() {               // Auto positioning container
						Left = 1.5,
						Right = 1.5,
						Bottom = 1.5,
						Height = 3
					},
					RowSpacing = 1.5                                       // Space between visual representation rows
				},

				Text = new PadesVisualText() {                     // Setting visual representation text
					SignerName = signerCert.SubjectDisplayName,    // Include signer name
					NationalId = signerCert.PkiBrazil.CPF,         // Include national Id
					Container = new PadesVisualRectangle() {       // Setting a text container relative to visual representation rectangle
						Left = 0.2,
						Top = 0.2,
						Right = 0.2,
						Bottom = 0.2
					}
				},

				Image = new PadesVisualImage() {           // Background image for visual representation
					Content = Resources.PdfStamp_png
				}
			};
		}

		public void BrowseForFile() {

			var openFileDialog = new OpenFileDialog() {
				DefaultExt = ".pdf",
				Filter = "PDF documents (.pdf)|*.pdf"
			};

			if (openFileDialog.ShowDialog() == true) {
				PdfPath = openFileDialog.FileName;
			}
		}

		private string getSaveFilePath() {

			var saveFileDialog = new SaveFileDialog() {
				Filter = "PDF documents (.pdf)|*.pdf",
				FilterIndex = 1,
				RestoreDirectory = true,
				FileName = Path.GetFileNameWithoutExtension(PdfPath) + "-signed.pdf"
			};

			if (saveFileDialog.ShowDialog() == true) {
				return saveFileDialog.FileName;
			}

			return null;
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
