using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;

namespace Qlstats
{
    class Program
    {
        static Dictionary<string, int> maps = new Dictionary<string, int>();
        static object sync = new object();
        static void Main(string[] args)
        {
            ThreadPool.SetMaxThreads(3, 3);
            ThreadPool.SetMinThreads(3, 3);
            File.Delete(@"C:\temp\log.txt");
            //Parallel.For(1, 153, page => CountMaps(page).Wait());

            var tasks = new List<Task>();
            var r = new Random();
            for (var i = 1; i <= 152; i++)
            {
                //tasks.Add(CountMaps(i));
                CountMaps(i).Wait();
                Thread.Sleep(r.Next(1, 5) * 1000);
            }
            Task.WaitAll(tasks.ToArray());

            //CountMaps(1).GetAwaiter().GetResult();

            using (var stream = File.AppendText(@"C:\temp\result.txt"))
            {
                foreach (var map in maps.Keys)
                {
                    stream.WriteLine($"{map}: {maps[map]}");
                    Console.WriteLine($"{map}: {maps[map]}");
                }
            }
            Console.ReadLine();
;       }

        static async Task CountMaps(int page_)
        {
            //for (var i = 1; i <= 152; i++)
            {
                var page = await GetRequest(page_);
                var result = GetMap(page);
                foreach (var map in result)
                {
                    lock (sync)
                    {
                        if (maps.ContainsKey(map))
                        {
                            maps[map]++;
                        }
                        else
                        {
                            maps[map] = 1;
                        }
                    }
                }
            }
        }

        static async Task<string> GetRequest(int page)
        {
            int count = 0;
            while (count < 5)
            {
                try
                {
                    Console.WriteLine($"page = {page}");
                    string url = "http://qlstats.net/player/155518/games?page={0}";
                    var http = new HttpClient();
                    return await http.GetStringAsync(String.Format(url, page));
                }
                catch (HttpRequestException)
                {
                    Thread.Sleep(3000);
                    count++;
                }
            }
            using (var stream = File.AppendText(@"C:\temp\log.txt"))
            {
                stream.WriteLine(page);
            }
            return string.Empty;
        }

        static List<string> GetMap(string page)
        {
            var maps = new List<string>();
            var dom = CQ.Create(page);
            dom.Find(".table-condensed tr").Each((i, o) =>
            {
                maps.Add(CQ.Create(CQ.Create(o).Find("td")[4]).Text());
            });
            Console.WriteLine("maps.count = " + maps.Count);
            return maps;
        }
    }
}
