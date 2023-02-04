using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadAndWriteFileAsyncBuMultyThreads
{
    public class FileCopier
    {
        private readonly Mutex _mutexRead = new Mutex();
        private readonly Mutex _mutexWrite = new Mutex();

        public Task CopyFileByMultyThreads(
            string sourceFilePath,
            string destinationFilePath,
            int threadsNumber,
            Action<int> updateProgressCallback = null)

        {
            return Task.Run(() =>
            {
                CreateEmptyFile(sourceFilePath, destinationFilePath);

                var fileLength = File.OpenRead(sourceFilePath).Length;
                var startIndexLengthPairs = SplitOnToFragments(fileLength, threadsNumber);
                Parallel.For(0, startIndexLengthPairs.Count, i =>
                {
                    var pair = startIndexLengthPairs[i];
                    CopyFile(
                        sourceFilePath,
                        destinationFilePath,
                        pair.StartIndex, pair.Length,
                        updateProgressCallback);
                });
            });
        }

        private List<(long StartIndex, int Length)> SplitOnToFragments(
            long lenght, int fragmentsCount)
        {
            var result = new List<(long StartIndex, int Length)>(fragmentsCount);

            long startIndex = 0;
            while (int.MaxValue / 3 < (lenght / fragmentsCount))
            {
                fragmentsCount++;
            }
            int averageFragmentLength = (int)(lenght / fragmentsCount);

            for (int i = 0; i < fragmentsCount; i++)
            {
                int currentLength;
                if (i == fragmentsCount - 1)
                {
                    currentLength = (int)lenght;
                }
                else
                {
                    currentLength = averageFragmentLength;
                }

                result.Add((startIndex, currentLength));
                startIndex += currentLength;
                lenght -= currentLength;
            }

            return result;
        }

        private void CopyFile(string sourceFilePath, string destinationFilePath, long startIndex, int length, Action<int> updateProgressCallback)
        {
            _mutexRead.WaitOne();
            byte[] bytes = ReadBytesFromFile(sourceFilePath, startIndex, length);
            _mutexRead.ReleaseMutex();

            _mutexWrite.WaitOne();
            WriteBytesToFile(destinationFilePath, startIndex, bytes, updateProgressCallback);
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

        private void WriteBytesToFile(
            string destinationFilePath, 
            long startIndex,
            byte[] bytes,
            Action<int> updateProgressCallback)
        {
            using (var fileStream = File.OpenWrite(destinationFilePath))
            {
                var fragmentLength = 256 * 1024;
                int fragmentsCount = bytes.Length / fragmentLength;
                fragmentsCount = fragmentsCount < 1 ? 1 : fragmentsCount;
                var fragments = SplitOnToFragments(bytes.Length, fragmentsCount);

                fileStream.Position = startIndex;
                foreach (var fragment in fragments)
                {
                    fileStream.Write(bytes, (int)fragment.StartIndex, fragment.Length);
                    updateProgressCallback?.Invoke(fragmentLength);
                }
            }
        }

        private void CreateEmptyFile(string sourceFilePath, string destinationFilePath)
        {
            using (var openFileStream = File.Open(sourceFilePath, FileMode.Open))
            using (var saveFileStream = File.Create(destinationFilePath))
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
}
