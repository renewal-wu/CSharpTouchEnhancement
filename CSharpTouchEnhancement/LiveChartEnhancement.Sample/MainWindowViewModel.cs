using LiveCharts;
using LiveCharts.Events;
using LiveCharts.Wpf;
using Prism.Commands;
using System;

namespace LiveChartEnhancement.Sample
{
    public class MainWindowViewModel
    {
        public DelegateCommand<ChartPoint> DataHoverCommand { get; set; }
        public DelegateCommand<ChartPoint> DataClickCommand { get; set; }
        public DelegateCommand<CartesianChart> UpdaterTickCommand { get; set; }
        public DelegateCommand<RangeChangedEventArgs> RangeChangedCommand { get; set; }

        public MainWindowViewModel()
        {
            DataClickCommand = new DelegateCommand<ChartPoint>(p =>
            {
                Console.WriteLine("[COMMAND] you clicked " + p.X + ", " + p.Y);
            });

            DataHoverCommand = new DelegateCommand<ChartPoint>(p =>
            {
                Console.WriteLine("[COMMAND] you hovered over " + p.X + ", " + p.Y);
            });

            UpdaterTickCommand = new DelegateCommand<CartesianChart>(c =>
            {
                Console.WriteLine("[COMMAND] Chart was updated!");
            });

            RangeChangedCommand = new DelegateCommand<RangeChangedEventArgs>(e =>
            {
                Console.WriteLine("[COMMAND] Axis range changed");
            });
        }
    }
}