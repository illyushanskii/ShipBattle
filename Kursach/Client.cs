using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursach
{
    public partial class Client : Form
    {
        //Ініціалізація семафорів
        Semaphore semAttack = new Semaphore(0, 1, "Global\\SemAttack"); //Семафор для керування атакою
        Semaphore semComp = new Semaphore(0, 1, "Global\\SemComp");     //Семафор для керування ходом комп'ютера
        Semaphore semPlayer = new Semaphore(0, 1, "Global\\SemPlayer"); //Семафор для керування ходом гравця

        Form1 form1;        //Головна форма гри
        Process attack;     //Процес, що відповідає за атаку
        Process comp;       //Процес комп'ютера

        Dictionary<string, Cell> cells = new();          //Клітинки поля з інформацією про стан
        Dictionary<string, PictureBox> pictureBoxes = new(); //Відповідність клітинок та PictureBox для відображення
        Dictionary<string, int> destroyedShips = new(); //Скільки частин кожного корабля знищено

        //Шлях до зображень кораблів, промахів тощо
        static string path = "D:/ДНУ ФФЕКС/Семестр 4/СП/Kursach/images";

        public Client(Form1 form, Dictionary<string, Cell> cells)
        {
            InitializeComponent();
            form1 = form;
            //Копіюємо клітинки щоб не працювати напряму з оригіналом
            this.cells = cells.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());

            //Прив'язуємо PictureBox до кожної клітинки по імені
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                {
                    PictureBox pb = (PictureBox)this.Controls.Find($"client{i}_{j}", true)[0];
                    pictureBoxes[$"{i}_{j}"] = pb;
                }

            //Встановлюємо початкові зображення кораблів на полі
            foreach (var cell in cells)
            {
                string key = cell.Key;
                var value = cell.Value;
                if (value.shipPart != "0")
                {
                    Bitmap bmp = new(path + "/" + value.shipPart + ".png");
                    if (value.rotate) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    pictureBoxes[key].Image = bmp;
                }
            }
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Закриваємо процеси при закритті форми
            comp.Kill();
            attack.Kill();
            Application.Exit();
        }

        private void Client_Load(object sender, EventArgs e)
        {
            //Ініціалізуємо канали передачі даних для комунікації між процесами
            var data = new DataTransfer("Attack");
            data.Write("*");

            data = new DataTransfer("AttackResult");
            data.Write("*");

            //Запускаємо процеси атаки та комп'ютера
            try
            {
                attack = Process.Start("D:/ДНУ ФФЕКС/Семестр 4/СП/Kursach_attack/bin/Debug/net8.0-windows/Kursach_attack.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при запуску атакуючого процесу: {ex.Message}");
            }

            try
            {
                comp = Process.Start("D:/ДНУ ФФЕКС/Семестр 4/СП/Kursach_comp/bin/Debug/net8.0-windows/Kursach_comp.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при запуску комп'ютерного процесу: {ex.Message}");
            }

            //Дозволяємо процесу атаки почати роботу (семафор)
            semAttack.Release();

            //Запускаємо фоновий цикл прослуховування вхідних даних
            Task.Run(() => GameLoop());
        }

        private void GameLoop()
        {
            while (true)
            {
                //Чекаємо сигнал від гравця (через семафор)
                semPlayer.WaitOne();

                //Читаємо координати атаки від процесу "Attack"
                var data = new DataTransfer("Attack");
                string compAttack = data.Read();

                //Підтверджуємо прийом ("*")
                data.Write("*");

                if (compAttack == "*") continue;

                string status = cells[compAttack].status;
                bool rotate = cells[compAttack].rotate;

                //Якщо у клітинці є корабель (статус починається з числа)
                if (int.TryParse(status.Substring(0, 1), out int size))
                {
                    //Позначаємо клітинку як знищену ("d_")
                    cells[compAttack].status = "d_" + status;

                    //Оновлюємо зображення через UpdateImage
                    string[] parts = cells[compAttack].shipPart.Split('_');
                    int shipPart = int.Parse(parts[1]);
                    UpdateImage(compAttack, new Bitmap(path + $"/d_{size}_{shipPart}.png"), rotate);

                    //Відстежуємо кількість пошкоджених частин корабля
                    if (!destroyedShips.ContainsKey(status))
                        destroyedShips[status] = 1;
                    else
                        destroyedShips[status]++;

                    //Якщо корабель повністю знищений
                    if (destroyedShips[status] == size)
                    {
                        //Позначаємо навколишні клітинки як "промах"
                        nearDrawing(compAttack, size, shipPart, rotate);

                        //Читаємо з "AttackResult" і відповідаємо результатом "++_..."
                        data = new DataTransfer("AttackResult");
                        string rot = rotate ? "1" : "0";
                        if (data.Read() == "*") data.Write($"++_{size}_{shipPart}_{rot}");
                    }
                    else
                    {
                        //Якщо корабель не знищений повністю — відправляємо "+"
                        data = new DataTransfer("AttackResult");
                        if (data.Read() == "*") data.Write("+");
                    }

                    //Дозволяємо процесу комп'ютера виконати хід
                    semComp.Release();
                }
                else if (status.StartsWith("d") || status == "miss")
                {
                    //Якщо клітинка вже оброблена, ігноруємо хід
                    continue;
                }
                else
                {
                    //Якщо промах — позначаємо клітинку і оновлюємо графіку
                    cells[compAttack].status = "miss";
                    cells[compAttack].shipPart = "0";
                    UpdateImage(compAttack, new Bitmap(path + "/miss.png"), false);

                    //Відправляємо "-" в "AttackResult"
                    data = new DataTransfer("AttackResult");
                    if (data.Read() == "*") data.Write("-");

                    //Дозволяємо хід комп'ютеру
                    semComp.Release();
                }

                //Перевіряємо, чи всі кораблі знищені
                checkShips();
            }
        }

        //Оновлення зображення клітинки, з урахуванням повороту
        private void UpdateImage(string key, Bitmap bmp, bool rotate)
        {
            if (!pictureBoxes.TryGetValue(key, out PictureBox pb)) return;

            if (rotate)
                bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);

            //Оновлюємо графіку в основному потоці (Invoke)
            pb.Invoke(() => pb.Image = bmp);
        }

        //Позначення навколишніх клітинок навколо знищеного корабля як "промах"
        private void nearDrawing(string center, int size, int part, bool rotate)
        {
            //Обчислення меж навколо корабля з урахуванням повороту

            string[] parts = center.Split('_');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            row = rotate ? row + part - 1 : row;
            col = rotate ? col : col - part + 1;

            int startRow = Math.Max(0, rotate ? row - size : row - 1);
            int endRow = Math.Min(9, rotate ? row + 1 : row + 1);
            int startCol = Math.Max(0, rotate ? col - 1 : col - 1);
            int endCol = Math.Min(9, rotate ? col + 1 : col + size);

            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = startCol; c <= endCol; c++)
                {
                    string key = $"{r}_{c}";
                    if (cells.ContainsKey(key) && cells[key].status != "miss" && !cells[key].status.StartsWith("d"))
                    {
                        cells[key].status = "miss";
                        cells[key].shipPart = "0";
                        UpdateImage(key, new Bitmap(path + "/miss.png"), false);
                    }
                }
            }
        }

        //Перевірка, чи всі кораблі знищені, та завершення гри
        private void checkShips()
        {
            int count = 0;
            foreach (var data in destroyedShips)
                if (int.Parse(data.Key.Substring(0, 1)) == data.Value)
                    count++;

            if (count == 10)
            {
                try
                {
                    //Завершуємо процеси, закриваємо форми
                    this.Invoke(() =>
                    {
                        comp.Kill();
                        attack.Kill();
                        form1.programClose = true;
                        form1.Close();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка показу форми програшу: " + ex.Message);
                }
            }
        }

    }
}
