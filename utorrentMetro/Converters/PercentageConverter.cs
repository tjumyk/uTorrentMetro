using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace utorrentMetro.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            double prog = double.Parse(value.ToString());
            return prog / 10 + " %";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            // actually no need to conver back
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
