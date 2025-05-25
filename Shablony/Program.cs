using System;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Ввод значений
        Console.Write("Введите a_value (номера принтеров через пробел или запятую): ");
        string rawAValue = Console.ReadLine();
        var aValues = rawAValue.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

        Console.Write("Введите n_value (набор чисел через пробел или запятую): ");
        string rawNValue = Console.ReadLine();
        var nValues = rawNValue.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

        // Поиск абсолютного пути к папке шаблонов
        string currentDir = Directory.GetCurrentDirectory();
        string relativeTemplatePath = Path.Combine("Shablony", "Shablony", "bin", "Release", "net8.0", "publish", "win-x64", "шаблоны видео джет");

        string templateFolder = FindDirectoryUpwards(currentDir, relativeTemplatePath);

        if (templateFolder == null || !Directory.Exists(templateFolder))
        {
            Console.WriteLine($"❌ Не удалось найти папку шаблонов по относительному пути: {relativeTemplatePath}");
            return;
        }

        // Получаем все шаблоны .ciff
        var templateFiles = Directory.GetFiles(templateFolder, "*.ciff");
        if (templateFiles.Length == 0)
        {
            Console.WriteLine("❌ В папке шаблонов нет файлов с расширением .ciff");
            return;
        }

        // Папка для вывода
        string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Shablony");
        Directory.CreateDirectory(outputFolder);

        foreach (var a in aValues)
        {
            foreach (var n in nValues)
            {
                foreach (var templatePath in templateFiles)
                {
                    string fileName = Path.GetFileName(templatePath);
                    string replacedName = fileName.Replace("{a value}", a).Replace("{n value}", n);
                    string printerFolder = Path.Combine(outputFolder, a); // Создаём отдельную папку для каждого принтера
                    Directory.CreateDirectory(printerFolder);

                    string outputPath = Path.Combine(printerFolder, replacedName);

                    ProcessTemplate(templatePath, outputPath, a, n);
                }
            }
        }

        Console.WriteLine("✅ Готово! Файлы сохранены в папке 'Shablony'.");
    }

    static string FindDirectoryUpwards(string startPath, string relativePath)
    {
        DirectoryInfo? dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            string combined = Path.Combine(dir.FullName, relativePath);
            if (Directory.Exists(combined))
                return combined;

            dir = dir.Parent;
        }
        return null;
    }

    static void ProcessTemplate(string templatePath, string outputPath, string aValue, string nValue)
    {
        byte[] ciffData = File.ReadAllBytes(templatePath);
        int totalReplaced = 0;

        totalReplaced += ReplacePattern(ciffData, "{a value}", aValue, out byte[] modifiedData1);
        ciffData = modifiedData1;

        totalReplaced += ReplacePattern(ciffData, "{n value}", nValue, out byte[] modifiedData2);
        ciffData = modifiedData2;

        File.WriteAllBytes(outputPath, ciffData);
        Console.WriteLine($"✅ Создан файл: {outputPath} (замен: {totalReplaced})");
    }

    static int ReplacePattern(byte[] data, string searchText, string replaceText, out byte[] modifiedData)
    {
        byte[] pattern = Encoding.Unicode.GetBytes(searchText);
        byte[] replacement = Encoding.Unicode.GetBytes(replaceText);
        int replaced = 0;

        byte[] currentData = (byte[])data.Clone();
        modifiedData = currentData;

        for (int i = 0; i <= currentData.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (currentData[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                int extra = 0;
                int p = i + pattern.Length;
                while (p + 1 < currentData.Length &&
                       ((currentData[p] == 32 && currentData[p + 1] == 0) || (currentData[p] == 125 && currentData[p + 1] == 0)))
                {
                    extra += 2;
                    p += 2;
                }

                int totalLength = pattern.Length + extra;

                byte[] newData = new byte[currentData.Length - totalLength + replacement.Length];

                Buffer.BlockCopy(currentData, 0, newData, 0, i);
                Buffer.BlockCopy(replacement, 0, newData, i, replacement.Length);
                Buffer.BlockCopy(currentData, i + totalLength, newData, i + replacement.Length, currentData.Length - (i + totalLength));

                currentData = newData;
                modifiedData = currentData;

                replaced++;
                i += replacement.Length - 1;
            }
        }

        return replaced;
    }
}
