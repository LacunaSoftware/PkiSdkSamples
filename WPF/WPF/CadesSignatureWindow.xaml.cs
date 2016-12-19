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
	/// Interaction logic for CadesSignatureWindow.xaml
	/// </summary>
	public partial class CadesSignatureWindow : Window {

		private CadesSignatureViewModel viewModel;

		public CadesSignatureWindow() {
			InitializeComponent();
			viewModel = (CadesSignatureViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			viewModel.Initialize(this);
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e) {
			viewModel.BrowseForFile();
		}

		private void BrowseCmsButton_Click(object sender, RoutedEventArgs e) {
			viewModel.BrowseForCms();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e) {
			viewModel.RefreshCertificates();
		}

		private void SignButton_Click(object sender, RoutedEventArgs e) {
			viewModel.Sign();
		}

		
	}
}
