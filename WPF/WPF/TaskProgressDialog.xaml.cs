using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace SampleWpfApp {
	/// <summary>
	/// Interaction logic for TaskProgressDialog.xaml
	/// </summary>
	public partial class TaskProgressDialog : Window {

		public static void Run(Func<TaskProgressDialog, Task> taskInvoker, Window owner = null) {
			var progressDialog = new TaskProgressDialog(taskInvoker, owner);
			progressDialog.ShowDialog();
			if (progressDialog.Exception != null) {
				throw progressDialog.Exception;
			}
		}

		public static T Run<T>(Func<TaskProgressDialog, Task<T>> taskInvoker, Window owner = null) {
			var progressDialog = new TaskProgressDialog(taskInvoker, owner);
			progressDialog.ShowDialog();
			if (progressDialog.Exception != null) {
				throw progressDialog.Exception;
			}
			return ((Task<T>)progressDialog.Task).Result;
		}

		private Func<TaskProgressDialog, Task> taskInvoker;
		private CancellationTokenSource tokenSource;

		public string Message {
			get {
				return MessageLabel.Content as string;
			}
			set {
				MessageLabel.Content = value;
			}
		}

		public double Progress {
			get {
				return TaskProgressBar.Value;
			}
			set {
				TaskProgressBar.Value = value;
			}
		}

		public Task Task { get; private set; }

		public CancellationToken CancellationToken {
			get {
				return tokenSource.Token;
			}
		}

		public Exception Exception { get; private set; }

		public TaskProgressDialog(Func<TaskProgressDialog, Task> taskInvoker, Window owner = null) {
			tokenSource = new CancellationTokenSource();
			if (owner != null) {
				this.Owner = owner;
				this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			} else {
				this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			}
			InitializeComponent();
			this.taskInvoker = taskInvoker;
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e) {
			Task = taskInvoker.Invoke(this);
			try {
				await Task;
				await Task.Delay(TimeSpan.FromMilliseconds(200)); // time for user to see an eventual final 100%
			} catch (Exception ex) {
				this.Exception = ex;
			} finally {
				this.Close();
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e) {
			CancelButton.IsEnabled = false;
			CancelButton.Content = "Cancelling ...";
			tokenSource.Cancel();
		}
	}
}
