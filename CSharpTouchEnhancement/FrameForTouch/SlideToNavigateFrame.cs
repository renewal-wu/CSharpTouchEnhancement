using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VisualToolkit;

namespace FrameForTouch
{
    public class SlideToNavigateFrame : Frame
    {
        public static readonly DependencyProperty NavigationTargetsProperty =
            DependencyProperty.Register(nameof(NavigationTargets), typeof(List<object>), typeof(SlideToNavigateFrame), new PropertyMetadata(default(List<object>), OnNavigationTargetsChanged));

        public List<object> NavigationTargets
        {
            get { return (List<object>)GetValue(NavigationTargetsProperty); }
            set { SetValue(NavigationTargetsProperty, value); }
        }

        private static void OnNavigationTargetsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slideToNavigateFrame = d as SlideToNavigateFrame;
            if (slideToNavigateFrame == null)
            {
                return;
            }

            slideToNavigateFrame.UpdateTouchEventHandler();
        }

        private TranslateTransform SlideToNavigateFrameTranslateTransform { get; set; }
        private bool IsMouseLeftButtonDowning { get; set; }
        private Point MouseLeftDownStartPoint { get; set; }
        private double CumulativeTranslate { get; set; }
        private bool IsLastestDirectToRight { get; set; }
        private double LastestDirectThreshold { get; } = 3;
        private double TranslateXThreshold { get; set; } = 30;
        private bool IsScrollViewerMode { get; set; } = false;
        private bool IsDraggingMode { get; set; } = false;
        private ScrollViewer OriginalSourceScrollViewer { get; set; }

        public SlideToNavigateFrame()
        {
            this.Loaded += SlideToNavigateFrame_Loaded;

            this.SlideToNavigateFrameTranslateTransform = new TranslateTransform();
            this.RenderTransform = SlideToNavigateFrameTranslateTransform;
            this.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void SlideToNavigateFrame_Loaded(object sender, RoutedEventArgs e)
        {
            ShowDefaultPage();
        }

        private void ShowDefaultPage()
        {
            if (Content != null || NavigationTargets == null || NavigationTargets.Count == 0)
            {
                return;
            }

            Navigate(NavigationTargets.FirstOrDefault());
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            UpdateTouchEventHandler();
        }

        private void UpdateTouchEventHandler()
        {
            this.PreviewMouseLeftButtonDown -= SlideToNavigateFrame_PreviewMouseLeftButtonDown;
            this.PreviewMouseMove -= SlideToNavigateFrame_PreviewMouseMove;
            this.PreviewMouseLeftButtonUp -= SlideToNavigateFrame_PreviewMouseLeftButtonUp;
            this.MouseLeave -= SlideToNavigateFrame_MouseLeave;

            if (NavigationTargets == null || NavigationTargets.Count == 0 || NavigationTargets.Contains(this.Content) == false)
            {
                // 若目前所導覽到的畫面並不存在於 NavigationTargets，則不啟用觸控滑動換頁
                return;
            }

            this.PreviewMouseLeftButtonDown += SlideToNavigateFrame_PreviewMouseLeftButtonDown;
            this.PreviewMouseMove += SlideToNavigateFrame_PreviewMouseMove;
            this.PreviewMouseLeftButtonUp += SlideToNavigateFrame_PreviewMouseLeftButtonUp;
            this.MouseLeave += SlideToNavigateFrame_MouseLeave;
        }

        private void SlideToNavigateFrame_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsMouseLeftButtonDowning = true;
            MouseLeftDownStartPoint = e.GetPosition(this);
            CumulativeTranslate = 0;

            var translateTransform = new TranslateTransform()
            {
                X = (SlideToNavigateFrameTranslateTransform)?.X ?? 0d,
                Y = (SlideToNavigateFrameTranslateTransform)?.Y ?? 0d
            };

            SlideToNavigateFrameTranslateTransform = translateTransform;
            this.RenderTransform = SlideToNavigateFrameTranslateTransform;

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

        private void SlideToNavigateFrame_PreviewMouseMove(object sender, MouseEventArgs e)
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

            IsScrollViewerMode = false;

            CumulativeTranslate += delta;

            if (e.MouseDevice?.Captured != null && Math.Abs(CumulativeTranslate) >= TranslateXThreshold)
            {
                // 位移超過門檻
                // 代表應該把 MouseDevice 的 Capture 從原有的 [Button][TextBox] 改為 null
                // 避免觸發原有控制項的其他事件，例如 Button.Click
                e.MouseDevice.Capture(null);
            }

            SlideToNavigateFrameTranslateTransform.X += delta;

            IsDraggingMode = SlideToNavigateFrameTranslateTransform.X != 0;

            this.Opacity = Math.Max(0d, 1d - Math.Abs(CumulativeTranslate) / 300d);

            IsLastestDirectToRight = delta == 0 || Math.Abs(delta) < LastestDirectThreshold ? IsLastestDirectToRight : delta > 0;

            Debug.WriteLine($"Container_MouseMove delta: {delta}");
        }

        private void SlideToNavigateFrame_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseRelease();
        }

        private void SlideToNavigateFrame_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseRelease();
        }

        private void MouseRelease()
        {
            if (IsMouseLeftButtonDowning == false)
            {
                return;
            }

            IsMouseLeftButtonDowning = false;

            try
            {
                var translateTransform = SlideToNavigateFrameTranslateTransform;
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
                    translateTransform.BeginAnimation(TranslateTransform.XProperty, restorePositionDoubleAnimation);
                    this.Opacity = 1d;
                }
                else
                {
                    object navigationTarget;
                    if (CumulativeTranslate < 0)
                    {
                        // next
                        navigationTarget = GetNextNavigationTarget();
                    }
                    else
                    {
                        // prev
                        navigationTarget = GetPreviousNavigationTarget();
                    }

                    var setNavigationTarget = SetNavigationTarget(navigationTarget);
                    if (setNavigationTarget == true)
                    {
                        return;
                    }

                    // SelectedIndex 超出範圍，執行還原位置動畫
                    translateTransform.BeginAnimation(TranslateTransform.XProperty, restorePositionDoubleAnimation);
                    this.Opacity = 1d;
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
        }

        private object GetNextNavigationTarget()
        {
            var currentContent = this.Content;
            if (NavigationTargets == null || NavigationTargets.Contains(currentContent) == false)
            {
                return null;
            }

            var index = NavigationTargets.IndexOf(currentContent) + 1;
            if (index >= NavigationTargets.Count)
            {
                index = 0;
            }

            return NavigationTargets[index];
        }

        private object GetPreviousNavigationTarget()
        {
            var currentContent = this.Content;
            if (NavigationTargets == null || NavigationTargets.Contains(currentContent) == false)
            {
                return null;
            }

            var index = NavigationTargets.IndexOf(currentContent) - 1;
            if (index < 0)
            {
                index = NavigationTargets.Count - 1;
            }

            return NavigationTargets[index];
        }

        private bool SetNavigationTarget(object navigationTarget)
        {
            if (navigationTarget == null)
            {
                return false;
            }

            var slideToNavigateFrameTranslateTransform = SlideToNavigateFrameTranslateTransform;

            var moveTargetPosition = (IsLastestDirectToRight ? 1 : -1) * 300;

            slideToNavigateFrameTranslateTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation()
            {
                To = moveTargetPosition,
                Duration = TimeSpan.FromMilliseconds(150)
            });

            slideToNavigateFrameTranslateTransform.Changed += (o, args) =>
            {
                if (slideToNavigateFrameTranslateTransform != SlideToNavigateFrameTranslateTransform)
                {
                    return;
                }

                this.Opacity = 1d - (slideToNavigateFrameTranslateTransform.X / moveTargetPosition);

                if (Math.Abs(Math.Abs(slideToNavigateFrameTranslateTransform.X) - Math.Abs(moveTargetPosition)) > 0.00000001)
                {
                    return;
                }

                // 位移動畫完成了
                this.Navigate(navigationTarget);

                SlideToNavigateFrameTranslateTransform = new TranslateTransform()
                {
                    X = moveTargetPosition * -1d
                };

                slideToNavigateFrameTranslateTransform = SlideToNavigateFrameTranslateTransform;
                this.RenderTransform = SlideToNavigateFrameTranslateTransform;

                moveTargetPosition *= -1;

                slideToNavigateFrameTranslateTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation()
                {
                    From = moveTargetPosition,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(150)
                });

                slideToNavigateFrameTranslateTransform.Changed += (obj, e) =>
                {
                    if (slideToNavigateFrameTranslateTransform != SlideToNavigateFrameTranslateTransform)
                    {
                        return;
                    }

                    this.Opacity = 1d - (slideToNavigateFrameTranslateTransform.X / moveTargetPosition);
                };
            };

            return true;
        }
    }
}