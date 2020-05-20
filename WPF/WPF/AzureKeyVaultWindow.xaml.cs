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
	/// Interaction logic for AzureKeyVaultWindow.xaml
	/// </summary>
	public partial class AzureKeyVaultWindow : Window {

		private AzureKeyVaultViewModel viewModel;

		public AzureKeyVaultWindow() {
			InitializeComponent();
			viewModel = (AzureKeyVaultViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			viewModel.Initialize(this);
			viewModel.PropertyChanged += OnViewModelPropertyChanged;
		}

		private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(AzureKeyVaultViewModel.Log)) {
				LogTextBox.ScrollToEnd();
			}
		}

		private async void GenerateCsrButton_Click(object sender, RoutedEventArgs e) {
			await viewModel.IssueCsrAsync(AppSecretPasswordBox.Password);
		}

		private async void TestKeyButton_Click(object sender, RoutedEventArgs e) {
			await viewModel.TestKeyAsync(AppSecretPasswordBox.Password);
		}
	}
}
