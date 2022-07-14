using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomationServices.EmguCv
{
    public class MatchOptions
    {
        public MatchOptions()
        {

        }

        public MatchOptions(int maxtimes, int interval)
        {
            MaxTimes = maxtimes;
            DelayInterval = interval;
        }

        /// <summary>
        /// 设置或获取 最大查找匹配次数，默认0 次将一直查找直到匹配成功
        /// </summary>
        public int MaxTimes { get; set; }

        /// <summary>
        /// 设置或获取 每次查找匹配的间隔时间，单位ms
        /// </summary>
        public int DelayInterval { get; set; }

        /// <summary>
        /// 设置或获取 匹配度阈值，越接近1则，相似匹配度越高
        /// </summary>
        public double Threshold { get; set; } = 0.98;

        /// <summary>
        /// 设置或获取 匹配模式, 默认为全屏查找
        /// </summary>
        public MatchMode MatchMode { get; set; }

        private Rectangle _WindowArea;
        /// <summary>
        /// 设置或获取 匹配区域，当MatchMode为Relatively，使用WindowArea在指定区域进行匹配
        /// </summary>
        public Rectangle WindowArea
        {
            get
            {
                if (MatchMode == MatchMode.Absolutely)
                    return Rectangle.Empty;
                else
                    return _WindowArea;
            }
            set
            {
                _WindowArea = value;
            }
        }

        /// <summary>
        /// 对图片加载处理模式 ，默认为Grayscale；
        /// </summary>
        internal ImreadModes ImreadModes { get; set; } = ImreadModes.Grayscale;

        public ImreadModesConvert ImreadModesConvert
        {
            get
            {
                return (ImreadModesConvert)ImreadModes;
            }
            set
            {
                ImreadModes = (ImreadModes)value;
            }
        }
    } 
}
