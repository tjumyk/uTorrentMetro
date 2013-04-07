using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace utorrentMetro.Converters
{
    class RatioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            double v = double.Parse(value.ToString());
            return v / 1000 ;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            // actually no need to conver back
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
