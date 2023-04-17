using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;

namespace WordReporter
{
    public class WordProcess
    {

        private Dictionary<string, object> repaceData;

        public Dictionary<string, object> RepaceData
        {
            get { return repaceData; }
            set
            {
                repaceData = value;
            }
        }

        private WordprocessingDocument _wordDoc;
        public WordprocessingDocument WordDocument
        {
            get { return _wordDoc; }
            set
            {
                _wordDoc = value;
            }
        }

        public WordProcess(WordprocessingDocument wd, Dictionary<string, object> data )
        {
            WordDocument = wd;
            repaceData = data;
        }


        #region
        /// <summary>
        /// 处理文字
        /// </summary>
        [Obsolete("方法已经弃用，这种方式不能完全找到对应Key值")]
        public void ProcessText()
        {
            try
            {
                //读取outxml
                string docText = null;
                using (StreamReader sr = new StreamReader(WordDocument.MainDocumentPart.GetStream()))
                {
                    docText = sr.ReadToEnd();
                }

                //遍历数据中Text 替换
                foreach (var item in RepaceData)
                {
                    if (item.Key.Split('_')[0] == "Text")
                    {
                        Regex regexText = new Regex(item.Key);
                        docText = regexText.Replace(docText, item.Value.ToString());
                    }
                }

                //保存
                using (StreamWriter sw = new StreamWriter(WordDocument.MainDocumentPart.GetStream(FileMode.Create)))
                {
                    sw.Write(docText);
                }
            }
            catch ( Exception ex) 
            {
                throw ex;
            }
           
        }

        /// <summary>
        /// 处理文字
        /// </summary>
        public void NewProcessText()
        {
            try
            {
                var tp = new TextProcess(_wordDoc);
                Dictionary<string, object> textRepaceData = RepaceData.Where(r => r.Key.Split('_')[0] == "Text").ToDictionary(item => item.Key, item => item.Value);
                tp.ParseWordParagraph(repaceData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
         
        }
        #endregion


        /// <summary>
        /// 处理表格
        /// </summary>
        public void ProcessTable()
        {
            try
            {
                IEnumerable<Table> tables = WordDocument.MainDocumentPart.Document.Descendants<Table>().ToList();

                //遍历数据中Table 添加数据行
                foreach (var item in RepaceData)
                {
                    if (item.Key.Split('_')[0] == "Table")
                    {
                        Table tablePart = FindTablePart(tables, item.Key);

                        tablePart.GetTableTemplateRow(); //1.先保存模板行
                        tablePart.ClearRowofTemplateExceptHeader(); //2.清除除表头的多余行

                        //3.循环添加行
                        foreach (string[] row in item.Value as string[][])
                        {
                            tablePart.AddTableRow(row);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        Table FindTablePart(IEnumerable<Table> tables, string key)
        {
            foreach (var table in tables)
            {
                TableCaption tbCap = table.Descendants<TableCaption>().FirstOrDefault();
                if (tbCap != null && tbCap.Val == key)
                {
                    return table;
                }
            }
            return null;
        }



        #region 图片处理

        /// <summary>
        /// 处理图片
        /// </summary>
        public void ProcessImage()
        {
            try
            {
                var imagesToRemove = new List<Drawing>();
                IEnumerable<Drawing> drawings = WordDocument.MainDocumentPart.Document.Descendants<Drawing>().ToList();

                //遍历数据中 Image 替换
                foreach (var item in RepaceData)
                {
                    if (item.Key.Split('_')[0] == "Image")
                    {
                        if (!File.Exists(item.Value.ToString())) //图片文件不存在跳过
                        {
                            continue;
                        }

                        OpenXmlPart imagePart = FindImagePart(drawings, item.Key, out Drawing dw);

                        if(imagePart == null)
                        {
                            continue;
                        }
                        byte[] newImage = FileToByte(item.Value.ToString());

                        if (newImage == null && imagePart != null)
                        {
                            imagesToRemove.Add(dw);
                        }
                        else
                        {
                            using (var writer = new BinaryWriter(imagePart.GetStream()))
                            {
                                writer.Write(newImage);
                            }
                        }
                    }
                }

                //如果是空 字节图片，可清空需要替换的图片
                foreach (var image in imagesToRemove)
                {
                    image.Remove();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 定位图片部分
        /// </summary>
        /// <param name="drawings"></param>
        /// <param name="key"></param>
        /// <param name="dw"></param>
        /// <returns></returns>
        OpenXmlPart FindImagePart(IEnumerable<Drawing> drawings, string key,out Drawing dw)
        {
            dw = null;
            foreach (Drawing drawing in drawings)
            {
                DocProperties dpr = drawing.Descendants<DocProperties>().FirstOrDefault();
                if (dpr != null && dpr.Name == key)
                {
                    foreach (Blip b in drawing.Descendants<Blip>().ToList())
                    {
                        OpenXmlPart imagePart = WordDocument.MainDocumentPart.GetPartById(b.Embed);
                        dw = drawing;
                        return imagePart;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 转文件成 Byte
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static byte[] FileToByte(string path)
        {
            try
            {
                Stream fsForRead = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    //读入一个字节
                    Console.WriteLine("文件的第一个字节为：" + fsForRead.ReadByte().ToString());
                    //Console.ReadLine();
                    //读写指针移到距开头10个字节处
                    fsForRead.Seek(0, SeekOrigin.Begin);
                    byte[] bs = new byte[fsForRead.Length];
                    int log = Convert.ToInt32(fsForRead.Length);
                    //从文件中读取10个字节放到数组bs中
                    fsForRead.Read(bs, 0, log);
                    fsForRead.Close();
                    fsForRead.Dispose();
                    return bs;

                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                    throw ex;
                }
                finally
                {
                    fsForRead.Close();
                    fsForRead.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
                throw e;
            }
        }

        #endregion
    }
}
