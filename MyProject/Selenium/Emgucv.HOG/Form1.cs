using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.OCR;
using Emgu.CV.ML;
using Emgu.CV.Features2D;
using System.IO;
using System.Xml;
using System.Threading;

namespace CNNClassifier
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            DirectoryInfo di_P = new DirectoryInfo("samples/pos");
            DirectoryInfo di_N = new DirectoryInfo("samples/neg");

            PSP.Text = di_P.FullName.ToString();
            NSP.Text = di_N.FullName.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fDialog = new FolderBrowserDialog();
            fDialog.Description = "please select positve floder";

            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                PSP.Text = fDialog.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fDialog = new FolderBrowserDialog();
            fDialog.Description = "please Negative positve floder";

            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                NSP.Text = fDialog.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ThreadStart ts = new ThreadStart(trainSamples);
            Thread t = new Thread(ts);
            t.Start();
        }


        Matrix<float> HogFeatureData = new Matrix<float>(967, 900);//500样本+467负样本，HOG纬度=(36-12)/6 + 1) *(36-12)/6 + 1) * 4 *9
        Matrix<int> featureClasses = new Matrix<int>(967, 1);//样本类别，1为正样本，-1为负样本
        Emgu.CV.ML.SVM svm;
        private void trainSamples()
        {
            DirectoryInfo diPos = new DirectoryInfo(PSP.Text);
            DirectoryInfo diNeg = new DirectoryInfo(NSP.Text);//正副样本地址
            int posNum = diPos.GetFiles().Length;
            int negNum = diNeg.GetFiles().Length;
            FileInfo[] posFiles = diPos.GetFiles();
            FileInfo[] negFiles = diNeg.GetFiles();

            #region 获取样本，并计算HOG特征

            HOGDescriptor hog = new HOGDescriptor(new Size(36, 36), 
                new Size(12, 12), 
                new Size(6, 6), 
                new Size(6, 6), 
                9);// 定义HOG描述子

            #region 正样本
            for (int fNum = 0; fNum < posNum; fNum++)
            {
                string filePath = diPos.FullName + "\\" + posFiles[fNum].Name;

                Image<Gray, byte> im = new Emgu.CV.Image<Gray, byte>(filePath);

                float[] fArr = hog.Compute(im);//计算HOG特征

                for (int i = 0; i < fArr.Length; i++)
                {
                    HogFeatureData[fNum, i] = fArr[i];
                }

                featureClasses.Data[fNum, 0] = 1;

                #region 进度
                PFN.Text = diPos.GetFiles()[fNum].Name;
                float tempF = (float.Parse((fNum + 1).ToString()) * 100 / float.Parse(posNum.ToString()));
                PPB.Value = (int)tempF;
                #endregion
            }

            #endregion

            #region 负样本
            for (int fNum = 0; fNum < diNeg.GetFiles().Length; fNum++)
            {
                string filePath = diNeg.FullName + "\\" + negFiles[fNum].Name;
                Image<Gray, byte> im = new Emgu.CV.Image<Gray, byte>(filePath);

                float[] fArr = hog.Compute(im);

                for (int i = 0; i < fArr.Length; i++)
                {
                    HogFeatureData[fNum + posNum, i] = fArr[i];
                }

                featureClasses.Data[fNum + posNum, 0] = -1;

                #region 进度
                NFN.Text = diNeg.GetFiles()[fNum].Name;
                float tempF = (float.Parse((fNum + 1).ToString()) * 100 / float.Parse(negNum.ToString()));
                NPB.Value = (int)tempF;
                #endregion
            }
            #endregion

            #endregion

            #region 训练并保存训练结果
           
            svm = new Emgu.CV.ML.SVM();
            svm.Type = SVM.SvmType.CSvc;
            svm.SetKernel(SVM.SvmKernelType.Linear);//线性
            svm.C =1;
            svm.TermCriteria = new MCvTermCriteria(1000, 0.001);//1000次或者收敛达到0.001就跳出


            svm.Train(HogFeatureData, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, featureClasses);
            svm.Save("HogFeatures.xml");
            
            #endregion


            MessageBox.Show("训练完毕，训练数据保存在：HogFeatures.xml！");
        }

        private void HogSvmPredict()
        {
            #region HOG检测

            #region 生成自定义detector向量
            XmlDocument xml = new XmlDocument();
            xml.Load(trainedFile.Text);
            get_rho(xml.DocumentElement);
            get_Alpha(xml.DocumentElement);
            getSv_count(xml.DocumentElement);
            getAlpha();

            int supportVectors = svm.GetSupportVectors().Height;
            int DescriptorDim = svm.GetSupportVectors().Width;

            //int supportVectors = 1;
            //int DescriptorDim = 900;


            svmMat = new Matrix<float>(supportVectors, DescriptorDim);
            get_supportVector(xml.DocumentElement);


            Matrix<float> alphaMat = new Matrix<float>(1, supportVectors);
            for (int i = 0; i < alphaArr.Length; i++)
            {
                alphaMat[0, i] = alphaArr[i];
            }


            Matrix<float> resultMat = new Matrix<float>(1, DescriptorDim);

            resultMat = -1 * alphaMat * svmMat;


            float[] mydetector = new float[DescriptorDim + 1];
            for (int i = 0; i < DescriptorDim; i++)
            {
                mydetector[i] = resultMat[0, i];
            }
            mydetector[DescriptorDim] = rhoValue;

            #endregion

            Rectangle[] regions;
            Mat image = new Mat("111.jpg", Emgu.CV.CvEnum.LoadImageType.Color);

            HOGDescriptor hog = new HOGDescriptor(new Size(36, 36), new Size(12, 12), new Size(6, 6), new Size(6, 6), 9);// 定义HOG描述子
            hog.SetSVMDetector(mydetector);
            MCvObjectDetection[] results = hog.DetectMultiScale(image);
            regions = new Rectangle[results.Length];
            for (int i = 0; i < results.Length; i++)
            {
                regions[i] = results[i].Rect;
            }

            //using (HOGDescriptor hog = new HOGDescriptor())
            //{
            //    hog.SetSVMDetector(mydetector);
            //    //var b = HOGDescriptor.GetDefaultPeopleDetector();
            //    //des.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());

            //    MCvObjectDetection[] results = hog.DetectMultiScale(image);
            //    regions = new Rectangle[results.Length];
            //    for (int i = 0; i < results.Length; i++)
            //    {
            //        regions[i] = results[i].Rect;
            //    }
            //}

            using (Graphics g = Graphics.FromImage(image.Bitmap))
            {
                foreach (Rectangle rc in regions)
                {
                    g.DrawRectangle(new Pen(Color.White, 2), rc);//给识别出的行人画矩形框
                }
            }
            GC.Collect();
            imageBox1.Image = image;
            #endregion
        }

        #region  常用方法
        int sv_count = 0;
        string alphaValue = "";
        float rhoValue = 0;
        Matrix<float> svmMat;
        #region 获取xml文件中的getSv_count
       
        public void getSv_count(XmlNode nodes)
        {
            if (nodes.HasChildNodes)
            {
                foreach (XmlNode node in nodes.ChildNodes)
                {
                    if (nodes.Name == "sv_count")
                    {
                        sv_count = int.Parse(nodes.InnerText);
                        return;
                    }
                    getSv_count(node);
                }
            }
        }
        #endregion

        #region 获取xml文件中的alpha值 alphaArr
        
        public void get_Alpha(XmlNode nodes)
        {
            if (nodes.HasChildNodes)
            {
                foreach (XmlNode node in nodes.ChildNodes)
                {
                    if (nodes.Name == "alpha")
                    {
                        alphaValue = nodes.InnerText;
                        return;
                    }
                    get_Alpha(node);
                }
            }
        }

        public float[] alphaArr;
        public void getAlpha()
        {
            byte[] array = Encoding.ASCII.GetBytes(alphaValue);
            MemoryStream stream = new MemoryStream(array);
            StreamReader sr = new StreamReader(stream);
            alphaArr = new float[sv_count];
            sr.ReadLine();
            int i = 0;
            while (true)
            {
                string tmp = sr.ReadLine();
                if (tmp == "")
                    continue;

                string[] tmp2 = tmp.Split(' ');
                foreach (string ele in tmp2)
                {
                    if (ele != "")
                    {
                        alphaArr[i] = float.Parse(ele);
                        i++;
                    }
                }
                if (i == sv_count)
                    break;
            }
        }
        #endregion

        #region  获取xml文件中的rho值 rhoValue
        
        public void get_rho(XmlNode nodes)
        {
            if (nodes.HasChildNodes)
            {
                foreach (XmlNode node in nodes.ChildNodes)
                {
                    if (nodes.Name == "rho")
                    {
                        rhoValue = float.Parse(nodes.InnerText);
                        return;
                    }
                    get_rho(node);
                }
            }
        }
        #endregion

        #region  从xml中读取已训练的数据
        
        public void get_supportVector(XmlNode nodes)
        {
            if (nodes.HasChildNodes)
            {
                foreach (XmlNode node in nodes.ChildNodes)
                {
                    if (nodes.Name == "support_vectors")
                    {
                        for (int i = 0; i < node.ChildNodes.Count; i++)
                        {
                            string strValue = node.ChildNodes[i].InnerText;
                            string[] tempStrArr = strValue.Replace("\r\n", "")
                                .Replace("  ", " ").Replace("  ", " ").Replace("  ", " ")
                                .Replace("  ", " ").Replace("  ", " ").Replace("  ", " ")
                                .Replace("  ", " ").Split(' ');
                            int z = 0;
                            for (int j = 0; j < tempStrArr.Length; j++)
                            {
                                if (tempStrArr[j] != "")
                                {
                                    svmMat[i, z] = float.Parse(tempStrArr[j]);
                                    z++;
                                }
                            }
                        }
                        return;
                    }
                    get_supportVector(node);
                }
            }
        }
        #endregion
        #endregion

        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "选择文件";
            fileDialog.Filter = "XML文件 (*.xml)|*.xml";
            fileDialog.FilterIndex = 1;
            fileDialog.RestoreDirectory = true;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                trainedFile.Text = fileDialog.FileName;
            }
        }
        

        private void button8_Click(object sender, EventArgs e)
        {
            #region 读取已经训练好的XML
            svm = new Emgu.CV.ML.SVM();
            FileStorage fsr = new FileStorage("HogFeatures.xml", FileStorage.Mode.Read);
            svm.Read(fsr.GetFirstTopLevelNode());
            #endregion

            #region 生成自定义detector向量 mydetector
            XmlDocument xml = new XmlDocument();
            xml.Load(trainedFile.Text);
            get_rho(xml.DocumentElement);
            get_Alpha(xml.DocumentElement);
            getSv_count(xml.DocumentElement);
            getAlpha();

            int supportVectors = svm.GetSupportVectors().Height;
            int DescriptorDim = svm.GetSupportVectors().Width;

            svmMat = new Matrix<float>(supportVectors, DescriptorDim);
            get_supportVector(xml.DocumentElement);


            Matrix<float> alphaMat = new Matrix<float>(1, supportVectors);
            for (int i = 0; i < alphaArr.Length; i++)
            {
                alphaMat[0, i] = alphaArr[i];
            }


            Matrix<float> resultMat = new Matrix<float>(1, DescriptorDim);

            resultMat = -1 * alphaMat * svmMat;


            float[] mydetector = new float[DescriptorDim + 1];
            for (int i = 0; i < DescriptorDim; i++)
            {
                mydetector[i] = resultMat[0, i];
            }
            mydetector[DescriptorDim] = rhoValue;

            #endregion


            myRect[] regions;
            Mat image = new Mat("333.jpg", Emgu.CV.CvEnum.LoadImageType.Color);

            using (HOGDescriptor hog = new HOGDescriptor(new Size(36, 36), new Size(12, 12), new Size(6, 6), new Size(6, 6), 9))
            {
                hog.SetSVMDetector(mydetector);

                MCvObjectDetection[] results = hog.DetectMultiScale(image);
                regions = new myRect[results.Length];
                for (int i = 0; i < results.Length; i++)
                {
                    regions[i] = new myRect();
                    regions[i].rct = results[i].Rect;
                    regions[i].score = results[i].Score;
                }
            }

            using (Graphics g = Graphics.FromImage(image.Bitmap))
            {
                foreach (myRect rc in regions)
                {
                    g.DrawRectangle(new Pen(Color.White, 2), rc.rct);//给识别出的行人画矩形框
                    #region 得到匹配姓名，并画出
                    
                    Font font = new Font("宋体", 16, GraphicsUnit.Pixel);
                    SolidBrush fontLine = new SolidBrush(Color.Yellow);
                    float xPos = rc.rct.X;
                    float yPos = rc.rct.Y - 21;
                    g.DrawString(Math.Round(rc.score, 2).ToString(), font, fontLine, xPos, yPos);
                    #endregion
                }
            }
            GC.Collect();
            imageBox1.Image = image;

            GC.Collect();
        }

        public class myRect
        {
            public Rectangle rct;
            public float score;
        }
    }
}
