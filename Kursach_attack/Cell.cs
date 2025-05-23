using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kursach_attack
{
    public class Cell
    {
        public string status;//Зберігає розмір корабля та його номер, наприклад 2_3
        public bool rotate;//Перевернутий корабль чи ні
        public string shipPart;//Зберігає розмір корабля та номер частини корабля яка відображається на клітці 2_1

        public Cell()
        {
            status = "empty";
            shipPart = "0";
            rotate = false;
        }

        //Метод для копіювання екземпляра класа
        public Cell Clone()
        {
            Cell clone = new Cell()
            {
                status = this.status,
                rotate = this.rotate,
                shipPart = this.shipPart
            };
            return clone;
        }

    }
}
