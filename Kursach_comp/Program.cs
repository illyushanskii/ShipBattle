using System.Diagnostics;

namespace Kursach_comp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            //Первірка чи запущений основний потік
            bool isRunning = Process.GetProcessesByName("Kursach").Length > 0;
            if (isRunning)
            {
                //Так, запускаємо даний процес
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());
            }
            else
            {
                //Ні, повідомлення та вихід
                MessageBox.Show("Запустіть програму через основний процес!");
                Application.Exit();
            }
        }
    }
}