using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace ReadAndWriteFileAsyncBuMultyThreads
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenFileDialog _openFileDialog;
        private SaveFileDialog _saveFileDialog;

        public MainWindow()
        {
            InitializeComponent();
            _openFileDialog = new OpenFileDialog();
            _saveFileDialog = new SaveFileDialog();
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (button == _sourceButton)
            {
                if (_openFileDialog.ShowDialog() == true)
                {
                    _sourceFileTxtBx.Text = _openFileDialog.FileName;
                    _progressBar.Maximum = _openFileDialog.OpenFile().Length;
                }
            }
            else
            {
                if (_saveFileDialog.ShowDialog() == true)
                {
                    _destinationFileTxtBx.Text = _saveFileDialog.FileName;
                }
            }

            if (!string.IsNullOrWhiteSpace(_sourceFileTxtBx.Text))
            {
                _destinationButton.IsEnabled = true;

                if (!string.IsNullOrWhiteSpace(_destinationFileTxtBx.Text))
                    _startButton.IsEnabled = true;
                else
                    _startButton.IsEnabled = false;
            }
        }

        private async void StartButtonClick(object sender, RoutedEventArgs e)
        {
            _progressBar.Value = 0;
            SetButtonsEnabled(false);

            await new FileCopier().CopyFileByMultyThreads(
                _sourceFileTxtBx.Text,
                _destinationFileTxtBx.Text,
                (int)_threadsNumberCmbBx.SelectedItem,
                UpdateProgressBar);

            SetButtonsEnabled(true);
        }

        private void SetButtonsEnabled(bool isEnabled)
        {
            _sourceButton.IsEnabled = isEnabled;
            _destinationButton.IsEnabled = isEnabled;
            _startButton.IsEnabled = isEnabled;
        }

        private void UpdateProgressBar(int addingValue)
        {
            Dispatcher.Invoke(() => _progressBar.Value += addingValue);
        }
    }
}
