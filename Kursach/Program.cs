namespace Kursach
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
            ApplicationConfiguration.Initialize();

            //Запуск основной форми
            Application.Run(new Form1());

            //Запуск форми програшу після закриття основної
            Application.Run(new LoseForm());
        }
    }
}