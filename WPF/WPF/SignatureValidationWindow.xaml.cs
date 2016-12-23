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
	
	public partial class SignatureValidationWindow : Window {

		private SignatureValidationViewModel viewModel;

		public SignatureValidationWindow() {
			InitializeComponent();
			viewModel = (SignatureValidationViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			viewModel.Initialize(this);
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e) {
			viewModel.BrowseForFile();
		}

		private void ValidateButton_Click(object sender, RoutedEventArgs e) {
			viewModel.Validate();
		}

		private void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			viewModel.ShowSignerValidationResults();
		}
	}
}
