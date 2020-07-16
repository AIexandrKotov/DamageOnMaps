using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace DamageOnMaps
{
    public class ReplayInfo
    {
        public string Tank { get; set; }
        public DateTime Time { get; set; }
        public string Map { get; set; }
        public int TankLevel { get; set; }
        public int Damage { get; set; }

        public ReplayInfo(string path)
        {
            Time = File.GetLastAccessTime(path);
            ParseFileName(path);
        }

        private const string ttx = "\"damageDealt\": ";
        private static readonly char[] chararray = "_".ToCharArray();
        private static readonly char[] chararray2 = "_".ToCharArray();
        private static Encoding ANSI = Encoding.GetEncoding(1252);
        public void ParseFileName(string path)
        {
            var filename = Path.GetFileNameWithoutExtension(path);
            var tw = filename.Split(chararray, StringSplitOptions.RemoveEmptyEntries);
            var mapindx = Array.FindLastIndex(tw, x => x.All(y => char.IsDigit(y)));
            Tank = new ArraySegment<string>(tw, 2, mapindx - 2).JoinIntoString("_");
            Map = TryReplaceMap(new ArraySegment<string>(tw, mapindx, tw.Length - mapindx).JoinIntoString("_").Replace('-', ' ').Replace('_', '-'));
            var json = string.Empty;
            using (var stream = File.OpenRead(path))
            {
                using (var br = new BinaryReader(stream))
                {
                    br.BaseStream.Position = 4;
                    var jsonscount = br.ReadInt32();
                    var jsonbuilder = new StringBuilder();
                    for (var i = 0; i < jsonscount; i++)
                    {
                        jsonbuilder.Append(ANSI.GetString(br.ReadBytes(br.ReadInt32())));
                    }
                    json = jsonbuilder.ToString();
                }
            }
            var damageindx = json.IndexOf(ttx);
            if (damageindx == -1) Damage = -1;
            else
            {
                var i2 = json.Substring(damageindx, 12 + ttx.Length);
                Damage = int.Parse(i2.Substring(ttx.Length, i2.IndexOf(',') - ttx.Length));
            };
        }

        public static Dictionary<string, string> MapLocal = new Dictionary<string, string>();
        public static string TryReplaceMap(string that)
        {
            if (MapLocal.ContainsKey(that)) return MapLocal[that]; else return that;
        }

        public bool IsLegal()
        {
            if (Map.EndsWith("se20")) return false;
            if (Map.Contains("-epic-")) return false;
            if (Damage == -1) return false;
            if (Tank.EndsWith("_bob")) return false;
            if (Tank.EndsWith("Roket_Sturmtiger")) return false;
            return true;
        }

        static ReplayInfo()
        {
            MapLocal.Add("44-north-america", "Лайв Окс");
            MapLocal.Add("45-north-america", "Хайвей");
            MapLocal.Add("06-ensk", "Энск");
            MapLocal.Add("35-steppes", "Степи");
            MapLocal.Add("31-airfield", "Аэродром");
            MapLocal.Add("04-himmelsdorf", "Химмельсдорф");
            MapLocal.Add("08-ruinberg", "Руинберг");
            MapLocal.Add("63-tundra", "Тундра");
            MapLocal.Add("11-murovanka", "Мурованка");
            MapLocal.Add("18-cliff", "Утёс");
            MapLocal.Add("34-redshire", "Редшир");
            MapLocal.Add("14-siegfried-line", "Линия Зигфрида");
            MapLocal.Add("10-hills", "Рудники");
            MapLocal.Add("19-monastery", "Монастырь");
            MapLocal.Add("23-westfeld", "Вестфилд");
            MapLocal.Add("47-canada-a", "Тихий берег");
            MapLocal.Add("02-malinovka", "Малиновка");
            MapLocal.Add("36-fishing-bay", "Рыбацкая бухта");
            MapLocal.Add("114-czech", "Промзона");
            MapLocal.Add("83-kharkiv", "Харьков");
            MapLocal.Add("07-lakeville", "Лассвилль");
            MapLocal.Add("37-caucasus", "Перевал");
            MapLocal.Add("115-sweden", "Штиль");
            MapLocal.Add("01-karelia", "Карелия");
            MapLocal.Add("13-erlenberg", "Эрленберг");
            MapLocal.Add("29-el-hallouf", "Эль-Халлуф");
            MapLocal.Add("95-lost-city-ctf", "Затеряный город");
            MapLocal.Add("33-fjord", "Фьорды");
            MapLocal.Add("59-asia-great-wall", "Граница империи");
            MapLocal.Add("05-prohorovka", "Прохоровка");
            MapLocal.Add("28-desert", "Песчаная река");
            MapLocal.Add("90-minsk", "Минск");
            MapLocal.Add("38-mannerheim-line", "Линия маннергейма");
            MapLocal.Add("99-poland", "Студзянки");
            MapLocal.Add("112-eiffel-tower-ctf", "Париж");
            MapLocal.Add("17-munchen", "Уайдпарк");
            MapLocal.Add("101-dday", "Оверлорд");
            MapLocal.Add("03-campania-big", "Провинция");
            MapLocal.Add("105-germany", "Берлин");
        }
    }

    public class ReplayList
    {
        public List<ReplayInfo> Replays = new List<ReplayInfo>();

        public ReplayList()
        {
#if DEBUG
            CollectReplays("D:\\Games\\World_of_Tanks_RU\\replays");
#else
            CollectReplays(Environment.CurrentDirectory);
#endif
        }

        public ReplayList(string s)
        {
            CollectReplays(s);
        }

        public void CollectReplays(string dir)
        {
            var files = Directory.GetFiles(dir, "*.wotreplay");
            var errors = 0;
            for (var i = 0; i < files.Length; i++)
            {
                Console.Title = $"{i - errors, 6} / {files.Length - errors, -6} ({(double)i/files.Length:p})";
                try
                {
                    var ri = new ReplayInfo(files[i]);
                    if (ri.IsLegal())
                    {
                        Replays.Add(ri);
                    }
                    else errors += 1;
                }
                catch (Exception e)
                {
                    errors += 1;
                }
            }
        }
    }

    public static class Program
    {
        public static string JoinIntoString<T>(this IEnumerable<T> e, string delim)
        {
            var g = e.GetEnumerator();
            var sb = new StringBuilder("");
            if (g.MoveNext())
            {
                sb.Append(g.Current.ToString());
                while (g.MoveNext()) sb.Append(delim + g.Current.ToString());
            }
            return sb.ToString();
        }

        public static string JoinIntoString<T>(this IEnumerable<T> e)
        {
            if (typeof(T) == typeof(char)) return e.JoinIntoString("");
            else return e.JoinIntoString(" ");
        }

        public static int GetAverageDamage(this IGrouping<string, ReplayInfo> replays)
        {
            var battles = 0;
            long overalldamage = 0;
            foreach (var x in replays)
            {
                battles += 1;
                overalldamage += x.Damage;
            }
            return (int)Math.Round((double)overalldamage / battles);
        }

        public static string GetTankName(this string str)
        {
            var iof = str.IndexOf("_");
            return str.Substring(iof + 1).Replace("_", " ");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Эта программа создаёт отчёт по сыгранным реплеям");
            Environment.CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName);
            var list = new ReplayList();
            Console.Title = "Средний дамаг на картах by Alexandr Kotov";
            if (list.Replays.Count > 0)
            {
                var groups = list.Replays.GroupBy(x => x.Map).Select(x => (x.Key, x.GroupBy(y => y.Tank).Select(y => (y.Key, y.GetAverageDamage())).ToList()));
                var sb = new StringBuilder();
                sb.AppendLine("===== Отчёт по реплеям =====");
                sb.AppendLine("============================");
                sb.AppendLine();

                var lst0 = groups.ToList();
                lst0.Sort((x, y) => (int)y.Item2.Average(z => z.Item2) - (int)x.Item2.Average(z => z.Item2));
                foreach (var map in lst0)
                {
                    sb.AppendLine($"    Карта {map.Key}:");
                    var lst = map.Item2;
                    lst.Sort((x, y) => y.Item2 - x.Item2);
                    foreach (var tank in lst)
                    {
                        sb.AppendLine($"        Танк {tank.Key.GetTankName()}:");
                        sb.AppendLine($"            Средний урон: {tank.Item2}");
                        sb.AppendLine();
                    }
                    sb.AppendLine("--------------------------------------------");
                }
                File.WriteAllText("replayinfo.txt", sb.ToString(), Encoding.UTF8);
            }
            else
            {
                Console.WriteLine("Поместите программу в папку с реплеями!");
                Console.ReadKey(true);
            }
        }
    }
}
