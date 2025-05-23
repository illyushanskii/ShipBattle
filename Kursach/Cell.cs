using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kursach
{
    public class Cell
    {
        public Point start{ get; }//Координати ліво-верхньої частини клітки
        private Point end;
        public Point middle { get; }//Координати середини клітки
        public string status;//Зберігає розмір корабля та його номер, наприклад 2_3
        public bool rotate;//Перевернутий корабль чи ні
        public string shipPart;//Зберігає розмір корабля та номер частини корабля яка відображається на клітці 2_1

        public Cell(Point st)
        {
            start = st;
            end= new Point(start.X + 50,start.Y + 50);
            middle = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            status = "empty";
            shipPart = "0";
            rotate = false;
        }

        //Метод для копіювання екземпляра класа
        public Cell Clone()
        {
            Cell clone = new Cell(this.start)
            {
                status = this.status,
                rotate = this.rotate,
                shipPart = this.shipPart
            };
            return clone;
        }
    }
}
