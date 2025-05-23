using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursach_comp
{
    public partial class WinForm : Form
    {
        public WinForm()
        {
            InitializeComponent();

            //Робимо вікно завжди зверху над іншими
            this.TopMost = true;

            //Активуємо вікно (воно стане у фокус)
            this.Activate();
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            try
            {
                //Запускаємо головний exe-файл гри
                Process.Start("D:/ДНУ ФФЕКС/Семестр 4/СП/Kursach/bin/Debug/net8.0-windows/Kursach.exe");
            }
            catch (Exception ex)
            {
                //Якщо виникла помилка при запуску — показуємо повідомлення
                MessageBox.Show("Помилка запуску процесу:\n" + ex.ToString());
                return;
            }

            //Завершуємо поточний процес, щоб не було декількох екземплярів гри
            Environment.Exit(0);
        }

        private void buttonGo_MouseEnter(object sender, EventArgs e)
        {
            //Змінюємо кольори кнопки при наведенні миші
            buttonGo.BackColor = Color.DarkBlue;
            buttonGo.ForeColor = Color.LimeGreen;
        }

        private void buttonGo_MouseLeave(object sender, EventArgs e)
        {
            //Повертаємо стандартні кольори кнопки при відведенні миші
            buttonGo.BackColor = Color.White;
            buttonGo.ForeColor = Color.Black;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            //Завершуємо роботу програми при натисканні кнопки "Вихід"
            Application.Exit();
        }

        private void buttonExit_MouseEnter(object sender, EventArgs e)
        {
            //Змінюємо кольори кнопки "Вихід" при наведенні миші
            buttonExit.BackColor = Color.DarkBlue;
            buttonExit.ForeColor = Color.Red;
        }

        private void buttonExit_MouseLeave(object sender, EventArgs e)
        {
            //Повертаємо стандартні кольори кнопки "Вихід" при відведенні миші
            buttonExit.BackColor = Color.White;
            buttonExit.ForeColor = Color.Black;
        }

        private void WinForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Завершуємо додаток
            Application.Exit();
        }
    }
}
