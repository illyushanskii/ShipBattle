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
        bool dragging = false; //�� ����� �������������

        //�������� ��� �������, �������, ���������� ������� �� ����� �������
        Dictionary<string, PictureBox> pictureBoxes = new Dictionary<string, PictureBox>();
        Dictionary<string, PictureBox> ships = new Dictionary<string, PictureBox>();
        Dictionary<string, Point> defaultShipPoints = new Dictionary<string, Point>();
        Dictionary<string, Cell> cells = new Dictionary<string, Cell>();

        public bool programClose = false; //��������� ��� �������� ��������

        //���������� ��� ������������� � ��� ����
        Point dragCursorPoint;
        Point dragPictureBoxPoint;
        Point startField;
        Point endField;
        Point lastShipPoint = new Point(-1, -1);

        static string path = "D:/��� �����/������� 4/��/Kursach/images"; //���� �� ���������
        private readonly Bitmap emptyBitmap = new Bitmap(path + "/empty.png");
        private readonly Bitmap nearBitmap = new Bitmap(path + "/near.png");

        PictureBox movingShip; //��������, �� ������������
        string movingShipNum; //���������� ������� �������
        bool isRotate = false; //�� ���������� ��������

        public Form1()
        {
            InitializeComponent();

            //���������� ��� ����
            startField = pictureBox0_0.Location;
            endField = new Point(pictureBox9_9.Location.X + CELL_SIZE, pictureBox9_9.Location.Y + CELL_SIZE);

            //����������� ������� ����
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    PictureBox found = (PictureBox)this.Controls.Find($"pictureBox{i}_{j}", true).FirstOrDefault();
                    cells.Add($"{i}_{j}", new Cell(found.Location));
                    pictureBoxes[$"{i}_{j}"] = found;

                    //����'���� ��������� ���� ��� �������
                    found.MouseDown += field_MouseDown;
                    found.MouseMove += field_MouseMove;
                    found.MouseUp += field_MouseUp;
                }
            }

            //����������� ������� � �� ���������
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


        //����� ���������� ������� �� ����
        private void shipMove()
        {
            if (dragging && movingShip != null)
            {
                //���������� ���� ���������� 5 ��.
                Point shipCenter = new Point(movingShip.Location.X + 25, movingShip.Location.Y + 25);
                int dx = Cursor.Position.X - dragCursorPoint.X;
                int dy = Cursor.Position.Y - dragCursorPoint.Y;

                dx = (dx / 5) * 5;
                dy = (dy / 5) * 5;

                Point newLocation = new Point(dragPictureBoxPoint.X + dx, dragPictureBoxPoint.Y + dy);

                if (newLocation == movingShip.Location)
                    return;

                movingShip.Location = newLocation;
                //�������� - �������� ����������� � �������� ���
                if (movingShip.Left >= startField.X && movingShip.Right <= endField.X && movingShip.Top >= startField.Y && movingShip.Bottom <= endField.Y)
                {
                    movingShip.Visible = false;
                    int size = Convert.ToInt32(movingShipNum.Substring(0, 1));
                    foreach (KeyValuePair<string, Cell> data in cells)
                    {   //������ �� ��� ����� ����� ��������
                        int X = Math.Abs(data.Value.middle.X - shipCenter.X);
                        int Y = Math.Abs(data.Value.middle.Y - shipCenter.Y);
                        if (X <= 25 && Y <= 25)
                        {

                            if (lastShipPoint.X > -1 && lastShipPoint.Y > -1)
                            {
                                //������� ������� ��������� �������
                                ClearLastShipZone(lastShipPoint, size, isRotate);
                            }

                            string[] parts = data.Key.Split('_');
                            int row = int.Parse(parts[0]);
                            int col = int.Parse(parts[1]);
                            lastShipPoint = new Point(col, row);
                            //�������� ��� ������ �� �������� � ������ ���� ������ ���������
                            if (!CanPlaceShip(row, col, size, isRotate))
                            {
                                movingShip.Visible = true;
                                return;
                            }

                            //������� ��������
                            shipDrawing(isRotate, row, col, size);

                            //���������� �������� ���� ��� �������
                            int startRow = isRotate ? Math.Max(0, row - 1) : Math.Max(0, row - 1);
                            int endRow = isRotate ? Math.Min(9, row + size) : Math.Min(9, row + 1);

                            int startCol = isRotate ? Math.Max(0, col - 1) : Math.Max(0, col - 1);
                            int endCol = isRotate ? Math.Min(9, col + 1) : Math.Min(9, col + size);

                            //������� �� ����
                            nearDrawing(startRow, endRow, startCol, endCol);
                        }
                    }
                }
                else
                {
                    //���� ������� �� ������ �������� ���� �������� ��������
                    movingShip.Visible = true;
                    ClearLastShipZone(lastShipPoint, Convert.ToInt32(movingShipNum.Substring(0, 1)), isRotate);
                }
            }
        }
        //����� �������� �� ���
        private void shipDrawing(bool rotate, int row, int col, int size)
        {
            for (int i = 0; i < size; i++)
            {
                //��������� ���������� ��� ����� ������� �������
                int r = rotate ? row + i : row;
                int c = rotate ? col : col + i;
                string key = $"{r}_{c}";

                if (cells.ContainsKey(key))
                {
                    //��������� ����� ������� ������� (��� ����������)
                    int part = rotate ? size - i : i + 1;
                    cells[key].status = movingShipNum; //���������� ������ �������
                    cells[key].shipPart = $"{size}_{part}"; //�������� ����� � �������
                    cells[key].rotate = rotate; //�������� ��������

                    if (pictureBoxes.TryGetValue(key, out PictureBox found))
                    {
                        //������������ �������� ����������
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

        //����� ������� ������� ������� �� "��� �������"
        private void nearDrawing(int startRow, int endRow, int startCol, int endCol)
        {
            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = startCol; c <= endCol; c++)
                {
                    string key = $"{r}_{c}";

                    //���� ������� ������� � ������� �������� "��� �������"
                    if (cells.ContainsKey(key) && cells[key].status == "empty")
                    {
                        cells[key].status = $"n_{movingShipNum}";
                    }
                    //���� ��� � �������� ������ ������� � ������ ��������� "near"
                    else if (cells.ContainsKey(key) && cells[key].status.StartsWith("n_"))
                    {
                        cells[key].status = "near";
                    }
                }
            }
        }

        //��������, �� ����� ��������� �������� � ������� �������
        private bool CanPlaceShip(int row, int col, int size, bool rotated)
        {
            for (int i = 0; i < size; i++)
            {
                //��������� ���� �������
                string key = rotated ? $"{row + i}_{col}" : $"{row}_{col + i}";

                //���� ������� �� ���� � �� ����� �������
                if (!cells.ContainsKey(key))
                    return false;

                string status = cells[key].status;

                //���� ������� ������� ����� ��������
                if (status != "empty" && status != movingShipNum)
                    return false;

                //���� ������� ��� ������ �������
                if (status.StartsWith("n_") && status != $"n_{movingShipNum}")
                    return false;
            }

            return true;
        }

        //������� �������� � ����� �������
        private void ClearLastShipZone(Point shipPoint, int size, bool rotate)
        {
            int row = shipPoint.Y;
            int col = shipPoint.X;

            int startRow = Math.Max(0, row - size);
            int endRow = rotate ? Math.Min(9, row + size) : Math.Min(9, row + 1);
            int startCol = Math.Max(0, col - 1);
            int endCol = rotate ? Math.Min(9, col + 1) : Math.Min(9, col + size);
            ConcurrentBag<string> updatedKeys = new();

            //������������� Parallel.For ��� ����������� �������� ������ ������
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
                    //Invoke ��������������� ��� ������� �� ��������� ��������
                    found.Invoke(() => found.Image = emptyBitmap);
                }
            }

            DeleteTotalNear();
        }

        //�������� ������ near-�������
        private void DeleteTotalNear()
        {
            // ������ �������, �� ������� ��������
            List<string> toClear = new List<string>();

            // ������ �������, ���� ����� �������� ������ "n_..."
            List<string> toUpdateToNear = new List<string>();

            // ������� ��� ��������� ������� ��� �������
            Dictionary<string, string> nearStatuses = new Dictionary<string, string>();

            // �������� �� ������� �� ���
            foreach (var kv in cells)
            {
                string key = kv.Key;
                var cell = kv.Value;

                // �������� ����� � ���������, �� ����� ������ "near"
                if (cell.status != "near") continue;

                // ��������� ���������� �������
                int sep = key.IndexOf('_');
                int row = int.Parse(key[..sep]);
                int col = int.Parse(key[(sep + 1)..]);

                // ������� �� ������� ������� �������, �� �������� �� �������
                HashSet<string> nearbyShips = new HashSet<string>();

                // ���������� ������� ������� �������
                for (int r = row - 1; r <= row + 1; r++)
                {
                    for (int c = col - 1; c <= col + 1; c++)
                    {
                        // ���������� ���� �������
                        if (r == row && c == col) continue;

                        // ���������� ����� �� ��� ����
                        if (r < 0 || r > 9 || c < 0 || c > 9) continue;

                        string neighborKey = $"{r}_{c}";

                        // ����������, �� ���� ������ �������
                        if (!cells.TryGetValue(neighborKey, out var neighbor)) continue;

                        string status = neighbor.status;

                        // ���� ������� � ������� ������� (��� �� "empty", "near", "n_...")
                        if (!string.IsNullOrEmpty(status) &&
                            status != "empty" &&
                            status != "near" &&
                            !status.StartsWith("n_"))
                        {
                            nearbyShips.Add(status);
                        }
                    }
                }

                // ���� ����� ���� ���� ��������, ������ ����'���� �� �����
                if (nearbyShips.Count == 1)
                {
                    string shipStatus = nearbyShips.First();
                    cell.status = $"n_{shipStatus}";
                    nearStatuses[key] = $"n_{shipStatus}";
                    toUpdateToNear.Add(key);
                }
                // ���� ����� ����� � ������� �������
                else if (nearbyShips.Count == 0)
                {
                    cell.status = "empty";
                    cell.shipPart = "0";
                    toClear.Add(key);
                }
            }

            // ��������� �������� �������, �� ����� "��� �������"
            foreach (var key in toUpdateToNear)
            {
                if (pictureBoxes.TryGetValue(key, out PictureBox found))
                {
                    // ������������� Invoke ��� ��������� ���������� � ������ UI
                    found.Invoke(() => found.Image = nearBitmap);
                }
            }

            // ������� �������, �� ����� ��������
            foreach (var key in toClear)
            {
                if (pictureBoxes.TryGetValue(key, out PictureBox found))
                {
                    found.Invoke(() => found.Image = emptyBitmap);
                }
            }
        }


        //����� ��� ��������, �� � ���� � ���� �������� �� ���
        private bool oneShipInField()
        {
            foreach (var data in ships)
            {
                if (!data.Value.Visible)
                {
                    return true; //���� ���� � ���� �������� �� �����, �� �� ���
                }
            }
            return false; //�� ������ �� ����
        }

        //����� ��� ����������� ��� �������
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
                            found.Image = nearBitmap; //³��������� ���������� ��� ������ �������
                    }
                    else if (Int32.TryParse(check, out int num))
                    {
                        if (data.Value.rotate)
                        {
                            if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                                found.Image = new Bitmap(path + $"/nr_{data.Value.shipPart}.png"); //���������� ��� ������������� �������
                        }
                        else
                        {
                            if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                                found.Image = new Bitmap(path + $"/n_{data.Value.shipPart}.png"); //���������� ��� ��������������� �������
                        }
                    }
                }
            }
        }
        //����� ��� ������������ ����������� ��� �� ��� �������� ����
        private void hideBorders()
        {
            //��������� ���������� ������� ������� ������� � ��� "���"
            foreach (var data in cells)
            {
                string check = data.Value.status.Substring(0, 1);
                if (data.Value.status.StartsWith("n_") || data.Value.status == "near")
                {
                    if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                        found.Image = emptyBitmap; //������� ������ ����������
                }
                else if (Int32.TryParse(check, out int num))
                {
                    if (pictureBoxes.TryGetValue(data.Key, out PictureBox found))
                    {
                        //³��������� ���������� ������� �������
                        found.Image = new Bitmap(path + $"/{data.Value.shipPart}.png");

                        //���� �������� ���������� � ��������� ��������
                        if (data.Value.rotate)
                            found.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                }
            }
        }

        private void hideBorders(string shipNum)
        {
            //��������� ���������� ��� ����������� ������� �� ������
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
            //��������� �������� �� ��������� �������, ���� �� ������� � ��������
            if (movingShip != null && movingShip.Visible == true)
            {
                foreach (var data in defaultShipPoints)
                {
                    if (movingShipNum == data.Key)
                    {
                        movingShip.Location = data.Value;

                        //���� �������� ����������, ������ ����� � ����������
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

        //��������� ���� ������� �����
        private void ship_MouseMove(object sender, MouseEventArgs e)
        {
            shipMove();
        }
        private void ship_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox ship)
            {
                dragging = true; //������� �������������
                dragCursorPoint = Cursor.Position;
                dragPictureBoxPoint = ship.Location;
                movingShip = ship;
                isRotate = ship.Size.Width < ship.Size.Height ? true : false;
                movingShipNum = ship.Name.Replace("ship", "");
                ship.BringToFront();
                showBorders(); //�������� ���������� ������� �������
            }
        }
        private void ship_MouseUp(object sender, MouseEventArgs e)
        {
            returnDefaultPosition(); //��������� �������� �� ����, ���� �����
            hideBorders();           //��������� ����������
            playButton.Enabled = AllShipInField(); //���������� ������ ���, ���� �� ������ �� ���
            dragging = false;        //���������� �������������
        }

        //������ ��� ���������� ������� ����������
        private System.Windows.Forms.Timer holdTimer;
        private bool isLongPress = false;

        private void field_MouseDown(object sender, MouseEventArgs e)
        {
            //��������� ������ ��� ������� ���������� �� ������� ����
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

                        //���� � ������� � ��������, �������� �������������
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
            shipMove(); //��� ������� �� ��� ������������� ������
        }

        private void field_MouseUp(object sender, MouseEventArgs e)
        {
            holdTimer?.Stop();

            if (!isLongPress)
            {
                //���� ������� ���������� � ��������� �������
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
                //���������� ������������� � ���������� ������� � ��������� ����������
                returnDefaultPosition();
                hideBorders();
                playButton.Enabled = AllShipInField();
                dragging = false;
            }
        }

        private Point LastShipPoint(string key, string shipPart, bool rotate)
        {
            //���������� ������� ����� ������� �� ��� � ��������� �� ��������
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

        //����� ��� ��������� ������� �� ���
        private void RotateShip(string key)
        {
            //�������� �������� ���� ��������� � ����� �������
            bool rotate = cells[key].rotate;
            string status = cells[key].status;
            movingShipNum = status;
            if (!int.TryParse(status.Substring(0, 1), out int size))
                return;

            //��������� ������� �� ������ ������� �������
            string[] inxs = key.Split('_');
            int row = int.Parse(inxs[0]);
            int col = int.Parse(inxs[1]);
            string[] info = cells[key].shipPart.Split('_');
            int partIndex = int.Parse(info[1]) - 1;

            //������� ����� ������� ������� �� ���
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

            //����������, �� ����� ��������� �������� � ���� ��������
            bool canPlace = !rotate ? CanPlaceShip(row - size + 1, col, size, !rotate) : CanPlaceShip(row, col, size, !rotate);
            if (!canPlace)
            {
                //���� �� ����� � ���������� ������ ���� � ��������� ������
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

            //�������� �������� � ���� �������� � ��������� ������ � �������� �������
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

            //��������� ���� ������� �������
            int startRow = !rotate ? Math.Max(0, row - size) : Math.Max(0, row - 1);
            int endRow = Math.Min(9, row + 1);
            int startCol = Math.Max(0, col - 1);
            int endCol = !rotate ? Math.Min(9, col + 1) : Math.Min(9, col + size);
            nearDrawing(startRow, endRow, startCol, endCol);

            //��������� ������ ������ ������� ���� ���������
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

        //�������� ������ "���������" � ��������� ���������� ������ �� ���
        private void shuffleButton_Click(object sender, EventArgs e)
        {
            RandShipsPlace();
        }

        private void RandShipsPlace()
        {
            clearCells();               //������� ����
            playButton.Enabled = true;  //�������� ������ "�����"
            Random rand = new Random();

            List<string> shipsArr = new List<string>();
            List<string> dUseCells = new List<string>();

            //���������� ������ ��� ������� (��� �������� ������)
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

            //������� ������ ������� (4 ������ �� 1 ��������)
            int j = 0;
            for (int i = 4; i > 0; i--)
            {
                j++;
                for (int k = j; k > 0; k--)
                    shipsArr.Add($"{i}_{k}");
            }

            //������������ ����� �������� ���������, ���������� ����� ����
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

                //�������� ������� ������� � ������� � ������� ��������
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

                //��������� ���� ������� ������� �� "near"
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

                //��������� ������ ������ ������� (��������� PictureBox)
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

        //����� �� ������� ����: ����� ������, ������� ������� � ��������, ������� ����� ��������
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

        //�������� ������ "��������" � ����� ����, ������/�������� ������, ������� ������ �� ������� �������
        private void clearButton_Click(object sender, EventArgs e)
        {
            playButton.Enabled = false;  //�������� ������ "�����"
            clearCells();                //������� ����
            for (int i = 0; i < 10; i++)
            {
                for (int b = 0; b < 10; b++)
                {
                    string sh = $"{i}_{b}";
                    if (ships.TryGetValue(sh, out PictureBox found))
                    {
                        found.Visible = true;                     //�������� ��������
                        found.Size = new Size(i * CELL_SIZE, CELL_SIZE);       //������������ ����� �� ������� �����
                        found.Image = new Bitmap(path + $"/{i}.png"); //������������ �������� �������
                        found.Location = defaultShipPoints[sh]; //��������� �� �������� �������
                    }
                }
            }
        }

        //���� ���� ������ "���������" ��� �������� ������� � ��������� ���� ���� ��� ����� �������
        private void shuffleButton_MouseLeave(object sender, EventArgs e)
        {
            shuffleButton.BackColor = Color.White;
        }

        //���� ���� ������ "���������" ��� �������� ������� � �����������
        private void shuffleButton_MouseEnter(object sender, EventArgs e)
        {
            shuffleButton.BackColor = Color.FromArgb(207, 207, 244);
        }

        //ϳ���������� ������ "��������" ��� ��������
        private void clearButton_MouseEnter(object sender, EventArgs e)
        {
            clearButton.BackColor = Color.FromArgb(207, 207, 244);
        }

        //���������� ������� ������ "��������" ��� ����� �������
        private void clearButton_MouseLeave(object sender, EventArgs e)
        {
            clearButton.BackColor = Color.White;
        }

        //ϳ���������� ������ "�����" ��� ��������
        private void playButton_MouseEnter(object sender, EventArgs e)
        {
            playButton.BackColor = Color.FromArgb(207, 207, 244);
        }

        //���������� ������� ������ "�����" ��� ����� �������
        private void playButton_MouseLeave(object sender, EventArgs e)
        {
            playButton.BackColor = Color.White;
        }

        //��������, �� �� 10 ������� ������� �� ��� (������� � ������� �� ���)
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

        //�������� ������ "�����" � ���� ������� �����, ������� ����� �볺��� ���
        private void playButton_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            Client form = new Client(this, cells);
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 150);
            form.ShowDialog();
        }

        //�������� �������� ����� � ������� ����� ���������� ��������, ���� �� ����������� �������� ������
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