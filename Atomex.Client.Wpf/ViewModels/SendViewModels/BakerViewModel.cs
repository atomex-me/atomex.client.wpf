using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class BakerViewModel : BaseViewModel
    {
        public string Logo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Fee { get; set; }
    }
}
