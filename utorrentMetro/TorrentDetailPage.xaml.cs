using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “基本页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234237 上有介绍

namespace utorrentMetro
{
    /// <summary>
    /// 基本页，提供大多数应用程序通用的特性。
    /// </summary>
    public sealed partial class TorrentDetailPage : utorrentMetro.Common.LayoutAwarePage
    {
        private bool keepUpdate = true;
        private bool userRemoved = false;
        public ObservableCollection<UtorrentClient.File> currentFileList = new ObservableCollection<UtorrentClient.File>();

        public TorrentDetailPage()
        {
            this.InitializeComponent();
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
            if (navigationParameter != null && navigationParameter is UtorrentClient.Torrent)
            {
                UtorrentClient.Torrent t = navigationParameter as UtorrentClient.Torrent;
                this.DefaultViewModel["Torrent"] = t;
                t.PropertyChanged += TorrentPropertyChanged;

                keepUpdate = true;
                userRemoved = false;
                this.DefaultViewModel["FileList"] = currentFileList;
                updateFileList();
            }
            
        }

        private async void updateFileList() {
            while (!App.stopBackgroundTask && keepUpdate) {
                Util.log("Refreshing file list...");
                UtorrentClient.Torrent t = DefaultViewModel["Torrent"] as UtorrentClient.Torrent;
                List<UtorrentClient.File> list = await MainPage.client.getFileList(t);
                if (list.Count != currentFileList.Count) // New List
                {
                    currentFileList.Clear();
                    foreach (UtorrentClient.File f in list)
                        currentFileList.Add(f);
                }
                else                                    // Update List
                {
                    int index = 0;
                    foreach (UtorrentClient.File f in list)
                    {
                        Util.update(currentFileList[index],f);
                        index++;
                    }
                }
                if (int.Parse(t.Progress) >= 1000)// Torrent alreay finished
                {
                    bool finished = currentFileList.Count > 0; // Test if the file list alreay finished
                    foreach (UtorrentClient.File f in currentFileList) {
                        if (f.Downloaded != f.Size) {
                            finished = false;
                            break;
                        }
                    }
                    if (finished)
                    {
                        keepUpdate = false;
                        break;
                    }
                }
                await Task.Delay(UtorrentClient.UPDATE_INTERVAL);
            }
        }

        // To keep an eye on if this torrent has been deleted
        private async void TorrentPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ToBeRemoved") {
                UtorrentClient.Torrent t = sender as UtorrentClient.Torrent;
                if (t.ToBeRemoved == true) {
                    keepUpdate = false;
                    if (!userRemoved)
                    {
                        MessageDialog md = new MessageDialog("The torrent you are watching has been removed, please go back to homepage.");
                        await md.ShowAsync();
                    }
                    this.Frame.GoBack();
                }
            }
        }

        /// <summary>
        /// 保留与此页关联的状态，以防挂起应用程序或
        /// 从导航缓存中放弃此页。值必须符合
        /// <see cref="SuspensionManager.SessionState"/> 的序列化要求。
        /// </summary>
        /// <param name="pageState">要使用可序列化状态填充的空字典。</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            keepUpdate = false;
            (this.DefaultViewModel["Torrent"] as UtorrentClient.Torrent).PropertyChanged -= TorrentPropertyChanged;
        }

        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            keepUpdate = false;
            (this.DefaultViewModel["Torrent"] as UtorrentClient.Torrent).PropertyChanged -= TorrentPropertyChanged;
            base.GoBack(sender, e);
        }

        private void TorrentStartButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                MainPage.client.excuteAction((this.DefaultViewModel["Torrent"] as UtorrentClient.Torrent),
                    UtorrentClient.TORRENT_CMD_START);
            }
            catch (Exception e1)
            {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
        }
        private void TorrentPauseButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                MainPage.client.excuteAction((this.DefaultViewModel["Torrent"] as UtorrentClient.Torrent),
                    UtorrentClient.TORRENT_CMD_PAUSE);
            }
            catch (Exception e1)
            {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
        }
        private void TorrentStopButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                MainPage.client.excuteAction((this.DefaultViewModel["Torrent"] as UtorrentClient.Torrent),
                    UtorrentClient.TORRENT_CMD_STOP);
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
                UtorrentClient.Torrent tor = this.DefaultViewModel["Torrent"] as UtorrentClient.Torrent;
                if (tor != null)
                {
                    PopupMenu pm = new PopupMenu();
                    pm.Commands.Add(new UICommand("Remove", (command) =>
                    {
                        MainPage.client.excuteAction(tor,UtorrentClient.TORRENT_CMD_REMOVE);
                    }));
                    pm.Commands.Add(new UICommand("Remove with Torrent", (command) =>
                    {
                        MainPage.client.excuteAction(tor,UtorrentClient.TORRENT_CMD_REMOVE_TORRENT);
                    }));
                    pm.Commands.Add(new UICommand("Remove with Data", (command) =>
                    {
                        MainPage.client.excuteAction(tor,UtorrentClient.TORRENT_CMD_REMOVE_DATA);
                    }));
                    pm.Commands.Add(new UICommand("Remove with Torrent and Data", (command) =>
                    {
                        MainPage.client.excuteAction(tor,UtorrentClient.TORRENT_CMD_REMOVE_DATA_TORRENT);
                    }));
                    await pm.ShowForSelectionAsync(Util.GetElementRect((FrameworkElement)sender));
                }
            }
            catch (Exception e1)
            {
                MessageDialog md = new MessageDialog(e1.Message);
                md.ShowAsync();
            }
        }

        private async void TorrentFileItemClicked(object sender, ItemClickEventArgs e) {
            UtorrentClient.File file = e.ClickedItem as UtorrentClient.File;
            if (file.Downloaded == file.Size)
            {
                String uriStr = MainPage.client.getTorrentFileDownloadURL(this.DefaultViewModel["Torrent"] as UtorrentClient.Torrent, file.Index);
                Uri uri;
                if (Uri.TryCreate(uriStr, UriKind.Absolute, out uri))
                {
                    try
                    {
                        PopupMenu pm = new PopupMenu();
                        //pm.Commands.Add(new UICommand("Download", (command) =>
                        //{
                        //    MessageDialog md = new MessageDialog("Not implemented!");
                        //    md.ShowAsync();
                        //}));
                        pm.Commands.Add(new UICommand("Download by external broswer", (command) =>
                        {
                            Windows.System.Launcher.LaunchUriAsync(uri);
                        }));
                        //pm.Commands.Add(new UICommand("Preview", (command) =>
                        //{
                        //    this.Frame.Navigate(typeof(PreviewPage), uri);
                        //}));
                        await pm.ShowForSelectionAsync(Util.GetElementRect((FrameworkElement)sender));
                    }
                    catch (Exception e1)
                    {
                        MessageDialog md = new MessageDialog(e1.Message);
                        md.ShowAsync();
                    }
                }
            }
        }
    }
}
