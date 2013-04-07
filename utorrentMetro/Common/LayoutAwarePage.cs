using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace utorrentMetro.Common
{
    /// <summary>
    /// 提供几方面重要便利的 Page 的典型实现:
    /// <list type="bullet">
    /// <item>
    /// <description>应用程序视图状态到可视状态的映射</description>
    /// </item>
    /// <item>
    /// <description>GoBack、GoForward 和 GoHome 事件处理程序</description>
    /// </item>
    /// <item>
    /// <description>用于导航的鼠标和键盘快捷键</description>
    /// </item>
    /// <item>
    /// <description>用于导航和进程生命期管理的状态管理</description>
    /// </item>
    /// <item>
    /// <description>默认视图模型</description>
    /// </item>
    /// </list>
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class LayoutAwarePage : Page
    {
        /// <summary>
        /// 标识 <see cref="DefaultViewModel"/> 依赖属性。
        /// </summary>
        public static readonly DependencyProperty DefaultViewModelProperty =
            DependencyProperty.Register("DefaultViewModel", typeof(IObservableMap<String, Object>),
            typeof(LayoutAwarePage), null);

        private List<Control> _layoutAwareControls;

        /// <summary>
        /// 初始化 <see cref="LayoutAwarePage"/> 类的新实例。
        /// </summary>
        public LayoutAwarePage()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled) return;

            // 创建空默认视图模型
            this.DefaultViewModel = new ObservableDictionary<String, Object>();

            // 当此页是可视化树的一部分时，进行两个更改:
            // 1) 将应用程序视图状态映射到页的可视状态
            // 2) 处理键盘和鼠标导航请求
            this.Loaded += (sender, e) =>
            {
                this.StartLayoutUpdates(sender, e);

                // 仅当占用整个窗口时，键盘和鼠标导航才适用
                if (this.ActualHeight == Window.Current.Bounds.Height &&
                    this.ActualWidth == Window.Current.Bounds.Width)
                {
                    // 直接侦听窗口，因此无需焦点
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                        CoreDispatcher_AcceleratorKeyActivated;
                    Window.Current.CoreWindow.PointerPressed +=
                        this.CoreWindow_PointerPressed;
                }
            };

            // 当页不再可见时，撤消相同更改
            this.Unloaded += (sender, e) =>
            {
                this.StopLayoutUpdates(sender, e);
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerPressed -=
                    this.CoreWindow_PointerPressed;
            };
        }

        /// <summary>
        /// <see cref="IObservableMap&lt;String, Object&gt;"/> 的实现，该实现旨在
        /// 用作普通视图模型。
        /// </summary>
        protected IObservableMap<String, Object> DefaultViewModel
        {
            get
            {
                return this.GetValue(DefaultViewModelProperty) as IObservableMap<String, Object>;
            }

            set
            {
                this.SetValue(DefaultViewModelProperty, value);
            }
        }

        #region 导航支持

        /// <summary>
        /// 作为事件处理程序进行调用，以向后导航页的关联
        /// <see cref="Frame"/>，直至它达到导航堆栈顶部。
        /// </summary>
        /// <param name="sender">触发事件的实例。</param>
        /// <param name="e">描述导致事件的条件的事件数据。</param>
        protected virtual void GoHome(object sender, RoutedEventArgs e)
        {
            // 使用导航框架返回最顶层的页
            if (this.Frame != null)
            {
                while (this.Frame.CanGoBack) this.Frame.GoBack();
            }
        }

        /// <summary>
        /// 作为事件处理程序进行调用，以向后导航与此页的 <see cref="Frame"/>
        /// 关联的导航堆栈。
        /// </summary>
        /// <param name="sender">触发事件的实例。</param>
        /// <param name="e">描述导致事件的条件的事件
        /// 数据。</param>
        protected virtual void GoBack(object sender, RoutedEventArgs e)
        {
            // 使用导航框架返回上一页
            if (this.Frame != null && this.Frame.CanGoBack) this.Frame.GoBack();
        }

        /// <summary>
        /// 作为事件处理程序进行调用，以向后导航导航堆栈
        /// 关联的导航堆栈。
        /// </summary>
        /// <param name="sender">触发事件的实例。</param>
        /// <param name="e">描述导致事件的条件的事件
        /// 数据。</param>
        protected virtual void GoForward(object sender, RoutedEventArgs e)
        {
            // 使用导航框架移至下一页
            if (this.Frame != null && this.Frame.CanGoForward) this.Frame.GoForward();
        }

        /// <summary>
        /// 当此页处于活动状态并占用整个窗口时，在每次
        /// 击键(包括系统键，如 Alt 组合键)时调用。用于检测页之间的键盘
        /// 导航(即使在页本身没有焦点时)。
        /// </summary>
        /// <param name="sender">触发事件的实例。</param>
        /// <param name="args">描述导致事件的条件的事件数据。</param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender,
            AcceleratorKeyEventArgs args)
        {
            var virtualKey = args.VirtualKey;

            // 仅当按向左、向右或专用上一页或下一页键时才进一步
            // 调查
            if ((args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                args.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right ||
                (int)virtualKey == 166 || (int)virtualKey == 167))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) ||
                    (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // 在按上一页键或 Alt+向左键时向后导航
                    args.Handled = true;
                    this.GoBack(this, new RoutedEventArgs());
                }
                else if (((int)virtualKey == 167 && noModifiers) ||
                    (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // 在按下一页键或 Alt+向右键时向前导航
                    args.Handled = true;
                    this.GoForward(this, new RoutedEventArgs());
                }
            }
        }

        /// <summary>
        /// 当此页处于活动状态并占用整个窗口时，在每次鼠标单击、触摸屏点击
        /// 或执行等效交互时调用。用于检测浏览器样式下一页和
        /// 上一步鼠标按钮单击以在页之间导航。
        /// </summary>
        /// <param name="sender">触发事件的实例。</param>
        /// <param name="args">描述导致事件的条件的事件数据。</param>
        private void CoreWindow_PointerPressed(CoreWindow sender,
            PointerEventArgs args)
        {
            var properties = args.CurrentPoint.Properties;

            // 忽略与鼠标左键、右键和中键的键关联
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed) return;

            // 如果按下后退或前进(但不是同时)，则进行相应导航
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                args.Handled = true;
                if (backPressed) this.GoBack(this, new RoutedEventArgs());
                if (forwardPressed) this.GoForward(this, new RoutedEventArgs());
            }
        }

        #endregion

        #region 可视状态切换

        /// <summary>
        /// 作为事件处理程序调用，通常是在页中发生 <see cref="Control"/> 的
        /// <see cref="FrameworkElement.Loaded"/> 事件时，以指示发送方应
        /// 开始接收与应用程序视图状态更改对应的可视状态
        /// 管理更改。
        /// </summary>
        /// <param name="sender">支持与视图状态对应的可视状态
        /// 管理的 <see cref="Control"/> 实例。</param>
        /// <param name="e">描述如何进行请求的事件数据。</param>
        /// <remarks>在请求布局更新时，会立即使用当前视图
        /// 状态设置对应可视状态。强烈建议使用连接到 <see cref="StopLayoutUpdates"/>
        /// 的对应 <see cref="FrameworkElement.Unloaded"/> 事件
        /// 处理程序。<see cref="LayoutAwarePage"/> 的实例
        /// 会自动在其 Loaded 和 Unloaded 事件中调用这些
        /// 处理程序。</remarks>
        /// <seealso cref="DetermineVisualState"/>
        /// <seealso cref="InvalidateVisualState"/>
        public void StartLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null) return;
            if (this._layoutAwareControls == null)
            {
                // 当更新中存在相关控件时，开始侦听视图状态更改
                Window.Current.SizeChanged += this.WindowSizeChanged;
                this._layoutAwareControls = new List<Control>();
            }
            this._layoutAwareControls.Add(control);

            // 设置控件的初始可视状态
            VisualStateManager.GoToState(control, DetermineVisualState(ApplicationView.Value), false);
        }

        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            this.InvalidateVisualState();
        }

        /// <summary>
        /// 作为事件处理程序调用，通常是在发生 <see cref="Control"/> 的
        /// <see cref="FrameworkElement.Unloaded"/> 事件时，以指示发送方应开始接收
        /// 与应用程序视图状态更改对应的可视状态管理更改。
        /// </summary>
        /// <param name="sender">支持与视图状态对应的可视状态
        /// 管理的 <see cref="Control"/> 实例。</param>
        /// <param name="e">描述如何进行请求的事件数据。</param>
        /// <remarks>在请求布局更新时，会立即使用当前视图
        /// 状态设置对应可视状态。</remarks>
        /// <seealso cref="StartLayoutUpdates"/>
        public void StopLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null || this._layoutAwareControls == null) return;
            this._layoutAwareControls.Remove(control);
            if (this._layoutAwareControls.Count == 0)
            {
                // 当更新中没有相关控件时，停止侦听视图状态更改
                this._layoutAwareControls = null;
                Window.Current.SizeChanged -= this.WindowSizeChanged;
            }
        }

        /// <summary>
        /// 针对页中的可视状态管理将 <see cref="ApplicationViewState"/> 值
        /// 转换为字符串。默认实现使用枚举值的名称。
        /// 子类可以重写此方法以控制使用的映射方案。
        /// </summary>
        /// <param name="viewState">需要可视状态的视图状态。</param>
        /// <returns>用于驱动
        /// <see cref="VisualStateManager"/> 的可视状态名称</returns>
        /// <seealso cref="InvalidateVisualState"/>
        protected virtual string DetermineVisualState(ApplicationViewState viewState)
        {
            return viewState.ToString();
        }

        /// <summary>
        /// 使用正确的可视状态更新侦听可视状态更改的
        /// 所有控件。
        /// </summary>
        /// <remarks>
        /// 通常与重写 <see cref="DetermineVisualState"/> 结合使用以
        ///  通知可能返回了不同值，即使未更改视图状态也是
        /// 如此。
        /// </remarks>
        public void InvalidateVisualState()
        {
            if (this._layoutAwareControls != null)
            {
                string visualState = DetermineVisualState(ApplicationView.Value);
                foreach (var layoutAwareControl in this._layoutAwareControls)
                {
                    VisualStateManager.GoToState(layoutAwareControl, visualState, false);
                }
            }
        }

        #endregion

        #region 进程生命期管理

        private String _pageKey;

        /// <summary>
        /// 在此页将要在 Frame 中显示时进行调用。
        /// </summary>
        /// <param name="e">描述如何访问此页的事件数据。Parameter
        /// 属性提供要显示的组。</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // 通过导航返回缓存页不应触发状态加载
            if (this._pageKey != null) return;

            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // 在向导航堆栈添加新页时清除向前导航的
                // 现有状态
                var nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // 将导航参数传递给新页
                this.LoadState(e.Parameter, null);
            }
            else
            {
                // 通过将相同策略用于加载挂起状态并从缓存重新创建
                // 放弃的页，将导航参数和保留页状态传递
                // 给页
                this.LoadState(e.Parameter, (Dictionary<String, Object>)frameState[this._pageKey]);
            }
        }

        /// <summary>
        /// 当此页不再在 Frame 中显示时调用。
        /// </summary>
        /// <param name="e">描述如何访问此页的事件数据。Parameter
        /// 属性提供要显示的组。</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<String, Object>();
            this.SaveState(pageState);
            frameState[_pageKey] = pageState;
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
        protected virtual void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// 保留与此页关联的状态，以防挂起应用程序或
        /// 从导航缓存中放弃此页。值必须符合
        /// <see cref="SuspensionManager.SessionState"/> 的序列化要求。
        /// </summary>
        /// <param name="pageState">要使用可序列化状态填充的空字典。</param>
        protected virtual void SaveState(Dictionary<String, Object> pageState)
        {
        }

        #endregion

        /// <summary>
        /// 支持重新进入以用作默认视图模型的 IObservableMap 的
        /// 实现。
        /// </summary>
        private class ObservableDictionary<K, V> : IObservableMap<K, V>
        {
            private class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<K>
            {
                public ObservableDictionaryChangedEventArgs(CollectionChange change, K key)
                {
                    this.CollectionChange = change;
                    this.Key = key;
                }

                public CollectionChange CollectionChange { get; private set; }
                public K Key { get; private set; }
            }

            private Dictionary<K, V> _dictionary = new Dictionary<K, V>();
            public event MapChangedEventHandler<K, V> MapChanged;

            private void InvokeMapChanged(CollectionChange change, K key)
            {
                var eventHandler = MapChanged;
                if (eventHandler != null)
                {
                    eventHandler(this, new ObservableDictionaryChangedEventArgs(change, key));
                }
            }

            public void Add(K key, V value)
            {
                this._dictionary.Add(key, value);
                this.InvokeMapChanged(CollectionChange.ItemInserted, key);
            }

            public void Add(KeyValuePair<K, V> item)
            {
                this.Add(item.Key, item.Value);
            }

            public bool Remove(K key)
            {
                if (this._dictionary.Remove(key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                    return true;
                }
                return false;
            }

            public bool Remove(KeyValuePair<K, V> item)
            {
                V currentValue;
                if (this._dictionary.TryGetValue(item.Key, out currentValue) &&
                    Object.Equals(item.Value, currentValue) && this._dictionary.Remove(item.Key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, item.Key);
                    return true;
                }
                return false;
            }

            public V this[K key]
            {
                get
                {
                    return this._dictionary[key];
                }
                set
                {
                    this._dictionary[key] = value;
                    this.InvokeMapChanged(CollectionChange.ItemChanged, key);
                }
            }

            public void Clear()
            {
                var priorKeys = this._dictionary.Keys.ToArray();
                this._dictionary.Clear();
                foreach (var key in priorKeys)
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                }
            }

            public ICollection<K> Keys
            {
                get { return this._dictionary.Keys; }
            }

            public bool ContainsKey(K key)
            {
                return this._dictionary.ContainsKey(key);
            }

            public bool TryGetValue(K key, out V value)
            {
                return this._dictionary.TryGetValue(key, out value);
            }

            public ICollection<V> Values
            {
                get { return this._dictionary.Values; }
            }

            public bool Contains(KeyValuePair<K, V> item)
            {
                return this._dictionary.Contains(item);
            }

            public int Count
            {
                get { return this._dictionary.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
            {
                int arraySize = array.Length;
                foreach (var pair in this._dictionary)
                {
                    if (arrayIndex >= arraySize) break;
                    array[arrayIndex++] = pair;
                }
            }
        }
    }
}
