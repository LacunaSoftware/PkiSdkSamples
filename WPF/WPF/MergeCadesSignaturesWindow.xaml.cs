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
	/// Interaction logic for MergeCadesSignaturesWindow.xaml
	/// </summary>
	public partial class MergeCadesSignaturesWindow : Window {

		private MergeCadesSignaturesViewModel viewModel;

		public MergeCadesSignaturesWindow() {
			InitializeComponent();
			viewModel = (MergeCadesSignaturesViewModel)this.DataContext;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			this.viewModel.Initialize(this);
		}

		private void AddFileButton_Click(object sender, RoutedEventArgs e) {
			viewModel.BrowseForSignatureFile();
		}

		private void BrowseDataFileButton_Click(object sender, RoutedEventArgs e) {
			viewModel.BrowseForEncapsulatedContentFile();
		}

		private void MergeSignaturesButton_Click(object sender, RoutedEventArgs e) {
			viewModel.MergeSignatures();
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e) {
			viewModel.ClearSignatureFiles();
		}
	}
}
