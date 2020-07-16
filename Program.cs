using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace DamageOnMaps
{
    public class ReplayInfo
    {
        public string Tank { get; set; }
        public DateTime Time { get; set; }
        public string Map { get; set; }
        public int TankLevel { get; set; }
        public int Damage { get; set; }
        public string Mode { get; set; }

        public ReplayInfo(string path)
        {
            Time = File.GetLastAccessTime(path);
            ParseFileName(path);
        }

        private const string ttx = "\"damageDealt\": ";
        private const string gameplay = "\"gameplayID\": ";
        private const string battleType = "\"battleType\": ";
        private static readonly char[] chararray = "_".ToCharArray();
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
            var gameplayindx = json.IndexOf(gameplay);
            if (gameplayindx == -1) Mode = null;
            else
            {
                if (gameplayindx == -1) Mode = null;
                else
                {
                    var i2 = json.Substring(gameplayindx, 50 + gameplay.Length + battleType.Length);
                    var part1 = i2.Substring(gameplay.Length, i2.IndexOf(',') - gameplay.Length).Replace("\"", "");
                    var battleindx = i2.IndexOf(battleType);
                    var i3 = i2.Substring(battleindx, 10 + battleType.Length);
                    var part2 = i3.Substring(battleType.Length, i3.IndexOf(',') - battleType.Length).Replace("\"", "");
                    Mode = TryReplaceMode($"{part1}_{part2}");
                }
            }
        }

        public static Dictionary<string, string> MapLocal = new Dictionary<string, string>();
        public static Dictionary<string, string> ModeLocal = new Dictionary<string, string>();
        public static string TryReplaceMap(string that)
        {
            if (MapLocal.ContainsKey(that)) return MapLocal[that]; else return that;
        }

        public static string TryReplaceMode(string that)
        {
            if (ModeLocal.ContainsKey(that)) return ModeLocal[that]; else return that;
        }

        public bool IsLegal()
        {
            if (Damage == -1) return false;
            if (Mode == "ctf_9") return false;
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

            ModeLocal.Add("epic_27", "\"Линия фронта\"");
            ModeLocal.Add("domination_32", "\"Битва блогеров 2020\"");
            ModeLocal.Add("ctf_22", "Ранговый бой");
            ModeLocal.Add("ctf_2", "Тренировочный бой");

            ModeLocal.Add("ctf_1", "Случайный бой");
            ModeLocal.Add("domination_1", "Случайный бой / Встречный");
            ModeLocal.Add("assault_1", "Случайный бой / Штурм");
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
                catch
                {
                    errors += 1;
                }
            }
        }
    }

    public class ReportInformation : IEnumerable<ModeContainer>
    {
        public List<ModeContainer> ModeContainers { get; set; }
        public int BattlesCount => ModeContainers.Sum(x => x.BattlesCount);
        public ReportInformation(List<ModeContainer> mods)
        {
            ModeContainers = mods;
            ModeContainers.Sort((x, y) => string.Compare(x.ModeName, y.ModeName));
        }

        public IEnumerator<ModeContainer> GetEnumerator()
        {
            return ModeContainers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ModeContainer : IEnumerable<MapContainer>
    {
        public string ModeName { get; set; }
        public List<MapContainer> MapContainers { get; set; }
        public int BattlesCount => MapContainers.Sum(x => x.BattlesCount);
        public ModeContainer(string modename, List<MapContainer> maps)
        {
            ModeName = modename;
            MapContainers = maps;
            MapContainers.Sort((x, y) => y.AverageDamage - x.AverageDamage);
        }

        public IEnumerator<MapContainer> GetEnumerator()
        {
            return MapContainers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class MapContainer : IEnumerable<TankContainer>
    {
        public string MapName { get; set; }
        public List<TankContainer> TanksContainers { get; set; }
        public int BattlesCount => TanksContainers.Sum(x => x.BattlesCount);
        public int AverageDamage => (int)TanksContainers.Average(x => x.AverageDamage);
        public MapContainer(string mapname, List<TankContainer> tanks)
        {
            MapName = mapname;
            TanksContainers = tanks;
            TanksContainers.Sort((x, y) => y.AverageDamage - x.AverageDamage);
        }

        public IEnumerator<TankContainer> GetEnumerator()
        {
            return TanksContainers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class TankContainer
    {
        public string TankName { get; set; }
        public int BattlesCount { get; set; }

        //params:
        public int AverageDamage { get; set; }

        public TankContainer(string tankname, IEnumerable<ReplayInfo> replays)
        {
            TankName = tankname;
            AverageDamage = (int)replays.Average(x => x.Damage);
            BattlesCount = replays.Count();
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

        public static string GetTankName(this TankContainer str)
        {
            var iof = str.TankName.IndexOf("_");
            return str.TankName.Substring(iof + 1).Replace("_", " ");
        }

        static void Main(string[] args)
        {
            Console.Title = "DamageOnMaps by Alexandr Kotov";
            Console.WriteLine("Эта программа создаёт отчёт по сыгранным реплеям");
            Environment.CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName);
            var list = new ReplayList();
#if DEBUG
            //режимы игры
            var xxxx = list.Replays.GroupBy(x => x.Mode).Select(x => (x.Key, x.Count()));
            foreach (var x in xxxx)
            {
                Console.WriteLine($"   {x.Key,25}  =  {x.Item2}");
            }
            Console.ReadLine();
#endif
            if (list.Replays.Count > 0)
            {
                var reportInformation = new ReportInformation(list.Replays.GroupBy(x => x.Mode,
                    (modename, map) => new ModeContainer(modename, map.GroupBy(x => x.Map,
                    (mapname, tank) => new MapContainer(mapname, tank.GroupBy(x => x.Tank,
                    (tankname, rep) => new TankContainer(tankname, rep))
                    .ToList())).ToList())).ToList());

                var x0 = new ReportInformation(list.Replays.GroupBy(x => x.Mode).Select(mode =>
                    new ModeContainer(mode.Key, mode.GroupBy(x => x.Map).Select(map =>
                    new MapContainer(map.Key, map.GroupBy(x => x.Tank).Select(tank =>
                    new TankContainer(tank.Key, tank.ToList())).ToList())).ToList())).ToList());
                
                var sb = new StringBuilder();
                sb.AppendLine("(c)      DamageOnMaps by Alexandr Kotov 2020");
                sb.AppendLine();
                sb.AppendLine("==========   Отчёт  по  реплеям   ==========");
                sb.AppendLine("============================================");
                sb.AppendLine($"  Общее количество боёв в отчёте: {reportInformation.BattlesCount}");
                sb.AppendLine("============================================");
                sb.AppendLine();

                foreach (var mode in reportInformation)
                {
                    sb.AppendLine($"===> Режим {mode.ModeName} ({mode.BattlesCount})");
                    foreach (var map in mode)
                    {
                        sb.AppendLine($"     ---> Карта {map.MapName} ({map.BattlesCount})");
                        sb.AppendLine($"            │    {"Танк",        -50 } │  Боёв │  Урон");
                        sb.AppendLine($"          ──┼────{new string('─', 50)}─┼───────┼──────────");
                        foreach (var tank in map)
                        {
                            sb.AppendLine($"            │    {tank.GetTankName(), -50} │ {tank.BattlesCount, 5} │ {tank.AverageDamage, 5}");
                        }
                        sb.AppendLine($"     <{new string('-', 20)}");
                        sb.AppendLine();
                    }
                    sb.AppendLine($"<{new string('=', 20)}");
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
