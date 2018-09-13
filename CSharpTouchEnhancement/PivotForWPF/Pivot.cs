using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PivotForWPF
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(PivotItem))]
    [TemplatePart(Name = "ScrollViewer", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "IndicatorContainer", Type = typeof(Panel))]
    public class Pivot : Selector
    {
        public static readonly DependencyProperty ScrollingStyleProperty =
            DependencyProperty.Register(nameof(ScrollingStyle), typeof(ScrollingStyle), typeof(Pivot), new PropertyMetadata(default(ScrollingStyle), OnScrollingStyleChanged));

        public ScrollingStyle ScrollingStyle
        {
            get { return (ScrollingStyle)GetValue(ScrollingStyleProperty); }
            set { SetValue(ScrollingStyleProperty, value); }
        }

        private static void OnScrollingStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pivot = d as Pivot;
            if (pivot == null)
            {
                return;
            }

            var isSystemAnimation = pivot.ScrollingStyle == ScrollingStyle.System;
            pivot.SetSlidingMode(isSystemAnimation);
            pivot.ResetContainerSize();
            pivot.RefreshIndicator();
        }

        /// <summary>
        /// 是否為元素全部串在一起的樣式
        /// </summary>
        private bool IsConnectedStyle
        {
            get
            {
                return this.ScrollingStyle == ScrollingStyle.Connected || this.ScrollingStyle == ScrollingStyle.Single;
            }
        }

        public static readonly DependencyProperty PivotDirectionProperty =
            DependencyProperty.Register(nameof(PivotDirection), typeof(PivotDirection), typeof(Pivot), new PropertyMetadata(default(PivotDirection), OnPivotDirectionChanged));

        public PivotDirection PivotDirection
        {
            get { return (PivotDirection)GetValue(PivotDirectionProperty); }
            set { SetValue(PivotDirectionProperty, value); }
        }

        private static void OnPivotDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pivot = d as Pivot;
            if (pivot == null)
            {
                return;
            }

            var isSystemAnimation = pivot.ScrollingStyle == ScrollingStyle.System;
            pivot.SetSlidingMode(isSystemAnimation);
            pivot.ResetContainerSize();
        }

        public static readonly DependencyProperty IndicatorVisibilityProperty =
            DependencyProperty.Register(nameof(IndicatorVisibility), typeof(Visibility), typeof(Pivot), new PropertyMetadata(Visibility.Collapsed));

        public Visibility IndicatorVisibility
        {
            get { return (Visibility)GetValue(IndicatorVisibilityProperty); }
            set { SetValue(IndicatorVisibilityProperty, value); }
        }

        public static readonly DependencyProperty IndicatorStyleProperty =
            DependencyProperty.Register(nameof(IndicatorStyle), typeof(Style), typeof(Pivot), new PropertyMetadata(default(Style), OnIndicatorVisibilityChanged));

        public Style IndicatorStyle
        {
            get { return (Style)GetValue(IndicatorStyleProperty); }
            set { SetValue(IndicatorStyleProperty, value); }
        }

        private static void OnIndicatorVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pivot = d as Pivot;
            if (pivot == null)
            {
                return;
            }

            if (pivot.IndicatorVisibility != Visibility.Visible)
            {
                return;
            }

            pivot.RefreshIndicator();
        }

        public static readonly DependencyProperty IndicatorSelectedColorProperty =
            DependencyProperty.Register(nameof(IndicatorSelectedColor), typeof(Color), typeof(Pivot), new PropertyMetadata(Colors.Transparent));

        public Color IndicatorSelectedColor
        {
            get { return (Color)GetValue(IndicatorSelectedColorProperty); }
            set { SetValue(IndicatorSelectedColorProperty, value); }
        }

        public static readonly DependencyProperty IndicatorNormalColorProperty =
            DependencyProperty.Register(nameof(IndicatorNormalColor), typeof(Color), typeof(Pivot), new PropertyMetadata(Colors.Transparent));

        public Color IndicatorNormalColor
        {
            get { return (Color)GetValue(IndicatorNormalColorProperty); }
            set { SetValue(IndicatorNormalColorProperty, value); }
        }

        /// <summary>
        /// 是否正處於滑鼠按下左鍵的狀態
        /// </summary>
        private bool IsMouseLeftButtonDowning { get; set; } = false;

        /// <summary>
        /// 從按下滑鼠左鍵到放開滑鼠左鍵之間的位移量
        /// </summary>
        private double CumulativeTranslate { get; set; } = 0;

        /// <summary>
        /// 預計位移的對象
        /// </summary>
        private PivotItem MovingTarget { get; set; }

        /// <summary>
        /// 按下滑鼠左鍵的起始點
        /// </summary>
        private Point MouseLeftDownStartPoint { get; set; }

        /// <summary>
        /// 執行位移動畫的 X 門檻值，若 X 低於門檻值則執行還原位置動畫
        /// </summary>
        private double TranslateXThreshold { get; set; } = 30;

        /// <summary>
        /// 目前最新的位移方向是否為 '往右' (true: 往右)
        /// </summary>
        private bool IsLastestDirectToRight { get; set; } = false;

        /// <summary>
        /// 設定最新位移方向的門檻值，若最新位移低於門檻值則無視
        /// </summary>
        private double LastestDirectThreshold { get; set; } = 3;

        /// <summary>
        /// 是否強制執行往左的位移動畫
        /// </summary>
        private bool IsForceGoLeft { get; set; } = false;

        /// <summary>
        /// 是否強制執行往右的位移動畫
        /// </summary>
        private bool IsForceGoRight { get; set; } = false;

        /// <summary>
        /// 是否跳過當次位移動畫
        /// </summary>
        private bool IsSkipAnimationOnce { get; set; } = false;

        private ScrollViewer ScrollViewer { get; set; }

        private Panel IndicatorContainer { get; set; }

        private PageData PageData
        {
            get
            {
                if (this.Items == null || this.Items.Count == 0)
                {
                    return new PageData()
                    {
                        PageSize = 1,
                        PageCount = 0
                    };
                }

                var itemCount = this.Items.Count;

                if (ScrollingStyle != ScrollingStyle.Single)
                {
                    return new PageData()
                    {
                        PageSize = 1,
                        PageCount = itemCount
                    };
                }

                var pageSize = 1;

                for (int i = 0; i < this.Items.Count; i++)
                {
                    var itemWidth = (this.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement)?.ActualWidth;
                    if (itemWidth.HasValue && itemWidth.Value > 0)
                    {
                        pageSize = (int)(this.ActualWidth / itemWidth.Value);

                        var modItemCount = itemCount % pageSize;
                        itemCount = itemCount / pageSize + (modItemCount > 0 ? 1 : 0);
                        break;
                    }
                }

                return new PageData()
                {
                    PageSize = pageSize,
                    PageCount = itemCount
                };
            }
        }

        private bool IsFirstLoaded { get; set; } = true;

        static Pivot()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Pivot), new FrameworkPropertyMetadata(typeof(Pivot)));
        }

        public Pivot()
        {
            this.Loaded += Pivot_Loaded;
            this.Unloaded += Pivot_Unloaded;
            this.SizeChanged += Pivot_SizeChanged;
            this.SelectionChanged += Pivot_SelectionChanged;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            IndicatorContainer = GetTemplateChild("IndicatorContainer") as Panel;

            if (ScrollViewer != null)
            {
                ScrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
                ScrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }

            RefreshIndicator();
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            switch (PivotDirection)
            {
                case PivotDirection.Horizontal:
                    SetSelectedIndex((int)e.HorizontalOffset, true);
                    break;

                case PivotDirection.Vertical:
                    SetSelectedIndex((int)e.VerticalOffset, true);
                    break;
            }
        }

        private void Pivot_Loaded(object sender, RoutedEventArgs e)
        {
            if (Items == null || Items.Count == 0)
            {
                return;
            }

            if (IsFirstLoaded)
            {
                SetSelectedIndex(0);
                IsFirstLoaded = false;
            }

            var observableCollection = Items as INotifyCollectionChanged;
            if (observableCollection == null)
            {
                return;
            }

            observableCollection.CollectionChanged -= ObservableCollection_CollectionChanged;
            observableCollection.CollectionChanged += ObservableCollection_CollectionChanged;
        }

        private void Pivot_Unloaded(object sender, RoutedEventArgs e)
        {
            var observableCollection = Items as INotifyCollectionChanged;
            if (observableCollection == null)
            {
                return;
            }

            observableCollection.CollectionChanged -= ObservableCollection_CollectionChanged;
        }

        private void ObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Items == null || Items.Count <= 0 || SelectedIndex >= 0)
            {
                return;
            }

            SetSelectedIndex(0, true);
            ScrollViewer?.ScrollToHorizontalOffset(0);
        }

        private void Pivot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetContainerSize();

            if (ScrollingStyle == ScrollingStyle.Single)
            {
                // Single Style 的分頁大小會隨著畫面尺寸而改變，所以 SelectedIndex 也需要跟著改變
                RefreshSelectedIndex();

                // Single Style 的 Indicator 與畫面尺寸有關，所以需要重新畫
                RefreshIndicator();
            }
        }

        private void ResetContainerSize()
        {
            if (this.Items == null || this.ItemContainerGenerator == null)
            {
                return;
            }

            foreach (var item in this.Items)
            {
                var container = this.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                ResetContainerSize(container);
            }
        }

        private void ResetContainerSize(FrameworkElement container)
        {
            if (container == null)
            {
                return;
            }

            var targetWidth = ScrollingStyle == ScrollingStyle.Single ? double.NaN : this.ActualWidth;
            var targetHeight = ScrollingStyle == ScrollingStyle.Single ? double.NaN : this.ActualHeight;

            if (ScrollingStyle != ScrollingStyle.Single && targetHeight > 0 && IndicatorContainer != null && IndicatorVisibility == Visibility.Visible)
            {
                targetHeight -= IndicatorContainer.ActualHeight;
            }

            if (targetWidth > 0)
            {
                container.Width = targetWidth;
            }

            if (targetHeight > 0)
            {
                container.Height = targetHeight;
            }

            container.RenderTransform = new TranslateTransform()
            {
                X = 0,
                Y = 0
            };
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            FrameworkElement container = element as FrameworkElement;
            if (container == null)
            {
                return;
            }

            ResetContainerSize(container);

            container.PreviewMouseLeftButtonDown -= Container_PreviewMouseLeftButtonDown;
            container.PreviewMouseLeftButtonDown += Container_PreviewMouseLeftButtonDown;

            container.PreviewMouseLeftButtonUp -= Container_PreviewMouseLeftButtonUp;
            container.PreviewMouseLeftButtonUp += Container_PreviewMouseLeftButtonUp;

            container.PreviewMouseMove -= Container_PreviewMouseMove;
            container.PreviewMouseMove += Container_PreviewMouseMove;

            container.MouseLeave -= Container_MouseLeave;
            container.MouseLeave += Container_MouseLeave;
        }

        /// <summary>
        /// Clear the element to display the item.
        /// </summary>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
        }

        /// <summary> Return true if the item is (or is eligible to be) its own ItemContainer </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is PivotItem);
        }

        /// <summary> Create or identify the element used to display the given item. </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            var pivotItem = new PivotItem();
            return pivotItem;
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ScrollingStyle == ScrollingStyle.System)
                {
                    return;
                }

                RefreshIndicator();

                if (IsSkipAnimationOnce)
                {
                    return;
                }

                if (Items == null || ItemContainerGenerator == null || e == null || e.AddedItems == null || e.RemovedItems == null)
                {
                    return;
                }

                var addIndex = e.AddedItems.Count > 0 ? Items.IndexOf(e.AddedItems[e.AddedItems.Count - 1]) : -1;
                var removeIndex = e.RemovedItems.Count > 0 ? Items.IndexOf(e.RemovedItems[e.RemovedItems.Count - 1]) : -1;

                if (addIndex < 0 && removeIndex < 0)
                {
                    return;
                }

                if (addIndex >= 0 && removeIndex < 0)
                {
                    // initialed
                    var addContainer = this.ItemContainerGenerator.ContainerFromIndex(addIndex) as FrameworkElement;
                    if (addContainer == null)
                    {
                        return;
                    }

                    var translateTransform = addContainer.RenderTransform as TranslateTransform;
                    if (translateTransform == null)
                    {
                        return;
                    }

                    var movementPropertyInfo = GetPropertyInfo<TranslateTransform>();
                    movementPropertyInfo.SetValue(translateTransform, 0d);
                }
                else if (addIndex >= 0 && removeIndex >= 0)
                {
                    var addContainer = this.ItemContainerGenerator.ContainerFromIndex(addIndex) as FrameworkElement;
                    var removeContainer = this.ItemContainerGenerator.ContainerFromIndex(removeIndex) as FrameworkElement;
                    if (removeContainer == null)
                    {
                        return;
                    }

                    if (addContainer == null || removeContainer == null)
                    {
                        if (IsConnectedStyle && removeContainer != null)
                        {
                            addContainer = removeContainer;
                        }
                        else
                        {
                            // 預計要選擇的項目正處於虛擬化狀態，直接滾動 ScrollViewer 到該滾的位置，不用跑動畫
                            ScrollToTargetIndex(addIndex);
                            return;
                        }
                    }

                    // 往左 or 往上: -1
                    // 往右 or 往下:  1
                    var factor = (addIndex > removeIndex || IsForceGoLeft) && IsForceGoRight == false ? -1 : 1;

                    double addTargetPosition = 0d;
                    var sizePropertyInfo = GetPropertyInfo<FrameworkElement>();

                    if (ScrollingStyle == ScrollingStyle.Single)
                    {
                        // ScrollingStyle.Single 每一個元素都有自己的尺寸，需要一個一個計算才能知道位移位置
                        double? lastRemoveContainerSize = 0d;

                        var startIndex = addIndex > removeIndex ? removeIndex : addIndex;
                        var endIndex = startIndex == addIndex ? removeIndex : addIndex;

                        for (int i = startIndex; i < endIndex; i++)
                        {
                            var container = this.ItemContainerGenerator.ContainerFromIndex(i);
                            var frameworkElement = container as FrameworkElement;
                            var containerSize = frameworkElement == null ? default(double?) : (double)sizePropertyInfo.GetValue(frameworkElement);
                            if (containerSize.HasValue == false || containerSize.Value <= 0)
                            {
                                if (lastRemoveContainerSize == 0)
                                {
                                    lastRemoveContainerSize = removeContainer == null ? default(double?) : (double)sizePropertyInfo.GetValue(removeContainer);
                                }

                                containerSize = lastRemoveContainerSize;
                            }
                            else
                            {
                                lastRemoveContainerSize = containerSize;
                            }

                            addTargetPosition += containerSize.Value;
                        }

                        addTargetPosition *= factor;
                    }
                    else
                    {
                        addTargetPosition = factor * (double)sizePropertyInfo.GetValue(addContainer);
                    }

                    var addTranslateTransform = addContainer.RenderTransform as TranslateTransform;
                    if (addTranslateTransform == null)
                    {
                        // 找不到 addTranslateTransform，沒得位移，只好直接使用 ScrollViewer 移動
                        ScrollToTargetIndex(addIndex);
                        return;
                    }

                    DoubleAnimation doubleAnimation = new DoubleAnimation()
                    {
                        BeginTime = TimeSpan.FromMilliseconds(ScrollingStyle == ScrollingStyle.Whole ? 200 : 0),
                        To = addTargetPosition,
                        Duration = TimeSpan.FromMilliseconds(Math.Max(Math.Abs(addTargetPosition) / 5, 150))
                    };

                    var translateTransformDependencyProperty = GetTranslateTransformDependencyProperty();
                    addTranslateTransform.BeginAnimation(translateTransformDependencyProperty, doubleAnimation);

                    addTranslateTransform.Changed += (o, args) =>
                    {
                        var translateTransformPropertyInfo = GetPropertyInfo<TranslateTransform>();
                        if (Math.Abs(Math.Abs((double)translateTransformPropertyInfo.GetValue(addTranslateTransform)) - Math.Abs(addTargetPosition)) > 0.00000001)
                        {
                            return;
                        }

                        if (addTranslateTransform != addContainer.RenderTransform)
                        {
                            return;
                        }

                        // 位移動畫完成了，要把 ScrollViewer 移到對的位置以便啟動 UI 虛擬化
                        ScrollToTargetIndex(addIndex);
                    };

                    if (IsConnectedStyle)
                    {
                        // 元素全部串在一起的樣式，不用跑移除動畫
                        return;
                    }

                    if (removeContainer == null)
                    {
                        // removeContainer 莫名其妙不見了，沒得跑移除動畫
                        return;
                    }

                    var removeTargetPosition = factor * (double)sizePropertyInfo.GetValue(removeContainer);
                    var removeTranslateTransform = removeContainer.RenderTransform as TranslateTransform;
                    if (removeTranslateTransform == null)
                    {
                        return;
                    }

                    doubleAnimation = new DoubleAnimation()
                    {
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut },
                        To = removeTargetPosition,
                        Duration = TimeSpan.FromMilliseconds(Math.Max(removeTargetPosition / 5, 200))
                    };

                    removeTranslateTransform.BeginAnimation(translateTransformDependencyProperty, doubleAnimation);
                }
            }
            finally
            {
                IsSkipAnimationOnce = false;
                IsForceGoLeft = false;
                IsForceGoRight = false;
            }
        }

        private void ScrollToTargetIndex(int targetIndex)
        {
            if (ScrollViewer == null)
            {
                return;
            }

            foreach (var container in GetAllContainer())
            {
                container.RenderTransform = new TranslateTransform();
            }

            switch (PivotDirection)
            {
                case PivotDirection.Horizontal:
                    ScrollViewer.ScrollToHorizontalOffset(targetIndex);
                    break;

                case PivotDirection.Vertical:
                    ScrollViewer.ScrollToVerticalOffset(targetIndex);
                    break;
            }
        }

        private void Container_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ScrollingStyle == ScrollingStyle.System || e == null)
            {
                return;
            }

            IsMouseLeftButtonDowning = true;
            MouseLeftDownStartPoint = e.GetPosition(sender as Control);
            CumulativeTranslate = 0;
            MovingTarget = sender as PivotItem;

            if (MovingTarget == null)
            {
                return;
            }

            var translateTransform = new TranslateTransform()
            {
                X = (MovingTarget?.RenderTransform as TranslateTransform)?.X ?? 0d,
                Y = (MovingTarget?.RenderTransform as TranslateTransform)?.Y ?? 0d
            };
            MovingTarget.RenderTransform = translateTransform;

            if (IsConnectedStyle)
            {
                foreach (var container in GetAllContainer())
                {
                    container.RenderTransform = translateTransform;
                }
            }

            Debug.WriteLine($"startPoint: {MouseLeftDownStartPoint}");
        }

        private void Container_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ScrollingStyle == ScrollingStyle.System || e == null)
            {
                return;
            }

            if (IsMouseLeftButtonDowning == false || e.LeftButton == MouseButtonState.Released)
            {
                return;
            }

            var firstPivotItem = sender as PivotItem;
            if (firstPivotItem == null)
            {
                return;
            }

            var translateTransform = MovingTarget?.RenderTransform as TranslateTransform;
            if (translateTransform == null)
            {
                return;
            }

            var currentPoint = e.GetPosition(MovingTarget);

            var pointProperty = GetPropertyInfo<Point>();
            var currentPointPosition = (double)pointProperty.GetValue(currentPoint);
            var startPointPosition = (double)pointProperty.GetValue(MouseLeftDownStartPoint);

            var delta = currentPointPosition == startPointPosition ? 0d : currentPointPosition - startPointPosition;
            CumulativeTranslate += delta;

            if (e.MouseDevice?.Captured != null && Math.Abs(CumulativeTranslate) >= TranslateXThreshold)
            {
                // 位移超過門檻
                // 代表應該把 MouseDevice 的 Capture 從原有的 [Button][TextBox] 改為 null
                // 避免觸發原有控制項的其他事件，例如 Button.Click
                e.MouseDevice.Capture(null);
            }

            var translateTransformProperty = GetPropertyInfo<TranslateTransform>();
            var translateTransformPosition = (double)translateTransformProperty.GetValue(translateTransform);
            translateTransformProperty.SetValue(translateTransform, translateTransformPosition + delta);

            IsLastestDirectToRight = delta == 0 || Math.Abs(delta) < LastestDirectThreshold ? IsLastestDirectToRight : delta > 0;

            Debug.WriteLine($"Container_MouseMove delta: {delta}, CumulativeTranslate: {CumulativeTranslate}");
        }

        private void Container_MouseLeave(object sender, MouseEventArgs e)
        {
            // workaround
            // 若起始點為 (X, 0) 或 (0, Y)，系統容易誤觸發 MouseLeave 事件 (即使滑鼠沒有離開)
            // 所以在此判斷若 MouseLeave 且滑鼠位置在合理範圍，則視為沒有離開
            if (IsConnectedStyle)
            {
                var relatedPosition = e.GetPosition(this);

                Debug.WriteLine($"X: {relatedPosition.X}, Y: {relatedPosition.Y}");

                if (relatedPosition.X >= 0 && relatedPosition.Y >= 0 &&
                    relatedPosition.X < this.ActualWidth && relatedPosition.Y < this.ActualHeight)
                {
                    return;
                }
            }

            MouseRelease();
        }

        private void Container_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MouseRelease();
        }

        private void MouseRelease()
        {
            if (ScrollingStyle == ScrollingStyle.System)
            {
                return;
            }

            if (IsMouseLeftButtonDowning == false)
            {
                return;
            }

            IsMouseLeftButtonDowning = false;

            try
            {
                var translateTransform = MovingTarget?.RenderTransform as TranslateTransform;
                if (translateTransform == null)
                {
                    return;
                }

                var restorePositionDoubleAnimation = new DoubleAnimation()
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new CubicEase()
                    {
                        EasingMode = EasingMode.EaseOut
                    }
                };

                // check the delta value
                if (Math.Abs(CumulativeTranslate) < TranslateXThreshold ||
                    (CumulativeTranslate < 0 && IsLastestDirectToRight) ||
                    (CumulativeTranslate > 0 && IsLastestDirectToRight == false))
                {
                    // 位移量不夠，執行還原位置動畫
                    translateTransform.BeginAnimation(GetTranslateTransformDependencyProperty(), restorePositionDoubleAnimation);
                }
                else
                {
                    int selectedIndex;
                    if (CumulativeTranslate < 0)
                    {
                        // next
                        IsForceGoLeft = true;
                        selectedIndex = GetNextSelectedIndex();
                    }
                    else
                    {
                        // prev
                        IsForceGoRight = true;
                        selectedIndex = GetPreviousSelectedIndex();
                    }

                    var setSelectedIndexResult = SetSelectedIndex(selectedIndex);
                    if (setSelectedIndexResult == true)
                    {
                        return;
                    }

                    // SelectedIndex 超出範圍，執行還原位置動畫
                    IsForceGoLeft = false;
                    IsForceGoRight = false;

                    translateTransform.BeginAnimation(GetTranslateTransformDependencyProperty(), restorePositionDoubleAnimation);
                }
            }
            finally
            {
                MovingTarget = null;
                IsLastestDirectToRight = false;

                Debug.WriteLine("MouseRelease");
            }
        }

        private int GetNextSelectedIndex()
        {
            if (ItemContainerGenerator?.Items == null || ScrollingStyle != ScrollingStyle.Single)
            {
                return SelectedIndex + 1;
            }

            var pageData = PageData;
            var targetSelectedIndex = SelectedIndex + pageData.PageSize;
            var diff = this.Items.Count - targetSelectedIndex;

            if (targetSelectedIndex > this.Items.Count)
            {
                targetSelectedIndex = this.Items.Count;
            }
            else if (diff < pageData.PageSize)
            {
                // 不足以顯示一頁
                targetSelectedIndex = SelectedIndex + diff;
            }

            return targetSelectedIndex;
        }

        private int GetPreviousSelectedIndex()
        {
            if (ItemContainerGenerator?.Items == null || ScrollingStyle != ScrollingStyle.Single)
            {
                return SelectedIndex - 1;
            }

            var pageData = PageData;
            var targetSelectedIndex = SelectedIndex - pageData.PageSize;
            var modResult = targetSelectedIndex % pageData.PageSize;

            if (targetSelectedIndex <= 0)
            {
                targetSelectedIndex = 0;
            }
            else if (modResult != 0)
            {
                targetSelectedIndex = SelectedIndex - modResult;
            }

            return targetSelectedIndex;
        }

        private bool SetSelectedIndex(int selectedIndex, bool isSkipScrollAnimation = false)
        {
            if (Items == null || Items.Count == 0)
            {
                return false;
            }

            if (selectedIndex >= Items.Count)
            {
                return false;
            }
            else if (selectedIndex < 0)
            {
                return false;
            }
            else if (SelectedIndex == selectedIndex)
            {
                return false;
            }

            IsSkipAnimationOnce = isSkipScrollAnimation;
            SelectedIndex = selectedIndex;

            return true;
        }

        private IEnumerable<PivotItem> GetAllContainer()
        {
            if (ItemContainerGenerator?.Items == null || ItemContainerGenerator.Items.Count == 0)
            {
                yield break;
            }

            foreach (object item in ItemContainerGenerator.Items)
            {
                var container = ItemContainerGenerator.ContainerFromItem(item) as PivotItem;
                if (container == null)
                {
                    continue;
                }

                yield return container;
            }
        }

        private void SetSlidingMode(bool isSystemAnimation)
        {
            if (isSystemAnimation)
            {
                switch (PivotDirection)
                {
                    case PivotDirection.Horizontal:
                        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Auto);
                        ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
                        break;

                    case PivotDirection.Vertical:
                        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
                        ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Auto);
                        break;
                }

                ScrollViewer.SetPanningMode(this, PanningMode.Both);
                ScrollViewer.SetCanContentScroll(this, false);
            }
            else
            {
                ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
                ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
                ScrollViewer.SetPanningMode(this, PanningMode.None);
                ScrollViewer.SetCanContentScroll(this, true);
            }
        }

        private PropertyInfo GetPropertyInfo<T>()
        {
            switch (PivotDirection)
            {
                case PivotDirection.Horizontal:
                    if (typeof(T) == typeof(Point))
                    {
                        return typeof(Point).GetProperty(nameof(Point.X));
                    }
                    if (typeof(T) == typeof(TranslateTransform))
                    {
                        return typeof(TranslateTransform).GetProperty(nameof(TranslateTransform.X));
                    }
                    if (typeof(T) == typeof(FrameworkElement))
                    {
                        return typeof(FrameworkElement).GetProperty(nameof(FrameworkElement.ActualWidth));
                    }
                    break;

                case PivotDirection.Vertical:
                    if (typeof(T) == typeof(Point))
                    {
                        return typeof(Point).GetProperty(nameof(Point.Y));
                    }
                    if (typeof(T) == typeof(TranslateTransform))
                    {
                        return typeof(TranslateTransform).GetProperty(nameof(TranslateTransform.Y));
                    }
                    if (typeof(T) == typeof(FrameworkElement))
                    {
                        return typeof(FrameworkElement).GetProperty(nameof(FrameworkElement.ActualHeight));
                    }
                    break;
            }

            return default(PropertyInfo);
        }

        private DependencyProperty GetTranslateTransformDependencyProperty()
        {
            switch (PivotDirection)
            {
                case PivotDirection.Horizontal:
                    return TranslateTransform.XProperty;

                case PivotDirection.Vertical:
                    return TranslateTransform.YProperty;
            }

            return default(DependencyProperty);
        }

        private void RefreshIndicator()
        {
            if (IndicatorVisibility != Visibility.Visible)
            {
                return;
            }

            if (IndicatorContainer == null || IndicatorContainer.Children == null)
            {
                return;
            }

            if (this.Items == null)
            {
                IndicatorContainer.Children.Clear();
                return;
            }

            var pageData = PageData;
            var pageCount = pageData.PageCount;
            if (pageCount <= 1)
            {
                // 0 個 item 或 1 個 item 時，不顯示 indicator
                IndicatorContainer.Children.Clear();
                return;
            }

            var pageSize = pageData.PageSize;

            var currentIndicatorCount = IndicatorContainer.Children.Count;

            if (pageCount > currentIndicatorCount)
            {
                IndicatorContainer.Children.Clear();

                for (int i = 0; i < pageCount; i++)
                {
                    var ellipse = new Ellipse()
                    {
                        Style = IndicatorStyle
                    };

                    WeakEventManager<Ellipse, MouseButtonEventArgs>.AddHandler(ellipse, nameof(ellipse.MouseLeftButtonUp), Ellipse_MouseLeftButtonUp);

                    IndicatorContainer.Children.Add(ellipse);
                }
            }
            else if (pageCount < currentIndicatorCount)
            {
                for (int i = 0; i < currentIndicatorCount - pageCount; i++)
                {
                    IndicatorContainer.Children.RemoveAt(0);
                }
            }

            RefreshSelectedIndicator(pageCount, pageSize);
        }

        private void RefreshSelectedIndicator(int pageCount, int pageSize)
        {
            var selectedIndex = this.SelectedIndex;
            if (ScrollingStyle == ScrollingStyle.Single && pageSize > 0)
            {
                selectedIndex = Convert.ToInt32(Math.Ceiling((double)this.SelectedIndex / (double)pageSize));
            }

            for (int i = 0; i < pageCount; i++)
            {
                var ellipse = IndicatorContainer.Children[i] as Ellipse;
                if (ellipse == null)
                {
                    continue;
                }

                var isSelected = i == selectedIndex;
                var targetColor = isSelected ? IndicatorSelectedColor : IndicatorNormalColor;
                var currentBrush = ellipse.Fill as SolidColorBrush;
                if (currentBrush == null)
                {
                    ellipse.Fill = new SolidColorBrush(targetColor);
                }
                else if (currentBrush.Color != targetColor)
                {
                    currentBrush.Color = targetColor;
                }
            }
        }

        private void RefreshSelectedIndex()
        {
            if (ScrollingStyle != ScrollingStyle.Single)
            {
                return;
            }

            var pageData = PageData;
            var currentSelectedIndex = this.SelectedIndex;
            var modResult = currentSelectedIndex % pageData.PageSize;
            if (modResult == 0)
            {
                return;
            }

            var divideResult = currentSelectedIndex / pageData.PageSize;
            var targetSelectedIndex = pageData.PageSize * divideResult;
            ScrollToTargetIndex(targetSelectedIndex);
        }

        private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var ellipse = sender as Ellipse;
            if (ellipse == null)
            {
                return;
            }

            var targetIndex = IndicatorContainer.Children.IndexOf(ellipse);
            if (targetIndex < 0)
            {
                return;
            }

            if (ScrollingStyle == ScrollingStyle.Single)
            {
                var pageData = PageData;
                targetIndex = targetIndex * pageData.PageSize;
            }

            ScrollToTargetIndex(targetIndex);
        }
    }
}