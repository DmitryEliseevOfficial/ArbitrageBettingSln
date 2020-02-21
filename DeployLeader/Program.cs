using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;

namespace DeployLeader
{
    internal static class Program
    {
        private static string _clientPatch = @"";
        private static Version _lastVersion;

        private static void Main()
        {

            Deploy();
            Console.ReadKey();
        }

        private static void Deploy()
        {

            _clientPatch = new DirectoryInfo(Environment.CurrentDirectory).Parent?.FullName + @"\ABClient\bin\Release";


            if (!CheckLastVersion()) return;

            if (!DeleteAndRename()) return;
            if (!CompressFiles()) return;
            if (!UploadFile("Release")) return;
            if (!UploadFile("Update")) return;
            SaveVersion();
            WriteLine("Проект успешно развернут", ConsoleColor.DarkGreen);
        }

        private static bool CheckLastVersion()
        {

            string asseblyPath = _clientPatch + @"\ABShared.dll";
            if (!File.Exists(asseblyPath))
            {
                WriteLine("Не удалось найти ABShared", ConsoleColor.Red);
                return false;
            }

            try
            {
                Assembly abShared = Assembly.LoadFrom(asseblyPath);
                Type projectVersionType = abShared.GetType("ABShared.ProjectVersion");


                object pVObj = Activator.CreateInstance(projectVersionType);
                PropertyInfo filds = projectVersionType.GetProperty("Version");
                Version currentVersion = Version.Parse(filds.GetValue(pVObj).ToString());

                if (!File.Exists("lastVersion.txt"))
                {
                    _lastVersion = currentVersion;
                }
                else
                {
                    _lastVersion = Version.Parse(File.ReadAllText("lastVersion.txt"));
                    if (currentVersion <= _lastVersion)
                    {
                        WriteLine($"Последняя загруженная версия: {_lastVersion} \nТекущая версия: {currentVersion} \nНеобходимо обновить версию билда!", ConsoleColor.Red);
                        return false;
                    }
                }

                WriteLine($"Последняя загруженная версия: {_lastVersion} \nТекущая версия: {currentVersion} \nВерсия успешно проверенна", ConsoleColor.DarkGreen);
                return true;
            }
            catch (Exception ex)
            {
                WriteLine("Не удалось загузить ABShared", ConsoleColor.Red);
                WriteLine(ex.Message, ConsoleColor.DarkRed);
                return false;
            }


        }

        private static void SaveVersion()
        {
            File.WriteAllText("lastVersion.txt", _lastVersion.ToString());
            WriteLine("Информация о последнем релизе сохранена.", ConsoleColor.DarkGreen);

        }

        private static bool DeleteAndRename()
        {

            if (!Directory.Exists(_clientPatch))
            {
                WriteLine("Ошибка. Папки с релизом не существует", ConsoleColor.Red);
                return false;
            }
            if (!File.Exists($@"{_clientPatch}\ABClient.exe"))
            {
                WriteLine("Ошибка. Релизного файла не существует", ConsoleColor.Red);
                return false;
            }

            if (File.Exists($@"{_clientPatch}\Fork_King.exe"))
            {
                try
                {
                    File.Delete($@"{_clientPatch}\Fork_King.exe");
                    WriteLine("Файл с ForkKing был удален", ConsoleColor.DarkGreen);

                }
                catch (Exception e)
                {
                    WriteLine("Не удалось удалить файл Fork_King.exe!", ConsoleColor.Red);
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            try
            {
                File.Move($@"{_clientPatch}\ABClient.exe", $@"{_clientPatch}\Fork_King.exe");
                WriteLine("Файл успешно переименован в ForkKing.exe", ConsoleColor.DarkGreen);
            }
            catch (Exception e)
            {
                WriteLine("Ошибка переименования:", ConsoleColor.Red);
                Console.WriteLine(e.Message);
                return false;
            }
            if (Directory.Exists($@"{_clientPatch}\Sdk\Cef\cache"))
            {
                try
                {
                    Directory.Delete($@"{_clientPatch}\Sdk\Cef\cache", true);
                    WriteLine("Куки очещенны", ConsoleColor.DarkGreen);
                }
                catch (Exception e)
                {
                    WriteLine("Не удалось очистить папку с куками!", ConsoleColor.Red);
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            if (Directory.Exists($@"{_clientPatch}\Screenshots"))
            {
                try
                {
                    Directory.Delete($@"{_clientPatch}\Screenshots", true);
                    WriteLine("Скриншоты удалены", ConsoleColor.DarkGreen);
                }
                catch (Exception e)
                {
                    WriteLine("Не удалось очистить папку с скриншотами!", ConsoleColor.Red);
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            List<string> files = new List<string>()
            {
                @"\Sdk\Cef\Log.txt",
                @"\Settings.dat",
                @"\ABClient.exe.CodeAnalysisLog.xml",
                @"\ABClient.exe.config",
                @"\ABClient.pdb",
                @"\ABShared.pdb",
                @"\CefSharp.Core.xml",
                @"\CefSharp.Wpf.xml",
                @"\CefSharp.xml",
                @"\Updater.exe.config",
                @"\Updater.pdb"
            };

            foreach (string file in files)
            {
                if (File.Exists($@"{_clientPatch}{file}"))
                {
                    try
                    {
                        File.Delete($@"{_clientPatch}{file}");
                        WriteLine("Файл с логами был удален", ConsoleColor.DarkGreen);

                    }
                    catch (Exception e)
                    {
                        WriteLine($"Не удалось удалить файл {file}", ConsoleColor.Red);
                        Console.WriteLine(e.Message);
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool CompressFiles()
        {
            if (File.Exists($@"{_clientPatch}\..\Release.zip"))
            {
                try
                {
                    File.Delete($@"{_clientPatch}\..\Release.zip");
                    WriteLine("Файл Release.zip удален", ConsoleColor.DarkGreen);
                }
                catch (Exception e)
                {
                    WriteLine("Не удалось удалить Release.zip", ConsoleColor.Red);
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            try
            {
                ZipFile.CreateFromDirectory(_clientPatch, $@"{_clientPatch}\..\Release.zip", CompressionLevel.Optimal, false, Encoding.UTF8);
                WriteLine("Файлы были успешно заархивированны", ConsoleColor.DarkGreen);

                return true;
            }
            catch (Exception e)
            {
                WriteLine("Не удалось создать архив", ConsoleColor.Red);
                Console.WriteLine(e.Message);
                return false;
            }

        }

        private static bool UploadFile(string name)
        {
            try
            {
                byte[] data = File.ReadAllBytes($@"{_clientPatch}\..\Release.zip");
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://f-king.ru/{name}.zip");
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential("vik790a_deploy", "Xervam957");
                request.ContentLength = data.Length;
                Stream stream = request.GetRequestStream();

                stream.Write(data, 0, data.Length);

                stream.Close();

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                WriteLine($"Архив {name} был успешно залит.Код операции: {response.StatusCode}", ConsoleColor.DarkGreen);
                return true;
            }
            catch (Exception e)
            {
                WriteLine($"Не удалось залить архив {name}", ConsoleColor.Red);
                Console.WriteLine(e.Message);
                return false;
            }

        }

        private static void WriteLine(string message, ConsoleColor color)
        {
            Console.WriteLine($"{DateTime.Now:G}");
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.WriteLine("----------------------");
        }
    }
}
