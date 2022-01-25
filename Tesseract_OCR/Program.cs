using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;

namespace Tesseract_OCR
{
    class Program
    {
        static void Main(string[] args)
        {
            string pic = @"C:\Users\YR\Desktop\ocrTest.png";
            List<string> a  = GetProductNumberFromImage(pic);

            Console.ReadLine();
        }

        private static List<string> GetProductNumberFromImage(string imagePath)
        {
            List<string> resultList = new List<string>();
            using (var ocr = new TesseractEngine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"), "chi_sim", EngineMode.Default))
            {
                var pix = PixConverter.ToPix(new Bitmap(imagePath));
                using (var page = ocr.Process(pix))
                {
                    string text = page.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        string pattern = @"品号([\s\S])(\d+)";
                        Regex regex = new Regex(pattern);
                        var mathResult = regex.Matches(text);
                        foreach (Match item in mathResult)
                        {
                            if (item.Groups.Count >= 2)
                            {
                                resultList.Add(item.Groups[2].Value);
                            }
                            else
                            {
                                resultList.Add(item.Value);
                            }
                        }
                    }
                }
            }
            return resultList;
        }
    }
}
