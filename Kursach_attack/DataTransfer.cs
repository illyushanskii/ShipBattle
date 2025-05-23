using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kursach_attack
{
    public class DataTransfer
    {
        private readonly string name;
        private readonly MemoryMappedFile mmf;

        public DataTransfer(string name)
        {
            this.name = name;

            try
            {
                //Спроба відкрити існуючий файл
                mmf = MemoryMappedFile.OpenExisting(name);
            }
            catch (FileNotFoundException)
            {
                //Не знайшли - створюємо новий
                mmf = MemoryMappedFile.CreateOrOpen(name, 1024);
            }
        }

        //Метод для читання файлу, повертає рядок
        public string Read()
        {
            using var stream = mmf.CreateViewStream();
            BinaryReader reader = new BinaryReader(stream);
            return reader.ReadString();
        }

        //Метод для запису рядка в файл
        public void Write(string text)
        {
            using var stream = mmf.CreateViewStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8);
            writer.Write(text);
            writer.Flush();//Очистка буфера
        }
    }
}
