using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace AutoSopCreator
{
    public class StepModel : INotifyPropertyChanged
    {
        private int _stepNumber;
        public int StepNumber 
        { 
            get => _stepNumber;
            set 
            {
                _stepNumber = value;
                OnPropertyChanged(nameof(StepNumber));
            }
        }

        public DateTime Timestamp { get; set; }
        public string ActionType { get; set; }
        public Point ClickCoordinates { get; set; }
        public Bitmap Screenshot { get; set; }

        public BitmapImage DisplayImage
        {
            get
            {
                if (Screenshot == null) return null;
                return BitmapToImageSource(Screenshot);
            }
        }

        private string _description;
        public string Description 
        { 
            get => _description;
            set 
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) 
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
    }
}