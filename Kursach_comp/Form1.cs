using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursach_comp
{
    public partial class Form1 : Form
    {
        //Глобальні семафори для синхронізації між процесами
        Semaphore semAttack = new Semaphore(0, 1, "Global\\SemAttack");
        Semaphore semComp = new Semaphore(0, 1, "Global\\SemComp");
        Semaphore semPlayer = new Semaphore(0, 1, "Global\\SemPlayer");

        //Шлях до зображень
        string path = "D:/ДНУ ФФЕКС/Семестр 4/СП/Kursach/images";

        //Для збереження стану атак
        string oldAttack = "";
        string nextAttack = "";

        //Словники для доступу до клітинок, пікчербоксів, клітинок користувача, знищених кораблів
        Dictionary<string, PictureBox> pictureBoxes = new();
        Dictionary<string, Cell> cells = new();
        static Dictionary<string, Cell> userCells = new();
        Dictionary<string, int> destroyedShips = new();

        //Логіка атаки для бота
        SmartAttack smartAttack = new(userCells);
        Random rand = new();

        public Form1()
        {
            InitializeComponent();

            //Розташування форми на екрані
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.FromControl(this).Bounds.Width - this.Size.Width - 5, 150);

            //Ініціалізація клітинок
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                {
                    PictureBox pb = (PictureBox)this.Controls.Find($"client{i}_{j}", true)[0];
                    pictureBoxes[$"{i}_{j}"] = pb;
                    cells[$"{i}_{j}"] = new Cell();
                    userCells[$"{i}_{j}"] = new Cell();
                }

            //Розставлення кораблів суперника
            RandShipsPlace();
            Task.Run(() => GameLoop());
        }

        private void GameLoop()
        {
            while (true)
            {
                //Очікування сигналу від іншого процесу (гравця), що він зробив атаку
                semComp.WaitOne();

                //Зчитування координат атаки гравця через DataTransfer
                var data = new DataTransfer("Attack");
                string userAttack = data.Read();
                data.Write("*"); //Підтвердження прийому

                //Якщо отримана атака — координати, а не службовий символ
                if (!userAttack.StartsWith("*"))
                {
                    //Оновлення кольору — працюємо через Invoke(), оскільки виконуємо з фону
                    Invoke(() => label1.ForeColor = Color.Red);
                    labelStatus.Text = "Атака гравця -> " + userAttack;

                    string status = cells[userAttack].status;
                    bool rotate = cells[userAttack].rotate;

                    if (int.TryParse(status.Substring(0, 1), out int size)) //Влучання по кораблю
                    {
                        cells[userAttack].status = "d_" + status;

                        string[] parts = cells[userAttack].shipPart.Split('_');
                        int shipPart = int.Parse(parts[1]);

                        //Зміна картинки для PictureBox — лише в головному потоці
                        if (pictureBoxes.TryGetValue(userAttack, out PictureBox found))
                        {
                            Bitmap bmp = new(path + $"/d_{size}_{shipPart}.png");
                            if (rotate) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            Invoke(() => found.Image = bmp); //Безпечне оновлення зображення
                            found.Refresh();
                        }

                        //Підрахунок попадань по одному кораблю
                        if (!destroyedShips.ContainsKey(status))
                            destroyedShips[status] = 1;
                        else
                            destroyedShips[status]++;

                        string rot = rotate ? "1" : "0";
                        data = new DataTransfer("AttackResult");

                        if (destroyedShips[status] == size) //Корабель знищено
                        {
                            nearDrawing(userAttack, size, shipPart, rotate, true, cells);
                            if (data.Read() == "*")
                                data.Write($"++_{size}_{status.Substring(2, 1)}_{shipPart}_{rot}");
                        }
                        else //Тільки поранення
                        {
                            if (data.Read() == "*")
                                data.Write($"+_{size}_{status.Substring(2, 1)}_{shipPart}_{rot}");
                        }

                        semAttack.Release(); //Дозвіл на наступну атаку гравця або бота
                    }
                    else //Промах
                    {
                        cells[userAttack].status = "miss";

                        if (pictureBoxes.TryGetValue(userAttack, out PictureBox found))
                        {
                            //Безпечне оновлення зображення через Invoke()
                            Invoke(() =>
                            {
                                found.Image = new Bitmap(path + "/miss.png");
                                found.Refresh();
                                Application.DoEvents();
                            });
                        }

                        data = new DataTransfer("AttackResult");
                        data.Write("-");
                        semAttack.Release(); //Дозвіл на наступну дію
                    }

                    checkShips(); //Чи переміг гравець
                }
                else //Бот отримує результат своєї атаки
                {
                    Invoke(() => label1.ForeColor = Color.LimeGreen);

                    data = new DataTransfer("AttackResult");
                    string result = data.Read(); //Читаємо результат
                    data.Write("*"); //Підтвердження

                    if (result.StartsWith("++")) //Корабель знищено
                    {
                        userCells[oldAttack].status = "d";

                        string[] parts = result.Split('_');
                        int size = int.Parse(parts[1]);
                        int shipPart = int.Parse(parts[2]);
                        bool rotate = parts[3] == "1";

                        nearDrawing(oldAttack, size, shipPart, rotate, false, userCells);
                        oldAttack = "";
                        nextAttack = "";
                        smartAttack.RegisterKill(); //Очищення внутрішнього стану ШІ

                        GenerateRandomAttack(); //Наступна атака випадково
                    }
                    else if (result.StartsWith("+")) //Поранено
                    {
                        userCells[oldAttack].status = "d";

                        //Визначення, який із ходів був вдалим
                        if (nextAttack == "")
                            smartAttack.RegisterHit(oldAttack);
                        else
                            smartAttack.RegisterHit(nextAttack);

                        nextAttack = smartAttack.GetNextTarget(); //ШІ обирає наступну ціль
                        oldAttack = nextAttack;

                        data = new DataTransfer("Attack");
                        if (data.Read() == "*") data.Write(nextAttack);

                        labelStatus.Text = "Бот продовжує атаку -> " + nextAttack;
                        Thread.Sleep(1000);//Затримка на секунду, щоб не було моментальної атаки
                        semPlayer.Release(); //Гравець тепер обробляє атаку
                    }
                    else if (result.StartsWith("-")) //Промах
                    {
                        userCells[oldAttack].status = "miss";
                        oldAttack = "";
                        smartAttack.RegisterMiss(); //ШІ фіксує промах
                        Invoke(() => label1.ForeColor = Color.Red); //Зміна кольору через Invoke()
                        semAttack.Release(); //Передаємо хід гравцеві
                    }
                    else if (result.StartsWith("*")) //Потрібна ще одна атака від бота
                    {
                        if (nextAttack != "")
                        {
                            nextAttack = smartAttack.GetNextTarget();
                            userCells[nextAttack].status = "miss";
                            oldAttack = nextAttack;

                            data = new DataTransfer("Attack");
                            if (data.Read() == "*")
                                data.Write(nextAttack);

                            labelStatus.Text = "Бот продовжує атаку -> " + nextAttack;
                            Thread.Sleep(1000);//Затримка на секунду, щоб не було моментальної атаки
                            semPlayer.Release(); //Повідомляємо гравця про атаку
                        }
                        else
                        {
                            GenerateRandomAttack(); //Бот атакує навмання
                        }
                    }
                }
            }
        }

        //Генерація випадкової атаки для бота
        private void GenerateRandomAttack()
        {
            string target;
            while (true)
            {
                // Генеруємо випадкові координати (рядок і стовпець)
                int r = rand.Next(0, 10);
                int c = rand.Next(0, 10);
                target = $"{r}_{c}";

                // Якщо ця клітинка існує і ще не була атакована — виходимо з циклу
                if (userCells.ContainsKey(target) && userCells[target].status == "empty")
                    break;
            }

            // Позначаємо клітинку як промах
            userCells[target].status = "miss";
            oldAttack = target;

            // Створюємо об'єкт для обміну даними між процесами
            var data = new DataTransfer("Attack");

            // Якщо відповідь з іншого процесу — очікування ("*"), тоді надсилаємо координати атаки
            if (data.Read() == "*")
                data.Write(target);

            // Виводимо інформацію про атаку
            labelStatus.Text = "Випадкова атака бота -> " + target;

            // Затримка в 1 секунду, щоб атака не виглядала миттєвою
            Thread.Sleep(1000);

            // Оновлення інтерфейсу — зелений колір, щоб показати дію гравця
            Invoke(() => label1.ForeColor = Color.LimeGreen);

            // Дозволяємо гравцю зробити хід (семафор)
            semPlayer.Release();
        }

        // Позначити клітинки навколо знищеного корабля як "мимо"
        private void nearDrawing(string hit, int size, int part, bool rotate, bool getPicture, Dictionary<string, Cell> field)
        {
            // Розділяємо координати клітинки
            var parts = hit.Split('_');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            // Обчислюємо позицію першої частини корабля (в залежності від обертання)
            if (rotate)
                row += (part - 1);
            else
                col -= (part - 1);

            // Обчислюємо діапазон навколо корабля, який потрібно перевірити
            int startRow = Math.Max(0, rotate ? row - size : row - 1);
            int endRow = Math.Min(9, rotate ? row + 1 : row + 1);
            int startCol = Math.Max(0, rotate ? col - 1 : col - 1);
            int endCol = Math.Min(9, rotate ? col + 1 : col + size);

            try
            {
                // Проходимо по кожній навколишній клітинці
                for (int r = startRow; r <= endRow; r++)
                    for (int c = startCol; c <= endCol; c++)
                    {
                        string key = $"{r}_{c}";

                        // Якщо клітинка пуста, помічаємо її як "мимо"
                        if (field.ContainsKey(key) && field[key].status == "empty")
                        {
                            field[key].status = "miss";

                            // Якщо потрібно — оновлюємо зображення
                            if (getPicture && pictureBoxes.ContainsKey(key))
                            {
                                pictureBoxes[key].Invoke(() =>
                                {
                                    pictureBoxes[key].Image = new Bitmap(path + "/miss.png");
                                });
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                // Ігноруємо винятки (наприклад, якщо поле не існує)
                ex.ToString();
            }
        }

        // Перевірка, чи виграв гравець
        private void checkShips()
        {
            int count = 0;

            // Перевіряємо, чи всі частини кожного корабля були знищені
            foreach (var data in destroyedShips)
                if (int.Parse(data.Key.Substring(0, 1)) == data.Value)
                    count++;

            if (count == 10)
            {
                try
                {
                    // Через Invoke викликаємо зміну інтерфейсу (перехід до форми перемоги)
                    this.Invoke(() =>
                    {
                        // Закриваємо пов’язані процеси гри
                        string[] targets = { "Kursach_attack", "Kursach" };
                        foreach (var proc in Process.GetProcesses())
                        {
                            try
                            {
                                if (targets.Contains(proc.ProcessName))
                                    proc.Kill();
                            }
                            catch { /* Ігноруємо помилки завершення процесів */ }
                        }

                        // Приховуємо поточну форму та показуємо вікно перемоги
                        this.Visible = false;
                        new WinForm().Show();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка показу форми перемоги: " + ex.Message);
                }
            }
        }

        // Розстановка кораблів комп’ютера
        private void RandShipsPlace()
        {
            Random rand = new();
            List<string> dUseCells = new(); // Список доступних клітинок

            // Заповнюємо всі клітинки ігрового поля
            for (int i = 0; i < 10; i++)
                for (int k = 0; k < 10; k++)
                    dUseCells.Add($"{i}_{k}");

            List<string> shipsArr = new(); // Масив кораблів у вигляді рядків (розмір_частина)
            int j = 0;

            // Створення списку кораблів: 1x4, 2x3, 3x2, 4x1
            for (int i = 4; i > 0; i--)
            {
                j++;
                for (int k = j; k > 0; k--)
                    shipsArr.Add($"{i}_{k}");
            }

            foreach (string sh in shipsArr)
            {
                int size = int.Parse(sh.Substring(0, 1));
                bool rotate = rand.Next(2) == 0;
                int row, col;

                // Генеруємо координати до тих пір, поки всі частини корабля вміщуються в доступні клітинки
                do
                {
                    row = rotate ? rand.Next(size - 1, 10) : rand.Next(10);
                    col = rotate ? rand.Next(10) : rand.Next(0, 11 - size);
                }
                while (!Enumerable.Range(0, size).All(i =>
                    dUseCells.Contains(rotate ? $"{row - i}_{col}" : $"{row}_{col + i}")));

                // Розставляємо корабель по клітинках
                for (int i = 0; i < size; i++)
                {
                    string key = rotate ? $"{row - i}_{col}" : $"{row}_{col + i}";
                    cells[key].status = sh;
                    cells[key].rotate = rotate;
                    cells[key].shipPart = $"{size}_{i + 1}";
                    dUseCells.Remove(key);

                    // Встановлюємо відповідне зображення корабля
                    if (pictureBoxes.TryGetValue(key, out PictureBox pb))
                    {
                        Bitmap bmp = new(path + $"/{size}_{i + 1}.png");
                        if (rotate) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        pb.Image = bmp;
                    }
                }

                // Видаляємо навколишні клітинки з доступних — щоб кораблі не були поруч
                int sr = Math.Max(0, rotate ? row - size : row - 1);
                int er = Math.Min(9, rotate ? row + 1 : row + 1);
                int sc = Math.Max(0, rotate ? col - 1 : col - 1);
                int ec = Math.Min(9, rotate ? col + 1 : col + size);

                for (int r = sr; r <= er; r++)
                    for (int c = sc; c <= ec; c++)
                        dUseCells.Remove($"{r}_{c}");
            }
        }

        // Закриття форми — закриває пов'язані процеси гри
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Invoke(() =>
            {
                string[] targets = { "Kursach_attack", "Kursach" };
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (targets.Contains(proc.ProcessName))
                            proc.Kill();
                    }
                    catch { } // Ігнор помилок завершення процесів
                }

                Application.Exit(); // Вихід з додатку
            });
        }

    }
}
