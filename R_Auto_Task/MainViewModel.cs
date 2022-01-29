using DevExpress.Mvvm;
using R_Auto_Task.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace R_Auto_Task
{
    public class MainViewModel : ViewModelBase
    {

      

        public ObservableCollection<Operation> OperationList
        {
            get { return GetProperty(() => OperationList); }
            set { SetProperty(() => OperationList, value); }
        }

        public Array ActionEnumType
        {
            get { return GetProperty(() => ActionEnumType); }
            set { SetProperty(() => ActionEnumType, value); }
        }

        public MainViewModel()
        {
            TaskManager.Instance.LoadConfig();
            if(TaskManager.Instance.OperationList == null)
            {
                OperationList = new ObservableCollection<Operation>();
            }
            else
            {
                OperationList = TaskManager.Instance.OperationList;
            }
            foreach (var operation in OperationList)
            {
                if (string.IsNullOrEmpty(operation.ImageUrl))
                    continue;
                var bitmapImage = new System.Drawing.Bitmap(operation.ImageUrl, true);
                operation.ImgSource = ImageHelper.BitmapToImageSource(bitmapImage);
            }

            ActionEnumType = Enum.GetValues(typeof(DoAction));
        }

        public void SaveOperationList()
        {
            TaskManager.Instance.OperationList = OperationList;
            TaskManager.Instance.SaveConfig();
        }
    }

    [Serializable]
    public class Operation : ViewModelBase
    {
        public Operation()
        {
            Content = new OtherContent();
        }

        public OperationType OpType
        {
            get { return GetProperty(() => OpType); }
            set { SetProperty(() => OpType, value); }
        }
        public string ImageUrl { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapImage ImgSource
        {
            get { return GetProperty(() => ImgSource); }
            set { SetProperty(() => ImgSource, value); }
        }

        public DoAction? ActionType
        {
            get { return GetProperty(() => ActionType); }
            set { SetProperty(() => ActionType, value); }
        }

        public OtherContent Content
        {
            get { return GetProperty(() => Content); }
            set { SetProperty(() => Content, value); }
        }
    }

    public class OtherContent : ViewModelBase
    {
       
        public double Similarity { get; set; } = 0.98;

        public Postion OperatPostion
        {
            get { return GetProperty(() => OperatPostion); }
            set { SetProperty(() => OperatPostion, value); }
        } 
        public double OffSetX { get; set; } = 0;
        public double OffSetY { get; set; } = 0;
        public int RepeatNumber { get; set; } = 1;
        public string PasteText { get; set; } = "";
    }

    public enum OperationType
    {
        SearchAndClickImage = 0,
        Key = 1,
        TextInput = 2,
        SearchImage = 3,
    }

    public enum Postion
    {
        Center,
        LeftTop,
        LeftBottom,
        RightTop,
        RightBottom,
    }
}
