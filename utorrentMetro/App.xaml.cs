﻿using MovieGuide;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白应用程序”模板在 http://go.microsoft.com/fwlink/?LinkId=234227 上有介绍

namespace utorrentMetro
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        public static Boolean stopBackgroundTask;

        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            stopBackgroundTask = false;
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 当启动应用程序以执行打开特定的文件或显示搜索结果等操作时
        /// 将使用其他入口点。
        /// </summary>
        /// <param name="args">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                    stopBackgroundTask = false;
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
                SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;
            }

            if (rootFrame.Content == null)
            {
                // 当未还原导航堆栈时，导航到第一页，
                // 并通过将所需信息作为导航参数传入来配置
                // 参数
                if (!rootFrame.Navigate(typeof(MainPage), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // 确保当前窗口处于活动状态
            Window.Current.Activate();
        }

        private void App_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            Rect _windowBounds = Window.Current.Bounds;
            double _settingsWidth = 346;

            var privacy = new SettingsCommand("privacy", "Privacy Policy", (handler) =>
            {
                var _settingsPopup = new Popup();
                SettingPanel control = new SettingPanel();
                _settingsPopup.Child = control;
                _settingsPopup.IsLightDismissEnabled = true;
                _settingsPopup.Width = _settingsWidth;
                _settingsPopup.Height = _windowBounds.Height;
                control.Width = _settingsWidth;
                control.Height = _windowBounds.Height;
                _settingsPopup.SetValue(Canvas.LeftProperty, _windowBounds.Width - _settingsWidth);
                _settingsPopup.SetValue(Canvas.TopProperty, 0);
                _settingsPopup.IsOpen = true;
                control.setTitle("Privacy");
                control.setContent("uTorrentMetro use your internet connection to communicate with WebUI API provided by your own local uTorrent client (For example, fetching torrent list, sending start/pause command, etc.). So, no external network connection is used in this application and none of your personal information could be collected or transmitted.");
            });

            args.Request.ApplicationCommands.Add(privacy);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。在不知道应用程序
        /// 将被终止还是恢复的情况下保存应用程序状态，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起的请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            stopBackgroundTask = true;
            deferral.Complete();
        }

        /// <summary>
        /// 在激活应用程序以显示搜索结果时调用。
        /// </summary>
        /// <param name="args">有关激活请求的详细信息。</param>
        protected async override void OnSearchActivated(Windows.ApplicationModel.Activation.SearchActivatedEventArgs args)
        {
            // TODO: 在 OnWindowCreated 中注册 Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().QuerySubmitted
            // 事件，以在应用程序运行后加快搜索

            // 如果窗口尚未使用框架导航，则插入我们自己的框架
            var previousContent = Window.Current.Content;
            var frame = previousContent as Frame;

            // 如果应用程序不包含顶级框架，则可能表示这是
            // 初次启动应用程序。一般而言，此方法和 App.xaml.cs 中的 OnLaunched 
            // 可调用公共方法。
            if (frame == null)
            {
                // 创建要充当导航上下文的框架，并将其与
                // SuspensionManager 键关联
                frame = new Frame();
                utorrentMetro.Common.SuspensionManager.RegisterFrame(frame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // 仅当适用时还原保存的会话状态
                    try
                    {
                        await utorrentMetro.Common.SuspensionManager.RestoreAsync();
                    }
                    catch (utorrentMetro.Common.SuspensionManagerException)
                    {
                        //还原状态时出现问题。
                        //假定没有状态并继续
                    }
                }
            }

            frame.Navigate(typeof(SearchResultsPage), args.QueryText);
            Window.Current.Content = frame;

            // 确保当前窗口处于活动状态
            Window.Current.Activate();
        }
    }
}
