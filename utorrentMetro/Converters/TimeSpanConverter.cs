using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace utorrentMetro.Converters
{
    class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            long v = long.Parse(value.ToString());
            if (v < 0)
                return "--";
            TimeSpan t = new TimeSpan(v * 10000000);
            if (t.Days > 62)
                return "> 2 months";
            else
            {
                StringBuilder sb = new StringBuilder();
                if (t.Days > 0)
                    sb.Append(t.Days + " day ");
                if (t.Hours > 0)
                    sb.Append(t.Hours + " hour ");
                if (t.Minutes > 0)
                    sb.Append(t.Minutes + " min ");
                sb.Append(t.Seconds + " sec");
                return sb.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            // actually no need to conver back
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
