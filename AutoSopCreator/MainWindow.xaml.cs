using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // for Keys

namespace AutoSopCreator
{
    public partial class MainWindow : Window
    {
        private MouseHookService _mouseHook;
        private ScreenCaptureService _screenCapture;
        private GeminiService _geminiService;
        private ExportService _exportService;
        public ObservableCollection<StepModel> RecordedSteps { get; set; }
        private bool _isRecording = false;
        private int _insertTargetIndex = -1; 

        public MainWindow()
        {
            InitializeComponent();
            _mouseHook = new MouseHookService();
            _screenCapture = new ScreenCaptureService();
            _geminiService = new GeminiService();
            _exportService = new ExportService();
            RecordedSteps = new ObservableCollection<StepModel>();
            StepListView.ItemsSource = RecordedSteps;
            _mouseHook.OnMouseClick += HandleMouseClick;
        }

        private void TxtApiKey_PasswordChanged(object sender, RoutedEventArgs e) => _geminiService.SetApiKey(TxtApiKey.Password);

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtApiKey.Password)) { System.Windows.MessageBox.Show("請先輸入 Google API Key！"); return; }
            _isRecording = true;
            RecordedSteps.Clear();
            _mouseHook.Start();
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            BtnExport.IsEnabled = false;
            StatusLabel.Content = "狀態：正在錄製中...";
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _isRecording = false;
            _mouseHook.Stop();
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            string selectedModel = ComboModel.Text;
            StatusLabel.Content = "正在分析圖片...";
            foreach (var step in RecordedSteps)
            {
                if (step.Description == "等待分析..." || string.IsNullOrEmpty(step.Description))
                {
                    step.Description = "AI 分析中...";
                    await AnalyzeStepAsync(step, selectedModel);
                }
            }
            StatusLabel.Content = "分析完成！您可以手動編輯內容或匯出。";
            BtnExport.IsEnabled = true;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is StepModel step)
            {
                RecordedSteps.Remove(step);
                RenumberSteps();
            }
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is StepModel step)
            {
                int currentIndex = RecordedSteps.IndexOf(step);
                _insertTargetIndex = currentIndex + 1;
                _isRecording = true;
                _mouseHook.Start();
                this.WindowState = WindowState.Minimized;
            }
        }

        private async void BtnRegen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is StepModel step)
            {
                step.Description = "AI 重新思考中...";
                string selectedModel = ComboModel.Text;
                await AnalyzeStepAsync(step, selectedModel);
            }
        }

        private async System.Threading.Tasks.Task AnalyzeStepAsync(StepModel step, string modelName)
        {
            try
            {
                string base64Img = ImageHelper.ToBase64(step.Screenshot, 60L);
                string prompt = "你是一個軟體操作手冊的撰寫專家。這張圖片是軟體操作的特寫截圖，紅框標示了操作位置。請用一句簡短、專業的中文描述這個動作。";
                string result = await _geminiService.AnalyzeImageAsync(modelName, base64Img, prompt);
                step.Description = result;
            }
            catch (Exception ex) { step.Description = $"錯誤: {ex.Message}"; }
        }

        private void HandleMouseClick(int x, int y)
        {
            if (!_isRecording) return;
            if (_insertTargetIndex == -1 && ChkHotkeyMode.IsChecked == true)
            {
                bool isCtrl = (System.Windows.Forms.Control.ModifierKeys & Keys.Control) == Keys.Control;
                bool isAlt = (System.Windows.Forms.Control.ModifierKeys & Keys.Alt) == Keys.Alt;
                if (!isCtrl || !isAlt) return;
            }

            Bitmap rawScreenshot = _screenCapture.CaptureScreen();
            
            Dispatcher.Invoke(async () => 
            {
                this.WindowState = WindowState.Minimized;
                SelectionWindow selWin = new SelectionWindow(rawScreenshot);
                selWin.ShowDialog();

                if (!selWin.IsConfirmed)
                {
                    this.WindowState = WindowState.Normal;
                    return;
                }

                Rectangle cropRect = selWin.SelectedRegion;
                Bitmap croppedScreenshot = ImageHelper.CropImage(rawScreenshot, cropRect);

                int newX = x - cropRect.X;
                int newY = y - cropRect.Y;

                HighlightEditorWindow editWin = new HighlightEditorWindow(croppedScreenshot, new System.Windows.Point(newX, newY));
                editWin.ShowDialog();

                this.WindowState = WindowState.Normal;
                this.Activate();

                if (editWin.IsConfirmed)
                {
                    Bitmap finalImage = editWin.FinalBitmap;
                    var step = new StepModel
                    {
                        StepNumber = 0, 
                        Timestamp = DateTime.Now,
                        ActionType = "Click",
                        ClickCoordinates = new System.Drawing.Point(newX, newY),
                        Screenshot = finalImage,
                        Description = "等待分析..."
                    };

                    if (_insertTargetIndex != -1)
                    {
                        RecordedSteps.Insert(_insertTargetIndex, step);
                        _mouseHook.Stop();
                        _isRecording = false;
                        _insertTargetIndex = -1;
                        RenumberSteps();
                        string selectedModel = ComboModel.Text;
                        await AnalyzeStepAsync(step, selectedModel);
                    }
                    else
                    {
                        step.StepNumber = RecordedSteps.Count + 1;
                        RecordedSteps.Add(step);
                        if (StepListView.Items.Count > 0)
                            StepListView.ScrollIntoView(StepListView.Items[StepListView.Items.Count - 1]);
                    }
                }
            });
        }

        private void RenumberSteps()
        {
            for (int i = 0; i < RecordedSteps.Count; i++) RecordedSteps[i].StepNumber = i + 1;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (RecordedSteps.Count == 0) return;
            bool isHtml = RbHtml.IsChecked == true;
            string filter = isHtml ? "HTML 文件|*.html" : "Word 文件|*.docx";
            string defaultExt = isHtml ? ".html" : ".docx";
            
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog 
            { 
                Filter = filter, 
                FileName = $"SOP_教學_{DateTime.Now:yyyyMMdd}{defaultExt}" 
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (isHtml) _exportService.ExportToHtml(RecordedSteps.ToList(), saveFileDialog.FileName);
                    else _exportService.ExportToWord(RecordedSteps.ToList(), saveFileDialog.FileName);
                    System.Windows.MessageBox.Show($"成功！檔案已儲存至：\n{saveFileDialog.FileName}");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = saveFileDialog.FileName, UseShellExecute = true });
                }
                catch (Exception ex) { System.Windows.MessageBox.Show($"匯出失敗：{ex.Message}"); }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _mouseHook?.Stop();
            base.OnClosed(e);
        }
    }
}