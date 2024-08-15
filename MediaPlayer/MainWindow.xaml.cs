using MediaPlayer.BLL;
using MediaPlayer.BLL.Services;
using MediaPlayer.DAL.Entities;
using System.Windows;
using System.Windows.Controls;

namespace MediaPlayer.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RecentSongsService _recentSongsService = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void cc(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SwapZIndex(UIElement element1, UIElement element2)
        {
            int zindex1 = Panel.GetZIndex(element1);
            int zindex2 = Panel.GetZIndex(element2);

            Panel.SetZIndex(element1, zindex2);
            Panel.SetZIndex(element2, zindex1);
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            SwapZIndex(PauseButton, PlayButton);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            SwapZIndex(PauseButton, PlayButton);
        }
        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            SwapZIndex(Screen, SongQueue);
        }

        private void OpenPlayQueueButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _recentSongsService.LoadData();
            FillSongQueue();
        }

        private void FillSongQueue()
        {

            SongQueue.ItemsSource = null;
            SongQueue.ItemsSource = _recentSongsService.RecentSongs.AsEnumerable().Reverse();
        }


        //trước khi tắt app thì nó thực hiện lưu recent sóng xuống file json
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _recentSongsService.SaveData();

        }



        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Music files (*.mp3;*.mp4)|*.mp3;*.mp4|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                // Nạp video vào MediaElement
                mediaElement.Source = new Uri(filePath, UriKind.RelativeOrAbsolute);
                // Bắt đầu phát video
                mediaElement.Play();
                // thêm vào recentSongs
                Song newSong = Utils.GetPropertiesFromFilePath(filePath);
                TitleCurSong.Text = newSong.Title;
                ArtistCurSong.Text = newSong.Artists;
                _recentSongsService.AddNewFile(newSong);
                //cập nhật recentsongs
                FillSongQueue();
            }
            else
            {
                MessageBox.Show("File format doesnot support!", "Open Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}