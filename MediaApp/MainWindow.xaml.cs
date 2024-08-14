using MediaApp.Models;
using NAudio.Wave;
using System.IO;
using System.Text;
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

namespace MediaApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        private OpenFileDialog openFileDialog; // Đối tượng OpenFileDialog để mở file
        private List<PlayListItem> playlistItems; // Danh sách đường dẫn của các file nhạc
        private int currentIndex; // Chỉ số của bài hát hiện tại
        private DispatcherTimer _timer;
        private DispatcherTimer hideStatusBarTimer;
        private bool userIsDraggingSlider = false;
        private bool isPlay = false;
        private bool isChoose = false; // true -> music; false -> video
        private string folderPath = "C:\\List";
        private bool isFullScreen = false;
        private Rect originalMediaElementRect; // Lưu trữ vị trí và kích thước gốc của MediaElement
        private Rect originalViewboxRect; // Lưu trữ vị trí và kích thước gốc của Viewbox
        private Rect originalStatusBarRect; // Lưu trữ vị trí và kích thước gốc của StatusBar

        public MainWindow()
        {
            InitializeComponent();
            currentIndex = -1;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            AddNewButton.Visibility = Visibility.Collapsed;
        }

        private void LoadFiles(string typeFile)
        {
            playlistItems = new List<PlayListItem>();
            if (Directory.Exists(folderPath))
            {
                var mp3Files = Directory.GetFiles(folderPath, typeFile);

                foreach (var filePath in mp3Files)
                {
                    var fileInfo = new FileInfo(filePath);

                    // Lấy thông tin thời lượng file sử dụng NAudio
                    string duration;
                    try
                    {
                        using (var reader = new AudioFileReader(filePath))
                        {
                            duration = reader.TotalTime.ToString(@"mm\:ss");
                        }
                    }
                    catch (Exception)
                    {
                        duration = "Unknown";
                    }

                    playlistItems.Add(new PlayListItem
                    {
                        Name = fileInfo.Name,
                        Duration = duration,
                        FilePath = filePath
                    });
                }
                ListDataGrid.ItemsSource = playlistItems;
            }
            else
            {

                // Kiểm tra nếu thư mục đích không tồn tại, tạo mới thư mục
                Directory.CreateDirectory(folderPath);
            }
        }

        private void MusicButton_Click(object sender, RoutedEventArgs e)
        {
            MusicButton.Background = new SolidColorBrush(Colors.LightBlue);
            VideoButton.Background = new SolidColorBrush(Colors.LightGray);
            AddNewButton.Visibility = Visibility.Visible;
            ListDataGrid.ItemsSource = null;
            string typeFile = "*.mp3";
            LoadFiles(typeFile);
            isChoose = true;
        }

        private void VideoButton_Click(object sender, RoutedEventArgs e)
        {
            VideoButton.Background = new SolidColorBrush(Colors.LightBlue);
            MusicButton.Background = new SolidColorBrush(Colors.LightGray);
            AddNewButton.Visibility = Visibility.Visible;
            ListDataGrid.ItemsSource = null;
            string typeFile = "*.mp4";
            LoadFiles(typeFile);
            isChoose = false;
        }

        // Đếm thời gian
        private void Timer_Tick(object sender, EventArgs e)
        {
            if ((PlaylistMediaElement.Source != null) && (PlaylistMediaElement.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = PlaylistMediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                sliProgress.Value = PlaylistMediaElement.Position.TotalSeconds;
            }
        }

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            var row = ListDataGrid.SelectedItem as PlayListItem;
            if (row != null)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.ico", UriKind.Relative)),
                };
                isPlay = true;
                currentIndex = ListDataGrid.Items.IndexOf(row);
                CheckToEnableButton();
                PlaylistMediaElement.Source = new Uri(row.FilePath);
                PlaylistMediaElement.Play();
                _timer.Start();

            }
        }

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            string typeFile = "*.mp4";
            if (isChoose)
            {
                typeFile = "*.mp3";
            }
            var row = ListDataGrid.SelectedItem as PlayListItem;
            if (row != null)
            {
                var comfirm = System.Windows.MessageBox.Show("Are you sure to delete this item?", "Comfirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (comfirm == MessageBoxResult.Yes)
                {
                    int index = ListDataGrid.Items.IndexOf(row);
                    File.Delete(playlistItems[index].FilePath);
                    playlistItems.RemoveAt(index);
                    LoadFiles(typeFile);
                    // Xóa bài hát khỏi danh sách phát nếu cần
                }
            }
        }

        private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            PlaylistMediaElement.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }

        // Sự kiện khi người dùng click vào ProgressBar để thay đổi âm lượng
        private void PbVolume_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateVolume(e);
        }

        // Sự kiện khi người dùng di chuyển chuột trên ProgressBar để thay đổi âm lượng
        private void PbVolume_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateVolume(e);
            }
        }

        // Hàm cập nhật giá trị âm lượng
        private void UpdateVolume(System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point position = e.GetPosition(pbVolume);
            double ratio = position.X / pbVolume.ActualWidth;  // Tính toán tỷ lệ từ vị trí con trỏ chuột
            pbVolume.Value = ratio;
            PlaylistMediaElement.Volume = pbVolume.Value;  // Cập nhật giá trị âm lượng của MediaElement
        }

        private void PlayAndPauseButton_Click(object sender, RoutedEventArgs e)
        {
            // Tạo đối tượng Image
            Image playImage = new Image();
            if (isPlay)
            {
                PlaylistMediaElement.Pause();
                playImage.Source = new BitmapImage(new Uri("Assets/Controls/play.ico", UriKind.Relative));

                // Gán đối tượng Image vào Button Content
                PlayAndPauseButton.Content = playImage;
                isPlay = false;
            }
            else
            {
                PlaylistMediaElement.Play();
                playImage.Source = new BitmapImage(new Uri("Assets/Controls/pause.ico", UriKind.Relative));

                // Gán đối tượng Image vào Button Content
                PlayAndPauseButton.Content = playImage;
                isPlay = true;
            }
        }

        // thêm mới file vào folder
        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            string filter = "MP4 files (*.mp4)|*.mp4";
            string typeFile = "*.mp4";
            if (isChoose)
            {
                filter = "MP3 files (*.mp3)|*.mp3";
                typeFile = "*.mp3";
            }

            openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Multiselect = true,
                Title = "Open"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (playlistItems.Find(x => x.FilePath == filePath) == null)
                    {
                        // Tạo đường dẫn tới file đích
                        string destinationFilePath = System.IO.Path.Combine(folderPath, System.IO.Path.GetFileName(filePath));

                        try
                        {
                            // Sao chép file
                            File.Copy(filePath, destinationFilePath, overwrite: true);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                LoadFiles(typeFile);
                System.Windows.MessageBox.Show("Tất cả các file đã được sao chép thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                //AddNewButton.Visibility = Visibility.Visible;
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
            if (playlistItems.Count() > currentIndex + 1)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.ico", UriKind.Relative)),
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
            else
            {
                NextButton.IsEnabled = false;
            }
        }

        // Ngăn selected item trong data grid trừ button
        private void DataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem phần tử nào đã được nhấn
            DependencyObject originalSource = e.OriginalSource as DependencyObject;

            // Duyệt ngược lên các phần tử cha để kiểm tra xem có phải là Button không
            while (originalSource != null && !(originalSource is DataGridRow))
            {
                if (originalSource is System.Windows.Controls.Button)
                {
                    // Nếu nhấn vào Button, không hủy sự kiện
                    return;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            // Hủy sự kiện để ngăn không cho chọn hàng
            e.Handled = true;
        }


        // Phát bài trước đó
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex > 0)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.ico", UriKind.Relative)),
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
            if (playlistItems.Count() > currentIndex + 1)
            {
                PlayAndPauseButton.Content = new Image
                {
                    Source = new BitmapImage(new Uri("Assets/Controls/pause.ico", UriKind.Relative)),
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
            } else
            {
                NextButton.IsEnabled = true;
            }

            if (currentIndex == 0)
            {
                PreviousButton.IsEnabled = false;
            } else
            {
                PreviousButton.IsEnabled = true;
            }
        }
    }
}