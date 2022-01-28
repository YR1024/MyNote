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
    [Serializable]
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
    }
}
