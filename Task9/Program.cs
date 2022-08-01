using System;
using System.IO;

namespace Task9
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var token = File.ReadAllText(@"C:\Users\Memento\Desktop\telegrToken.txt");
            new TelegramModule(token);

            while (true)
            {
                var text = Console.ReadLine();
                if (text.ToLower() == "quit")
                    break;
            }
        }
    }
}
