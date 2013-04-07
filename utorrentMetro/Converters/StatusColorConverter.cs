using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace utorrentMetro.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            int statusCode = int.Parse(value.ToString());
            if ((statusCode & UtorrentClient.STATE_ERROR) > 0)
                return App.Current.Resources["error_color"];
            else if ((statusCode & UtorrentClient.STATE_PAUSED) > 0)
                return App.Current.Resources["paused_color"];
            else
                return App.Current.Resources["normal_color"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            // actually cannot conver back
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
