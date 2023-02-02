using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReadAndWriteFileAsyncBuMultyThreads
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenFileDialog _openFileDialog;
        private SaveFileDialog _saveFileDialog;
        private Mutex _mutexRead;
        private Mutex _mutexWrite;

        public MainWindow()
        {
            InitializeComponent();
            _openFileDialog = new OpenFileDialog();
            _saveFileDialog = new SaveFileDialog();
            _mutexRead = new Mutex();
            _mutexWrite = new Mutex();
        }

        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (button == _sourceButton)
            {
                if (_openFileDialog.ShowDialog() == true)
                {
                    _sourceFileTxtBx.Text = _openFileDialog.FileName;
                }
            }
            else
            {
                if (_saveFileDialog.ShowDialog() == true)
                {
                    _destinationFileTxtBx.Text = _saveFileDialog.FileName;
                    using (var saveFileStream = File.Create(_saveFileDialog.FileName))
                    using (var openFileStream = _openFileDialog.OpenFile())
                    {
                        var length = openFileStream.Length;
                        while (length > 0)
                        {
                            int curLength = length >= 256 ? 256 : (int)length;
                            length -= curLength;
                            saveFileStream.Write(new byte[curLength], 0, curLength);
                        }
                    }
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
            SetButtonsEnabled(false);

            await CopyFileByMultyThreads(
                _sourceFileTxtBx.Text,
                _destinationFileTxtBx.Text,
                (int)_threadsNumberCmbBx.SelectedItem);

            SetButtonsEnabled(true);
        }

        private void SetButtonsEnabled(bool isEnabled)
        {
            _sourceButton.IsEnabled = isEnabled;
            _destinationButton.IsEnabled = isEnabled;
            _startButton.IsEnabled = isEnabled;
        }

        private Task CopyFileByMultyThreads(
            string sourceFilePath, 
            string destinationFilePath,
            int threadsNumber)
        {
            return Task.Run(() =>
            {
                var fileLength = File.OpenRead(sourceFilePath).Length;
                var startIndexLengthPairs = SplitFileOnFragments(fileLength, threadsNumber);
                for (int i = 0; i < startIndexLengthPairs.Count; i++)
                {
                    var pair = startIndexLengthPairs[i];
                    new Thread(() => CopyFile(
                        sourceFilePath,
                        destinationFilePath,
                        pair.StartIndex, pair.Length)).Start();
                }
            });
        }

        private List<(long StartIndex, int Length)> SplitFileOnFragments(long fileLength, int threadsNumber)
        {
            var result = new List<(long StartIndex, int Length)>(threadsNumber);

            long startIndex = 0;
            while(int.MaxValue / 3 < (fileLength / threadsNumber))
            {
                threadsNumber++;
            }
            int averageFragmentLength = (int)(fileLength / threadsNumber);

            for (int i = 0; i < threadsNumber; i++)
            {
                int currentLength;
                if (i == threadsNumber - 1)
                {
                    currentLength = (int)fileLength;
                }
                else
                {
                    currentLength = averageFragmentLength;
                }

                result.Add((startIndex, currentLength));
                startIndex += currentLength;
                fileLength -= currentLength;
            }

            return result;
        }

        private void CopyFile(string sourceFilePath, string destinationFilePath, long startIndex, int length)
        {
            _mutexRead.WaitOne();
            byte[] bytes = ReadBytesFromFile(sourceFilePath, startIndex, length);
            _mutexRead.ReleaseMutex();

            _mutexWrite.WaitOne();
            WriteBytesToFile(destinationFilePath, startIndex, bytes);
            _mutexWrite.ReleaseMutex();
        }

        private byte[] ReadBytesFromFile(string sourceFilePath, long startIndex, int length)
        {
            var result = new byte[length];
            using (var fileStream = File.OpenRead(sourceFilePath))
            {
                fileStream.Position = startIndex;
                fileStream.Read(result, 0, length);
            }
            return result;
        }

        private void WriteBytesToFile(string destinationFilePath, long startIndex, byte[] bytes)
        {
            using (var fileStream = File.OpenWrite(destinationFilePath))
            {
                fileStream.Position = startIndex;
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
