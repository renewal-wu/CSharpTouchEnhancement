using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameForTouch.Sample
{
    public class MainWindowViewModel
    {
        public List<object> NavigationTargets { get; } = new List<object>()
        {
            new HomePage(),
            new SettingPage(),
            new LoginPage()
        };

        public MainWindowViewModel()
        {
        }
    }
}