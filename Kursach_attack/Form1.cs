using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursach_attack
{
    public partial class Form1 : Form
    {
        //Семафори для синхронізації між процесами (глобальні імена)
        Semaphore semAttack = new Semaphore(0, 1, "Global\\SemAttack");
        Semaphore semComp = new Semaphore(0, 1, "Global\\SemComp");
        Semaphore semPlayer = new Semaphore(0, 1, "Global\\SemPlayer");

        string oldAttack = ""; //Зберігає попередню атаку гравця
        Dictionary<string, PictureBox> pictureBoxes = new(); //Всі клітинки PictureBox на полі
        Dictionary<string, Cell> cells = new(); //Всі логічні клітинки з інформацією про стан
        string userAttack = ""; //Атака, яку зробив гравець
        string path = "D:/ДНУ ФФЕКС/Семестр 4/СП/Kursach/images"; //Шлях до зображень

       

        public Form1()
        {
            InitializeComponent();

            //Встановлюємо позицію вікна на екрані
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(this.Size.Width + 5, 150);

            //Ініціалізація PictureBox-ів і логіки поля
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                {
                    var pb = (PictureBox)this.Controls.Find($"client{i}_{j}", true)[0]; //Знаходимо PictureBox по імені
                    pb.Click += field_Click; //Додаємо обробник кліку
                    pictureBoxes[$"{i}_{j}"] = pb; //Додаємо до словника для доступу по координатах
                    cells[$"{i}_{j}"] = new Cell(); //Ініціалізуємо клітинку
                }

            //Запускаємо фонове очікування дозволу атакувати
            Task.Run(() => GameLoop());
        }
        private bool userCanClick = true; //Чи дозволено гравцеві натискати на поле
        //Обробка кліку по клітинці поля
        private void field_Click(object sender, EventArgs e)
        {
            if (!userCanClick) return; //Заборонено клікати — чекаємо

            if (sender is PictureBox pb)
            {
                string key = pb.Name.Replace("client", ""); //Отримуємо координати клітинки
                if (cells.ContainsKey(key) && cells[key].status == "empty") //Якщо клітинка пуста
                {
                    userAttack = key;
                    userCanClick = false;
                    oldAttack = userAttack;

                    try
                    {
                        //Створюємо об’єкт для передачі даних
                        var data = new DataTransfer("Attack");
                        if (data.Read() == "*") //Якщо готово до прийому
                            data.Write(userAttack); //Надсилаємо координати атаки
                    }
                    catch (Exception er)
                    {
                        MessageBox.Show(er.ToString());
                    }

                    semComp.Release(); //Дозволяємо комп’ютеру обробляти хід
                }
            }
        }

        //Основний цикл, який чекає дозволу на хід і обробляє результат атаки
        private void GameLoop()
        {
            while (true)
            {
                semAttack.WaitOne(); //Чекаємо дозволу на атаку
                label1.Invoke(() => label1.ForeColor = Color.LimeGreen); //Підказка, що можна стріляти
                userCanClick = true;

                var resultData = new DataTransfer("AttackResult");
                string result = resultData.Read(); //Читаємо результат атаки
                resultData.Write("*"); //Повідомляємо, що зчитано

                if (result.StartsWith("*")) continue; //Якщо не готово — чекаємо далі

                userCanClick = false;

                if (result.StartsWith("+") || result.StartsWith("++")) //Якщо влучили або знищили
                {
                    string[] parts = result.Split('_');
                    int size = int.Parse(parts[1]); //Розмір корабля
                    int num = int.Parse(parts[2]); //Номер корабля
                    int shipPart = int.Parse(parts[3]); //Частина корабля
                    bool rotate = parts[4] == "1"; //Повернутий корабель?

                    try
                    {
                        //Оновлюємо логіку клітинки
                        cells[oldAttack].status = $"d_{size}_{num}";
                        cells[oldAttack].shipPart = $"{size}_{shipPart}";
                        cells[oldAttack].rotate = rotate;

                        //Змінюємо зображення на зруйновану клітинку
                        pictureBoxes[oldAttack].Invoke(() =>
                        {
                            pictureBoxes[oldAttack].Image = new Bitmap(path + "/destroyed.png");
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                    if (result.StartsWith("++")) //Якщо корабель повністю знищено
                    {
                        nearDrawing(oldAttack, size, shipPart, rotate); //Обводимо клітинки навколо

                        //Оновлюємо кожну частину корабля відповідним зображенням
                        foreach (var cell in cells)
                        {
                            if (cell.Value.status == $"d_{size}_{num}")
                            {
                                if (pictureBoxes.TryGetValue(cell.Key, out PictureBox found))
                                {
                                    Bitmap bmp = new(path + $"/d_{cell.Value.shipPart}.png");
                                    if (cell.Value.rotate)
                                        bmp.RotateFlip(RotateFlipType.Rotate270FlipNone); //Обертаємо картинку

                                    Invoke(() => found.Image = bmp);
                                    found.Refresh();
                                }
                            }
                        }
                    }

                    userCanClick = true; //Дозволяємо нову атаку
                    continue;
                }
                else if (result == "-") //Якщо промах
                {
                    label1.Invoke(() => label1.ForeColor = Color.Red); //Колір індикатора = червоний
                    cells[oldAttack].status = "miss";

                    pictureBoxes[oldAttack].Invoke(() =>
                    {
                        pictureBoxes[oldAttack].Image = new Bitmap(path + "/miss.png"); //Картинка промаху
                    });

                    semComp.Release(); //Передаємо хід комп’ютеру
                }
            }
        }

        //Позначає всі клітинки навколо знищеного корабля як "мимо"
        private void nearDrawing(string center, int size, int shipPart, bool rotate)
        {
            var parts = center.Split('_');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            //Знаходимо позицію передньої частини корабля
            row = rotate ? row + shipPart - 1 : row;
            col = rotate ? col : col - shipPart + 1;

            //Межі зони навколо корабля
            int startRow = rotate ? row - size : row - 1;
            int endRow = rotate ? row + 1 : row + 1;
            int startCol = rotate ? col - 1 : col - 1;
            int endCol = rotate ? col + 1 : col + size;

            //Обмеження в межах поля
            startRow = Math.Max(0, startRow);
            endRow = Math.Min(9, endRow);
            startCol = Math.Max(0, startCol);
            endCol = Math.Min(9, endCol);

            //Позначення всіх пустих клітинок навколо корабля як "мимо"
            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = startCol; c <= endCol; c++)
                {
                    string key = $"{r}_{c}";
                    if (cells.ContainsKey(key) && cells[key].status == "empty")
                    {
                        cells[key].status = "miss";
                        cells[key].shipPart = "0";
                        pictureBoxes[key].Invoke(() =>
                        {
                            pictureBoxes[key].Image = new Bitmap(path + "/miss.png");
                        });
                    }
                }
            }
        }

        //Обробка закриття форми
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Invoke(() =>
            {
                string[] targets = {
                    "Kursach_comp", //Процес комп’ютера
                    "Kursach"       //Головна гра
                };

                //Спроба завершити відповідні процеси
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (targets.Contains(proc.ProcessName))
                        {
                            proc.Kill();
                        }
                    }
                    catch
                    {
                        //Ігноруємо винятки при відсутності доступу
                    }
                }
            });

            Application.Exit(); //Закриваємо програму
        }
    }
}
