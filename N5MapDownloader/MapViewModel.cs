using N5MapDownloader.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N5MapDownloader
{
    public class MapViewModel : PropertyChanger
    {
        private bool _isSelected;
        private string _status;
        public enum SizeUnits
        {
            Byte, KB, MB, GB, TB, PB, EB, ZB, YB
        }
        public MapViewModel(string name, int size) 
        {           
            Name = name;
            Size = (size / (double)Math.Pow(1024, 2)).ToString("0.00") + " мб";
            Status = "Не загружено";
        }

        public string Name { get; set; }
        
        public bool IsSelected 
        { 
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Size { get; set; }
    }
}
