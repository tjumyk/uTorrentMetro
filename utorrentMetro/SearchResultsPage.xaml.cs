using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “搜索合同”项模板在 http://go.microsoft.com/fwlink/?LinkId=234240 上提供

namespace utorrentMetro
{
    /// <summary>
    /// 此页显示全局搜索定向到此应用程序时的搜索结果。
    /// </summary>
    public sealed partial class SearchResultsPage : utorrentMetro.Common.LayoutAwarePage
    {

        public SearchResultsPage()
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
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            var queryText = navigationParameter as String;

            // TODO: 特定于应用程序的搜索逻辑。搜索进程负责
            //       创建用户可选的结果类别列表:
            //
            //       filterList.Add(new Filter("<filter name>", <result count>));
            //
            //       仅第一个筛选器(通常为“全部”)应将 true 作为第三个参数传入
            //       以便以活动状态开始。活动筛选器的结果在
            //       下面的 Filter_SelectionChanged 中提供。

            var filterList = new List<Filter>();
            filterList.Add(new Filter("All", 0, true));

            // 通过视图模型沟通结果
            this.DefaultViewModel["QueryText"] = '\u201c' + queryText + '\u201d';
            this.DefaultViewModel["Filters"] = filterList;
            this.DefaultViewModel["ShowFilters"] = filterList.Count > 1;
        }

        /// <summary>
        /// 在使用处于对齐视图状态的 ComboBox 选择筛选器时进行调用。
        /// </summary>
        /// <param name="sender">ComboBox 实例。</param>
        /// <param name="e">描述如何更改选定筛选器的事件数据。</param>
        void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 确定选定的筛选器
            var selectedFilter = e.AddedItems.FirstOrDefault() as Filter;
            if (selectedFilter != null)
            {
                // 将结果镜像到相应的筛选器对象中，以允许
                // 在未对齐以反映更改时使用的 RadioButton 表示形式
                selectedFilter.Active = true;

                // TODO: 通过将 this.DefaultViewModel["Results"] 设置为具有可绑定的 Image、Title 和 Subtitle 属性的项集合，
                //       具有可绑定的 Image、Title、Subtitle 和 Description 属性的项的集合

                // 确保找到结果
                object results;
                ICollection resultsCollection;
                if (this.DefaultViewModel.TryGetValue("Results", out results) &&
                    (resultsCollection = results as ICollection) != null &&
                    resultsCollection.Count != 0)
                {
                    VisualStateManager.GoToState(this, "ResultsFound", true);
                    return;
                }
            }

            // 无搜索结果时显示信息性文本。
            VisualStateManager.GoToState(this, "NoResultsFound", true);
        }

        /// <summary>
        /// 在未对齐的情况下使用 RadioButton 选定筛选器时进行调用。
        /// </summary>
        /// <param name="sender">选定的 RadioButton 实例。</param>
        /// <param name="e">描述如何选定 RadioButton 的事件数据。</param>
        void Filter_Checked(object sender, RoutedEventArgs e)
        {
            // 将更改镜像到对应的 ComboBox 使用的 CollectionViewSource 中
            // 以确保在对齐后反映更改
            if (filtersViewSource.View != null)
            {
                var filter = (sender as FrameworkElement).DataContext;
                filtersViewSource.View.MoveCurrentTo(filter);
            }
        }

        /// <summary>
        /// 描述可用于查看搜索结果的筛选器之一的视图模型。
        /// </summary>
        private sealed class Filter : utorrentMetro.Common.BindableBase
        {
            private String _name;
            private int _count;
            private bool _active;

            public Filter(String name, int count, bool active = false)
            {
                this.Name = name;
                this.Count = count;
                this.Active = active;
            }

            public override String ToString()
            {
                return Description;
            }

            public String Name
            {
                get { return _name; }
                set { if (this.SetProperty(ref _name, value)) this.OnPropertyChanged("Description"); }
            }

            public int Count
            {
                get { return _count; }
                set { if (this.SetProperty(ref _count, value)) this.OnPropertyChanged("Description"); }
            }

            public bool Active
            {
                get { return _active; }
                set { this.SetProperty(ref _active, value); }
            }

            public String Description
            {
                get { return String.Format("{0} ({1})", _name, _count); }
            }
        }
    }
}
