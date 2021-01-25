using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Reflection;

namespace DamageOnMaps
{
    public class ReplayInfo
    {
        public string Tank { get; set; }
        public DateTime Time { get; set; }
        public string Map { get; set; }
        public int Damage { get; set; }
        public string OrigMode { get; set; }
        public string Mode { get; set; }
        public List<string> Vehicles { get; set; } = new List<string>();
        public int Artys { get; set; }

        public override string ToString()
        {
            return $"{Tank.GetTankName()} {OrigMode} {Damage} {Map}";
        }

        public ReplayInfo(string path)
        {
            Time = File.GetLastAccessTime(path);
            ParseFileName(path);
        }

        private const string damageDealt = "\"damageDealt\": ";
        private const string gameplayID = "\"gameplayID\": ";
        private const string battleType = "\"battleType\": ";
        private const string gameplayID2 = "\"gameplayID\":";
        private const string battleType2 = "\"battleType\":";
        private const string vehicleType = "\"vehicleType\": ";
        private const string vehicleType2 = "\"vehicleType\":";
        private static readonly char[] chararray = "_".ToCharArray();
        private static Encoding ANSI = Encoding.GetEncoding(1252);

        public string GetNextString(int buffer, string jsontext, string current, int startindex)
        {
            var damageindx = jsontext.IndexOf(current, startindex);
            if (damageindx == -1) return null;
            else
            {
                var i2 = jsontext.Substring(damageindx, buffer + current.Length);
                return i2.Substring(current.Length, i2.IndexOf(',') - current.Length);
            };
        }

        public string GetNextString(int buffer, string jsontext, string current)
        {
            var damageindx = jsontext.IndexOf(current);
            if (damageindx == -1) return null;
            else
            {
                var i2 = jsontext.Substring(damageindx, buffer + current.Length);
                return i2.Substring(current.Length, i2.IndexOf(',') - current.Length);
            };
        }

        public static int GetMaximumVehicles(string s)
        {
            if (s.Contains("30x30")) return 60;
            if (s == "ctf_9") return 20;
            return 30;
        }

        public void ParseFileName(string path)
        {
            var filename = Path.GetFileNameWithoutExtension(path);
            if (ReplayList.IsLeftDate || ReplayList.IsRightDate)
            {
                var date = File.GetLastWriteTime(path).Date;
                if (ReplayList.IsLeftDate && date < ReplayList.LeftDate) throw new NotImplementedException();
                if (ReplayList.IsRightDate && date > ReplayList.RightDate) throw new NotImplementedException();
            }
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

            {
                var damageindex = 0;
                do
                {
                    damageindex = json.IndexOf(damageDealt, damageindex + damageDealt.Length);
                    if (damageindex == -1)
                    {
                        Damage = -1;
                        break;
                    }
                    var damagestr = GetNextString(12, json, damageDealt, damageindex);
                    if (damagestr.Contains("}")) continue;
                    if (damagestr == null) Damage = -1; else Damage = int.Parse(damagestr.Replace("\"", ""));
                    break;
                }
                while (damageindex != -1) ;
            }

            {
                var gameplayidstr = GetNextString(24, json, gameplayID);
                var battletypestr = GetNextString(20, json, battleType);
                if (gameplayidstr == null || battletypestr == null)
                {
                    gameplayidstr = GetNextString(24, json, gameplayID2);
                    battletypestr = GetNextString(20, json, battleType2);
                }
                OrigMode = $"{gameplayidstr.Replace("\"", "")}_{battletypestr}";
                Mode = TryReplaceMode(OrigMode);
            }

            var owntankcount = 0;

            var indx = 0;
            var count = 0;
            var maxcount = GetMaximumVehicles(OrigMode);
            do
            {
                indx = json.IndexOf(vehicleType, indx + vehicleType.Length);
                if (indx == -1) break;
                var tankstr = GetNextString(48, json, vehicleType, indx);
                if (tankstr == null) tankstr = GetNextString(48, json, vehicleType2, indx);
                var tankname = ParseTankNameFronJSON(tankstr).Replace("\"", "");
                if (Tank == tankname)
                {
                    owntankcount++;
                    if (owntankcount > 1) Vehicles.Add(tankname);
                }
                else Vehicles.Add(tankname);
                count++;
            }
            while (indx != -1 && count < maxcount);
            Artys = Vehicles.Count(x => ArtysNames.Contains(x));
        }

        public static string ParseTankNameFronJSON(string tankname)
        {
            return tankname.Substring(tankname.IndexOf(":") + 1);
        }

        public static Dictionary<string, string> MapLocal = new Dictionary<string, string>();
        public static Dictionary<string, string> ModeLocal = new Dictionary<string, string>();
        public static List<string> ArtysNames = new List<string>();
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
            if (!ReplayList.CollectReplaysWithoutDamage && Damage == -1) return false;
            if (Mode == ModeLocal["ctf_9"] && !Tank.Contains("Sturmtiger")) return false;
            return true;
        }

        static ReplayInfo()
        {

            using (var stream = Assembly.GetEntryAssembly().GetManifestResourceStream("DamageOnMaps.arts.txt"))
            {
                using (var artys = new StreamReader(stream))
                {
                    ArtysNames.AddRange(artys.ReadToEnd().Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries));
                }
            }
            

            MapLocal.Add("01-karelia", "Карелия");
            MapLocal.Add("02-malinovka", "Малиновка");
            MapLocal.Add("03-campania-big", "Провинция");
            MapLocal.Add("04-himmelsdorf", "Химмельсдорф");
            MapLocal.Add("05-prohorovka", "Прохоровка");
            MapLocal.Add("06-ensk", "Энск");
            MapLocal.Add("07-lakeville", "Лассвилль");
            MapLocal.Add("08-ruinberg", "Руинберг");

            MapLocal.Add("10-hills", "Рудники");
            MapLocal.Add("11-murovanka", "Мурованка");

            MapLocal.Add("13-erlenberg", "Эрленберг");
            MapLocal.Add("14-siegfried-line", "Линия Зигфрида");


            MapLocal.Add("17-munchen", "Уайдпарк");
            MapLocal.Add("18-cliff", "Утёс");
            MapLocal.Add("19-monastery", "Монастырь");



            MapLocal.Add("23-westfeld", "Вестфилд");




            MapLocal.Add("28-desert", "Песчаная река");
            MapLocal.Add("29-el-hallouf", "Эль-Халлуф");

            MapLocal.Add("31-airfield", "Аэродром");

            MapLocal.Add("33-fjord", "Фьорды");
            MapLocal.Add("34-redshire", "Редшир");
            MapLocal.Add("35-steppes", "Степи");
            MapLocal.Add("36-fishing-bay", "Рыбацкая бухта");
            MapLocal.Add("37-caucasus", "Перевал");
            MapLocal.Add("38-mannerheim-line", "Линия маннергейма");



            MapLocal.Add("42-north-america", "Порт");
            MapLocal.Add("43-north-america", "Северо-запад");
            MapLocal.Add("44-north-america", "Лайв Окс");
            MapLocal.Add("45-north-america", "Хайвей");

            MapLocal.Add("47-canada-a", "Тихий берег");











            MapLocal.Add("59-asia-great-wall", "Граница империи");
            MapLocal.Add("60-asia-miao", "Жемчужная река");


            MapLocal.Add("63-tundra", "Тундра");


















            MapLocal.Add("83-kharkiv", "Харьков");
            MapLocal.Add("84-winter", "Виндсторм");     






            MapLocal.Add("90-minsk", "Минск");




            MapLocal.Add("95-lost-city-ctf", "Затеряный город");



            MapLocal.Add("99-poland", "Студзянки");

            MapLocal.Add("101-dday", "Оверлорд");



            MapLocal.Add("105-germany", "Берлин");






            MapLocal.Add("112-eiffel-tower-ctf", "Париж");

            MapLocal.Add("114-czech", "Промзона");
            MapLocal.Add("115-sweden", "Штиль");

            //MapLocal.Add("asia-korea", "Священная долина");
            //MapLocal.Add("action-stalingrad", "Винтерберг");
            //MapLocal.Add("ruinberg-winter", "Винтерберг");
            //MapLocal.Add("prohorovka-defense", "Огненная дуга");
            //MapLocal.Add("fallout-paris", "Монте-Роза");

            MapLocal.Add("208-bf-epic-normandy", "Нормандия");
            MapLocal.Add("209-wg-epic-suburbia", "Крафтверк");

            ModeLocal.Add("epic_27", "\"Линия фронта\"");
            ModeLocal.Add("domination_32", "\"Битва блогеров (Схватка)\"");
            ModeLocal.Add("ctf_22", "Ранговый бой");

            ModeLocal.Add("ctf_1", "Случайный бой");
            ModeLocal.Add("domination_1", "Случайный бой / Встречный");
            ModeLocal.Add("assault_1", "Случайный бой / Штурм");

            ModeLocal.Add("ctf_2", "Тренировочный бой");
            ModeLocal.Add("domination_2", "Тренировочный бой / Встречный");
            ModeLocal.Add("assault2_2", "Тренировочный бой / Штурм");

            ModeLocal.Add("ctf30x30_25", "Генеральное сражение");
            ModeLocal.Add("ctf_9", "10 на 10");

            ModeLocal.Add("ctf_20", "Вылазки");
        }
    }

    public class TankInfo
    {
        public enum TankType
        {
            Light,
            Medium,
            Heavy,
            Tankdestroyer,
            Artillery
        }
        public enum TankNation
        {
            Russia,
            Germany,
            America,
            France,
            GreatBritain,
            China,
            Japan,
            Czech,
            Sweden,
            Poland,
            Italy,
        }
        public string SystemName { get; set; }
        public int Tier { get; set; }
        public TankType Type { get; set; }
        public TankNation Nation { get; set; }
    }

    public class ReplayList
    {
        public static bool IsLeftDate { get; set; } = false;
        public static bool IsRightDate { get; set; } = false;
        public static DateTime LeftDate { get; set; } = DateTime.Now.Date;
        public static DateTime RightDate { get; set; } = DateTime.Now.Date;
        public static bool CollectReplaysWithoutDamage { get; set; } = false;

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
                try
                {
                    var ri = new ReplayInfo(files[i]);
                    if (ri.IsLegal())
                    {
                        Replays.Add(ri);
                        Console.WriteLine(ri);
                    }
                    else errors += 1;
                }
                catch
                {
                    errors += 1;
                }
                Console.Title = $"{i - errors,6} / {files.Length - errors,-6} ({(double)i / files.Length:p})";
            }
        }

        public List<(string, int)> GetMostPopularTanks()
        {
            var lst = Replays.SelectMany(x => x.Vehicles).GroupBy(x => x).Select(x => (x.Key, x.Count())).ToList();
            lst.Sort((x, y) => y.Item2 - x.Item2);
            return lst;
        }

        public ReportInformation GetReportOfModeMapsBattles()
        {
            return new ReportInformation(Replays);
        }

        public class ReportInformation : IEnumerable<ModeContainer>
        {
            public static SortMethod SortBy { get; set; } = SortMethod.AverageDamage;
            public enum SortMethod
            {
                AverageDamage = 1,
                BattlesCount = 2
            }

            public List<ModeContainer> ModeContainers { get; set; }
            public int BattlesCount => ModeContainers.Sum(x => x.BattlesCount);
            internal ReportInformation(List<ReplayInfo> replays)
            {
                ModeContainers = replays.GroupBy(x => x.Mode, (modename, map) => new ModeContainer(modename, map)).ToList();
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
            public ModeContainer(string modename, IEnumerable<ReplayInfo> maps)
            {
                ModeName = modename;
                MapContainers = maps.GroupBy(x => x.Map, (mapname, tank) => new MapContainer(mapname, tank)).ToList();
                if (ReportInformation.SortBy == ReportInformation.SortMethod.AverageDamage) MapContainers.Sort((x, y) => y.AverageDamage - x.AverageDamage);
                if (ReportInformation.SortBy == ReportInformation.SortMethod.BattlesCount) MapContainers.Sort((x, y) => y.BattlesCount - x.BattlesCount);

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
            public int AverageDamage
            {
                get
                {
                    var battles = 0;
                    long alldamage = 0;
                    foreach (var tankinfo in TanksContainers)
                    {
                        battles += tankinfo.BattlesCount;
                        alldamage += tankinfo.AverageDamage * tankinfo.BattlesCount;
                    }
                    return (int)(alldamage / (double)battles);
                }
            }

            public MapContainer(string mapname, IEnumerable<ReplayInfo> tanks)
            {
                MapName = mapname;
                TanksContainers = tanks.GroupBy(x => x.Tank, (tankname, replays) => new TankContainer(tankname, replays)).ToList();
                
                if (ReportInformation.SortBy == ReportInformation.SortMethod.AverageDamage) TanksContainers.Sort((x, y) => y.AverageDamage - x.AverageDamage);
                if (ReportInformation.SortBy == ReportInformation.SortMethod.BattlesCount) TanksContainers.Sort((x, y) => y.BattlesCount - x.BattlesCount);
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
            public double AverageArtys { get; set; }

            public TankContainer(string tankname, IEnumerable<ReplayInfo> replays)
            {
                TankName = tankname;
                var reps = replays.Where(x => x.Damage != -1);
                if (reps.Any()) AverageDamage = (int)reps.Average(x => x.Damage);
                else AverageDamage = -1;
                AverageArtys = replays.Average(x => x.Artys);
                BattlesCount = replays.Count();
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

        public static string GetTankName(this ReplayList.TankContainer str)
        {
            var iof = str.TankName.IndexOf("_");
            return str.TankName.Substring(iof + 1).Replace("_", " ");
        }
        public static string GetTankName(this string str)
        {
            var iof = str.IndexOf("_");
            return str.Substring(iof + 1).Replace("_", " ");
        }

        static void Help()
        {
            Console.WriteLine("Эта программа создаёт отчёт по сыгранным реплеям");
            Console.WriteLine(" │ Enter │ ─ чтобы создать отчёт");
            Console.WriteLine(" │ W │ ─ изменить, читать ли реплеи в которых игрок вышел из боя");
            Console.WriteLine(" │ S │ ─ изменить метод сортировки карт (по среднему урону/по количеству боёв)");
            Console.WriteLine(" │ T │ ─ задать время, от которого считать реплеи");
            Console.WriteLine(" │ Y │ ─ задать время, до которого считать реплеи");
            Console.WriteLine(" │ Ctrl+T │ │ Ctrl+Y │ ─ удалить границу времени");
            Console.WriteLine(" │ P │ ─ показать параметры анализа");
            Console.WriteLine(" │ H │ ─ снова показать список клавиш");
            Console.WriteLine();
        }

        static void TimeHelp()
        {
            Console.WriteLine(" │ Enter │ ─ подтвердить");
            Console.WriteLine(" │ Esc │ ─ отменить");
            Console.WriteLine(" │ A │ ─ предыдущая дата");
            Console.WriteLine(" │ D │ ─ следующая дата");
            Console.WriteLine(" │ Ctrl+A │ │ Ctrl+D │ ─ перескочить на месяц");
        }

        static void Params()
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Параметры анализа реплеев:");
            Console.WriteLine($"Считать реплеи в которых игрок вышел из боя: {ReplayList.CollectReplaysWithoutDamage}");
            Console.WriteLine($"Метод сортировки карт: {ReplayList.ReportInformation.SortBy}");
            Console.WriteLine($"Поиск по времени: {(ReplayList.IsLeftDate ? ReplayList.LeftDate.ToShortDateString() : "no")} - {(ReplayList.IsRightDate ? ReplayList.RightDate.ToShortDateString() : "no")}");
            Console.WriteLine("------------------------------------------");
        }

        static void Main(string[] args)
        {
            Console.Title = "DamageOnMaps by Alexandr Kotov";
            Help();
            Params();
            while (true)
            {
                var keyinfo = Console.ReadKey(true);
                var key = keyinfo.Key;
                if (key == ConsoleKey.Enter)
                {
                    Params();
                    Console.WriteLine("Вы уверены, что хотите проанализировать свои реплеи с этими параметрами?");
                    Console.WriteLine(" │ Enter │ ─ да");
                    Console.WriteLine(" │ Escape │ ─ нет");
                    Console.WriteLine();
                    var choose = false;
                    while (true)
                    {
                        var keyinfo2 = Console.ReadKey(true);
                        var key2 = keyinfo2.Key;
                        if (key2 == ConsoleKey.Escape)
                        {
                            choose = false;
                            break;
                        }
                        if (key2 == ConsoleKey.Enter)
                        {
                            choose = true;
                            break;
                        }
                    }
                    if (choose) break;
                    else Help();
                }
                if (key == ConsoleKey.P) Params();
                if (key == ConsoleKey.W)
                {
                    ReplayList.CollectReplaysWithoutDamage = !ReplayList.CollectReplaysWithoutDamage;
                    Console.WriteLine($"Учитывание реплеев из которых игрок вышел из боя изменено на {ReplayList.CollectReplaysWithoutDamage}");
                }
                if (key == ConsoleKey.S)
                {
                    ReplayList.ReportInformation.SortBy++;
                    if (ReplayList.ReportInformation.SortBy > ReplayList.ReportInformation.SortMethod.BattlesCount) ReplayList.ReportInformation.SortBy = ReplayList.ReportInformation.SortMethod.AverageDamage;
                    Console.WriteLine($"Сортировка изменена на {ReplayList.ReportInformation.SortBy}");
                }
                if (key == ConsoleKey.H)
                {
                    Help();
                }
                if (key == ConsoleKey.T)
                {
                    if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        ReplayList.IsLeftDate = false;
                        Console.WriteLine("Нижняя граница времени удалена");
                    }
                    else
                    {
                        ReplayList.IsLeftDate = true;
                        TimeHelp();
                        Console.WriteLine(ReplayList.LeftDate.ToShortDateString());
                        while (true)
                        {
                            var keyinfo2 = Console.ReadKey(true);
                            var key2 = keyinfo2.Key;
                            if (key2 == ConsoleKey.Escape)
                            {
                                ReplayList.IsLeftDate = false;
                                Console.WriteLine("Нижняя граница времени удалена");
                                break;
                            }
                            if (key2 == ConsoleKey.D)
                            {
                                if (keyinfo2.Modifiers.HasFlag(ConsoleModifiers.Control)) ReplayList.LeftDate = ReplayList.LeftDate.AddMonths(1);
                                else ReplayList.LeftDate = ReplayList.LeftDate.AddDays(1);
                                Console.WriteLine(ReplayList.LeftDate.ToShortDateString());
                            }
                            if (key2 == ConsoleKey.A)
                            {
                                if (keyinfo2.Modifiers.HasFlag(ConsoleModifiers.Control)) ReplayList.LeftDate = ReplayList.LeftDate.AddMonths(-1);
                                else ReplayList.LeftDate = ReplayList.LeftDate.AddDays(-1);
                                Console.WriteLine(ReplayList.LeftDate.ToShortDateString());
                            }
                            if (key2 == ConsoleKey.H)
                            {
                                TimeHelp();
                                Console.WriteLine(ReplayList.LeftDate.ToShortDateString());
                            }
                            if (key2 == ConsoleKey.Enter)
                            {
                                Console.WriteLine($"Нижняя граница времени установлена на {ReplayList.LeftDate.ToShortDateString()}");
                                Console.WriteLine();
                                Help();
                                break;
                            }
                        }
                    }
                }
                if (key == ConsoleKey.Y)
                {
                    if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        ReplayList.IsRightDate = false;
                        Console.WriteLine("Верхняя граница времени удалена");
                    }
                    else
                    {
                        ReplayList.IsRightDate = true;
                        TimeHelp();
                        Console.WriteLine(ReplayList.RightDate.ToShortDateString());
                        while (true)
                        {
                            var keyinfo2 = Console.ReadKey(true);
                            var key2 = keyinfo2.Key;
                            if (key2 == ConsoleKey.Escape)
                            {
                                ReplayList.IsRightDate = false;
                                Console.WriteLine("Верхняя граница времени удалена");
                                break;
                            }
                            if (key2 == ConsoleKey.D)
                            {
                                if (keyinfo2.Modifiers.HasFlag(ConsoleModifiers.Control)) ReplayList.RightDate = ReplayList.RightDate.AddMonths(1);
                                else ReplayList.RightDate = ReplayList.RightDate.AddDays(1);
                                Console.WriteLine(ReplayList.RightDate.ToShortDateString());
                            }
                            if (key2 == ConsoleKey.A)
                            {
                                if (keyinfo2.Modifiers.HasFlag(ConsoleModifiers.Control)) ReplayList.RightDate = ReplayList.RightDate.AddMonths(-1);
                                else ReplayList.RightDate = ReplayList.RightDate.AddDays(-1);
                                Console.WriteLine(ReplayList.RightDate.ToShortDateString());
                            }
                            if (key2 == ConsoleKey.H)
                            {
                                TimeHelp();
                                Console.WriteLine(ReplayList.RightDate.ToShortDateString());
                            }
                            if (key2 == ConsoleKey.Enter)
                            {
                                Console.WriteLine($"Верхняя граница времени установлена на {ReplayList.RightDate.ToShortDateString()}");
                                Console.WriteLine();
                                Help();
                                break;
                            }
                        }
                    }
                }
                if (key == ConsoleKey.Escape) return;
            }
            Environment.CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName);
            var AllReplays = new ReplayList();

            if (AllReplays.Replays.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("(c)      DamageOnMaps by Alexandr Kotov 2020");
                sb.AppendLine();
                sb.AppendLine("==========   Отчёт  по  реплеям   ==========");
                sb.AppendLine($"Временной промежуток: {(ReplayList.IsLeftDate ? ReplayList.LeftDate.ToShortDateString() : "no")} - {(ReplayList.IsRightDate ? ReplayList.RightDate.ToShortDateString() : "no")}");
                sb.AppendLine($"Сортировка карт {ReplayList.ReportInformation.SortBy,28}");
                if (ReplayList.CollectReplaysWithoutDamage) sb.AppendLine($"Собраны реплеи по покинутым боям");
                sb.AppendLine("============================================");
                sb.AppendLine();

                var tankInformation = AllReplays.GetMostPopularTanks();
                sb.AppendLine("==>  Самые  частые  танки  в  реплеях    <==");
                sb.AppendLine();
                sb.AppendLine($"  │  {"Танк", -50} │ {"Раз встретился", 20} │");
                sb.AppendLine($"──┼──{new string('─', 50)}─┼─{new string('─', 20)}─┼──");
                foreach (var x in tankInformation)
                {
                    sb.AppendLine($"  │  {x.Item1.GetTankName(),-50} │ {x.Item2,20} │");
                }
                sb.AppendLine();
                sb.AppendLine("============================================");
                sb.AppendLine();
                sb.AppendLine();

                var reportInformation = AllReplays.GetReportOfModeMapsBattles();
                sb.AppendLine("=============>   По режимам   <=============");
                sb.AppendLine($"  Общее количество боёв в отчёте: {reportInformation.BattlesCount}");
                sb.AppendLine("============================================");
                sb.AppendLine();
                foreach (var mode in reportInformation)
                {
                    sb.AppendLine($"===> Режим {mode.ModeName} ({mode.BattlesCount})");
                    foreach (var map in mode)
                    {
                        sb.AppendLine($"     ---> Карта {map.MapName} ({map.BattlesCount})");
                        sb.AppendLine($"            │    {"Танк",        -50 } │  Боёв │  Урон  │ Арт ");
                        sb.AppendLine($"          ──┼────{new string('─', 50)}─┼───────┼────────┼──────");
                        foreach (var tank in map)
                        {
                            sb.AppendLine($"            │    {tank.GetTankName(), -50} │ {tank.BattlesCount, 5} │ {tank.AverageDamage, 6} │ {Math.Round(tank.AverageArtys / 2, 3), -4}");
                        }
                        sb.AppendLine($"     <{new string('-', 20)}");
                        sb.AppendLine();
                    }
                    sb.AppendLine($"<{new string('=', 20)}");
                }
                sb.AppendLine("============================================");
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
