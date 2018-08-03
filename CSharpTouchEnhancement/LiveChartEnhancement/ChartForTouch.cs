using LiveCharts.Dtos;
using LiveCharts.Wpf.Charts.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LiveChartEnhancement
{
    public class ChartForTouch
    {
        private static int zoomTouchCountLimit = 2;
        private static double zoomDistanceTreshold = 10d;

        public static readonly DependencyProperty IsPinchEnabledProperty =
            DependencyProperty.RegisterAttached("IsPinchEnabled", typeof(bool), typeof(Chart), new FrameworkPropertyMetadata(false));

        public static void SetIsPinchEnabled(UIElement element, bool value)
        {
            element.SetValue(IsPinchEnabledProperty, value);

            var chart = element as Chart;
            if (chart == null)
            {
                return;
            }

            if (value)
            {
                RegisterTouchEventHandler(chart);
            }
            else
            {
                UnregisterTouchEventHandler(chart);
            }
        }

        public static bool GetIsPinchEnabled(UIElement element)
        {
            return (bool)element.GetValue(IsPinchEnabledProperty);
        }

        private static readonly DependencyProperty CurrentTouchDevicesProperty =
            DependencyProperty.RegisterAttached("CurrentTouchDevices", typeof(Dictionary<int, Point>), typeof(Chart), new FrameworkPropertyMetadata(new Dictionary<int, Point>()));

        private static void SetCurrentTouchDevices(UIElement element, Dictionary<int, Point> value)
        {
            element.SetValue(CurrentTouchDevicesProperty, value);
        }

        private static Dictionary<int, Point> GetCurrentTouchDevices(UIElement element)
        {
            return (Dictionary<int, Point>)element.GetValue(CurrentTouchDevicesProperty);
        }

        private static readonly DependencyProperty ZoomStartingDistanceProperty =
            DependencyProperty.RegisterAttached("ZoomStartingDistance", typeof(double), typeof(Chart), new FrameworkPropertyMetadata(default(double)));

        private static void SetZoomStartingDistance(UIElement element, double value)
        {
            element.SetValue(ZoomStartingDistanceProperty, value);
        }

        private static double GetZoomStartingDistance(UIElement element)
        {
            return (double)element.GetValue(ZoomStartingDistanceProperty);
        }

        private static void RegisterTouchEventHandler(Chart chart)
        {
            WeakEventManager<Chart, TouchEventArgs>.AddHandler(chart, nameof(chart.PreviewTouchDown), Chart_PreviewTouchDown);
            WeakEventManager<Chart, TouchEventArgs>.AddHandler(chart, nameof(chart.PreviewTouchMove), Chart_PreviewTouchMove);
            WeakEventManager<Chart, TouchEventArgs>.AddHandler(chart, nameof(chart.PreviewTouchUp), Chart_PreviewTouchUp);
            WeakEventManager<Chart, TouchEventArgs>.AddHandler(chart, nameof(chart.TouchLeave), Chart_TouchLeave);
        }

        private static void UnregisterTouchEventHandler(Chart chart)
        {
            WeakEventManager<Chart, TouchEventArgs>.RemoveHandler(chart, nameof(chart.PreviewTouchDown), Chart_PreviewTouchDown);
            WeakEventManager<Chart, TouchEventArgs>.RemoveHandler(chart, nameof(chart.PreviewTouchMove), Chart_PreviewTouchMove);
            WeakEventManager<Chart, TouchEventArgs>.RemoveHandler(chart, nameof(chart.PreviewTouchUp), Chart_PreviewTouchUp);
            WeakEventManager<Chart, TouchEventArgs>.RemoveHandler(chart, nameof(chart.TouchLeave), Chart_TouchLeave);
        }

        private static void Chart_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            var chart = sender as Chart;
            if (chart == null)
            {
                return;
            }

            var currentTouchDevices = GetCurrentTouchDevices(chart);
            if (currentTouchDevices == null)
            {
                return;
            }

            if (currentTouchDevices.Count >= zoomTouchCountLimit)
            {
                return;
            }

            currentTouchDevices.Add(e.TouchDevice.Id, e.GetTouchPoint(chart).Position);
            Debug.WriteLine($"TouchDown: {e.TouchDevice.Id}");

            if (currentTouchDevices.Count < zoomTouchCountLimit)
            {
                return;
            }

            var firstPoint = currentTouchDevices.ElementAt(0).Value;
            var secondPoint = currentTouchDevices.ElementAt(1).Value;

            var zoomStartingDistance = CalculateDistance(firstPoint, secondPoint);
            SetZoomStartingDistance(chart, zoomStartingDistance);
        }

        private static void Chart_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            var chart = sender as Chart;
            if (chart == null)
            {
                return;
            }

            var currentTouchDevices = GetCurrentTouchDevices(chart);
            if (currentTouchDevices == null)
            {
                return;
            }

            if (currentTouchDevices.Count < zoomTouchCountLimit)
            {
                return;
            }

            var touchDeviceId = e.TouchDevice.Id;
            if (currentTouchDevices.ContainsKey(touchDeviceId) == false)
            {
                return;
            }

            currentTouchDevices[touchDeviceId] = e.GetTouchPoint(chart).Position;

            var firstPoint = currentTouchDevices.ElementAt(0).Value;
            var secondPoint = currentTouchDevices.ElementAt(1).Value;

            var zoomStartingDistance = GetZoomStartingDistance(chart);
            var newDistance = CalculateDistance(firstPoint, secondPoint);
            var diffDistance = zoomStartingDistance - newDistance;

            Debug.WriteLine($"DiffDistance {diffDistance}");

            if (Math.Abs(diffDistance) < zoomDistanceTreshold)
            {
                return;
            }

            SetZoomStartingDistance(chart, newDistance);

            var corePoint = new CorePoint((firstPoint.X + secondPoint.X) / 2, (firstPoint.Y + secondPoint.Y) / 2);
            if (diffDistance < 0)
            {
                chart.Model.ZoomIn(corePoint);
            }
            else
            {
                chart.Model.ZoomOut(corePoint);
            }
        }

        private static void Chart_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            var chart = sender as Chart;
            if (chart == null)
            {
                return;
            }

            var currentTouchDevices = GetCurrentTouchDevices(chart);
            if (currentTouchDevices == null)
            {
                return;
            }

            currentTouchDevices.Clear();
            SetZoomStartingDistance(chart, 0d);
        }

        private static void Chart_TouchLeave(object sender, TouchEventArgs e)
        {
            var chart = sender as Chart;
            if (chart == null)
            {
                return;
            }

            var currentTouchDevices = GetCurrentTouchDevices(chart);
            if (currentTouchDevices == null)
            {
                return;
            }

            currentTouchDevices.Clear();
            SetZoomStartingDistance(chart, 0d);
        }

        private static double CalculateDistance(Point firstPoint, Point secondPoint)
        {
            return Math.Sqrt(Math.Pow(firstPoint.X - secondPoint.X, 2) + Math.Pow(firstPoint.Y - secondPoint.Y, 2));
        }
    }
}