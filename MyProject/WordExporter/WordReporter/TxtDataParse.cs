using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WordReporter
{
    public class TxtDataParse
    {

        public Dictionary<string, object> DataSource = new Dictionary<string, object>();
        //Tuple<int, string, string> person = new Tuple<int, string, string>();


        private string txtFilePath;
        public string TxtContent;

        public Dictionary<string, object> Source = new Dictionary<string, object>();
      

        public TxtDataParse(string _txtFile)
        {
            txtFilePath = _txtFile;

            ReadTextContent();
        }

        void ReadTextContent()
        {
            if (File.Exists(txtFilePath))
            {
                try
                {
                    using (Stream fs = new FileStream(txtFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        //string s_con = string.Empty;
                        // 创建一个 StreamReader 的实例来读取文件 
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            //string line;


                            TxtContent = sr.ReadToEnd();
                        }

                        //TxtContent = s_con;
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

       

        public async void ParseData()
        {
            try
            {
                string s_con = string.Empty;

                StringToStream(TxtContent);
                using (StreamReader sr = new StreamReader(StringToStream(TxtContent)))
                {
                    string line;
                    string tabKey = string.Empty;
                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()) != null)
                    {
                        s_con += line;
                        string[] lineData = StringSplit(line);

                        if (lineData.Length >= 2)
                        {
                            if(IsKeyString(lineData[0],out ValueType valueType))
                            {
                                if(valueType == ValueType.Table)
                                {
                                    int rowCount = int.Parse(lineData[1].Split(',')[0]);
                                    int colCount = int.Parse(lineData[1].Split(',')[1]);
                                    string[][] tableData = new string[rowCount][];
                                    for (int i = 0; i <= rowCount -1 ; i++)
                                    {
                                        string rowline;
                                        if (( rowline = sr.ReadLine()) != null)
                                        {
                                            string[] rowLineData = StringSplit(rowline);
                                            if(rowLineData.Count() == colCount)
                                            {
                                                tableData[i] = new string[colCount];
                                                for (int j = 0; j <= colCount - 1; j++)
                                                {
                                                    tableData[i][j] = rowLineData[j];
                                                }
                                            }
                                        }
                                    }
                                    AddDictionaryData(lineData[0], tableData);
                                }
                                else
                                {
                                    AddDictionaryData(lineData[0], lineData[1]);
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        Stream StringToStream(string str)
        {
            // convert string to stream
            byte[] array = Encoding.UTF8.GetBytes(str);
            MemoryStream stream = new MemoryStream(array);
            //StreamWriter writer = new StreamWriter(stream);
            //writer.Write(str);
            //writer.Flush();
            return stream;
        }

        static string[] StringSplit(string str)
        {
            return Regex.Split(str, "----", RegexOptions.IgnoreCase);
        }

        void AddDictionaryData(string key, object value)
        {
            if (DataSource.ContainsKey(key))
            {
                DataSource[key] = value;
            }
            else
            {
                DataSource.Add(key, value);
            }
        }

        static bool IsKeyString(string key,out ValueType valueType)
        {
            valueType = ValueType.Text;
            if (Regex.IsMatch(key, "Text_\\w+_Key"))
            {
                valueType = ValueType.Text;
                return true;
            }
            else if(Regex.IsMatch(key, "Image_\\w+_Key"))
            {
                valueType = ValueType.Image;
                return true;
            }
            else if (Regex.IsMatch(key, "Table_\\w+_Key"))
            {
                valueType = ValueType.Table;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public enum ValueType
    {
        Text,

        Image,

        Table
    }
}
