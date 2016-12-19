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
	/// Interaction logic for PdfSignatureWindow.xaml
	/// </summary>
	public partial class PdfSignatureWindow : Window {

		private PdfSignatureViewModel viewModel;

		public PdfSignatureWindow() {
			InitializeComponent();
			viewModel = (PdfSignatureViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			viewModel.Initialize(this);
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e) {
			viewModel.BrowseForFile();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e) {
			viewModel.RefreshCertificates();
		}

		private void SignButton_Click(object sender, RoutedEventArgs e) {
			viewModel.Sign();
		}
	}
}
