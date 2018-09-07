using MaterialDesignThemes.Wpf;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualToolkit;

namespace MaterialDesignInXamlToolkitEnhancement
{
    public class SwipeToShowDrawerHost : DrawerHost
    {
        public static readonly DependencyProperty IsSwipeToShowDrawerEnabledProperty =
            DependencyProperty.Register("IsSwipeToShowDrawerEnabled", typeof(bool), typeof(SwipeToShowDrawerHost), new FrameworkPropertyMetadata(false));

        public bool IsSwipeToShowDrawerEnabled
        {
            get { return (bool)GetValue(IsSwipeToShowDrawerEnabledProperty); }
            set { SetValue(IsSwipeToShowDrawerEnabledProperty, value); }
        }

        private bool IsMouseLeftButtonDowning { get; set; }
        private Point MouseLeftDownStartPoint { get; set; }
        private double CumulativeTranslate { get; set; }
        private bool IsLastestDirectToRight { get; set; }
        private double LastestDirectThreshold { get; } = 3;
        private double TranslateXThreshold { get; set; } = 30;
        private Panel LeftDrawer { get; set; }
        private bool IsScrollViewerMode { get; set; } = false;
        private bool IsDraggingMode { get; set; } = false;
        private ScrollViewer OriginalSourceScrollViewer { get; set; }

        public SwipeToShowDrawerHost()
            : base()
        {
            this.Loaded += SwipeToShowDrawerHost_Loaded;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            LeftDrawer = GetTemplateChild("PART_LeftDrawer") as Panel;
        }

        private void SwipeToShowDrawerHost_Loaded(object sender, RoutedEventArgs e)
        {
            if (LeftDrawerContent == null)
            {
                return;
            }

            this.PreviewMouseLeftButtonDown += SwipeToShowDrawerHost_PreviewMouseLeftButtonDown;
            this.PreviewMouseMove += SwipeToShowDrawerHost_PreviewMouseMove;
            this.PreviewMouseLeftButtonUp += SwipeToShowDrawerHost_PreviewMouseLeftButtonUp;
            this.MouseLeave += SwipeToShowDrawerHost_MouseLeave;
        }

        private void SwipeToShowDrawerHost_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsSwipeToShowDrawerEnabled == false)
            {
                return;
            }

            IsMouseLeftButtonDowning = true;
            MouseLeftDownStartPoint = e.GetPosition(this);
            CumulativeTranslate = 0d;

            var originalSource = e.OriginalSource as FrameworkElement;
            if (originalSource == null)
            {
                return;
            }

            var scrollViewer = originalSource.AncestorsAndSelf<ScrollViewer>().FirstOrDefault();
            if (scrollViewer == null)
            {
                return;
            }

            if (scrollViewer.ScrollableWidth == 0)
            {
                return;
            }

            OriginalSourceScrollViewer = scrollViewer;
        }

        private void SwipeToShowDrawerHost_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseLeftButtonDowning == false || e.LeftButton == MouseButtonState.Released)
            {
                return;
            }

            var currentPoint = e.GetPosition(this);
            var currentPointPosition = currentPoint.X;
            var startPointPosition = MouseLeftDownStartPoint.X;
            var delta = currentPointPosition == startPointPosition ? 0d : currentPointPosition - startPointPosition;

            if (OriginalSourceScrollViewer != null && IsDraggingMode == false)
            {
                if (IsScrollViewerMode ||
                    (OriginalSourceScrollViewer.HorizontalOffset != 0 && OriginalSourceScrollViewer.HorizontalOffset != OriginalSourceScrollViewer.ScrollableWidth) ||
                    (OriginalSourceScrollViewer.HorizontalOffset == 0 && delta < 0) ||
                    (OriginalSourceScrollViewer.HorizontalOffset == OriginalSourceScrollViewer.ScrollableWidth && delta > 0))
                {
                    OriginalSourceScrollViewer.ScrollToHorizontalOffset(OriginalSourceScrollViewer.HorizontalOffset - delta);
                    MouseLeftDownStartPoint = currentPoint;

                    IsScrollViewerMode = true;
                    return;
                }
            }

            if ((IsLeftDrawerOpen && delta > 0) ||
                (IsLeftDrawerOpen && delta < 0 && delta <= -1 * LeftDrawer.ActualWidth) ||
                (IsLeftDrawerOpen == false && delta < 0) ||
                (IsLeftDrawerOpen == false && delta > 0 && delta >= LeftDrawer.ActualWidth))
            {
                return;
            }

            if (e.MouseDevice?.Captured != null && Math.Abs(CumulativeTranslate) >= TranslateXThreshold)
            {
                // 位移超過門檻
                // 代表應該把 MouseDevice 的 Capture 從原有的 [Button][TextBox] 改為 null
                // 避免觸發原有控制項的其他事件，例如 Button.Click
                e.MouseDevice.Capture(null);
            }

            var diff = delta - CumulativeTranslate;
            IsLastestDirectToRight = diff == 0 || Math.Abs(delta) < LastestDirectThreshold ? IsLastestDirectToRight : diff > 0;

            CumulativeTranslate = delta;

            var baseMargin = IsLeftDrawerOpen ? 0 : (-1 * LeftDrawer.ActualWidth);
            var marginLeft = baseMargin + delta;
            LeftDrawer.Margin = new Thickness(marginLeft, 0, 0, 0);

            IsDraggingMode = true;
        }

        private void SwipeToShowDrawerHost_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MouseRelease();
        }

        private void SwipeToShowDrawerHost_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MouseRelease();
        }

        private void MouseRelease()
        {
            if (IsMouseLeftButtonDowning == false)
            {
                return;
            }

            Debug.WriteLine("MouseRelease");

            try
            {
                if (Math.Abs(CumulativeTranslate) < TranslateXThreshold ||
                    (CumulativeTranslate < 0 && IsLastestDirectToRight) ||
                    (CumulativeTranslate > 0 && IsLastestDirectToRight == false))
                {
                }
                else
                {
                    if (CumulativeTranslate < 0)
                    {
                        this.IsLeftDrawerOpen = false;
                    }
                    else
                    {
                        this.IsLeftDrawerOpen = true;
                    }
                }
            }
            finally
            {
                IsLastestDirectToRight = false;
                IsScrollViewerMode = false;
                IsDraggingMode = false;
                OriginalSourceScrollViewer = null;
                Debug.WriteLine("MouseRelease");
            }

            IsMouseLeftButtonDowning = false;
        }
    }
}