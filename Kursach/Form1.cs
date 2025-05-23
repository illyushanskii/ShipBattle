using System.Collections.Concurrent;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Windows.Forms;

namespace Kursach
{
    public partial class Form1 : Form
    {
        const int CELL_SIZE = 50;
        bool dragging = false; //Чи триває перетягування

        //Словники для клітинок, кораблів, початкових позицій та даних клітинок
        Dictionary<string, PictureBox> pictureBoxes = new Dictionary<string, PictureBox>();
        Dictionary<string, PictureBox> ships = new Dictionary<string, PictureBox>();
        Dictionary<string, Point> defaultShipPoints = new Dictionary<string, Point>();
        Dictionary<string, Cell> cells = new Dictionary<string, Cell>();

        public bool programClose = false; //Прапорець для закриття програми

        //Координати для перетягування і межі поля
        Point dragCursorPoint;
        Point dragPictureBoxPoint;
        Point startField;
        Point endField;
        Point lastShipPoint = new Point(-1, -1);

        static string path = "D:/ДНУ ФФЕКС/Семестр 4/СП/Kursach/images"; //Шлях до зображень
        private readonly Bitmap emptyBitmap = new Bitmap(path + "/empty.png");
        private readonly Bitmap nearBitmap = new Bitmap(path + "/near.png");

        PictureBox movingShip; //Корабель, що перетягується
        string movingShipNum; //Збереження статусу корабля
        bool isRotate = false; //Чи повернутий корабель

        public Form1()
        {
            InitializeComponent();

            //Визначення меж поля
            startField = pictureBox0_0.Location;
            endField = new Point(pictureBox9_9.Location.X + CELL_SIZE, pictureBox9_9.Location.Y + CELL_SIZE);

            //Ініціалізація клітинок поля
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    PictureBox found = (PictureBox)this.Controls.Find($"pictureBox{i}_{j}", true).FirstOrDefault();
                    cells.Add($"{i}_{j}", new Cell(found.Location));
                    pictureBoxes[$"{i}_{j}"] = found;

                    //Прив'язка обробників подій для клітинок
                    found.MouseDown += field_MouseDown;
                    found.MouseMove += field_MouseMove;
                    found.MouseUp += field_MouseUp;
                }
            }

            //Ініціалізація кораблів і їх обробників
            int k = 4;
            for (int i = 1; i <= 4; i++)
            {
                for (int j = 1; j <= k; j++)
                {
                    PictureBox found = (PictureBox)this.Controls.Find($"ship{i}_{j}", true).FirstOrDefault();
                    ships.Add($"{i}_{j}", found);
                    defaultShipPoints.Add($"{i}_{j}", found.Location);

                    found.MouseDown += ship_MouseDown;
                    found.MouseMove += ship_MouseMove;
                    found.MouseUp += ship_MouseUp;
                }
                k--;
            }
        }


        //Метод перемішення кораблів по формі
        private void shipMove()
        {
            if (dragging && movingShip != null)
            {
                //Вичеслення шагу переміщення 5 пк.
                Point shipCenter = new Point(movingShip.Location.X + 25, movingShip.Location.Y + 25);
                int dx = Cursor.Position.X - dragCursorPoint.X;
                int dy = Cursor.Position.Y - dragCursorPoint.Y;

                dx = (dx / 5) * 5;
                dy = (dy / 5) * 5;

                Point newLocation = new Point(dragPictureBoxPoint.X + dx, dragPictureBoxPoint.Y + dy);

                if (newLocation == movingShip.Location)
                    return;

                movingShip.Location = newLocation;
                //Перевірка - корабель знаходиться в ігровому полі
                if (movingShip.Left >= startField.X && movingShip.Right <= endField.X && movingShip.Top >= startField.Y && movingShip.Bottom <= endField.Y)
                {
                    movingShip.Visible = false;
                    int size = Convert.ToInt32(movingShipNum.Substring(0, 1));
                    foreach (KeyValuePair<string, Cell> data in cells)
                    {   //Шукаємо на яку клітку попав корабель
                        int X = Math.Abs(data.Value.middle.X - shipCenter.X);
                        int Y = Math.Abs(data.Value.middle.Y - shipCenter.Y);
                        if (X <= 25 && Y <= 25)
                        {

                            if (lastShipPoint.X > -1 && lastShipPoint.Y > -1)
                            {
                                //Очистка старого розміщення корабля
                                ClearLastShipZone(lastShipPoint, size, isRotate);
                            }

                            string[] parts = data.Key.Split('_');
                            int row = int.Parse(parts[0]);
                            int col = int.Parse(parts[1]);
                            lastShipPoint = new Point(col, row);
                            //Перевірка щоб кораль не потрапив в зайняті зони іншими кораблями
                            if (!CanPlaceShip(row, col, size, isRotate))
                            {
                                movingShip.Visible = true;
                                return;
                            }

                            //Малюємо корабель
                            shipDrawing(isRotate, row, col, size);

                            //Вираховуємо периметр зони біля корабля
                            int startRow = isRotate ? Math.Max(0, row - 1) : Math.Max(0, row - 1);
                            int endRow = isRotate ? Math.Min(9, row + size) : Math.Min(9, row + 1);

                            int startCol = isRotate ? Math.Max(0, col - 1) : Math.Max(0, col - 1);
                            int endCol = isRotate ? Math.Min(9, col + 1) : Math.Min(9, col + size);

                            //Малюємо цю зону
                            nearDrawing(startRow, endRow, startCol, endCol);
                        }
                    }
                }
                else
                {
                    //Якщо корабль за межами ігрового поля витираємо корабель
                    movingShip.Visible = true;
                    ClearLastShipZone(lastShipPoint, Convert.ToInt32(movingShipNum.Substring(0, 1)), isRotate);
                }
            }
        }
        //Малює корабель на полі
        private void shipDrawing(bool rotate, int row, int col, int size)
        {
            for (int i = 0; i < size; i++)
            {
                //Визначаємо координати для кожної частини корабля
                int r = rotate ? row + i : row;
                int c = rotate ? col : col + i;
                string key = $"{r}_{c}";

                if (cells.ContainsKey(key))
                {
                    //Визначаємо номер частини корабля (для зображення)
                    int part = rotate ? size - i : i + 1;
                    cells[key].status = movingShipNum; //Присвоюємо статус корабля
                    cells[key].shipPart = $"{size}_{part}"; //Зберігаємо розмір і частину
                    cells[key].rotate = rotate; //Пам’ятаємо орієнтацію

                    if (pictureBoxes.TryGetValue(key, out PictureBox found))
                    {
                        //Встановлюємо відповідне зображення
                        this.Invoke((MethodInvoker)(() =>
                        {
                            found.Image = new Bitmap(path + $"/{cells[key].shipPart}.png");
                            if (rotate)
                                found.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        }));
                    }
                }
            }
        }

        //Помічає клітинки навколо корабля як "біля корабля"
        private void nearDrawing(int startRow, int endRow, int startCol, int endCol)
        {
            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = startCol; c <= endCol; c++)
                {
                    string key = $"{r}_{c}";

                    //Якщо клітинка порожня — ставимо позначку "біля корабля"
                    if (cells.ContainsKey(key) && cells[key].status == "empty")
                    {
                        cells[key].status = $"n_{movingShipNum}";
                    }
                    //Якщо вже є позначка іншого корабля — просто позначаємо "near"
                    else if (cells.ContainsKey(key) && cells[key].status.StartsWith("n_"))
                    {
                        cells[key].status = "near";
                    }
                }
            }
        }

        //Перевіряє, чи можна поставити корабель у вказаній позиції
        private bool CanPlaceShip(int row, int col, int size, bool rotated)
        {
            for (int i = 0; i < size; i++)
            {
                //Визначаємо ключ клітинки
                string key = rotated ? $"{row + i}_{col}" : $"{row}_{col + i}";

                //Якщо клітинки не існує — не можна ставити
                if (!cells.ContainsKey(key))
                    return false;

                string status = cells[key].status;

                //Якщо клітинка зайнята іншим кораблем
                if (status != "empty" && status != movingShipNum)
                    return false;

                //Якщо клітинка біля іншого корабля
                if (status.StartsWith("n_") && status != $"n_{movingShipNum}")
                    return false;
            }

            return true;
        }

        //Видаляє корабель і сусідні клітинки
        private void ClearLastShipZone(Point shipPoint, int size, bool rotate)
        {
            int row = shipPoint.Y;
            int col = shipPoint.X;

            int startRow = Math.Max(0, row - size);
            int endRow = rotate ? Math.Min(9, row + size) : Math.Min(9, row + 1);
            int startCol = Math.Max(0, col - 1);
            int endCol = rotate ? Math.Min(9, col + 1) : Math.Min(9, col + size);
            ConcurrentBag<string> updatedKeys = new();

            //Використовуємо Parallel.For для прискорення очищення великої області
            Parallel.For(startRow, endRow + 1, r =>
            {
                for (int c = startCol; c <= endCol; c++)
                {
                    string key = $"{r}_{c}";
                    if (!cells.TryGetValue(key, out var cell)) continue;

                    if (cell.status == movingShipNum || cell.status == $"n_{movingShipNum}")
                    {
                        cell.status = "empty";
                        cell.shipPart = "0";
                        cell.rotate = false;
                        updatedKeys.Add(key);
                    }
                }
            });

            foreach (var key in updatedKeys)
            {
                if (pictureBoxes.TryGetValue(key, out PictureBox found))
                {
                    //Invoke використовується для доступу до графічних елементів
                    found.Invoke(() => found.Image = emptyBitmap);
                }
            }

            DeleteTotalNear();
        }

        //Очищення зайвих near-клітинок
        private void DeleteTotalNear()
        {
            // Список клітинок, які потрібно очистити
            List<string> toClear = new List<string>();

            // Список клітинок, яким треба присвоїти статус "n_..."
            List<string> toUpdateToNear = new List<string>();

            // Словник для оновлення статусів біля кораблів
            Dictionary<string, string> nearStatuses = new Dictionary<string, string>();

            // Обходимо всі клітинки на полі
            foreach (var kv in cells)
            {
                string key = kv.Key;
                var cell = kv.Value;

                // Працюємо тільки з клітинками, які мають статус "near"
                if (cell.status != "near") continue;

                // Визначаємо координати клітинки
                int sep = key.IndexOf('_');
                int row = int.Parse(key[..sep]);
                int col = int.Parse(key[(sep + 1)..]);

                // Збираємо всі унікальні статуси кораблів, які оточують цю клітинку
                HashSet<string> nearbyShips = new HashSet<string>();

                // Перевіряємо клітинки навколо поточної
                for (int r = row - 1; r <= row + 1; r++)
                {
                    for (int c = col - 1; c <= col + 1; c++)
                    {
                        // Пропускаємо саму клітинку
                        if (r == row && c == col) continue;

                        // Пропускаємо вихід за межі поля
                        if (r < 0 || r > 9 || c < 0 || c > 9) continue;

                        string neighborKey = $"{r}_{c}";

                        // Перевіряємо, чи існує сусідня клітинка
                        if (!cells.TryGetValue(neighborKey, out var neighbor)) continue;

                        string status = neighbor.status;

                        // Якщо клітинка — частина корабля (але не "empty", "near", "n_...")
                        if (!string.IsNullOrEmpty(status) &&
                            status != "empty" &&
                            status != "near" &&
                            !status.StartsWith("n_"))
                        {
                            nearbyShips.Add(status);
                        }
                    }
                }

                // Якщо поряд лише один корабель, додаємо прив'язку до нього
                if (nearbyShips.Count == 1)
                {
                    string shipStatus = nearbyShips.First();
                    cell.status = $"n_{shipStatus}";
                    nearStatuses[key] = $"n_{shipStatus}";
                    toUpdateToNear.Add(key);
                }
                // Якщо поряд нічого — очищаємо клітинку
                else if (nearbyShips.Count == 0)
                {
                    cell.status = "empty";
                    cell.shipPart = "0";
                    toClear.Add(key);
                }
            }

            // Оновлюємо візуально клітинки, які стали "біля корабля"
            foreach (var key in toUpdateToNear)
            {
                if (pictureBoxes.TryGetValue(key, out PictureBox found))
                {
                    // Використовуємо Invoke для оновлення зображення з потоку UI
                    found.Invoke(() => found.Image = nearBitmap);
                }
            }

            // Очищаємо клітинки, які стали порожніми
            foreach (var key in toClear)
            {
                if (pictureBoxes.TryGetValue(key, out PictureBox found))
                {
                    found.Invoke(() => found.Image = emptyBitmap);
                }
            }
        }


        //Метод для перевірки, чи є хоча б один корабель на полі
        private bool oneShipInField()
        {
            foreach (var data in ships)
            {
                if (!data.Value.Visible)
                {
                    return true; //Якщо хоча б один корабель не видно, він на полі
                }
            }
            return false; //Всі кораблі на місці
        }

        //Метод для відображення меж кораблів
        private void showBorders()
        {
            if (oneShipInField())
            {
                foreach (var data in cells)
                {
                    string check = data.Value.status.Substring(0, 1);
                    if (data.Value.status.StartsWith("n_") || data.Value.status == "near")
                    {
                        if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                            found.Image = nearBitmap; //Відображаємо зображення для сусідніх клітинок
                    }
                    else if (Int32.TryParse(check, out int num))
                    {
                        if (data.Value.rotate)
                        {
                            if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                                found.Image = new Bitmap(path + $"/nr_{data.Value.shipPart}.png"); //Зображення для вертикального корабля
                        }
                        else
                        {
                            if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                                found.Image = new Bitmap(path + $"/n_{data.Value.shipPart}.png"); //Зображення для горизонтального корабля
                        }
                    }
                }
            }
        }
        //Метод для приховування заборонених зон на всіх клітинках поля
        private void hideBorders()
        {
            //Прибираємо обрамлення навколо клітинок кораблів і зон "біля"
            foreach (var data in cells)
            {
                string check = data.Value.status.Substring(0, 1);
                if (data.Value.status.StartsWith("n_") || data.Value.status == "near")
                {
                    if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                        found.Image = emptyBitmap; //Ставимо порожнє зображення
                }
                else if (Int32.TryParse(check, out int num))
                {
                    if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                    {
                        //Відновлюємо зображення частини корабля
                        found.Image = new Bitmap(path + $"/{data.Value.shipPart}.png");

                        //Якщо корабель повернутий — повертаємо картинку
                        if (data.Value.rotate)
                            found.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                }
            }
        }

        private void hideBorders(string shipNum)
        {
            //Прибираємо обрамлення для конкретного корабля по номеру
            foreach (var data in cells)
            {
                if (data.Value.status == $"n_{shipNum}")
                {
                    if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                        found.Image = emptyBitmap;
                }
                else if (data.Value.status == shipNum)
                {
                    if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                    {
                        found.Image = new Bitmap(path + $"/{data.Value.shipPart}.png");
                        if (data.Value.rotate)
                            found.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                }
            }
        }

        private void returnDefaultPosition()
        {
            //Повертаємо корабель на початкову позицію, якщо він видимий і рухається
            if (movingShip != null && movingShip.Visible == true)
            {
                foreach (var data in defaultShipPoints)
                {
                    if (movingShipNum == data.Key)
                    {
                        movingShip.Location = data.Value;

                        //Якщо корабель повернутий, міняємо розмір і зображення
                        if (movingShip.Size.Width < movingShip.Size.Height || movingShip.Size.Width == movingShip.Size.Height)
                        {
                            movingShip.Size = new Size(movingShip.Size.Height, movingShip.Size.Width);
                            movingShip.Image = new Bitmap(path + $"/{movingShipNum.Substring(0, 1)}.png");
                            isRotate = false;
                            return;
                        }
                    }
                }
            }
        }

        //Обробники руху корабля мишею
        private void ship_MouseMove(object sender, MouseEventArgs e)
        {
            shipMove();
        }
        private void ship_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox ship)
            {
                dragging = true; //Початок перетягування
                dragCursorPoint = Cursor.Position;
                dragPictureBoxPoint = ship.Location;
                movingShip = ship;
                isRotate = ship.Size.Width < ship.Size.Height ? true : false;
                movingShipNum = ship.Name.Replace("ship", "");
                ship.BringToFront();
                showBorders(); //Показати обрамлення навколо корабля
            }
        }
        private void ship_MouseUp(object sender, MouseEventArgs e)
        {
            returnDefaultPosition(); //Повернути корабель на місце, якщо треба
            hideBorders();           //Приховати обрамлення
            playButton.Enabled = AllShipInField(); //Активувати кнопку гри, якщо всі кораблі на полі
            dragging = false;        //Завершення перетягування
        }

        //Таймер для визначення довгого натискання
        private System.Windows.Forms.Timer holdTimer;
        private bool isLongPress = false;

        private void field_MouseDown(object sender, MouseEventArgs e)
        {
            //Запускаємо таймер для довгого натискання на клітинці поля
            isLongPress = false;
            holdTimer = new System.Windows.Forms.Timer();
            holdTimer.Interval = 150;
            holdTimer.Tick += (s, args) =>
            {
                holdTimer.Stop();
                isLongPress = true;

                if (sender is PictureBox pb)
                {
                    string key = pb.Name.Replace("pictureBox", "");
                    if (cells.ContainsKey(key))
                    {
                        string status = cells[key].status;

                        //Якщо в клітинці є корабель, починаємо перетягування
                        if (!string.IsNullOrEmpty(status) && int.TryParse(status[0].ToString(), out int size))
                        {
                            isRotate = cells[key].rotate;
                            movingShipNum = status;
                            movingShip = ships[movingShipNum];
                            lastShipPoint = LastShipPoint(key, cells[key].shipPart, cells[key].rotate);

                            if (movingShip != null)
                            {
                                dragCursorPoint = Cursor.Position;
                                dragPictureBoxPoint = movingShip.Location;
                                showBorders();
                                hideBorders(status);
                                dragging = true;
                            }
                        }
                    }
                }
            };
            holdTimer.Start();
        }

        private void field_MouseMove(object sender, MouseEventArgs e)
        {
            shipMove(); //Рух корабля під час перетягування мишкою
        }

        private void field_MouseUp(object sender, MouseEventArgs e)
        {
            holdTimer?.Stop();

            if (!isLongPress)
            {
                //Якщо коротке натискання — обертання корабля
                isLongPress = true;
                if (sender is PictureBox pb)
                {
                    string key = pb.Name.Replace("pictureBox", "");
                    RotateShip(key);
                    return;
                }
            }
            else
            {
                //Завершення перетягування — повернення корабля і оновлення інтерфейсу
                returnDefaultPosition();
                hideBorders();
                playButton.Enabled = AllShipInField();
                dragging = false;
            }
        }

        private Point LastShipPoint(string key, string shipPart, bool rotate)
        {
            //Обчислюємо останню точку корабля на полі в залежності від повороту
            string[] parts = shipPart.Split("_");
            int size = int.Parse(parts[0]);
            int part = int.Parse(parts[1]) - 1;

            parts = key.Split("_");
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            if (!rotate)
                col -= part;
            else
                row += part;

            return new Point(col, row);
        }

        //Метод для обертання корабля на полі
        private void RotateShip(string key)
        {
            //Отримуємо поточний стан обертання і розмір корабля
            bool rotate = cells[key].rotate;
            string status = cells[key].status;
            movingShipNum = status;
            if (!int.TryParse(status.Substring(0, 1), out int size))
                return;

            //Розбираємо позицію та індекс частини корабля
            string[] inxs = key.Split('_');
            int row = int.Parse(inxs[0]);
            int col = int.Parse(inxs[1]);
            string[] info = cells[key].shipPart.Split('_');
            int partIndex = int.Parse(info[1]) - 1;

            //Очищуємо стару позицію корабля на полі
            if (!rotate)
            {
                col -= partIndex;
                ClearLastShipZone(new Point(col, row), size, rotate);
            }
            else
            {
                row += partIndex;
                ClearLastShipZone(new Point(col, row - size + 1), size, rotate);
            }

            string start = $"{row}_{col}";
            int Y = cells[start].start.Y - ((size - 1) * CELL_SIZE);

            //Перевіряємо, чи можна розмістити корабель у новій орієнтації
            bool canPlace = !rotate ? CanPlaceShip(row - size + 1, col, size, !rotate) : CanPlaceShip(row, col, size, !rotate);
            if (!canPlace)
            {
                //Якщо не можна — відновлюємо старий стан і оновлюємо вигляд
                if (ships.TryGetValue(status, out PictureBox found))
                {
                    found.Size = new Size(found.Height, found.Width);
                    found.Visible = true;
                    if (!rotate)
                    {
                        found.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        found.Location = new Point(cells[start].start.X, Y);
                    }
                    else
                    {
                        found.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        found.Location = cells[start].start;
                    }
                    isRotate = !rotate;
                    found.Refresh();
                    showBorders();
                }
                return;
            }

            //Розміщуємо корабель у новій орієнтації — оновлюємо статус і картинки клітинок
            if (!rotate)
            {
                for (int i = 0; i < size; i++)
                {
                    string newKey = $"{row - i}_{col}";
                    if (!cells.ContainsKey(newKey)) return;
                    cells[newKey].rotate = true;
                    cells[newKey].status = status;
                    cells[newKey].shipPart = $"{size}_{i + 1}";
                    if (pictureBoxes.TryGetValue(newKey, out PictureBox found))
                    {
                        Bitmap original = new Bitmap(path + $"/{size}_{i + 1}.png");
                        original.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        found.Image = original;
                    }
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    string newKey = $"{row}_{col + i}";
                    if (!cells.ContainsKey(newKey)) return;
                    cells[newKey].rotate = false;
                    cells[newKey].status = status;
                    cells[newKey].shipPart = $"{size}_{i + 1}";
                    if (pictureBoxes.TryGetValue(newKey, out PictureBox found))
                    {
                        Bitmap original = new Bitmap(path + $"/{size}_{i + 1}.png");
                        found.Image = original;
                    }
                }
            }

            //Оновлюємо зону навколо корабля
            int startRow = !rotate ? Math.Max(0, row - size) : Math.Max(0, row - 1);
            int endRow = Math.Min(9, row + 1);
            int startCol = Math.Max(0, col - 1);
            int endCol = !rotate ? Math.Min(9, col + 1) : Math.Min(9, col + size);
            nearDrawing(startRow, endRow, startCol, endCol);

            //Оновлюємо вигляд самого корабля після обертання
            if (ships.TryGetValue(key, out PictureBox found1))
            {
                found1.Size = new Size(found1.Height, found1.Width);
                if (!rotate)
                {
                    found1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    found1.Location = new Point(cells[start].start.X, Y);
                }
                else
                {
                    found1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    found1.Location = cells[start].start;
                }
            }

            hideBorders();
        }

        //Обробник кнопки "Перемішати" — випадково розставляє кораблі на полі
        private void shuffleButton_Click(object sender, EventArgs e)
        {
            RandShipsPlace();
        }

        private void RandShipsPlace()
        {
            clearCells();               //Очищаємо поле
            playButton.Enabled = true;  //Активуємо кнопку "Грати"
            Random rand = new Random();

            List<string> shipsArr = new List<string>();
            List<string> dUseCells = new List<string>();

            //Заповнюємо список всіх клітинок (для перевірки вільних)
            for (int i = 0; i < 10; i++)
            {
                for (int b = 0; b < 10; b++)
                {
                    string sh = $"{i}_{b}";
                    dUseCells.Add(sh);
                    if (ships.TryGetValue(sh, out PictureBox found))
                    {
                        found.Size = new Size(i * CELL_SIZE, CELL_SIZE);
                        found.Image = new Bitmap(path + $"/{i}.png");
                    }
                }
            }

            //Формуємо список кораблів (4 палубні до 1 палубних)
            int j = 0;
            for (int i = 4; i > 0; i--)
            {
                j++;
                for (int k = j; k > 0; k--)
                    shipsArr.Add($"{i}_{k}");
            }

            //Розставляємо кожен корабель випадково, перевіряючи вільне місце
            foreach (string sh in shipsArr)
            {
                bool rotate = rand.Next(0, 2) == 0;
                int size = int.Parse(sh.Substring(0, 1));

                int minRow = rotate ? size - 1 : 0;
                int maxRow = 9;
                int minCol = 0;
                int maxCol = rotate ? 9 : 10 - size;

                int row = 0, col = 0;
                bool keyIsOk = false;

                while (!keyIsOk)
                {
                    row = rand.Next(minRow, maxRow + 1);
                    col = rand.Next(minCol, maxCol + 1);

                    bool allFree = true;
                    for (int i = 0; i < size; i++)
                    {
                        string key = rotate ? $"{row - i}_{col}" : $"{row}_{col + i}";
                        if (!dUseCells.Contains(key))
                        {
                            allFree = false;
                            break;
                        }
                    }
                    keyIsOk = allFree;
                }

                //Записуємо позиції корабля в клітинки і ставимо картинки
                for (int i = 0; i < size; i++)
                {
                    string newKey = rotate ? $"{row - i}_{col}" : $"{row}_{col + i}";
                    cells[newKey].rotate = rotate;
                    cells[newKey].status = sh;
                    cells[newKey].shipPart = $"{size}_{i + 1}";
                    dUseCells.Remove(newKey);

                    if (pictureBoxes.TryGetValue(newKey, out PictureBox found))
                    {
                        Bitmap original = new Bitmap(path + $"/{size}_{i + 1}.png");
                        if (rotate)
                            original.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        found.Image = original;
                    }
                }

                //Оновлюємо зону навколо корабля як "near"
                int startRow = rotate ? row - size : row - 1;
                int endRow = row + 1;
                int startCol = col - 1;
                int endCol = rotate ? col + 1 : col + size;

                startRow = Math.Max(0, startRow);
                endRow = Math.Min(9, endRow);
                startCol = Math.Max(0, startCol);
                endCol = Math.Min(9, endCol);

                for (int r = startRow; r <= endRow; r++)
                {
                    for (int c = startCol; c <= endCol; c++)
                    {
                        string key1 = $"{r}_{c}";
                        if (cells.ContainsKey(key1) && cells[key1].status == "empty")
                            cells[key1].status = $"n_{sh}";
                        else if (cells.ContainsKey(key1) && cells[key1].status.StartsWith("n_"))
                            cells[key1].status = "near";
                        dUseCells.Remove(key1);
                    }
                }

                //Оновлюємо вигляд самого корабля (приховуємо PictureBox)
                string start = $"{row}_{col}";
                if (ships.TryGetValue(sh, out PictureBox found1))
                {
                    found1.Visible = false;
                    int Y = cells[start].start.Y - ((size - 1) * CELL_SIZE);
                    if (rotate)
                    {
                        found1.Size = new Size(found1.Height, found1.Width);
                        found1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        found1.Location = new Point(cells[start].start.X, Y);
                    }
                    else
                    {
                        found1.Location = cells[start].start;
                    }
                    found1.Refresh();
                }
            }
        }

        //Очищає всі клітинки поля: скидає статус, частину корабля і орієнтацію, ставить пусту картинку
        private void clearCells()
        {
            foreach (var cell in cells)
            {
                cell.Value.shipPart = "0";
                cell.Value.status = "empty";
                cell.Value.rotate = false;

                if (pictureBoxes.TryGetValue(cell.Key, out PictureBox found))
                {
                    found.Image = new Bitmap(path + $"/empty.png");
                }
            }
        }

        //Обробник кнопки "Очистити" — скидає поле, активує/деактивує кнопки, повертає кораблі на стартові позиції
        private void clearButton_Click(object sender, EventArgs e)
        {
            playButton.Enabled = false;  //Деактивує кнопку "Грати"
            clearCells();                //Очищаємо поле
            for (int i = 0; i < 10; i++)
            {
                for (int b = 0; b < 10; b++)
                {
                    string sh = $"{i}_{b}";
                    if (ships.TryGetValue(sh, out PictureBox found))
                    {
                        found.Visible = true;                     //Показуємо корабель
                        found.Size = new Size(i * CELL_SIZE, CELL_SIZE);       //Встановлюємо розмір за кількістю палуб
                        found.Image = new Bitmap(path + $"/{i}.png"); //Встановлюємо картинку корабля
                        found.Location = defaultShipPoints[sh]; //Повертаємо на стартову позицію
                    }
                }
            }
        }

        //Зміна фону кнопки "Перемішати" при наведенні курсору — повертаємо білий колір при виході курсору
        private void shuffleButton_MouseLeave(object sender, EventArgs e)
        {
            shuffleButton.BackColor = Color.White;
        }

        //Зміна фону кнопки "Перемішати" при наведенні курсору — підсвічування
        private void shuffleButton_MouseEnter(object sender, EventArgs e)
        {
            shuffleButton.BackColor = Color.FromArgb(207, 207, 244);
        }

        //Підсвічування кнопки "Очистити" при наведенні
        private void clearButton_MouseEnter(object sender, EventArgs e)
        {
            clearButton.BackColor = Color.FromArgb(207, 207, 244);
        }

        //Повернення кольору кнопки "Очистити" при виході курсору
        private void clearButton_MouseLeave(object sender, EventArgs e)
        {
            clearButton.BackColor = Color.White;
        }

        //Підсвічування кнопки "Грати" при наведенні
        private void playButton_MouseEnter(object sender, EventArgs e)
        {
            playButton.BackColor = Color.FromArgb(207, 207, 244);
        }

        //Повернення кольору кнопки "Грати" при виході курсору
        private void playButton_MouseLeave(object sender, EventArgs e)
        {
            playButton.BackColor = Color.White;
        }

        //Перевіряє, чи всі 10 кораблів розміщені на полі (невидимі — значить на полі)
        private bool AllShipInField()
        {
            int count = 0;
            foreach (var sh in ships)
            {
                if (!sh.Value.Visible)
                    count++;
            }
            return count == 10;
        }

        //Обробник кнопки "Грати" — ховає поточну форму, відкриває форму клієнта гри
        private void playButton_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            Client form = new Client(this, cells);
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 150);
            form.ShowDialog();
        }

        //Обробник закриття форми — гарантує повне завершення програми, якщо не планувалося закриття інакше
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!programClose)
            {
                Application.Exit();
                Environment.Exit(0);
            }
        }

    }
}