﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Reflection.Emit;
using System.Resources;
using System.Security.Cryptography;
using System.Text;

namespace vatacalculator
{
    class Program
    {
        /*
         * https://meteoinfo.ru/clim-moscow-daily
         * */

        static DirectoryInfo dataDir, meteoDir;
        static void Main(string[] args)
        {
            try
            {
                // Console.WriteLine("Hello World!");
                dataDir  = new DirectoryInfo("data");
                meteoDir = new DirectoryInfo("meteo");
                if (!dataDir.Exists)
                {
                    dataDir.Create();
                }

                if (!meteoDir.Exists)
                {
                    Console.WriteLine("Программа повреждена: не найдена директория 'meteo'. Попробуйте ещё раз скачать программу");
                    Console.ReadLine();
                    return;
                }

                mainCycle();
            }
            catch (Exception e)
            {
                Console.WriteLine("Произошла фатальная ошибка: " + e.Message + "\r\n" + e.StackTrace + "\r\n");
                Console.ReadLine();
                return;
            }
        }

        delegate string MainCycleFunc();
        static SortedList<string, MainCycleFunc> mainCycleFuncs = new SortedList<string, MainCycleFunc>(256);

        private static void mainCycle()
        {
            mainCycleFuncs.Add("main menu", mainMenu);
            mainCycleFuncs.Add("create",    create);
            mainCycleFuncs.Add("calc",      calc);

            string nextState = "main menu";
            while (nextState != "q")
            {
                nextState = mainCycleFuncs[nextState]();
            }
        }

        private static string mainMenu()
        {
            string nextState = "q";

            var files = dataDir.GetFiles();
            if (files.Length > 0)
            {
                nextState = MainMenu1();
            }
            else
            {
                nextState = create();
            }

            return nextState;
        }

        private static string MainMenu1()
        {
            Console.Clear();
            Console.WriteLine("Введите на клавиатуре номер варианта");
            Console.WriteLine("1. Выбрать готовый файл расчёта");
            Console.WriteLine("2. Создать новый");
            Console.WriteLine("0. q или e - выход");
            string str = getStringFromConsole();

            if (str == "0" || str == "q" || str == "e" || str == "quit" || str == "exit")
            {
                return "q";
            }

            if (str == "2" || str == "c" || str == "create")
            {
                return "create";
            }

            if (str == "1" || str == "calc")
            {
                return "calc";
            }

            return "main menu";
        }

        private static string getStringFromConsole()
        {
            return Console.ReadLine().Trim().ToLowerInvariant();
        }

        private static int getLongFromConsole(out long result, bool require = true, long defaultVal = long.MinValue)
        {
            return getLongFromConsole(null, out result, require, defaultVal);
        }

        private static int getLongFromConsole(string val, out long result, bool require = true, long defaultVal = long.MinValue)
        {
            while (true)
            {
                result = defaultVal;
                if (val == null)
                    val = getStringFromConsole();

                if (val.Length == 0 && !require)
                    return 0;

                if (val == "q")
                    return -1;

                if (long.TryParse(val, out result))
                    return 1;

                //return 2;
                Console.WriteLine("Введено неверное значение: '" + val + "'. Пожалуйста, введите целое число");
            }
        }


        class CalculationData
        {
            public string FileName;
            public string Name;
            public string Comment;
            public long   СтоимостьТеплоизоляции;
            public long   СтоимостьМонтажаТеплоизоляцииКв;
            public long   СтоимостьМонтажаТеплоизоляцииКуб;
            public long   СтоимостьПоддержкиОдногоКилограмма;
            public long   ВесТеплоизоляции;
            public long   Теплопроводность;
            public long   СтоимостьЭлектроэнергии;
            public long   ВремяЭксплуатацииТеплоизоляции;
            public long   ВремяЭксплуатацииНесущихКонструкций;
            public long   КредитнаяСтавка;
            public long   ДисконтированиеЭлектроэнергии;
            public long   ТемператураВКомнате;

            public int    State;

            public string CurrentCalculationFileName;

            public CalculationData()
            {}

            public void SetValuesFromConsole()
            {
                Console.Clear();
                Console.WriteLine("Создание файла для рассчётов");
                Console.WriteLine("Имя расчёта:");
                Name = Console.ReadLine();

                Console.WriteLine("Комментарий:");
                Comment = Console.ReadLine();

                Console.WriteLine("Далее вводите целые числа");

                Console.WriteLine("Стоимость одного КУБИЧЕСКОГО метра теплоизоляции, не включая монтаж:");
                Console.WriteLine("Обратите внимание, стоимость именно кубического метра, а не квадратного");
                State = getLongFromConsole(out СтоимостьТеплоизоляции);
                if (State < 0)
                    return;

                Console.WriteLine("Стоимость монтажа одного КВАДРАТНОГО метра теплоизоляции без учёта толщины изоляции (допустимо не вводить)");
                State = getLongFromConsole(out СтоимостьМонтажаТеплоизоляцииКв, false, 0);
                if (State < 0)
                    return;

                Console.WriteLine("Стоимость монтажа одного КУБИЧЕСКОГО метра теплоизоляции (допустимо не вводить)");
                State = getLongFromConsole(out СтоимостьМонтажаТеплоизоляцииКуб, false, 0);
                if (State < 0)
                    return;

                Console.WriteLine("Вес кубического метра теплоизоляции, кг:");
                State = getLongFromConsole(out ВесТеплоизоляции);
                if (State < 0)
                    return;

                Console.WriteLine("Стоимость поддержания несущими конструкциями одного килограмма теплоизоляции (включая монтаж)");
                Console.WriteLine("(для самонесущих стен из конструкционно-теплоизоляционных материалов, напрмер, полистиролбетона - 0; для простоты расчёта можно ввести 0)");
                State = getLongFromConsole(out СтоимостьПоддержкиОдногоКилограмма, false, 0);
                if (State < 0)
                    return;

                Console.WriteLine("Теплопроводность теплоизоляции, милливат на метр на Кельвин");
                Console.WriteLine("Обратите внимание, ввод идёт в милливатах. Например, коэффициент 0,034 - это 34. Коэффициент 0,45 - это 450.");
                State = getLongFromConsole(out Теплопроводность);
                if (State < 0)
                    return;

                Console.WriteLine("Стоимость одного киловат-часа электроэнергии в __копейках__");
                Console.WriteLine("(либо киловат-часа другой энергии, потраченной на обогрев)");
                Console.WriteLine("Обратите внимание, 6,1 рубля - это 610 копеек!");
                State = getLongFromConsole(out СтоимостьЭлектроэнергии);
                if (State < 0)
                    return;

                Console.WriteLine("Количество лет до смены теплоизоляции:");
                State = getLongFromConsole(out ВремяЭксплуатацииТеплоизоляции);
                if (State < 0)
                    return;

                Console.WriteLine("Количество лет до смены несущих конструкций (можете оставить пустым - просто нажмите Enter):");
                State = getLongFromConsole(out ВремяЭксплуатацииНесущихКонструкций, false, ВремяЭксплуатацииТеплоизоляции);
                if (State < 0)
                    return;

                Console.WriteLine("Ставка по кредиту на строительство дома (пусто - 14%), в процентах");
                Console.WriteLine("Даже если вы очень богаты и не планируете брать кредит, вводите больше, чем значение инфляции");
                State = getLongFromConsole(out КредитнаяСтавка);
                if (State < 0)
                    return;

                Console.WriteLine("Ставка дисконтирования для цен на электроэнергию, в процентах");
                Console.WriteLine("(пусто - равно ставке по кредиту; рекомендуется оставить пустым - просто нажмите Enter)");
                State = getLongFromConsole(out ДисконтированиеЭлектроэнергии, false, КредитнаяСтавка);
                if (State < 0)
                    return;

                Console.WriteLine("Температура в комнате:");
                State = getLongFromConsole(out ТемператураВКомнате);
                if (State < 0)
                    return;

                Label:
                FileInfo fi = null;
                try
                {
                    Console.WriteLine("Введите имя файла расчёта (будет сохранён в директории 'data'):");
                    FileName = Console.ReadLine();

                    fi = new FileInfo(Path.Combine("data", FileName));
                    if (fi.Exists)
                    {
                        Console.WriteLine("Такой файл уже существует");
                        goto Label;
                    }
                }
                catch
                {
                    goto Label;
                }

                SaveToFile();
                Console.WriteLine("Сохранено в файл " + fi.FullName);
                Console.WriteLine("При необходимости изменения параметров, это можно сделать вручную из текстового редактора для простого текстового формата");
            }

            private void SaveToFile()
            {
                File.WriteAllText(Path.Combine("data", FileName), this.ToString());
            }

            public override string ToString()
            {
                var sb = new StringBuilder(1024);

                sb.AppendLine("Name");
                sb.AppendLine(Name);
                sb.AppendLine("Comment");
                sb.AppendLine(Comment);
                sb.AppendLine("СтоимостьТеплоизоляции");
                sb.AppendLine(СтоимостьТеплоизоляции.ToString());
                sb.AppendLine("СтоимостьМонтажаТеплоизоляцииКв");
                sb.AppendLine(СтоимостьМонтажаТеплоизоляцииКв.ToString());
                sb.AppendLine("СтоимостьМонтажаТеплоизоляцииКуб");
                sb.AppendLine(СтоимостьМонтажаТеплоизоляцииКуб.ToString());
                sb.AppendLine("ВесТеплоизоляции");
                sb.AppendLine(ВесТеплоизоляции.ToString());
                sb.AppendLine("СтоимостьПоддержкиОдногоКилограмма");
                sb.AppendLine(СтоимостьПоддержкиОдногоКилограмма.ToString());
                sb.AppendLine("Теплопроводность");
                sb.AppendLine(Теплопроводность.ToString());
                sb.AppendLine("СтоимостьЭлектроэнергии");
                sb.AppendLine(СтоимостьЭлектроэнергии.ToString());
                sb.AppendLine("ВремяЭксплуатацииТеплоизоляции");
                sb.AppendLine(ВремяЭксплуатацииТеплоизоляции.ToString());
                sb.AppendLine("ВремяЭксплуатацииНесущихКонструкций");
                sb.AppendLine(ВремяЭксплуатацииНесущихКонструкций.ToString());
                sb.AppendLine("КредитнаяСтавка");
                sb.AppendLine(КредитнаяСтавка.ToString());
                sb.AppendLine("ДисконтированиеЭлектроэнергии");
                sb.AppendLine(ДисконтированиеЭлектроэнергии.ToString());
                sb.AppendLine("ТемператураВКомнате");
                sb.AppendLine(ТемператураВКомнате.ToString());

                return sb.ToString();
            }

            public CalculationData(string FileName)
            {
                var strings = File.ReadAllLines(FileName);


                for (int i = 1; i < strings.Length; i += 2)
                {
                    var name = strings[i - 1].Trim();
                    var val  = strings[i - 0];

                    if (name.Length < 0 || name.StartsWith("#"))
                    {
                        i--;
                        continue;
                    }

                    switch (name)
                    {
                        case "Name":
                                    Name = val;
                                    break;
                        case "Comment":
                                    Comment = val;
                                    break;
                        case "СтоимостьТеплоизоляции":
                                    СтоимостьТеплоизоляции = long.Parse(val);
                                    break;
                        case "СтоимостьМонтажаТеплоизоляцииКуб":
                                    СтоимостьМонтажаТеплоизоляцииКуб = long.Parse(val);
                                    break;
                        case "СтоимостьМонтажаТеплоизоляцииКв":
                                    СтоимостьМонтажаТеплоизоляцииКв = long.Parse(val);
                                    break;
                        case "ВесТеплоизоляции":
                                    ВесТеплоизоляции = long.Parse(val);
                                    break;
                        case "СтоимостьПоддержкиОдногоКилограмма":
                                    СтоимостьПоддержкиОдногоКилограмма = long.Parse(val);
                                    break;
                        case "Теплопроводность":
                                    Теплопроводность = long.Parse(val);
                                    break;
                        case "СтоимостьЭлектроэнергии":
                                    СтоимостьЭлектроэнергии = long.Parse(val);
                                    break;
                        case "ВремяЭксплуатацииТеплоизоляции":
                                    ВремяЭксплуатацииТеплоизоляции = long.Parse(val);
                                    break;
                        case "ВремяЭксплуатацииНесущихКонструкций":
                                    ВремяЭксплуатацииНесущихКонструкций = long.Parse(val);
                                    break;
                        case "КредитнаяСтавка":
                                    КредитнаяСтавка = long.Parse(val);
                                    break;
                        case "ДисконтированиеЭлектроэнергии":
                                    ДисконтированиеЭлектроэнергии = long.Parse(val);
                                    break;
                        case "ТемператураВКомнате":
                                    ТемператураВКомнате = long.Parse(val);
                                    break;
                        default:
                            throw new Exception("Встречена неожиданная строка: " + name + "\r\nДля комментирования строк в файле используйте символ #");
                    }
                }
            }

        }

        private static string create()
        {
            var cd = new CalculationData();
            cd.SetValuesFromConsole();

            if (cd.State < 0)
                return "main menu";

            return "main menu";
        }

        public static string calc()
        {
            var dataFiles = dataDir.GetFiles();

            SortedList<string, CalculationData> data = new SortedList<string, CalculationData>(dataFiles.Length);
            foreach (var df in dataFiles)
            {
                var cd = new CalculationData(df.FullName);
                data.Add(df.Name, cd);
            }

            
            SortedList<long, string> keys = new SortedList<long, string>(64);
            long st, result;
            do
            {
                keys.Clear();
                Console.Clear();
                Console.WriteLine("0. Выход");
                Console.WriteLine("----------------------------------------------------------------");

                int i = 0;
                int endE  = 5;
                int end   = endE;
                string ch = null;
                for (; i < data.Keys.Count/* && i < end*/; i++)
                {
                    ch = null;

                    Console.WriteLine("" + (i+1).ToString("D2") + ". " + data.Keys[i]);
                    Console.WriteLine(data.Values[i].Name);
                    Console.WriteLine(data.Values[i].Comment);
                    Console.WriteLine("----------------------------------------------------------------");

                    keys.Add(i+1, data.Keys[i]);

                    end--;

                    if (end <= 0)
                    {
                        end = endE;
                        Console.WriteLine("Нажмите Enter для продолжения списка или 'q' и Enter для выбора");
                        ch = Console.ReadLine();
                        if (ch == "q")
                        {
                            ch = null;
                            break;
                        }

                        if (ch == "0")
                            return "main menu";
                        if (ch.Trim() == "")
                            continue;

                        st = getLongFromConsole(ch, out result, true);
                        if (st == 1)
                            break;
                    }
                }

                Console.WriteLine("Выберите файл (введите номер файла и нажмите Enter)");

                st = getLongFromConsole(ch, out result, true);
                if (st < 0 || result == 0)
                    return "main menu";
            }
            while (st != 1 || result < 0 || result > data.Keys.Count);
            
            var fileName = keys[result];
            var calcData = data[fileName];

            Console.Clear();
            Console.WriteLine("Выбран файл " + fileName);
            Console.WriteLine(calcData.Name);
            Console.WriteLine(calcData.Comment);

            calcAndPrintResult(calcData);

            Console.ReadLine();

            return "calc";
        }

        private static void calcAndPrintResult(CalculationData calcData)
        {
            var cl = askClimatFile();
            var sb = new StringBuilder();
            sb.AppendLine("Файл расчёта " + calcData.Name);
            sb.AppendLine("Файл климата " + cl.Name);

            var now = DateTime.Now;
            calcData.CurrentCalculationFileName = now.Year.ToString("D4") + "-" + now.Month.ToString("D2") + now.Day.ToString("D2") + "-" + now.Hour.ToString("D2") + now.Minute.ToString("D2") + now.Second.ToString("D2") + ".txt";
            calcData.CurrentCalculationFileName = Path.Combine("results", calcData.CurrentCalculationFileName);

            if (!Directory.Exists("results"))
                Directory.CreateDirectory("results");

            SortedList<double, double> S = new SortedList<double, double>(1024);

            int    count  = 0, cnt = 0;
            double H      = 0.001;
            double dh     = 0.001;
            double lastH  = double.MaxValue;
            double lastS  = double.MaxValue;
            // Оптимизация идёт простым перебором
            while (count < 10 && cnt < 1e6)
            {
                cnt++;
                // double dx = 1e-3;
                // double H2 = H * (1.0 + dx);
                double s1 = РассчитатьСуммарнуюСтоимость(calcData, H , cl);
                // double s2 = РассчитатьСуммарнуюСтоимость(calcData, H2, cl);

                S.Add(H, s1);
                if (s1 > lastS)
                    count++;

                lastS = s1;
                H += dh;
            }

            foreach (var val in S)
            {
                if (val.Value < lastS)
                {
                    lastS = val.Value;
                    lastH = val.Key;
                }
            }

            H = lastH;

            double se = РассчитатьСуммарнуюСтоимость(calcData, H , cl, true);

            sb.AppendLine("----------------------------------------------------------------");
            sb.AppendLine("Оптимальная толщина выбранного вида теплоизоляции " + (H*1000.0).ToString("F0") + " миллиметров");

            Console.WriteLine(sb.ToString());

            sb.AppendLine("Суммарная стоимость расходов с учётом процентов " + se.ToString("C"));

            se = РассчитатьСуммарнуюСтоимость(calcData, H + 0.01 , cl, false);
            sb.AppendLine("Суммарная стоимость расходов с учётом процентов " + se.ToString("C") + " для толщины " + ((H + 0.01)*1000.0).ToString("F0"));
            se = РассчитатьСуммарнуюСтоимость(calcData, Math.Abs(H - 0.01) , cl, false);
            sb.AppendLine("Суммарная стоимость расходов с учётом процентов " + se.ToString("C") + " для толщины " + (Math.Abs(H - 0.01)*1000.0).ToString("F0"));

            File.AppendAllText(calcData.CurrentCalculationFileName, sb.ToString());
            Console.WriteLine("Некоторая информация сохранена в файл " + calcData.CurrentCalculationFileName);
        }

        private static double РассчитатьСуммарнуюСтоимость(CalculationData calcData, double H, Climat cl, bool toFile = false)
        {
            double СуммаНаЭлектроэнергию1 = РассчитатьСтоимостьЭлектроэнергии(H, calcData, cl, toFile);
            double СуммаНаТеплоизоляцию1 = РассчитатьСтоимостьТеплоизоляции(H, calcData, toFile);

            double s1 = СуммаНаЭлектроэнергию1 + СуммаНаТеплоизоляцию1;
            return s1;
        }

        private static double РассчитатьСтоимостьТеплоизоляции(double H, CalculationData d, bool toFile = false)
        {
            var sb = new StringBuilder();

            // Стоимость монтажа квадратного метра
            double result = d.СтоимостьМонтажаТеплоизоляцииКв;

            // Монтаж куба изоляции и сам этот куб
            var cube = (d.СтоимостьТеплоизоляции + d.СтоимостьМонтажаТеплоизоляцииКуб)*H;
            result += cube;

            // Рассчитываем сложные проценты
            var percent = Math.Pow(1.0 + d.КредитнаяСтавка/100.0, d.ВремяЭксплуатацииТеплоизоляции);
            result *= percent;

            var a = d.ВесТеплоизоляции * d.СтоимостьПоддержкиОдногоКилограмма * d.ВремяЭксплуатацииТеплоизоляции / d.ВремяЭксплуатацииНесущихКонструкций;
            result += a * Math.Pow(1.0 + d.КредитнаяСтавка/100.0, d.ВремяЭксплуатацииНесущихКонструкций);

            sb.AppendLine("Стоимость монтажа квадратного метра " + d.СтоимостьМонтажаТеплоизоляцииКв.ToString("C"));
            sb.AppendLine("Монтаж куба изоляции и сам этот куб " + cube.ToString("C"));
            sb.AppendLine("Стоимость несущих конструкций " + a.ToString("C"));
            sb.AppendLine("Проценты за время эксплуатации теплоизоляции (в разах; например, число 1,0 при ставке 0% годовых) " + percent);

            sb.AppendLine("Всего затрат на теплоизоляцию 1 кв. метра (включая проценты) " + result.ToString("C"));

            if (toFile)
                File.AppendAllText(d.CurrentCalculationFileName, sb.ToString());

            return result;
        }

        public static string[] monthes = {"Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"};

        private static double РассчитатьСтоимостьЭлектроэнергии(double H, CalculationData d, Climat cl, bool toFile = false)
        {
            var sb = new StringBuilder();
            double result = 0;

            // Стоимость потери одного вата круглый год
            // / 100_000.0 - т.к. исходная цена в копейках за киловатт-час, а нам нужны рубли за ватт-час
            double WCostPerYear = d.СтоимостьЭлектроэнергии / 100_000.0 * 365.25 * 24;

            // Стоимость потери всех ватт, которые приходятся на кв. метр и на ОДИН градус разницы
            // / 1000 - т.к. это в милливаттах
            double W           = d.Теплопроводность / 1000.0 / H;
            double CostPerYear = WCostPerYear * W;

            sb.AppendLine("Стоимость потери одного ватта в год на один градус разницы температуры " + WCostPerYear.ToString("C"));
            sb.AppendLine("Стоимость потери всех ватт в год на один градус разницы температуры " +  CostPerYear.ToString("C"));
            sb.AppendLine("Потери тепла ватт на кв. метр на градус " +  W.ToString("F2"));
            sb.AppendLine("Затраты на кв. месяц по месяцам без учёта процентов");

            // Начинаем с одного, т.к. считаем, что платим в конце года
            for (int y = 1; y <= d.ВремяЭксплуатацииТеплоизоляции; y++)
            {
                // Оплата по месяцам
                for (int m = 0; m < 12; m++)
                {
                    var temp = d.ТемператураВКомнате - cl.degrees[m];
                    if (temp < 0)
                        continue;

                    var CurrentCost = CostPerYear / 12.0 * temp;
                    result += CurrentCost * Math.Pow(1.0 + d.ДисконтированиеЭлектроэнергии/100.0, d.ВремяЭксплуатацииТеплоизоляции - y - m/12.0);

                    if (y == 1)
                    {
                        sb.AppendLine(monthes[m] + ": " + CurrentCost.ToString("C") + ", разница температур " + temp.ToString("F1") + ", потери " + (W*temp).ToString("F2") + " ватт на кв. метр");
                    }
                }
            }

            sb.AppendLine("Всего затрат на отопление за срок эксплуатации теплоизоляции с учётом ставки дисконтирования " + result.ToString("C"));

            if (toFile)
                File.AppendAllText(d.CurrentCalculationFileName, sb.ToString());

            return result;
        }

        
        class Climat
        {
            public string Name;
            public string Comment;
            public bool   daily;

            public float[] degrees = new float[12];

            public Climat(string FileName)
            {
                var f = File.ReadAllLines(FileName);
                Name    = f[0];
                Comment = f[1];
                daily   = f[3].Trim().ToLower() == "daily";

                int j = 0;
                for (int i = 4; j < 12; i++)
                {
                    var str = f[i].Trim();

                    if (str.Length <= 0)
                        continue;

                    float deg;
                    try
                    {
                        deg = float.Parse(str, NumberFormatInfo.InvariantInfo);
                    }
                    catch
                    {
                        try
                        {
                            deg = float.Parse(str, NumberFormatInfo.CurrentInfo);
                        }
                        catch
                        {
                            throw new Exception("Не удалось прочитать файл климата: число неверное, строка " + i + "\r\nЗначение строки " + str);
                        }
                    }

                    degrees[j] = deg;
                    j++;
                }
            }
        }

        private static Climat askClimatFile()
        {
            Console.Clear();
            var dataFiles = meteoDir.GetFiles();

            SortedList<string, Climat> data = new SortedList<string, Climat>(dataFiles.Length);
            foreach (var df in dataFiles)
            {
                try
                {
                    var cd = new Climat(df.FullName);
                    data.Add(df.Name, cd);
                }
                catch (Exception ex)
                {
                    //throw new Exception("Не удалось прочитать файл климата " + df.Name + "\r\n" + ex.Message);
                    Console.WriteLine("Не удалось прочитать файл климата " + df.Name + "\r\n" + ex.Message);
                }
            }

            SortedList<long, string> keys = new SortedList<long, string>(64);
            long st, result;
            string ch = null;
            do
            {
                ch = null;
                keys.Clear();
                Console.Clear();

                int i = 0;
                int endE = 5;
                int end  = endE;
                for (; i < data.Keys.Count/* && i < end*/; i++)
                {
                    Console.WriteLine("" + i.ToString("D2") + ". " + data.Keys[i]);
                    Console.WriteLine(data.Values[i].Name);
                    Console.WriteLine(data.Values[i].Comment);
                    Console.WriteLine("----------------------------------------------------------------");

                    keys.Add(i, data.Keys[i]);

                    end--;

                    if (end <= 0)
                    {
                        end = endE;
                        Console.WriteLine("Нажмите Enter для продолжения списка или 'q' и Enter для выбора");
                        ch = Console.ReadLine();
                        if (ch == "q")
                        {
                            ch = null;
                            break;
                        }

                        st = getLongFromConsole(ch, out result, true);
                        if (st == 1)
                            break;
                    }
                }

                Console.WriteLine("Выберите файл (введите номер файла и нажмите Enter)");

                st = getLongFromConsole(ch, out result, true);
            }
            while (st != 1 || result < 0 || result >= data.Keys.Count);

            var fileName = keys[result];
            var calcData = data[fileName];

            Console.Clear();
            Console.WriteLine("Выбран файл " + fileName);
            Console.WriteLine(calcData.Name);
            Console.WriteLine(calcData.Comment);

            return calcData;
        }

    }
}
