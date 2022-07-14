using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomationServices.EmguCv
{
    /// <summary>
    /// 匹配模式
    /// </summary>
    public enum MatchMode
    {
        /// <summary>
        /// 完全匹配，相对整个屏幕
        /// </summary>
        Absolutely,

        /// <summary>
        /// 相对模式，相对窗口的位置
        /// </summary>
        Relatively
    }
}
