using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace CNNClassifier
{
    public partial class changeFileName : Form
    {
        public changeFileName()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DirectoryInfo diPos = new DirectoryInfo("Z:\\车辆分类正负样本1\\车辆分类正负样本\\pos");

            int files = diPos.GetFiles().Length;
            FileInfo[] fiArr = diPos.GetFiles();
            
            for (int fNum = 0; fNum < files; fNum++)
            {
                fiArr[fNum].MoveTo("Z:\\车辆分类正负样本1\\车辆分类正负样本\\pos1\\" + fNum + fiArr[fNum].Extension);
            }
        }
    }
}
