using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestNuget
{
    internal class OCR
    {

        static string path = AppDomain.CurrentDomain.BaseDirectory;
        Tesseract _ocr ;

        private void InitOcr(String path, String lang, OcrEngineMode mode)
        {
            try
            {
                if (_ocr != null)
                {
                    _ocr.Dispose();
                    _ocr = null;
                }

                if (String.IsNullOrEmpty(path))
                    path = ".";

                TesseractDownloadLangFile(path, lang);
                TesseractDownloadLangFile(path, "osd"); //script orientation detection
                String pathFinal = path.Length == 0 || path.Substring(path.Length - 1, 1).Equals(Path.DirectorySeparatorChar.ToString())
                    ? path
                    : String.Format("{0}{1}", path, System.IO.Path.DirectorySeparatorChar);

                String subfolderName = "tessdata";
                String folderName = System.IO.Path.Combine(pathFinal, subfolderName);
                _ocr = new Tesseract(folderName, lang, mode);

            }
            catch (Exception e)
            {
                _ocr = null;
                System.Diagnostics.Debug.Print(e.Message, "Failed to initialize tesseract OCR engine");
            }
        }
        private static void TesseractDownloadLangFile(String folder, String lang)
        {
            String subfolderName = "tessdata";
            String folderName = System.IO.Path.Combine(folder, subfolderName);
            if (!System.IO.Directory.Exists(folderName))
            {
                System.IO.Directory.CreateDirectory(folderName);
            }
            String dest = System.IO.Path.Combine(folderName, String.Format("{0}.traineddata", lang));
            if (!System.IO.File.Exists(dest))
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {
                    String source =
                        String.Format("https://github.com/tesseract-ocr/tessdata/blob/4592b8d453889181e01982d22328b5846765eaad/{0}.traineddata?raw=true", lang);

                    Console.WriteLine(String.Format("Downloading file from '{0}' to '{1}'", source, dest));
                    webclient.DownloadFile(source, dest);
                    Console.WriteLine(String.Format("Download completed"));
                }
        }



        public void Test() 
        {
            var imageSource = new Image<Bgr, byte>(@"C:\Users\YR\Desktop\ocr.png");
            Image<Gray, byte> imageGrayscale = imageSource.Convert<Gray, Byte>();
            var iName = imageGrayscale.ThresholdBinary(new Gray(100), new Gray(255));

            //var bitmap = new Bitmap(@"C:\Users\YR\Desktop\ocr.png");
            //Image<Gray, Byte> NumberImage = new Image<Gray, Byte>(bitmap);

            InitOcr(path, "chi_sim", OcrEngineMode.TesseractOnly);
            //_ocr.PageSegMode = PageSegMode.SingleBlock;
            _ocr.SetImage(imageSource);
            int result = _ocr.Recognize();
            if (result != 0)
            {
                Console.WriteLine("识别失败！");
                return;
            }
            String Message = _ocr.GetUTF8Text().TrimEnd('\n', '\r');
            Tesseract.Character[] characters = _ocr.GetCharacters();//获取识别数据
            String text = _ocr.GetUTF8Text();//得到识别字符串。

        }
    }
}
