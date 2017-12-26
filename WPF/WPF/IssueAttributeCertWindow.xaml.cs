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
	/// Interaction logic for IssueAttributeCertWindow.xaml
	/// </summary>
	public partial class IssueAttributeCertWindow : Window {

		private IssueAttributeCertViewModel viewModel;

		public IssueAttributeCertWindow() {
			InitializeComponent();
			viewModel = (IssueAttributeCertViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			viewModel.Initialize(this);
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e) {
			viewModel.RefreshCertificates();
		}

		private void IssueButton_Click(object sender, RoutedEventArgs e) {
			viewModel.Issue();
		}
	}
}
