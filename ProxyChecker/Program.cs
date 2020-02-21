using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using xNet;

namespace ProxyChecker
{
    public static class Program
    {
        public static void Main()
        {
            if (!File.Exists("mirrors.txt"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Нет файла mirrors.txt, с зеркалами");
                Console.ReadKey();
                return;
            }
            try
            {
                List<string> sitesList = File.ReadAllLines("mirrors.txt").ToList();
                List<string> proxyList = File.ReadAllLines($"{Environment.CurrentDirectory}\\proxy.txt").ToList();
                List<string> rezultList = new List<string>();

                HttpRequest req = new HttpRequest
                {
                    CharacterSet = Encoding.UTF8,
                    ConnectTimeout = 1500,
                    ReadWriteTimeout = 1500
                };

                foreach (string proxy in proxyList)
                {
                    Console.WriteLine("---------------");
                    try
                    {

                        req.Proxy = ProxyClient.Parse(proxy);
                        req.Proxy.ConnectTimeout = 3000;
                        req.Proxy.ReadWriteTimeout = 3000;
                        foreach (string site in sitesList)
                        {
                            Stopwatch swStopwatch = new Stopwatch();
                            swStopwatch.Start();
                            HttpResponse res;
                            try
                            {
                                res = req.Get(site);
                                var t = res.ToString();
                                if(t.Contains("has been blacklisted due to a high volume of requests"))
                                    throw new ArgumentException("marafon забанен");
                            }

                            catch (Exception e)

                            {
                                var t = req.Response.ToString();
                                throw new ArgumentException("marafon забанен");
                            }
                            swStopwatch.Stop();

                            //if (site.Contains("fonbet") && res.ToString().StartsWith("<"))
                            //    throw new ArgumentException("not fonbet");
                            if (swStopwatch.Elapsed.TotalMilliseconds > 3000 ||
                                res.StatusCode == HttpStatusCode.Forbidden)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine($"{site} : {swStopwatch.Elapsed.TotalMilliseconds} мс.");
                                Console.ResetColor();
                                throw new ArgumentException();
                            }
                            Console.WriteLine($"{site} : {swStopwatch.Elapsed.TotalMilliseconds} мс.");
                        }

                        rezultList.Add(proxy);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine($"Good proxy: {proxy} ; Total.Count: {rezultList.Count} ");
                        Console.ResetColor();
                    }

                    catch (HttpException ex)
                    {
                       
                    }
                    catch (Exception ex)
                    {

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"Bad proxy: {proxy} ;{ex.Message}");
                        Console.ResetColor();
                    }
                    //Console.WriteLine("---------------");
                }

                Console.WriteLine("Stoped");
                Console.ReadKey();

                File.WriteAllLines($"{Environment.CurrentDirectory}\\good.txt", rezultList);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }

        }
    }
}
