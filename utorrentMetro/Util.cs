using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace utorrentMetro
{
    public class Util
    {
        public static readonly String DEFAULT_TOAST_IMAGE = "ms-appx:///Assets/Logo.png";
        public static readonly String DEFAULT_TOAST_AUDIO = "Notification.Default";
        public static readonly String APPLICATION_ID = "App";
        
        public static void sendInfoToast(String msg) {
            sendToast(new List<String>() { msg }, ToastTemplateType.ToastText01, DEFAULT_TOAST_IMAGE, DEFAULT_TOAST_AUDIO);
        }

        public static void sendWarningToast(String msg) {
            sendToast(new List<String>() { msg },ToastTemplateType.ToastText01,DEFAULT_TOAST_IMAGE,"Notification.IM");
        }

        public static void sendToast(List<String> msgs, ToastTemplateType type, String imageUrl, String audioName)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(type);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            int length = Math.Min(msgs.Count,(int)stringElements.Length);
            for (int i = 0; i < length; i++)
            {
                stringElements[i].AppendChild(toastXml.CreateTextNode(msgs[i]));
            }

            XmlNodeList list = toastXml.GetElementsByTagName("image");
            if(list != null && list.Count > 0)
                list.First().Attributes[1].NodeValue = imageUrl;

            XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:"+audioName);
            toastXml.SelectSingleNode("/toast").AppendChild(audio);

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier(APPLICATION_ID).Show(toast);
        }

        public static long getTime()
        {
            DateTime timeStamp = new DateTime(1970, 1, 1);  //get timestamp of 1970,1,1
            return (DateTime.UtcNow.Ticks - timeStamp.Ticks) / 10000000;  //use UtcNow to avoid timezone problem
        }

        public static void log(object obj, [CallerMemberName] string caller = "?")
        {
            DateTime now = DateTime.Now;
            Debug.WriteLine("["+now.ToString("yyyy/MM/dd HH:mm:ss:fff")+"]["+caller+"] "+obj.ToString());
        }

        public static void update(object target, object newObj) {
            TypeInfo info = target.GetType().GetTypeInfo();
            foreach (PropertyInfo pinfo in info.DeclaredProperties)
            {
                object newValue = pinfo.GetValue(newObj, null);
                if (pinfo.GetValue(target, null) != newValue)
                    pinfo.SetValue(target, newValue);
            }
        }

        //public async static void scrollToAnimation(ScrollViewer view, double toOffset,bool isHorizontal = true) {
        //    int frames = 20;
        //    int time = 300;
        //    int interval = time / frames;
        //    if (isHorizontal)
        //    {
        //        double move = (toOffset - view.HorizontalOffset)/frames;
        //        for (int i = 0; i < frames; i++)
        //        {
        //            view.ScrollToHorizontalOffset(view.HorizontalOffset + move);
        //            await Task.Delay(interval);
        //        }
        //    }else {
        //        double move = (toOffset - view.VerticalOffset) / frames;
        //        for (int i = 0; i < frames; i++)
        //        {
        //            view.ScrollToVerticalOffset(view.VerticalOffset + move);
        //            await Task.Delay(interval);
        //        }
        //    }
        //}

        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }
    }
}
