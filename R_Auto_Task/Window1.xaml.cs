using R_Auto_Task.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace R_Auto_Task
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        public MainViewModel ViewModel;
        public string ImagePath = AppDomain.CurrentDomain.BaseDirectory + "Image\\";

        public Window1()
        {
            InitializeComponent();
            ViewModel = this.DataContext as MainViewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OperationList.Add(new Operation());
        }

        private void SetImageFile_Click(object sender, RoutedEventArgs e)
        {
            var Operation = (sender as Button).Tag as Operation;
            //创建一个保存文件的对话框
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog()
            {
                //Filter = "All Files|*.config*",
                //InitialDirectory = @"D:\",
            };
            //调用ShowDialog()方法显示该对话框，该方法的返回值代表用户是否点击了确定按钮
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string FileName = dialog.FileName;
                ImageHelper.SaveImage(FileName, ImagePath + dialog.SafeFileName);
                Operation.ImageUrl = ImagePath + dialog.SafeFileName;
                var bitmapImage = new Bitmap(FileName, true);
                Operation.ImgSource = ImageHelper.BitmapToImageSource(bitmapImage);
            }
        }

        //删除行
        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (gridControl.SelectedItem == null)
                return;
            var operation = gridControl.SelectedItem as Operation;
            ViewModel.OperationList.Remove(operation); 
            
            ImageHelper.DeleteImage(ImagePath, System.IO.Path.GetFileName(operation.ImageUrl));
            gridControl.SelectedItem = null;
        }

        /// <summary>
        /// 操作动作下拉框 输入筛选过滤
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActionTypeCmb_KeyUp(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;

            DoAction[] a = new DoAction[ViewModel.ActionEnumType.Length];
            ViewModel.ActionEnumType.CopyTo(a, 0);
            List<DoAction> mylist = new List<DoAction>();
            mylist = a.ToList().FindAll(delegate(DoAction s) { return s.ToString().ToLower().Contains(comboBox.Text.Trim().ToLower()); });
            comboBox.ItemsSource = mylist;
            comboBox.IsDropDownOpen = true;
           
        }

        private void ActionTypeCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (sender as ComboBox);
            if (comboBox.SelectedItem == null)
            {
                return;
            }
            var Operation = comboBox.Tag as Operation;
            var doAction = (DoAction)comboBox.SelectedItem;

            Operation.ActionType = doAction;
        }

        Task t;
        private void StartAutoTask(object sender, RoutedEventArgs e)
        {
            t= Task.Run(()=> {
                foreach (var Operation in ViewModel.OperationList)
                {
                    ParseOperation(Operation);
                    while (true)
                    {
                        if (WorkTask.Status == TaskStatus.RanToCompletion ||
                            WorkTask.Status == TaskStatus.Faulted)
                        {
                            var operationTaskResult = WorkTask.Result as OperationTaskResult;
                            if (operationTaskResult != null) //图片操作
                            {
                                if (operationTaskResult.OperatResult == OperationResult.MatchImage) //找到图片
                                {
                                    Rectangle = operationTaskResult.Rect;
                                    break;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else //按键鼠标操作
                            {

                            }
                        }
                        Thread.Sleep(50);
                    }
                    Thread.Sleep(100);
                    t.Wait();
                }

            });
            
        }

        /// <summary>
        /// 相似度
        /// </summary>
        double Similarity;
        Task<OperationTaskResult> WorkTask;

        System.Drawing.Rectangle Rectangle;

        private void ParseOperation(Operation operation)
        {
            WorkTask = new Task<OperationTaskResult>(
                    () => {
                        if (!string.IsNullOrEmpty(operation.ImageUrl))
                        {
                            int i = 0;
                            while (i < 60)
                            {
                                string pic = operation.ImageUrl;
                                var rct = EmguCvHelper.GetMatchPos(pic, out Similarity);
                                if (rct != System.Drawing.Rectangle.Empty)
                                {
                                   
                                    if (operation.ActionType != null)
                                    {
                                        DoAction ActionType = (DoAction)operation.ActionType;
                                        if (IsMouseOperation(ActionType))
                                        {
                                            MouseEvent(operation, rct);
                                        }
                                        else
                                        {
                                            WinIoHelper.KeyDownUp(DoActionToKeysMapping.GetKeys(ActionType));
                                        }
                                    }
                                    return new OperationTaskResult()
                                    {
                                        Rect = rct,
                                        OperatResult = OperationResult.MatchImage,
                                    };
                                }
                                Thread.Sleep(1000);
                                i++;
                            }
                            return new OperationTaskResult() 
                            {
                                Rect = System.Drawing.Rectangle.Empty,
                                OperatResult = OperationResult.UnMatchImage,
                            } ;
                        }
                        else
                        {
                            DoAction ActionType = (DoAction)operation.ActionType;
                            if (IsMouseOperation(ActionType))
                            {
                                MouseHelper.MouseDownUp(Rectangle.X + Rectangle.Width / 2, Rectangle.Y + Rectangle.Height / 2);
                            }
                            else
                            {
                                WinIoHelper.KeyDownUp(DoActionToKeysMapping.GetKeys(ActionType));
                            }

                            return null;
                        }
                    }
            );
            WorkTask.Start();
        }

        public bool IsMouseOperation(DoAction doAction)
        {
            if (doAction == DoAction.LeftMouseClick ||
                doAction == DoAction.LeftMouseDoubleClick ||
                doAction == DoAction.LeftMouseDown ||
                doAction == DoAction.LeftMouseUp ||
                doAction == DoAction.RightMouseClick ||
                doAction == DoAction.RightMouseDoubleClick ||
                doAction == DoAction.RightMouseDown ||
                doAction == DoAction.RightMouseUp ||
                doAction == DoAction.MiddleMouseClick ||
                doAction == DoAction.MouseDownWheel ||
                doAction == DoAction.MouseUpWheel
                )
                return true;
            return false;
        }

        void MouseEvent(Operation operation, System.Drawing.Rectangle Rect)
        {
            switch (operation.ActionType)
            {
                case DoAction.LeftMouseClick:
                    MouseHelper.MouseDownUp(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2); break;
                case DoAction.LeftMouseDoubleClick:
                    break;
                case DoAction.LeftMouseDown:
                    MouseHelper.MouseDown(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2); break;
                case DoAction.LeftMouseUp:
                    MouseHelper.MouseUp(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2); break;
                case DoAction.RightMouseClick:
                    MouseHelper.RightMouseDownUp(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2); break;
                case DoAction.RightMouseDoubleClick:
                    break;
                case DoAction.RightMouseDown:
                    MouseHelper.RightMouseDown(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2); break;
                case DoAction.RightMouseUp:
                    MouseHelper.RightMouseUp(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2); break;

                case DoAction.MiddleMouseClick:
                    break;
                case DoAction.MouseDownWheel:
                    break;
                case DoAction.MouseUpWheel:
                    break;
            }
        }        

        private void SaveOperationList(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveOperationList();
        }
    }



    public class OperationTaskResult
    {
        public System.Drawing.Rectangle Rect;
        public OperationResult OperatResult;
    }


    public enum OperationResult
    {
        MatchImage,
        UnMatchImage,

    }
}
