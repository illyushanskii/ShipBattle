using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kursach_comp
{
    public class SmartAttack
    {
        private string firstHit = "";
        private string lastHit = "";
        private string currentDirection = "";
        private List<string> directionsLeft = new List<string>();
        private string mode = "search";
        private readonly Dictionary<string, Cell> cells;
        private readonly Random rand = new Random();

        public SmartAttack(Dictionary<string, Cell> gameCells)
        {
            this.cells = gameCells;
        }

        // Реєстрація влучання
        public void RegisterHit(string cell)
        {
            if (mode == "search")
            {
                // Перехід у режим прицільної атаки
                firstHit = cell;
                lastHit = cell;
                mode = "target";
                directionsLeft = new List<string> { "up", "down", "left", "right" };
                currentDirection = GetRandomDirection();
            }
            else
            {
                lastHit = cell;
            }
        }

        // Реєстрація промаху
        public void RegisterMiss()
        {
            if (mode == "target")
            {
                if (lastHit == firstHit)
                {
                    // Якщо це перша клітинка — змінити напрямок
                    directionsLeft.Remove(currentDirection);
                    currentDirection = GetRandomDirection();
                }
                else
                {
                    // Якщо вже було декілька влучань — змінити напрямок на протилежний
                    currentDirection = GetOppositeDirection(currentDirection);
                    lastHit = firstHit;
                }
            }
        }

        // Реєстрація знищення корабля
        public void RegisterKill()
        {
            Reset();
        }

        // Отримання наступної цілі для атаки
        public string GetNextTarget()
        {
            if (mode != "target" || directionsLeft.Count == 0)
                return null;

            while (directionsLeft.Count > 0)
            {
                var parts = lastHit.Split('_');
                int row = int.Parse(parts[0]);
                int col = int.Parse(parts[1]);

                // Вибір координат наступної клітинки залежно від напрямку
                switch (currentDirection)
                {
                    case "up": row--; break;
                    case "down": row++; break;
                    case "left": col--; break;
                    case "right": col++; break;
                }

                // Перевірка, чи не виходимо за межі поля
                if (row < 0 || row > 9 || col < 0 || col > 9)
                {
                    directionsLeft.Remove(currentDirection);
                    currentDirection = GetRandomDirection();
                    lastHit = firstHit;
                    continue;
                }

                string target = $"{row}_{col}";

                // Перевірка, чи ця клітинка ще не була атакована
                if (cells.ContainsKey(target) && cells[target].status == "empty")
                {
                    return target;
                }
                else
                {
                    // Якщо клітинка недоступна — пробуємо інший напрямок
                    directionsLeft.Remove(currentDirection);
                    currentDirection = GetRandomDirection();
                    lastHit = firstHit;
                }
            }

            Reset(); // Всі можливості вичерпані
            return null;
        }

        // Отримання випадкового напрямку з доступних
        private string GetRandomDirection()
        {
            if (directionsLeft.Count == 0) return null;
            int index = rand.Next(directionsLeft.Count);
            return directionsLeft[index];
        }

        // Отримання протилежного напрямку
        private string GetOppositeDirection(string dir)
        {
            return dir switch
            {
                "up" => "down",
                "down" => "up",
                "left" => "right",
                "right" => "left",
                _ => ""
            };
        }

        // Скидання всіх змінних до початкового стану
        private void Reset()
        {
            firstHit = "";
            lastHit = "";
            currentDirection = "";
            directionsLeft.Clear();
            mode = "search";
        }
    }
}
