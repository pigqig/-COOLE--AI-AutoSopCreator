using System;
using System.Collections.Generic;
using System.Drawing; // System.Drawing.Common
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutoSopCreator
{
    public partial class HighlightEditorWindow : Window
    {
        private Bitmap _originalBitmap;
        public Bitmap FinalBitmap { get; private set; }
        public bool IsConfirmed { get; private set; } = false;

        // 狀態變數
        private Grid _selectedGrid = null; // 當前選取的紅框容器
        private bool _isDraggingBox = false;
        private bool _isResizing = false;
        private System.Windows.Point _clickPoint;
        private double _origX, _origY, _origW, _origH;
        private string _resizeDir = "";

        public HighlightEditorWindow(Bitmap image, System.Windows.Point initialClickPoint)
        {
            InitializeComponent();
            _originalBitmap = image;
            ImgPreview.Source = BitmapToImageSource(image);

            // 建立第一個初始紅框
            CreateResizableBox(initialClickPoint.X, initialClickPoint.Y);
        }

        // --- 核心：動態建立可調整的紅框 ---
        private void CreateResizableBox(double centerX, double centerY)
        {
            double w = 100, h = 60;
            double left = centerX - (w / 2);
            double top = centerY - (h / 2);
            if (left < 0) left = 0;
            if (top < 0) top = 0;

            // 容器 Grid
            Grid grid = new Grid();
            grid.Width = w;
            grid.Height = h;
            Canvas.SetLeft(grid, left);
            Canvas.SetTop(grid, top);

            // 主紅框
            Border border = new Border();
            border.BorderBrush = System.Windows.Media.Brushes.Red;
            border.BorderThickness = new Thickness(3);
            border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 255, 255, 255)); // 幾乎透明以利抓取
            border.Cursor = Cursors.SizeAll;
            border.MouseLeftButtonDown += Box_MouseDown;
            border.MouseLeftButtonUp += Box_MouseUp;
            border.MouseMove += Box_MouseMove;
            grid.Children.Add(border);

            // 加入 8 個控制點
            AddHandle(grid, HorizontalAlignment.Left, VerticalAlignment.Top, Cursors.SizeNWSE, "NW", -4, -4, 0, 0);
            AddHandle(grid, HorizontalAlignment.Center, VerticalAlignment.Top, Cursors.SizeNS, "N", 0, -4, 0, 0);
            AddHandle(grid, HorizontalAlignment.Right, VerticalAlignment.Top, Cursors.SizeNESW, "NE", 0, -4, -4, 0);
            AddHandle(grid, HorizontalAlignment.Left, VerticalAlignment.Center, Cursors.SizeWE, "W", -4, 0, 0, 0);
            AddHandle(grid, HorizontalAlignment.Right, VerticalAlignment.Center, Cursors.SizeWE, "E", 0, 0, -4, 0);
            AddHandle(grid, HorizontalAlignment.Left, VerticalAlignment.Bottom, Cursors.SizeNESW, "SW", -4, 0, 0, -4);
            AddHandle(grid, HorizontalAlignment.Center, VerticalAlignment.Bottom, Cursors.SizeNS, "S", 0, 0, 0, -4);
            AddHandle(grid, HorizontalAlignment.Right, VerticalAlignment.Bottom, Cursors.SizeNWSE, "SE", 0, 0, -4, -4);

            OverlayCanvas.Children.Add(grid);
            SelectGrid(grid);
        }

        private void AddHandle(Grid parent, HorizontalAlignment ha, VerticalAlignment va, Cursor cursor, string tag, double l, double t, double r, double b)
        {
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
            rect.Style = (Style)FindResource("HandleStyle");
            rect.HorizontalAlignment = ha;
            rect.VerticalAlignment = va;
            rect.Cursor = cursor;
            rect.Tag = tag;
            rect.Margin = new Thickness(l, t, r, b);
            rect.MouseLeftButtonDown += Handle_MouseDown;
            rect.MouseLeftButtonUp += Handle_MouseUp;
            rect.MouseMove += Handle_MouseMove;
            parent.Children.Add(rect);
        }

        private void SelectGrid(Grid grid)
        {
            // 如果之前有選取的，可以做視覺復原(選用)
            _selectedGrid = grid;
        }

        // --- 事件處理：新增與刪除 ---
        private void BtnAddBox_Click(object sender, RoutedEventArgs e)
        {
            // 在畫面中心新增
            CreateResizableBox(ImgPreview.ActualWidth / 2, ImgPreview.ActualHeight / 2);
        }

        private void BtnDeleteBox_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid != null && OverlayCanvas.Children.Contains(_selectedGrid))
            {
                OverlayCanvas.Children.Remove(_selectedGrid);
                _selectedGrid = null;
            }
        }

        // --- 移動邏輯 ---
        private void Box_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBox = true;
            _selectedGrid = (Grid)((FrameworkElement)sender).Parent; // 選取該框
            _clickPoint = e.GetPosition(OverlayCanvas);
            _origX = Canvas.GetLeft(_selectedGrid);
            _origY = Canvas.GetTop(_selectedGrid);
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }

        private void Box_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingBox || _selectedGrid == null) return;
            var currentPos = e.GetPosition(OverlayCanvas);
            double newX = _origX + (currentPos.X - _clickPoint.X);
            double newY = _origY + (currentPos.Y - _clickPoint.Y);
            Canvas.SetLeft(_selectedGrid, newX);
            Canvas.SetTop(_selectedGrid, newY);
        }

        private void Box_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBox = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        // --- 調整大小邏輯 ---
        private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing = true;
            _resizeDir = ((FrameworkElement)sender).Tag.ToString();
            _selectedGrid = (Grid)((FrameworkElement)sender).Parent;
            _clickPoint = e.GetPosition(OverlayCanvas);
            _origX = Canvas.GetLeft(_selectedGrid);
            _origY = Canvas.GetTop(_selectedGrid);
            _origW = _selectedGrid.Width;
            _origH = _selectedGrid.Height;
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizing || _selectedGrid == null) return;
            var curPos = e.GetPosition(OverlayCanvas);
            double dx = curPos.X - _clickPoint.X;
            double dy = curPos.Y - _clickPoint.Y;

            double newX = _origX, newY = _origY, newW = _origW, newH = _origH;

            if (_resizeDir.Contains("E")) newW = _origW + dx;
            if (_resizeDir.Contains("S")) newH = _origH + dy;
            if (_resizeDir.Contains("W")) { newW = _origW - dx; newX = _origX + dx; }
            if (_resizeDir.Contains("N")) { newH = _origH - dy; newY = _origY + dy; }

            if (newW >= 10 && newH >= 10)
            {
                _selectedGrid.Width = newW;
                _selectedGrid.Height = newH;
                Canvas.SetLeft(_selectedGrid, newX);
                Canvas.SetTop(_selectedGrid, newY);
            }
        }

        private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        // --- 確認並繪製所有紅框 ---
        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            FinalBitmap = new Bitmap(_originalBitmap);
            using (Graphics g = Graphics.FromImage(FinalBitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Red, 4))
                {
                    // 遍歷 Canvas 上所有的 Grid (紅框)
                    foreach (var child in OverlayCanvas.Children)
                    {
                        if (child is Grid grid)
                        {
                            double x = Canvas.GetLeft(grid);
                            double y = Canvas.GetTop(grid);
                            double w = grid.Width;
                            double h = grid.Height;
                            g.DrawRectangle(pen, (int)x, (int)y, (int)w, (int)h);
                        }
                    }
                }
            }
            IsConfirmed = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
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