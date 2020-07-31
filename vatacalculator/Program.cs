using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
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

        private static int getLongFromConsole(out long result, bool require = true)
        {
            while (true)
            {
                result = long.MinValue;
                var a  = getStringFromConsole();

                if (a.Length == 0 && !require)
                    return 0;

                if (a == "q")
                    return -1;

                if (long.TryParse(a, out result))
                    return 1;

                //return 2;
                Console.WriteLine("Введено неверное значение: '" + a + "'. Пожалуйста, введите целое число");
            }
        }


        class CalculationData
        {
            public string FileName;
            public string Name;
            public string Comment;
            public long   СтоимостьТеплоизоляции;
            public long   СтоимостьМонтажаТеплоизоляции;
            public long   ВесТеплоизоляции;
            public long   Теплопроводность;
            public long   ВремяЭксплуатацииНесущихКонструкций;
            public long   СтоимостьЭлектроэнергии;
            public long   ВремяЭксплуатацииТеплоизоляции;
            public long   ВремяЭксплуатацииНесуихКонструкций;
            public long   КредитнаяСтавка;
            public long   ДисконтированиеЭлектроэнергии;
            public long   ТемператураВКомнате;
            public long   ТемператураВКомнате2;

            public int    State;

            public CalculationData()
            {}

            public void SetValuesFromConsole()
            {
                Console.WriteLine("Создание файла для рассчётов");
                Console.WriteLine("Имя расчёта:");
                Name = Console.ReadLine();

                Console.WriteLine("Комментарий:");
                Comment = Console.ReadLine();

                Console.WriteLine("Далее вводите целые числа");

                Console.WriteLine("Стоимость одного кубического метра теплоизоляции, не включая монтаж:");
                Console.WriteLine("Обратите внимание, стоимость именно кубического метра, а не квадратного");
                State = getLongFromConsole(out СтоимостьТеплоизоляции);
                if (State < 0)
                    return;

                Console.WriteLine("Стоимость монтажа одного квадратного метра теплоизоляции для толщины 150 мм:");
                Console.WriteLine("Для большей толщины стоимость будет пропорционально увеличена");
                State = getLongFromConsole(out СтоимостьМонтажаТеплоизоляции);
                if (State < 0)
                    return;

                Console.WriteLine("Вес кубического метра теплоизоляции, кг:");
                State = getLongFromConsole(out ВесТеплоизоляции);
                if (State < 0)
                    return;

                Console.WriteLine("Стоимость поддержания несущими конструкциями одного килограмма теплоизоляции (включая монтаж)");
                Console.WriteLine("(для самонесущих стен из конструкционно-теплоизоляционных материалов, напрмер, полистиролбетона - 0)");
                State = getLongFromConsole(out ВремяЭксплуатацииНесущихКонструкций, false);
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
                State = getLongFromConsole(out ВремяЭксплуатацииНесуихКонструкций, false);
                if (State < 0)
                    return;

                Console.WriteLine("Ставка по кредиту на строительство дома (пусто - 14%)");
                Console.WriteLine("Даже если вы очень богаты и не планируете брать кредит, вводите больше, чем значение инфляции");
                State = getLongFromConsole(out КредитнаяСтавка);
                if (State < 0)
                    return;

                Console.WriteLine("Ставка дисконтирования для цен на электроэнергию");
                Console.WriteLine("(пусто - равно ставке по кредиту; рекомендуется оставить пустым - просто нажмите Enter)");
                State = getLongFromConsole(out ДисконтированиеЭлектроэнергии, false);
                if (State < 0)
                    return;

                Console.WriteLine("Температура в комнате:");
                State = getLongFromConsole(out ТемператураВКомнате);
                if (State < 0)
                    return;
                Console.WriteLine("Вторая температура в комнате (можно оставить пустым):");
                State = getLongFromConsole(out ТемператураВКомнате2, false);
                if (State < 0)
                    return;

                Label:
                try
                {
                    Console.WriteLine("Введите имя файла расчёта (будет сохранён в директории 'data'):");
                    FileName = Console.ReadLine();

                    var fi = new FileInfo(Path.Combine("data", FileName));
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
            }

            private void SaveToFile()
            {
                File.WriteAllText(FileName, this.ToString());
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
                sb.AppendLine("СтоимостьМонтажаТеплоизоляции");
                sb.AppendLine(СтоимостьМонтажаТеплоизоляции.ToString());
                sb.AppendLine("ВесТеплоизоляции");
                sb.AppendLine(ВесТеплоизоляции.ToString());
                sb.AppendLine("СтоимостьНесущихКонструкций");
                sb.AppendLine(ВремяЭксплуатацииНесущихКонструкций.ToString());
                sb.AppendLine("Теплопроводность");
                sb.AppendLine(Теплопроводность.ToString());
                sb.AppendLine("СтоимостьЭлектроэнергии");
                sb.AppendLine(СтоимостьЭлектроэнергии.ToString());
                sb.AppendLine("ВремяЭксплуатацииТеплоизоляции");
                sb.AppendLine(ВремяЭксплуатацииТеплоизоляции.ToString());
                sb.AppendLine("ВремяЭксплуатацииНесуихКонструкций");
                sb.AppendLine(ВремяЭксплуатацииНесуихКонструкций.ToString());
                sb.AppendLine("КредитнаяСтавка");
                sb.AppendLine(КредитнаяСтавка.ToString());
                sb.AppendLine("ДисконтированиеЭлектроэнергии");
                sb.AppendLine(ДисконтированиеЭлектроэнергии.ToString());
                sb.AppendLine("ТемператураВКомнате");
                sb.AppendLine(ТемператураВКомнате.ToString());
                sb.AppendLine("ТемператураВКомнате2");
                sb.AppendLine(ТемператураВКомнате2.ToString());

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
                        case "СтоимостьМонтажаТеплоизоляции":
                                    СтоимостьМонтажаТеплоизоляции = long.Parse(val);
                                    break;
                        case "ВесТеплоизоляции":
                                    ВесТеплоизоляции = long.Parse(val);
                                    break;
                        case "СтоимостьНесущихКонструкций":
                                    ВремяЭксплуатацииНесущихКонструкций = long.Parse(val);
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
                        case "ТемператураВКомнате2":
                                    ТемператураВКомнате2 = long.Parse(val);
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

            Console.WriteLine("Выберите файл");

            SortedList<string, CalculationData> data = new SortedList<string, CalculationData>(dataFiles.Length);
            foreach (var df in dataFiles)
            {
                var cd = new CalculationData(df.FullName);
                data.Add(df.Name, cd);
            }

            SortedList<int, string> keys = new SortedList<int, string>(64);
            int i = 0;
            Console.WriteLine("0. Выход");
            int end = i + 25;
            for (; i < data.Keys.Count/* && i < end*/; i++)
            {
                Console.WriteLine("" + (i+1).ToString("D2") + ". " + data.Keys[i]);
                Console.WriteLine(data.Values[i].Name);
                Console.WriteLine(data.Values[i].Comment);
            }

            return "main menu";
        }

    }
}
