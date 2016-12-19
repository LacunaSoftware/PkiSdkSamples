using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SampleWpfApp {

	[ValueConversion(typeof(bool), typeof(bool))]
	class NegateConverter : IValueConverter {

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return !(bool)value;
		}
	}

	[ValueConversion(typeof(bool), typeof(Visibility))]
	class VisibleWhenFalseConverter : IValueConverter {

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return !(bool)value ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotSupportedException();
		}
	}

	class EnumBooleanConverter : IValueConverter {

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {

			string parameterString = parameter as string;
			if (parameterString == null) {
				return DependencyProperty.UnsetValue;
			}
			if (Enum.IsDefined(value.GetType(), value) == false) {
				return DependencyProperty.UnsetValue;
			}

			object parameterValue = Enum.Parse(value.GetType(), parameterString);

			return parameterValue.Equals(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {

			string parameterString = parameter as string;
			if (parameterString == null || !(value is bool)) {
				return DependencyProperty.UnsetValue;
			}
			if (!(bool)value) {
				return DependencyProperty.UnsetValue;
			}

			return Enum.Parse(targetType, parameterString);
		}

	}

}
