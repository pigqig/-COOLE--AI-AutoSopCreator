using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Drawing; // System.Drawing.Common
using System.IO;

namespace AutoSopCreator
{
    public partial class SelectionWindow : Window
    {
        private System.Windows.Point _startPoint;
        private bool _isDragging = false;
        public System.Drawing.Rectangle SelectedRegion { get; private set; }
        public bool IsConfirmed { get; private set; } = false;

        public SelectionWindow(Bitmap fullScreenShot)
        {
            InitializeComponent();
            BgImage.Source = BitmapToImageSource(fullScreenShot);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _startPoint = e.GetPosition(DrawingCanvas);
            Canvas.SetLeft(SelectionBox, _startPoint.X);
            Canvas.SetTop(SelectionBox, _startPoint.Y);
            SelectionBox.Width = 0;
            SelectionBox.Height = 0;
            SelectionBox.Visibility = Visibility.Visible;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var currentPoint = e.GetPosition(DrawingCanvas);
            var x = Math.Min(currentPoint.X, _startPoint.X);
            var y = Math.Min(currentPoint.Y, _startPoint.Y);
            var w = Math.Abs(currentPoint.X - _startPoint.X);
            var h = Math.Abs(currentPoint.Y - _startPoint.Y);
            Canvas.SetLeft(SelectionBox, x);
            Canvas.SetTop(SelectionBox, y);
            SelectionBox.Width = w;
            SelectionBox.Height = h;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            double x = Canvas.GetLeft(SelectionBox);
            double y = Canvas.GetTop(SelectionBox);
            double w = SelectionBox.Width;
            double h = SelectionBox.Height;
            if (w < 10 || h < 10) { SelectionBox.Visibility = Visibility.Collapsed; return; }
            SelectedRegion = new System.Drawing.Rectangle((int)x, (int)y, (int)w, (int)h);
            IsConfirmed = true;
            this.Close();
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
                return bitmapImage;
            }
        }
    }
}