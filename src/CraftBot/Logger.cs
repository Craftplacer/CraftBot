using System;

namespace CraftBot
{
    public static class Logger
    {
        public static void Verbose(string message, string module = null) => _writeLine(module, ConsoleColor.DarkGray, message, ConsoleColor.DarkGray);

        public static void Info(string message, string module = null) => _writeLine(module, ConsoleColor.DarkGray, message, ConsoleColor.Gray);

        public static void Error(string message, string module = null) => _writeLine(module, ConsoleColor.DarkRed, message, ConsoleColor.Red);

        public static void Error(Exception exception, string module = null) => Error(exception.ToString(), module);

        public static void Warning(string message, string module = null) => _writeLine(module, ConsoleColor.DarkYellow, message, ConsoleColor.Yellow);

        private static void _writeLine(string module, ConsoleColor moduleColor, string message, ConsoleColor messageColor)
        {
            if (!string.IsNullOrWhiteSpace(module))
            {
                Console.ForegroundColor = moduleColor;
                Console.Write($"[{module}] ");
            }

            Console.ForegroundColor = messageColor;
            Console.WriteLine(message);
        }
    }
}