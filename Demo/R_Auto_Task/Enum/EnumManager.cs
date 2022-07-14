using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R_Auto_Task
{

    public enum OperationType
    {
        SearchAndClickImage = 0,    //搜寻图片位置并点击
        Key = 1,    //按键操作
        TextInput = 2,  //文本输入
        SearchImage = 3,    //仅搜寻图片
    }


    /// <summary>
    /// 点击图片的位置
    /// </summary>
    public enum Postion
    {
        /// <summary>
        /// 中心位置
        /// </summary>
        Center = 0,

        /// <summary>
        /// 左上角
        /// </summary>
        LeftTop = 1,

        /// <summary>
        /// 左下角
        /// </summary>
        LeftBottom = 2,

        /// <summary>
        /// 右上角
        /// </summary>
        RightTop = 3,

        /// <summary>
        /// 右下角
        /// </summary>
        RightBottom = 4,
    }

    public enum OperationResult
    {
        MatchImage,
        UnMatchImage,
    }
}
