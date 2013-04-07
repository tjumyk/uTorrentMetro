using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace utorrentMetro.Converters
{
    class TransferSpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            double size = double.Parse(value.ToString());
            if (size < 1024)
                return size + " Byte/s";
            else
            {
                size /= 1024;
                if (size < 1024)
                    return ((int)(size * 100)) / 100.0 + " KB/s";
                else
                {
                    size /= 1024;
                    if (size < 1024)
                        return ((int)(size * 100)) / 100.0 + " MB/s";
                    else
                    {
                        size /= 1024;
                        if (size < 1024)
                            return ((int)(size * 100)) / 100.0 + " GB/s";
                        else
                        {
                            size /= 1024;
                            return ((int)(size * 100)) / 100.0 + " TB/s";
                        }
                    }
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            // actually no need to conver back
            return Windows.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}
