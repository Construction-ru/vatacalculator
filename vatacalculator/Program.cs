using System;
using System.Collections.Generic;
using System.IO;

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
            Console.WriteLine("q или e - выход");
            string str = getStringFromConsole();

            if (str == "q" || str == "e" || str == "quit" || str == "exit")
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


        class CalculationData
        {
            public string Name;
            public string Comment;
            public long   СтоимостьТеплоизоляции;
            public long   ВесТеплоизоляции;
            public long   Теплопроводность;
            public long   СтоимостьНесущихКонструкций;
            public long   СтоимостьЭлектроэнергии;
            public long   ВремяЭксплуатацииТеплоизоляции;
            public long   ВремяЭксплуатацииНесуихКонструкций;
            public long   КредитнаяСтавка;

            public void SetValuesFromConsole()
            {
                Console.WriteLine("Создание файла для рассчётов");
                Console.WriteLine("Имя расчёта:");
                Console.WriteLine("Комментарий:");
                Console.WriteLine("Далее вводите целые числа");
                Console.WriteLine("Стоимость одного кубического метра теплоизоляции:");
                Console.WriteLine("Вес кубического метра теплоизоляции:");
                Console.WriteLine("Стоимость поддержания несущими конструкциями одного килограмма теплоизоляции:");
                Console.WriteLine("Теплопроводность теплоизоляции, милливат на метр на Кельвин");
                Console.WriteLine("Обратите внимание, ввод идёт в милливатах. Например, коэффициент 0,034 - это 34. Коэффициент 0,45 - это 450.");
                Console.WriteLine("Стоимость одного киловат-часа электроэнергии в __копейках__");
                Console.WriteLine("Обратите внимание, 6,1 рубля - это 610 копеек!");
                Console.WriteLine("Количество лет до смены теплоизоляции:");
                Console.WriteLine("Количество лет до смены несущих конструкций (можете оставить пустым - просто нажмите Enter):");
                Console.WriteLine("Ставка по кредиту на строительство дома (пусто - 14%)");
                Console.WriteLine("Даже если вы очень богаты и не планируете брать кредит, вводите больше, чем значение инфляции");
                Console.WriteLine("Ставка дисконтирования для цен на электроэнергию");
                Console.WriteLine("(пусто - равно ставке по кредиту; рекомендуется оставить пустым - просто нажмите Enter)");
            }
        }
        
        private static string create()
        {
            var cd = new CalculationData();
            cd.SetValuesFromConsole();


            return "main menu";
        }

    }
}
