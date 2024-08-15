using MusicApp.Models;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace WpfApp1Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenFileDialog openFileDialog; // Đối tượng OpenFileDialog để mở file
        private ObservableCollection<PlayListItem> playlistItems; // Danh sách đường dẫn của các file nhạc
        private int currentIndex; // Chỉ số của bài hát hiện tại
        private DispatcherTimer _timer;
        private DispatcherTimer hideStatusBarTimer;
        private bool userIsDraggingSlider = false;
        private bool isPlay = false;
        private bool isChoose = false; // true -> music; false -> video
        private StreamReader folderPath;
        private bool isFullScreen = false;
        private bool isMouseDown = false;
        private bool isVolume = false;
        private bool isLoop = true;
        private bool isRepeat = false;
        private Rect originalMediaElementRect; // Lưu trữ vị trí và kích thước gốc của MediaElement
        private Rect originalViewboxRect; // Lưu trữ vị trí và kích thước gốc của Viewbox
        private Rect originalStatusBarRect; // Lưu trữ vị trí và kích thước gốc của StatusBar
        private ObservableCollection<PlayListItem> _items;
        private PlayListItem _draggedItem;
        private DataGridRow _draggedRow;
        private const int DragDistance = 10;
        private Point _startPoint;

        public MainWindow()
        {
            InitializeComponent();
            currentIndex = -1;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;
            AddNewButton.Visibility = Visibility.Collapsed;
        }

        private void LoadFiles(string typeFile)
        {
            playlistItems = new ObservableCollection<PlayListItem>();
            string jsonPath = "songlist.json";
            if (typeFile == ".mp4")
            {
                jsonPath = "videolist.json";
            }

            // Đọc tệp JSON
            if (File.Exists(jsonPath))
            {
                string jsonString = File.ReadAllText(jsonPath);

                if (!string.IsNullOrEmpty(jsonString))
                {
                    if (jsonString.TrimStart().StartsWith("["))
                    {
                        // Nếu là mảng JSON, deserialize thành danh sách
                        playlistItems = JsonConvert.DeserializeObject<ObservableCollection<PlayListItem>>(jsonString);
                    }
                    else
                    {
                        // Nếu là đối tượng JSON, deserialize thành một đối tượng và thêm vào danh sách
                        var playlistItem = JsonConvert.DeserializeObject<PlayListItem>(jsonString);
                        playlistItems.Add(playlistItem);
                    }
                }
            }

            // Gán dữ liệu vào ListDataGrid
            ListDataGrid.ItemsSource = playlistItems;
        }

        private void MusicButton_Click(object sender, RoutedEventArgs e)
        {
            MusicButton.Background = new SolidColorBrush(Colors.LightBlue);
            VideoButton.Background = new SolidColorBrush(Colors.LightGray);
            AddNewButton.Visibility = Visibility.Visible;
            ListDataGrid.ItemsSource = null;
            string typeFile = ".mp3";
            LoadFiles(typeFile);
            isChoose = true;
        }

        private void VideoButton_Click(object sender, RoutedEventArgs e)
        {
            VideoButton.Background = new SolidColorBrush(Colors.LightBlue);
            MusicButton.Background = new SolidColorBrush(Colors.LightGray);
            AddNewButton.Visibility = Visibility.Visible;
            ListDataGrid.ItemsSource = null;
            string typeFile = ".mp4";
            LoadFiles(typeFile);
            isChoose = false;
        }

        // Đếm thời gian
        private void Timer_Tick(object sender, EventArgs e)
        {
            if ((PlaylistMediaElement.Source != null) && (PlaylistMediaElement.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                TimeProgressBar.Minimum = 0;
                TimeProgressBar.Maximum = PlaylistMediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                TimeProgressBar.Value = PlaylistMediaElement.Position.TotalSeconds;
            }
        }

        private async void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            string typeFile = ".mp4";
            string jsonPath = "videolist.json";
            if (isChoose)
            {
                typeFile = ".mp3";
                jsonPath = "songlist.json";
            }
            var row = ListDataGrid.SelectedItem as PlayListItem;
            if (row != null)
            {
                var comfirm = System.Windows.MessageBox.Show("Are you sure to delete this item?", "Comfirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (comfirm == MessageBoxResult.Yes)
                {
                    int index = ListDataGrid.Items.IndexOf(row);
                    playlistItems.RemoveAt(index);
                    string jsonString = JsonConvert.SerializeObject(playlistItems, Formatting.Indented);
                    await File.WriteAllTextAsync(jsonPath, jsonString);
                    PlaylistMediaElement.Stop();
                    PlaylistMediaElement.Source = null;
                    LoadFiles(typeFile);
                    // Xóa bài hát khỏi danh sách phát nếu cần
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PlaylistMediaElement.Volume = VolumeSlider.Value;
        }

        // Sự kiện khi người dùng click vào ProgressBar để thay đổi âm lượng
        private void TimeProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            userIsDraggingSlider = true;
            UpdateProgressBar(e);
        }

        private void TimeProgressBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            userIsDraggingSlider = false;
            UpdateProgressBar(e);
            PlaylistMediaElement.Volume = 50;
            if (isPlay)
            {
                PlaylistMediaElement.Play();
            }
        }

        // Sự kiện khi người dùng di chuyển chuột trên ProgressBar để thay đổi âm lượng
        private void TimeProgressBar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TimeProgressBar.Height = 5;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateProgressBar(e);
                PlaylistMediaElement.Volume = 0;
                PlaylistMediaElement.Pause();
            }
        }

        private void TimeProgressBar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TimeProgressBar.Height = 2;
            if (e.LeftButton == MouseButtonState.Released && userIsDraggingSlider)
            {
                PlaylistMediaElement.Volume = VolumeSlider.Value;
                PlaylistMediaElement.Play();
            }
        }

        private void TimeProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CountTimeTextBlock.Text = TimeSpan.FromSeconds(TimeProgressBar.Value).ToString(@"hh\:mm\:ss");
        }

        // Hàm cập nhật giá trị thời gian
        private void UpdateProgressBar(System.Windows.Input.MouseEventArgs e)
        {
            // Tính toán vị trí con trỏ chuột trên ProgressBar
            System.Windows.Point position = e.GetPosition(TimeProgressBar);
            double ratio = position.X / TimeProgressBar.ActualWidth;  // Tính tỷ lệ vị trí con trỏ với chiều rộng ProgressBar

            // Cập nhật giá trị ProgressBar (trong khoảng từ Minimum đến Maximum)
            double newValue = ratio * (TimeProgressBar.Maximum - TimeProgressBar.Minimum) + TimeProgressBar.Minimum;
            TimeProgressBar.Value = newValue;

            // Cập nhật vị trí phát lại của MediaElement
            if (PlaylistMediaElement != null)
            {
                PlaylistMediaElement.Position = TimeSpan.FromSeconds(newValue);
            }
        }

        private void PlayAndPauseButton_Click(object sender, RoutedEventArgs e)
        {
            // Tạo đối tượng Image
            Image playImage = new Image();
            if (isPlay)
            {
                PlaylistMediaElement.Pause();
                playImage.Source = new BitmapImage(new Uri("Assets/Controls/play.png", UriKind.Relative));

                // Gán đối tượng Image vào Button Content
                PlayAndPauseButton.Content = playImage;
                isPlay = false;
            }
            else
            {
                PlaylistMediaElement.Play();
                playImage.Source = new BitmapImage(new Uri("Assets/Controls/pause.png", UriKind.Relative));

                // Gán đối tượng Image vào Button Content
                PlayAndPauseButton.Content = playImage;
                isPlay = true;
            }
        }

        private async void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            string filter = "MP4 files (*.mp4)|*.mp4";
            string typeFile = ".mp4";

            string jsonPath = "videolist.json";
            if (isChoose)
            {
                filter = "MP3 files (*.mp3)|*.mp3";
                typeFile = ".mp3";
                jsonPath = "songlist.json";
            }

            openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Multiselect = true,
                Title = "Open"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<PlayListItem> playlistItems = new List<PlayListItem>();

                try
                {
                    if (File.Exists(jsonPath))
                    {
                        string existingJson = await File.ReadAllTextAsync(jsonPath);
                        if (!string.IsNullOrEmpty(existingJson))
                        {
                            playlistItems = JsonConvert.DeserializeObject<List<PlayListItem>>(existingJson);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Có lỗi xảy ra khi đọc tệp JSON: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (playlistItems.Find(x => x.FilePath == filePath) == null)
                    {
                        try
                        {
                            using (var reader = new AudioFileReader(filePath))
                            {
                                var duration = reader.TotalTime.ToString(@"mm\:ss");
                                var fileInfo = new FileInfo(filePath);

                                PlayListItem song = new PlayListItem
                                {
                                    Name = fileInfo.Name,
                                    Duration = duration,
                                    FilePath = filePath
                                };

                                // Thêm bài hát vào danh sách
                                playlistItems.Add(song);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Có lỗi xảy ra khi đọc file âm thanh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                try
                {
                    // Ghi danh sách cập nhật vào tệp JSON
                    string jsonString = JsonConvert.SerializeObject(playlistItems, Formatting.Indented);
                    await File.WriteAllTextAsync(jsonPath, jsonString);

                    System.Windows.MessageBox.Show("Tất cả các file đã được sao chép thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Có lỗi xảy ra khi ghi tệp JSON: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Tải lại các file
                LoadFiles(typeFile);
            }
        }


        // Phóng to màn hình
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            Image playImage = new Image();

            if (!isFullScreen)
            {
                // Lưu trữ kích thước và vị trí gốc của MediaElement và StatusBar
                originalMediaElementRect = new Rect(PlaylistMediaElement.Margin.Left, PlaylistMediaElement.Margin.Top, PlaylistMediaElement.Width, PlaylistMediaElement.Height);
                originalViewboxRect = new Rect(ScreenViewbox.Margin.Left, ScreenViewbox.Margin.Top, ScreenViewbox.Width, ScreenViewbox.Height);
                originalStatusBarRect = new Rect(ControlsStatusBar.Margin.Left, ControlsStatusBar.Margin.Top, ControlsStatusBar.Width, ControlsStatusBar.Height);

                // Phóng to MediaElement and ViewBox ra toàn màn hình
                PlaylistMediaElement.Margin = new Thickness(0);
                PlaylistMediaElement.Width = SystemParameters.PrimaryScreenWidth;
                PlaylistMediaElement.Height = SystemParameters.PrimaryScreenHeight;
                ScreenViewbox.Margin = new Thickness(0);
                ScreenViewbox.Width = SystemParameters.PrimaryScreenWidth;
                ScreenViewbox.Height = SystemParameters.PrimaryScreenHeight;
                DefaultImage.Margin = new Thickness(0);
                DefaultImage.Width = SystemParameters.PrimaryScreenWidth;
                DefaultImage.Height = SystemParameters.PrimaryScreenHeight;

                // Phóng to StatusBar ra toàn màn hình và đưa xuống đáy
                ControlsStatusBar.Width = SystemParameters.PrimaryScreenWidth;
                ControlsStatusBar.VerticalAlignment = VerticalAlignment.Bottom;
                ControlsStatusBar.Margin = new Thickness(0);

                // Phóng to cửa sổ ra toàn màn hình và vô hiệu hóa thay đổi kích thước
                this.WindowState = WindowState.Maximized;
                this.ResizeMode = ResizeMode.NoResize;

                // Ẩn các thành phần khác
                MusicButton.Visibility = Visibility.Collapsed;
                VideoButton.Visibility = Visibility.Collapsed;
                AddNewButton.Visibility = Visibility.Collapsed;
                ListDataGrid.Visibility = Visibility.Collapsed;
                ControlsStatusBar.Visibility = Visibility.Collapsed;


                // Khởi tạo timer để ẩn StatusBar sau một khoảng thời gian
                hideStatusBarTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3) // 3 giây không di chuyển chuột sẽ ẩn StatusBar
                };
                hideStatusBarTimer.Tick += HideStatusBarTimer_Tick;

                playImage.Source = new BitmapImage(new Uri("Assets/Controls/minimize-screen.ico", UriKind.Relative));

                // Gán đối tượng Image vào Button Content
                FullScreenButton.Content = playImage;

                isFullScreen = true;
            }
            else
            {
                // Khôi phục kích thước và vị trí gốc của MediaElement and ViewBox
                PlaylistMediaElement.Margin = new Thickness(originalMediaElementRect.Left, originalMediaElementRect.Top, 0, 0);
                PlaylistMediaElement.Width = originalMediaElementRect.Width;
                PlaylistMediaElement.Height = originalMediaElementRect.Height;
                ScreenViewbox.Margin = new Thickness(originalViewboxRect.Left, originalViewboxRect.Top, 0, 0);
                ScreenViewbox.Width = originalViewboxRect.Width;
                ScreenViewbox.Height = originalViewboxRect.Height;
                DefaultImage.Margin = new Thickness(originalViewboxRect.Left, originalViewboxRect.Top, 0, 0);
                DefaultImage.Width = originalViewboxRect.Width;
                DefaultImage.Height = originalViewboxRect.Height;

                // Khôi phục StatusBar về kích thước và vị trí gốc
                ControlsStatusBar.Width = originalStatusBarRect.Width;
                ControlsStatusBar.VerticalAlignment = VerticalAlignment.Top;
                ControlsStatusBar.Margin = new Thickness(originalStatusBarRect.Left, originalStatusBarRect.Top, 0, 0);

                // Khôi phục cửa sổ về trạng thái bình thường và cho phép thay đổi kích thước
                this.WindowState = WindowState.Normal;
                this.ResizeMode = ResizeMode.CanResize;

                // Hiển thị lại các thành phần khác
                MusicButton.Visibility = Visibility.Visible;
                VideoButton.Visibility = Visibility.Visible;
                AddNewButton.Visibility = Visibility.Visible;
                ListDataGrid.Visibility = Visibility.Visible;

                // Reset timer
                hideStatusBarTimer.Stop();

                playImage.Source = new BitmapImage(new Uri("Assets/Controls/full-screen.ico", UriKind.Relative));

                // Gán đối tượng Image vào Button Content
                FullScreenButton.Content = playImage;

                isFullScreen = false;
            }
        }

        private void StatusBar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isFullScreen)
            {
                // Hiển thị StatusBar khi di chuyển chuột
                ControlsStatusBar.Visibility = Visibility.Visible;

                // Reset timer
                hideStatusBarTimer.Stop();
                hideStatusBarTimer.Start();
            }
        }

        private void HideStatusBarTimer_Tick(object sender, EventArgs e)
        {
            // Ẩn StatusBar nếu timer kết thúc
            ControlsStatusBar.Visibility = Visibility.Collapsed;
            hideStatusBarTimer.Stop();
        }

        private void NextPlay_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (playlistItems.Count() >= currentIndex + 1)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.png", UriKind.Relative)),
                };
                isPlay = true;
                if (!isRepeat)
                {
                    currentIndex++;
                }
                if (isLoop && playlistItems.Count() == currentIndex)
                {
                    currentIndex = 0;
                }
                CheckToEnableButton();
                PlayListItem nextItem = ListDataGrid.Items.GetItemAt(currentIndex) as PlayListItem;
                ListDataGrid.SelectedItem = nextItem;
                // Phát nhạc tại currentIndex
                PlaylistMediaElement.Source = new Uri(nextItem.FilePath);
                PlaylistMediaElement.Play();
                _timer.Start();
            }
            else
            {
                NextButton.IsEnabled = false;
            }
        }

        // Phát bài trước đó
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex > 0)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.png", UriKind.Relative)),
                };
                isPlay = true;
                currentIndex--;
                CheckToEnableButton();
                PlayListItem nextItem = ListDataGrid.Items.GetItemAt(currentIndex) as PlayListItem;
                ListDataGrid.SelectedItem = nextItem;
                // Phát nhạc tại currentIndex
                PlaylistMediaElement.Source = new Uri(nextItem.FilePath);
                PlaylistMediaElement.Play();
                _timer.Start();
            }
        }

        // Phát bài tiếp theo
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (ListDataGrid.SelectedItem == null)
            {
                return;
            }
            if (playlistItems.Count() > currentIndex + 1)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.png", UriKind.Relative)),
                };
                isPlay = true;
                currentIndex++;
                CheckToEnableButton();
                PlayListItem nextItem = ListDataGrid.Items.GetItemAt(currentIndex) as PlayListItem;
                ListDataGrid.SelectedItem = nextItem;
                // Phát nhạc tại currentIndex
                PlaylistMediaElement.Source = new Uri(nextItem.FilePath);
                PlaylistMediaElement.Play();
                _timer.Start();
            }
        }

        // Hàm check bài đang phát trong danh sách để enable buttton
        private void CheckToEnableButton()
        {
            if (currentIndex == playlistItems.Count() - 1)
            {
                NextButton.IsEnabled = false;
            }
            else
            {
                NextButton.IsEnabled = true;
            }

            if (currentIndex == 0)
            {
                PreviousButton.IsEnabled = false;
            }
            else
            {
                PreviousButton.IsEnabled = true;
            }
        }

        private DataGridRow GetDataGridRow(System.Windows.Input.MouseEventArgs e)
        {
            var point = e.GetPosition(ListDataGrid);
            var hit = ListDataGrid.InputHitTest(point) as DependencyObject;
            while (hit != null && !(hit is DataGridRow))
            {
                hit = VisualTreeHelper.GetParent(hit);
            }
            return hit as DataGridRow;
        }

        private DataGridRow GetDataGridRow(System.Windows.DragEventArgs e)
        {
            var point = e.GetPosition(ListDataGrid);
            var hit = ListDataGrid.InputHitTest(point) as DependencyObject;
            while (hit != null && !(hit is DataGridRow))
            {
                hit = VisualTreeHelper.GetParent(hit);
            }
            return hit as DataGridRow;
        }

        private void ListDataGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_draggedItem != null && e.LeftButton == MouseButtonState.Pressed)
            {
                // Set drag effect
                DragDrop.DoDragDrop((UIElement)sender, _draggedItem, System.Windows.DragDropEffects.Move);
            }
        }

        private void ListDataGrid_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.Move;
            e.Handled = true;
        }

        private void ListDataGrid_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string jsonPath = "songlist.json";
            if (!isChoose)
            {
                jsonPath = "videolist.json";
            }
            if (e.Data.GetDataPresent(typeof(PlayListItem)))
            {
                var droppedItem = (PlayListItem)e.Data.GetData(typeof(PlayListItem));
                var targetRow = GetDataGridRow(e);

                if (targetRow != null)
                {
                    var targetItem = targetRow.DataContext as PlayListItem;
                    if (targetItem != null)
                    {
                        int oldIndex = playlistItems.IndexOf(droppedItem);
                        int newIndex = playlistItems.IndexOf(targetItem);

                        if (oldIndex != newIndex)
                        {
                            playlistItems.Move(oldIndex, newIndex);
                            ListDataGrid.ItemsSource = playlistItems;
                            string jsonString = JsonConvert.SerializeObject(playlistItems, Formatting.Indented);
                            File.WriteAllTextAsync(jsonPath, jsonString);
                        }
                    }
                }
            }
        }

        private void ListDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = GetDataGridRow(e);
            if (row != null)
            {
                _draggedItem = row.DataContext as PlayListItem;
                _draggedRow = row;

                if (_draggedItem != null)
                {
                    _startPoint = e.GetPosition(null); // Lưu vị trí điểm bắt đầu
                }
            }
        }

        private void ListDataGrid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_draggedItem == null) return;

            var pos = e.GetPosition(null);
            var diff = _startPoint - pos;
            if (e.LeftButton == MouseButtonState.Pressed && (Math.Abs(diff.X) > DragDistance || Math.Abs(diff.Y) > DragDistance))
            {
                DragDrop.DoDragDrop(_draggedRow, _draggedItem, System.Windows.DragDropEffects.Move);
                _draggedItem = null; // Reset draggedItem after drag operation
            }
        }

        private void SoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (isVolume == false)
            {
                VolumeSlider.Visibility = Visibility.Visible;
                isVolume = true;
            }
            else
            {
                VolumeSlider.Visibility = Visibility.Collapsed;
                isVolume = false;
            }

        }

        private void ListDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Đặt lại màu nền cho tất cả các hàng trước
            foreach (var item in ListDataGrid.Items)
            {
                var row = (DataGridRow)ListDataGrid.ItemContainerGenerator.ContainerFromItem(item);
                if (row != null)
                {
                    row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C3D37"));
                }
            }

            // Thay đổi màu nền cho hàng được chọn
            if (ListDataGrid.SelectedItem != null)
            {
                var selectedRow = (DataGridRow)ListDataGrid.ItemContainerGenerator.ContainerFromItem(ListDataGrid.SelectedItem);
                if (selectedRow != null)
                {
                    selectedRow.Background = Brushes.Gray; // Màu nền khi được chọn
                }
            }
        }

        private void ListDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = ListDataGrid.SelectedItem as PlayListItem;
            if (row != null)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.png", UriKind.Relative)),
                };
                isPlay = true;
                currentIndex = ListDataGrid.Items.IndexOf(row);
                CheckToEnableButton();
                var duration = TimeSpan.ParseExact(row.Duration, @"mm\:ss", CultureInfo.InvariantCulture);
                DurationTextBlock.Text = duration.ToString(@"hh\:mm\:ss");
                TitleTextBlock.Text = row.Name.Split('.')[0];
                PlaylistMediaElement.Source = new Uri(row.FilePath);
                PlaylistMediaElement.Play();
                _timer.Start();

            }
        }

        private void TypeLoopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoop)
            {
                TypeLoopButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/repeat.png", UriKind.Relative)),
                };
                isLoop = false;
                isRepeat = true;
            }
            else
            {
                TypeLoopButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/loop.png", UriKind.Relative)),
                };
                isLoop = true;
                isRepeat = false;
            }
        }
    }
}