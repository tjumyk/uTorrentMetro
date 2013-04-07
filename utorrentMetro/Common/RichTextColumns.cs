using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;

namespace utorrentMetro.Common
{
    /// <summary>
    /// 按适合可用内容所需数量创建附加溢出列的
    /// <see cref="RichTextBlock"/> 的包装。
    /// </summary>
    /// <example>
    /// 下面创建间距 50 像素、宽 400 像素的列的集合
    /// 以包含任意数据绑定内容:
    /// <code>
    /// <RichTextColumns>
    ///     <RichTextColumns.ColumnTemplate>
    ///         <DataTemplate>
    ///             <RichTextBlockOverflow Width="400" Margin="50,0,0,0"/>
    ///         </DataTemplate>
    ///     </RichTextColumns.ColumnTemplate>
    ///     
    ///     <RichTextBlock Width="400">
    ///         <Paragraph>
    ///             <Run Text="{Binding Content}"/>
    ///         </Paragraph>
    ///     </RichTextBlock>
    /// </RichTextColumns>
    /// </code>
    /// </example>
    /// <remarks>通常用于水平滚动区域，其中未限定的
    /// 空间量允许创建所需全部列。当用于垂直滚动
    /// 空间时，不会存在任何附加列。</remarks>
    [Windows.UI.Xaml.Markup.ContentProperty(Name = "RichTextContent")]
    public sealed class RichTextColumns : Panel
    {
        /// <summary>
        /// 标识 <see cref="RichTextContent"/> 依赖属性。
        /// </summary>
        public static readonly DependencyProperty RichTextContentProperty =
            DependencyProperty.Register("RichTextContent", typeof(RichTextBlock),
            typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

        /// <summary>
        /// 标识 <see cref="ColumnTemplate"/> 依赖属性。
        /// </summary>
        public static readonly DependencyProperty ColumnTemplateProperty =
            DependencyProperty.Register("ColumnTemplate", typeof(DataTemplate),
            typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

        /// <summary>
        /// 初始化 <see cref="RichTextColumns"/> 类的新实例。
        /// </summary>
        public RichTextColumns()
        {
            this.HorizontalAlignment = HorizontalAlignment.Left;
        }

        /// <summary>
        /// 获取或设置要用作第一列的初始 RTF 内容。
        /// </summary>
        public RichTextBlock RichTextContent
        {
            get { return (RichTextBlock)GetValue(RichTextContentProperty); }
            set { SetValue(RichTextContentProperty, value); }
        }

        /// <summary>
        /// 获取或设置用于创建附加
        /// <see cref="RichTextBlockOverflow"/> 实例的模板。
        /// </summary>
        public DataTemplate ColumnTemplate
        {
            get { return (DataTemplate)GetValue(ColumnTemplateProperty); }
            set { SetValue(ColumnTemplateProperty, value); }
        }

        /// <summary>
        /// 当更改内容或溢出模板以重新创建列布局时调用。
        /// </summary>
        /// <param name="d">发生更改的 <see cref="RichTextColumns"/> 的
        /// 实例。</param>
        /// <param name="e">描述特定更改的事件数据。</param>
        private static void ResetOverflowLayout(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 当发生重大更改时，从头开始重新生成列布局
            var target = d as RichTextColumns;
            if (target != null)
            {
                target._overflowColumns = null;
                target.Children.Clear();
                target.InvalidateMeasure();
            }
        }

        /// <summary>
        /// 列出已创建的溢出列。必须与初始 RichTextBlock
        /// 子级后跟的 <see cref="Panel.Children"/> 集合中的实例
        /// 保持 1:1 关系。
        /// </summary>
        private List<RichTextBlockOverflow> _overflowColumns = null;

        /// <summary>
        /// 确定是否需要附加溢出列以及是否可以移除
        /// 现有列。
        /// </summary>
        /// <param name="availableSize">可用空间的大小，用于约束
        /// 可以创建的附加列数。</param>
        /// <returns>原始内容加上所有附加列的最终大小。</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.RichTextContent == null) return new Size(0, 0);

            // 通过附加列列表的缺失指示此操作尚未
            // 进行，确保 RichTextBlock 是
            // 子级
            if (this._overflowColumns == null)
            {
                Children.Add(this.RichTextContent);
                this._overflowColumns = new List<RichTextBlockOverflow>();
            }

            // 首先度量原始 RichTextBlock 内容
            this.RichTextContent.Measure(availableSize);
            var maxWidth = this.RichTextContent.DesiredSize.Width;
            var maxHeight = this.RichTextContent.DesiredSize.Height;
            var hasOverflow = this.RichTextContent.HasOverflowContent;

            // 确保存在足够的溢出列
            int overflowIndex = 0;
            while (hasOverflow && maxWidth < availableSize.Width && this.ColumnTemplate != null)
            {
                // 在耗尽前使用现有溢出列，然后从
                // 提供的模板创建更多列
                RichTextBlockOverflow overflow;
                if (this._overflowColumns.Count > overflowIndex)
                {
                    overflow = this._overflowColumns[overflowIndex];
                }
                else
                {
                    overflow = (RichTextBlockOverflow)this.ColumnTemplate.LoadContent();
                    this._overflowColumns.Add(overflow);
                    this.Children.Add(overflow);
                    if (overflowIndex == 0)
                    {
                        this.RichTextContent.OverflowContentTarget = overflow;
                    }
                    else
                    {
                        this._overflowColumns[overflowIndex - 1].OverflowContentTarget = overflow;
                    }
                }

                // 度量新列并准备根据需要进行重复
                overflow.Measure(new Size(availableSize.Width - maxWidth, availableSize.Height));
                maxWidth += overflow.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, overflow.DesiredSize.Height);
                hasOverflow = overflow.HasOverflowContent;
                overflowIndex++;
            }

            // 断开附加列与溢出链的连接，从我们的专用列列表中移除它们，
            // 然后将它们作为子级移除
            if (this._overflowColumns.Count > overflowIndex)
            {
                if (overflowIndex == 0)
                {
                    this.RichTextContent.OverflowContentTarget = null;
                }
                else
                {
                    this._overflowColumns[overflowIndex - 1].OverflowContentTarget = null;
                }
                while (this._overflowColumns.Count > overflowIndex)
                {
                    this._overflowColumns.RemoveAt(overflowIndex);
                    this.Children.RemoveAt(overflowIndex + 1);
                }
            }

            // 报告最终确定大小
            return new Size(maxWidth, maxHeight);
        }

        /// <summary>
        /// 排列原始内容和所有附加列。
        /// </summary>
        /// <param name="finalSize">定义必须在其中排列子级的区域的
        /// 大小。</param>
        /// <returns>子级实际需要的区域的大小。</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double maxWidth = 0;
            double maxHeight = 0;
            foreach (var child in Children)
            {
                child.Arrange(new Rect(maxWidth, 0, child.DesiredSize.Width, finalSize.Height));
                maxWidth += child.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }
            return new Size(maxWidth, maxHeight);
        }
    }
}
