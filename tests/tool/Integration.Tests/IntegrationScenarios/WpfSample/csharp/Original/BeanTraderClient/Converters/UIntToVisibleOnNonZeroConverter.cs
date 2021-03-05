using BeanTrader.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BeanTraderClient.Converters
{
    // Converts positive uints to Visible and 0 or invalid values to Collapsed
#pragma warning disable CA1812 // Unused internal class
    class UIntToVisibleOnNonZeroConverter : IValueConverter
#pragma warning restore CA1812 // Unused internal class
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dictionary<Beans, uint> inventory &&
                parameter is Beans beanType &&
                inventory.ContainsKey(beanType) &&
                inventory[beanType] > 0)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }
            

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
