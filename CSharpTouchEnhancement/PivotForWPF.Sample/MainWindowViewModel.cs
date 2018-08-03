using System.Collections.Generic;

namespace PivotForWPF.Sample
{
    public class MainWindowViewModel
    {
        public List<DemoData> DemoDataCollection { get; } = new List<DemoData>();

        public MainWindowViewModel()
        {
            for (int i = 0; i < 1000; i++)
            {
                DemoDataCollection.Add(new DemoData()
                {
                    Name = i.ToString()
                });
            }
        }
    }

    public class DemoData
    {
        public string Name { get; set; }
    }
}