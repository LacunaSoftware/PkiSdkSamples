using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SampleWpfApp {
	/// <summary>
	/// Interaction logic for CertificateValidationWindow.xaml
	/// </summary>
	public partial class CertificateValidationWindow : Window {

		private CertificateValidationViewModel viewModel;

		public CertificateValidationWindow() {
			InitializeComponent();
			viewModel = (CertificateValidationViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
		}

		private void ValidateButton_Click(object sender, RoutedEventArgs e) {
			viewModel.Validate();
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e) {
			viewModel.BrowseForCertificate();
		}
	}
}
