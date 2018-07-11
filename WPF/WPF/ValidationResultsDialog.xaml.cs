using Lacuna.Pki;
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
	/// Interaction logic for ValidationResultsDialog.xaml
	/// </summary>
	public partial class ValidationResultsDialog : Window {

		public ValidationResultsDialog(Window owner, string title, ValidationResults vr) {
			InitializeComponent();
			this.Owner = owner;
			this.Title = title;
			ValidationResultsTextBox.Text = vr.ToString();
		}

		public ValidationResultsDialog(string title, ValidationResults vr) {
			InitializeComponent();
			this.Title = title;
			ValidationResultsTextBox.Text = vr.ToString();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}
	}
}
