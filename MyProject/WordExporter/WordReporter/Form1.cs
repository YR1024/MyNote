using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WordReporter
{
    public partial class Form1 : Form
    {
        string basePath = "C:\\Users\\YR\\Desktop\\";
        public Form1()
        {
            InitializeComponent();
        }
   

        WordprocessingDocument OpenTemplateDocAndClone(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (WordprocessingDocument _doc = WordprocessingDocument.Open(fs, true))
                {
                    WordprocessingDocument docCopy = _doc.Clone() as WordprocessingDocument;
                    return docCopy;
                }
            }
        }

        TxtDataParse txtDataParse;
        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "文本文件|*.txt";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtDataParse = new TxtDataParse(ofd.FileName);
                    txtDataParse.ParseData();
                    textBox1.Text = txtDataParse.TxtContent;
                    //MessageBox.Show("OK");
                }
            }
        }

        WordprocessingDocument wpd;
        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Word文档|*.docx|Word97-2003文档|*.doc";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    wpd = OpenTemplateDocAndClone(ofd.FileName);
                    MessageBox.Show("加载模板成功");
                }
            }
        }

        //导出
        private void button4_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog ofd = new SaveFileDialog())
            {
                ofd.Filter = "Word文档|*.docx|Word97-2003文档|*.doc";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var savefile = wpd.SaveAs(ofd.FileName);
                    savefile.Close();
                    MessageBox.Show("导出成功");
                }
            }
        }

        //替换
        private void FindKeyAndReplace(object sender, EventArgs e)
        {
            WordProcess wordProcess = new WordProcess(wpd, txtDataParse.DataSource);
            //wordProcess.ProcessText();
            wordProcess.NewProcessText();
            wordProcess.ProcessImage();
            wordProcess.ProcessTable();
            MessageBox.Show("替换完成");
        }
    }
}
