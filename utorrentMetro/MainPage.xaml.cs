using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “分组项页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234231 上提供

namespace utorrentMetro
{
    /// <summary>
    /// 显示分组的项集合的页。
    /// </summary>
    public sealed partial class MainPage : utorrentMetro.Common.LayoutAwarePage
    {
        public static UtorrentClient client;

        public static bool cancelUpdate = false;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        /// <summary>
        /// 使用在导航过程中传递的内容填充页。在从以前的会话
        /// 重新创建页时，也会提供任何已保存状态。
        /// </summary>
        /// <param name="navigationParameter">最初请求此页时传递给
        /// <see cref="Frame.Navigate(Type, Object)"/> 的参数值。
        /// </param>
        /// <param name="pageState">此页在以前会话期间保留的状态
        /// 字典。首次访问页面时为 null。</param>
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // Init page and objects
            if (client == null)
            {
                if (pageState == null){// first run
                    client = new UtorrentClient();

                    // load old settings
                    object tmp;
                    if (ApplicationData.Current.LocalSettings.Values.TryGetValue("host", out tmp)) hostTextBox.Text = tmp as String;
                    if (ApplicationData.Current.LocalSettings.Values.TryGetValue("port", out tmp)) portTextBox.Text = tmp as String;
                    if (ApplicationData.Current.LocalSettings.Values.TryGetValue("username", out tmp)) usernameTextBox.Text = tmp as String;
                    if (ApplicationData.Current.LocalSettings.Values.TryGetValue("pass", out tmp)) passwordBox.Password = tmp as String;
                }
                else // load state from cache
                {
                    object oldClient = pageState["utClient"];
                    if (oldClient != null && oldClient is UtorrentClient)
                    {
                        client = (UtorrentClient)oldClient;
                        hostTextBox.Text = client.hostAddress;
                        portTextBox.Text = client.port;
                        usernameTextBox.Text = client.username;
                        passwordBox.Password = client.password;
                        cancelUpdate = true; // cancel any formal update loops
                        await Task.Delay((int)(1.1 * UtorrentClient.UPDATE_INTERVAL));
                        cancelUpdate = false;
                        client.startUpdate(false); // start update instantly and no need of initial data
                    }
                    else
                        client = new UtorrentClient();
                }
            }
            else
            {
                cancelUpdate = true; // cancel any formal update loops
                await Task.Delay((int)(1.1 * UtorrentClient.UPDATE_INTERVAL));
                cancelUpdate = false;
                client.startUpdate(false); // start update instantly and no need of initial data
            }
            cancelUpdate = false; // Make sure update loop can go

            // Bind data to UI
            this.DefaultViewModel["Torrents"] = client.currentTorrentList;
            this.DefaultViewModel["Feeds"] = client.currentRssFeedList;

            // Adjust page size dynamiclly
            Rect bounds = Window.Current.Bounds;
            loginPage.Width = torrentsPage.Width = feedsPage.Width = bounds.Width;// missed: devicesPage.Width =
            torrentGrid.Width = feedListPanel.Width = bounds.Width - 100; // missed: deviceGrid.Width =
            torrentGrid.Height = feedListPanel.Height = bounds.Height - 300;// missed: deviceGrid.Height =
            feedItemList.Width = feedListPanel.Width - feedList.Width;
            feedList.Height = feedItemList.Height = feedListPanel.Height;
        }

        /// <summary>
        /// 保留与此页关联的状态，以防挂起应用程序或
        /// 从导航缓存中放弃此页。值必须符合
        /// <see cref="SuspensionManager.SessionState"/> 的序列化要求。
        /// </summary>
        /// <param name="pageState">要使用可序列化状态填充的空字典。</param>
        protected async override void SaveState(Dictionary<String, Object> pageState)
        {
            if (client != null) {
                pageState.Add("utClient", client);
            }
        }

        private async void loginBtnClicked(object sender, RoutedEventArgs e) {
            try
            {
                loginBtn.IsEnabled = false;
                client.hostAddress = hostTextBox.Text;
                client.port = portTextBox.Text;
                client.username = usernameTextBox.Text;
                client.password = passwordBox.Password;
                await client.login();
                hostTextBox.IsEnabled = false;
                portTextBox.IsEnabled = false;
                usernameTextBox.IsEnabled = false;
                passwordBox.IsEnabled = false;
                loginBtn.Visibility = Visibility.Collapsed;
                logoutBtn.Visibility = Visibility.Visible;
                client.startUpdate(true);
                scrollViewer.ScrollToHorizontalOffset(loginPage.Width);

                // store settings
                ApplicationData.Current.LocalSettings.Values["host"] = client.hostAddress;
                ApplicationData.Current.LocalSettings.Values["port"] = client.port;
                ApplicationData.Current.LocalSettings.Values["username"] = client.username;
                ApplicationData.Current.LocalSettings.Values["pass"] = client.password;
            }
            catch (Exception e1) {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
            loginBtn.IsEnabled = true;
        }

        private async void logoutBtnClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                logoutBtn.IsEnabled = false;
                await client.logout();
                hostTextBox.IsEnabled = true;
                portTextBox.IsEnabled = true;
                usernameTextBox.IsEnabled = true;
                passwordBox.IsEnabled = true;
                logoutBtn.Visibility = Visibility.Collapsed;
                loginBtn.Visibility = Visibility.Visible;
                scrollViewer.ScrollToHorizontalOffset(0);
            }
            catch (Exception e1)
            {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
            logoutBtn.IsEnabled = true;
        }

        private async void DebugBtnClicked(object sender, RoutedEventArgs e)
        {
            client.currentTorrentList[0].Progress = new Random().Next(0, 1000) + "";
            client.currentTorrentList[0].Status = "233";
        }

        private void torrentItemClicked(object sender, ItemClickEventArgs e) {
            this.Frame.Navigate(typeof(TorrentDetailPage), e.ClickedItem);
        }

        private void rssFeedClicked(object sender, ItemClickEventArgs e)
        {
            this.DefaultViewModel["FeedItems"] = (e.ClickedItem as UtorrentClient.RssFeed).Items;
        }

        private async void rssFeedItemClicked(object sender, ItemClickEventArgs e)
        {
            UtorrentClient.RssFeedItem item = e.ClickedItem as UtorrentClient.RssFeedItem;
            Uri uri;
            if (Uri.TryCreate(item.URL, UriKind.Absolute, out uri)) {
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        private void OnTestTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = ((TextBox)(sender));
            String placeholder = tb.DataContext.ToString();
            if (tb.Text.Equals(placeholder, StringComparison.OrdinalIgnoreCase))
            {
                tb.Text = string.Empty;
            }
        }

        private void OnTestTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = ((TextBox)(sender));
            String placeholder = tb.DataContext.ToString();
            if (string.IsNullOrEmpty(tb.Text))
            {
                tb.Text = placeholder;
            }
        }

        private async void debugPrint(Object obj) {
            MessageDialog md = new MessageDialog(obj.ToString());
            await md.ShowAsync();
        }

        private void appBarClosed(object sender, object e)
        {
            foreach (UIElement ue in appBarGrid.Children)
                ue.Visibility = Visibility.Collapsed;
            commonAppBar.Visibility = Visibility.Visible;
        }

        private void torrentGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (torrentGrid.SelectedItems.Count > 0)
            {
                foreach (UIElement ue in appBarGrid.Children)
                    ue.Visibility = Visibility.Collapsed;
                commonAppBar.Visibility = Visibility.Visible;
                torrentAppBar.Visibility = Visibility.Visible;
                appBar.IsSticky = true;
                appBar.IsOpen = true;
            }
            else {
                appBar.IsSticky = false;
                appBar.IsOpen = false;
            }
        }

        private async void AddTorrentButtonClicked(object sender, RoutedEventArgs e)
        {
            // TODO Implement add-torrent module
            MessageDialog md = new MessageDialog("Not implemented!");
            await md.ShowAsync();
        }
        private void TorrentStartButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                client.excuteAction(torrentGrid.SelectedItems, UtorrentClient.TORRENT_CMD_START);
                torrentGrid.SelectedItems.Clear();
                appBar.IsOpen = false;
            }
            catch (Exception e1) {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
        }
        private void TorrentPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            try{
                client.excuteAction(torrentGrid.SelectedItems, UtorrentClient.TORRENT_CMD_PAUSE);
                torrentGrid.SelectedItems.Clear();
                appBar.IsOpen = false;
            }
            catch (Exception e1)
            {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
        }
        private void TorrentStopButtonClicked(object sender, RoutedEventArgs e)
        {
            try{
                client.excuteAction(torrentGrid.SelectedItems, UtorrentClient.TORRENT_CMD_STOP);
                torrentGrid.SelectedItems.Clear();
                appBar.IsOpen = false;
            }
            catch (Exception e1)
            {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
        }
        private async void TorrentRemoveButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                PopupMenu pm = new PopupMenu();
                pm.Commands.Add(new UICommand("Remove", (command) =>
                {
                    client.excuteAction(torrentGrid.SelectedItems, UtorrentClient.TORRENT_CMD_REMOVE);
                }));
                pm.Commands.Add(new UICommand("Remove with Torrent", (command) =>
                {
                    client.excuteAction(torrentGrid.SelectedItems, UtorrentClient.TORRENT_CMD_REMOVE_TORRENT);
                }));
                pm.Commands.Add(new UICommand("Remove with Data", (command) =>
                {
                    client.excuteAction(torrentGrid.SelectedItems, UtorrentClient.TORRENT_CMD_REMOVE_DATA);
                }));
                pm.Commands.Add(new UICommand("Remove with Torrent and Data", (command) =>
                {
                    client.excuteAction(torrentGrid.SelectedItems, UtorrentClient.TORRENT_CMD_REMOVE_DATA_TORRENT);
                }));
                await pm.ShowForSelectionAsync(Util.GetElementRect((FrameworkElement)sender));
                torrentGrid.SelectedItems.Clear();
                appBar.IsOpen = false;
            }
            catch (Exception e1)
            {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
        }

        private void helpButtonPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            helpImage.Visibility = Visibility.Visible;
        }

        private void helpButtonPointerExited(object sender, PointerRoutedEventArgs e)
        {
            helpImage.Visibility = Visibility.Collapsed;
        }

        private void torrentCategoryButtonClicked(object sender, RoutedEventArgs e)
        {
            MessageDialog md = new MessageDialog("Not implemented!");
            md.ShowAsync();
        }
    }
}
