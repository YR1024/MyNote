using DevExpress.Mvvm;
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


        public MainViewModel()
        {
            OperationList = new ObservableCollection<Operation>();
        }
    }

    public class Operation : ViewModelBase
    {
        public string ImageUrl { get; set; }
        public BitmapImage ImgSource
        {
            get { return GetProperty(() => ImgSource); }
            set { SetProperty(() => ImgSource, value); }
        }
    }
}
