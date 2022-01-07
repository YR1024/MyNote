using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace PYAutoGui
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        //string pyPath = @"F:\github\MyNote\PYAutoGui\111\test.py";

        string pyPath = @"C:\Users\YR\AppData\Local\Programs\Python\Python38\Lib\site-packages\pyautogui\__init__.py";
        string pyPath1 = @"C:\Program Files\IronPython 3.4\Lib\site-packages\pyautogui\__init__.py";
        string pyPath2 = @"F:\github\MyNote\PYAutoGui\111\pyautogui\__init__.py";
        string img = @"F:\github\MyNote\PYAutoGui\111\111.png";


        string path = @"F:\github\MyNote\PYAutoGui\py.py";
        public string sourceImage = @"C:\Users\YR\Desktop\1.png";
        public string findImage = @"C:\Users\YR\Desktop\2.png";

        public MainWindow()
        {
            InitializeComponent();

            ScriptEngine pyEngine = Python.CreateEngine();//创建Python解释器对象
            dynamic py = pyEngine.ExecuteFile(pyPath2);//读取脚本文件
            //int[] array = new int[9] { 9, 3, 5, 7, 2, 1, 3, 6, 8 };
            //string reStr = py.MatchImage(sourceImage, findImage);//调用脚本文件中对应的函数
            //Console.WriteLine(reStr);
        }
    }
}
