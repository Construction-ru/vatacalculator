using System;
using System.IO;

namespace vatacalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Console.WriteLine("Hello World!");
                var dataDir  = new DirectoryInfo("data");
                var meteoDir = new DirectoryInfo("meteo");
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Произошла ошибка: " + e.Message + "\r\n" + e.StackTrace + "\r\n");
                Console.ReadLine();
                return;
            }

            var files = dataDir.GetFiles();

        }
    }
}
