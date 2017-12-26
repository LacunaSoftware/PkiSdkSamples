using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SampleWpfApp {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private MainWindowViewModel viewModel;

		public MainWindow() {
			InitializeComponent();
			viewModel = (MainWindowViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			viewModel.Initialize(this);
		}

		private void PdfSignatureButton_Click(object sender, RoutedEventArgs e) {
			viewModel.SignPdf();
		}

		private void CadesSignatureButton_Click(object sender, RoutedEventArgs e) {
			viewModel.SignCades();
		}

		private void CertificateValidationButton_Click(object sender, RoutedEventArgs e) {
			viewModel.ValidateCertificate();
		}

		private void SignatureValidationButton_Click(object sender, RoutedEventArgs e) {
			viewModel.ValidateSignature();
		}

		private void LoadLicenseButton_Click(object sender, RoutedEventArgs e) {
			viewModel.LoadLicense();
		}

		private void IssueAttributeCertButton_Click(object sender, RoutedEventArgs e) {
			viewModel.IssueAttributeCert();
		}
	}
}
