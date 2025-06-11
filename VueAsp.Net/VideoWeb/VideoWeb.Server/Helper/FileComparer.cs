using System.Collections;
using System.IO;

namespace VideoWeb.Server.Helper
{
    public class FileComparer: IComparer
    {
    
        int IComparer.Compare(object o1, object o2)
        {
            FileInfo fi1 = o1 as FileInfo;
            FileInfo fi2 = o2 as FileInfo;
            return fi1.LastWriteTime.CompareTo(fi2.LastWriteTime);
        }
    }
}