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

namespace Kursach
{
    // Форма, яка показується при програші в грі
    public partial class LoseForm : Form
    {
        // Конструктор форми — ініціалізує компоненти (елементи інтерфейсу)
        public LoseForm()
        {
            InitializeComponent();
        }

        // Обробник події натискання кнопки "Go" (почати заново)
        private void buttonGo_Click(object sender, EventArgs e)
        {
            // Перезапускає програму
            Application.Restart();
        }

        // Коли курсор миші заходить на кнопку "Go"
        private void buttonGo_MouseEnter(object sender, EventArgs e)
        {
            // Змінюємо колір фону кнопки на темно-синій
            buttonGo.BackColor = Color.DarkBlue;
            // Колір тексту робимо яскраво-зеленим (LimeGreen)
            buttonGo.ForeColor = Color.LimeGreen;
        }

        // Коли курсор миші покидає кнопку "Go"
        private void buttonGo_MouseLeave(object sender, EventArgs e)
        {
            // Повертаємо стандартний білий фон
            buttonGo.BackColor = Color.White;
            // Текст кнопки чорного кольору
            buttonGo.ForeColor = Color.Black;
        }

        // Обробник події натискання кнопки "Exit" (вийти з програми)
        private void buttonExit_Click(object sender, EventArgs e)
        {
            // Закриваємо додаток
            Application.Exit();
        }

        // Коли курсор заходить на кнопку "Exit"
        private void buttonExit_MouseEnter(object sender, EventArgs e)
        {
            // Темно-синій фон для кнопки
            buttonExit.BackColor = Color.DarkBlue;
            // Текст червоного кольору (позначає вихід)
            buttonExit.ForeColor = Color.Red;
        }

        // Коли курсор покидає кнопку "Exit"
        private void buttonExit_MouseLeave(object sender, EventArgs e)
        {
            // Білий фон і чорний текст (стандартний стиль)
            buttonExit.BackColor = Color.White;
            buttonExit.ForeColor = Color.Black;
        }

        // Обробник події закриття форми (наприклад, при натисканні на хрестик)
        private void LoseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // При закритті цієї форми — закриваємо додаток повністю
            Application.Exit();
        }
    }
}
