using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Task.Factory.StartNew(DownLoad);
        }

        private void DownLoad()
        {
            try
            {
                if (File.Exists("Update.zip"))
                    File.Delete("Update.zip");
                if (Directory.Exists("Update"))
                    Directory.Delete("Update",true);
                if (Directory.Exists("Backup"))
                    Directory.Delete("Backup", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления. " + ex.Message);
                Closing();
                return;
            }

            try
            {
                WebClient cl = new WebClient();
                cl.Encoding = Encoding.UTF8;
                cl.DownloadFile("http://f-king.ru/settings/Update/Update.zip", "Update.zip");
            }

            catch(Exception ex)
            {
                MessageBox.Show("Ошибка загрузки. Проверьте свое интернет соединение!");
                Closing();
                return;
            }

            this.Dispatcher.Invoke(delegate () { lblStatus.Content = "Распаковка файлов"; });

            try
            {
                
                ZipFile.ExtractToDirectory("Update.zip", "Update",Encoding.GetEncoding(1251));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки. "+ex.Message);
                Closing();
                return;
            }

            this.Dispatcher.Invoke(delegate () { lblStatus.Content = "Копирование файлов"; });

            try
            {               
                List<string> nonCopy = new List<string>();

                nonCopy.Add("Updater.exe");
                nonCopy.Add("Update.zip");
                nonCopy.Add("Update");
                nonCopy.Add("Backup");
                nonCopy.Add("Settings.dat");
                nonCopy.Add("Screenshots");


                DirectoryCopy(".", @"Backup", true, nonCopy);

                DdirectoryClear(".", nonCopy);

                DirectoryCopy("Update", ".", true, nonCopy);

                Directory.Delete("Update",true);
                Process.Start("Fork_King.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки. " + ex.Message);
                Closing();
            }


            Closing();
        }


        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs,List<string> NonCopyList=null)
        {

            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }


            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if(NonCopyList!=null)
                {
                    if (NonCopyList.Contains(file.Name))
                        continue;
                }
                string temppath = System.IO.Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }


            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    if (NonCopyList != null)
                    {
                        if (NonCopyList.Contains(subdir.Name))
                            continue;
                    }

                    string temppath = System.IO.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void DdirectoryClear(string sourceDirName, List<string> NonCopyList)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (NonCopyList != null)
                {
                    if (NonCopyList.Contains(file.Name))
                        continue;
                }
                
                file.Delete();
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                if (NonCopyList != null)
                {
                    if (NonCopyList.Contains(subdir.Name))
                        continue;
                }
                subdir.Delete(true);
            }

        }

        private void Closing()

        {
            this.Dispatcher.Invoke(delegate () { this.Close(); });
        }

    }
}
