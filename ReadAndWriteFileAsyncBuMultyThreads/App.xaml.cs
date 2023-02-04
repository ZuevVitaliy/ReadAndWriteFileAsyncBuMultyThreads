using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ReadAndWriteFileAsyncBuMultyThreads
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        private static void CreateEmptyFile(double length)
        {
            using (var fs = File.Create("D:\\test.dat"))
            {
                length = 0.5 * 1024l * 1024l * 1024l;
                byte[] bytes = new byte[1];

                for (long i = 0; i < length; i++)
                {
                    fs.Write(bytes, 0, 1);
                }
            }
        }
    }
}
