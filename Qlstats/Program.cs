using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Qlstats
{
    class Program
    {
        static Dictionary<string, int> maps = new Dictionary<string, int>();
        static Dictionary<string, int> types = new Dictionary<string, int>();
        static List<int> errorPages = new List<int>();
        static string filename;
        static int pages;
        static int id;


        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(@"C:\temp");
            filename = "pert2.txt";
            id = 111200;
            pages = 384;
            
            Work();
            Console.ReadLine();
;       }

        static void Work()
        {
            ThreadPool.SetMaxThreads(3, 3);
            ThreadPool.SetMinThreads(3, 3);
            
            var filenameTypes = Path.GetFileNameWithoutExtension(filename);
            filenameTypes += "_types.txt";
            File.Delete(filename);
            File.Delete(filenameTypes);
            File.Delete("log.txt");
            //Parallel.For(1, 153, page => CountMaps(page).Wait());

            var tasks = new List<Task>();
            var r = new Random();
            for (var i = 1; i <= pages; i++)
            {
                //tasks.Add(CountMaps(i));
                CountMaps(i).Wait();
                Thread.Sleep(r.Next(1, 5) * 1000);
            }
            Task.WaitAll(tasks.ToArray());

            foreach (var errorPage in errorPages)
            {
                CountMaps(errorPage, true).Wait();
                Thread.Sleep(r.Next(1, 5) * 1000);
            }


            //CountMaps(1).GetAwaiter().GetResult();

            using (var stream = File.AppendText(filename))
            {
                foreach (var map in maps.Keys)
                {                    
                    stream.WriteLine($"{map}: {maps[map]}");
                    Console.WriteLine($"{map}: {maps[map]}");
                }
            }

            using (var stream = File.AppendText(filenameTypes))
            {
                foreach (var type in types.Keys)
                {
                    stream.WriteLine($"{type}: {types[type]}");
                    Console.WriteLine($"{type}: {types[type]}");
                }
            }
        }

        static void Work2()
        {
            using (var stream = File.OpenText(@"C:\temp\gibi.txt"))
            {
                while (true)
                {
                    var line = stream.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    var mapCount = line.Split(':');
                    var map = mapCount[0].Trim();
                    var count = int.Parse(mapCount[1].Trim());
                    maps[map] = count;

                    Console.WriteLine($"{map} {count}");
                }
            }

            using (var stream = File.OpenText(@"C:\temp\log.txt"))
            {
                while (true)
                {
                    var line = stream.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    var page = int.Parse(line);
                    CountMaps(page).Wait();
                    var r = new Random();
                    Thread.Sleep(r.Next(1, 5) * 1000);
                }
            }

            using (var stream = File.AppendText(@"C:\temp\gibi2.txt"))
            {
                foreach (var map in maps.Keys)
                {
                    stream.WriteLine($"{map}: {maps[map]}");
                    Console.WriteLine($"{map}: {maps[map]}");
                }
            }
        }

        static async Task CountMaps(int page_, bool log = false)
        {
            //for (var i = 1; i <= 152; i++)
            {
                var page = await GetRequest(page_, log);
                var result = GetMaps(page);
                foreach (var map in result.Item1)
                {
                    if (!string.IsNullOrEmpty(map))
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

                foreach (var type in result.Item2)
                {
                    if (!string.IsNullOrEmpty(type))
                    {
                        if (types.ContainsKey(type))
                        {
                            types[type]++;
                        }
                        else
                        {
                            types[type] = 1;
                        }
                    }
                }
            }
        }

        static async Task<string> GetRequest(int page, bool log = false)
        {
            int count = 0;
            while (count < 5)
            {
                try
                {
                    Console.WriteLine($"page = {page}");
                    string url = "http://qlstats.net/player/{0}/games?page={1}";
                    var http = new HttpClient();
                    return await http.GetStringAsync(string.Format(url, id, page));
                }
                catch (HttpRequestException)
                {
                    Thread.Sleep(3000);
                    count++;
                }
            }
            
            if (log)
            {
                using (var stream = File.AppendText(@"C:\temp\log.txt"))
                {
                    stream.WriteLine(page);
                }
            }
            else
            {
                errorPages.Add(page);
            }
            return string.Empty;
        }

        static (List<string>, List<string>) GetMaps(string page)
        {
            var maps = new List<string>();
            var types = new List<string>();
            var dom = CQ.Create(page);
            dom.Find(".table-condensed tr").Each((i, o) =>
            {
                maps.Add(CQ.Create(CQ.Create(o).Find("td")[4]).Text().Trim());
                types.Add(CQ.Create(CQ.Create(o).Find("td")[2]).Text().Trim());
            });
            Console.WriteLine("maps.count = " + maps.Count);
            return (maps, types);
        }
    }
}
