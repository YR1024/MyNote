﻿using ICSharpCode.SharpZipLib.Zip;
using Microsoft.ML.Data;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Selenium
{
    /// <summary>
    /// 自动化操作类
    /// </summary>
    public class AutomatedSelenium : IDisposable
    {
        private EdgeDriver driver;
        private EdgeDriverService driverService;
        private IWebDriver LoginFrame;

        public Logger Logger;
        public bool ShowBrowserWnd = false;

        public AutomatedSelenium()
        {
            Logger = new Logger(driver);
        }

        public void StartTask()
        {
            try
            {
                CheckMsedgedriverVision();
                Logger.WriteLog("开始执行农场任务");
                StartEdge();
                if (!Login())
                {
                    return;
                }
                Farm();
                Pasture();
            }
            catch (Exception e)
            {
                Logger.WriteLog("发生错误异常：" + e.Message + e);
                //throw e;
            }
            finally
            {
                Close();
            }
        }

        #region 检查msedgedriver版本
        void CheckMsedgedriverVision()
        {
            Logger.WriteLog("检查msedgedriver.exe版本");
            string currentEdge = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            var EdgeFileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(currentEdge);

            var msedgedriverPath = AppDomain.CurrentDomain.BaseDirectory + "msedgedriver.exe";
            if (File.Exists(msedgedriverPath))
            {
                var MsedgedriverFileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(msedgedriverPath);
                if (MsedgedriverFileVersionInfo.FileVersion == EdgeFileVersionInfo.FileVersion)
                    return;
                Logger.WriteLog($"msedgedriver.版本不一致 {MsedgedriverFileVersionInfo.FileVersion } => {EdgeFileVersionInfo.FileVersion}");
                DownloadMsedgedriver(EdgeFileVersionInfo.FileVersion);

            }
            else
            {
                Logger.WriteLog("msedgedriver.exe文件不存在");
                DownloadMsedgedriver(EdgeFileVersionInfo.FileVersion);
            }
        }

        private void DownloadMsedgedriver(string fileVersion)
        {
            //https://msedgedriver.azureedge.net/105.0.1343.33/edgedriver_win64.zip
            var url = "https://msedgedriver.azureedge.net/" + fileVersion + "/edgedriver_win64.zip";
            Logger.WriteLog("开始下载：edgedriver_win64.zip");
            Logger.WriteLog("下载URL：" + url);
            HttpDownload(url, AppDomain.CurrentDomain.BaseDirectory + "edgedriver_win64.zip");
            Logger.WriteLog("下载完成：edgedriver_win64.zip");

            // Set the method that will be called on each file before extraction but after the OverwritePrompt (if applicable)
            //FastZipEvents events = new FastZipEvents();
            //events.ProcessFile = ProcessFileMethod;
            //FastZip fastZip = new FastZip(events);
            FastZip fastZip = new FastZip();

            // To conditionally extract files in FastZip, use the fileFilter and directoryFilter arguments.
            // 过滤器是用分号分隔的正则表达式值列表。以-开头的条目是排除项。
            // See the NameFilter class for more details.
            // 以下表达式包括所有以“”结尾的名称。dat，但“dummy.dat”除外
            //string fileFilter = @"+\.dat$;-^dummy\.dat$";
            string fileFilter = "";
            string directoryFilter = null;
            bool restoreDateTime = true;

            Logger.WriteLog("开始解压 edgedriver_win64.zip");
            // Will prompt to overwrite if target filenames already exist
            fastZip.ExtractZip(AppDomain.CurrentDomain.BaseDirectory + "edgedriver_win64.zip", AppDomain.CurrentDomain.BaseDirectory, FastZip.Overwrite.Prompt, OverwritePrompt,
                               fileFilter, directoryFilter, restoreDateTime);
            Logger.WriteLog("解压完成 edgedriver_win64.zip");


            //System.IO.Compression.ZipFile.CreateFromDirectory(@"e:\test", @"e:\test\test.zip"); //压缩
            //System.IO.Compression.ZipFile.ExtractToDirectory(@"E:\FarmSelenium\edgedriver_win64.zip", @"E:\FarmSelenium"); //解压

        }

        /// <summary>
        /// http下载文件
        /// </summary>
        /// <param name="url">下载文件地址</param>
        /// <returns></returns>
        public static void HttpDownload(string url, string localFileAddr)
        {
            using (var client = new WebDownload())
            {
                client.DownloadFile(url, localFileAddr);//下载临时文件
                //Console.WriteLine("Using " + tempFile);
                //return FileToStream(tempFile, true);
            }
        }

        void BatchDownloadImage()
        {
            try
            {
                for (int i = 50; i < 100; i++)
                {
                    TryFindElementInFrame(By.Id("slideBg"), out IWebElement slideBg, LoginFrame);
                    TryFindElementInFrame(By.XPath("/html/body/div/div[3]/div[2]/div[8]"), out IWebElement slide, LoginFrame);
                    var bgImg = slideBg.GetCssValue("background-image");
                    var slideImg = slide.GetCssValue("background-image");
                    Match match = Regex.Match(bgImg, "url\\(\"(.*?)\"\\)");
                    Match match2 = Regex.Match(slideImg, "url\\(\"(.*?)\"\\)");
                    if (match.Success)
                    {
                        string imgURL = match.Groups[1].Value;
                        HttpDownload(imgURL, $@"D:\360MoveData\Users\YR\Desktop\滑块验证码\Bg\{i}.png");
                    }
                    if (match.Success)
                    {
                        string imgURL2 = match2.Groups[1].Value;
                        //HttpDownload(imgURL2, $@"D:\360MoveData\Users\YR\Desktop\滑块验证码\Slider\{i}.png");
                    }
                    TryFindElementInFrame(By.Id("reload"), out IWebElement reload, LoginFrame);
                    reload.Click();
                    Thread.Sleep(2000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw e;
            }
        }
        /// <summary>
        /// 覆盖提示
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool OverwritePrompt(string fileName)
        {
            //// In this method you can choose whether to overwrite a file.
            //DialogResult dr = MessageBox.Show("Overwrite " + fileName, "Overwrite?", MessageBoxButtons.YesNoCancel);
            //if (dr == DialogResult.Cancel)
            //{
            //    _stop = true;
            //    // Must return true if we want to abort processing, so that the ProcessFileMethod will be called.
            //    // When the ProcessFileMethod sets ContinueRunning false, processing will immediately stop.
            //    return true;
            //}
            //return dr == DialogResult.Yes;
            return true;
        }


        #endregion

        /// <summary>
        /// 启动浏览器
        /// </summary>
        private void StartEdge()
        {
            var msedgedriverPath = AppDomain.CurrentDomain.BaseDirectory;
            driverService = EdgeDriverService.CreateDefaultService(msedgedriverPath); //此处为msedgedriver.exe的存放路径
            EdgeOptions options = new EdgeOptions();
            if (!ShowBrowserWnd)
            {
                options.AddArgument("--headless"); //浏览器静默模式启动
            }
            driver = new EdgeDriver(driverService, options);
            Logger.driver = driver;
            Logger.WriteLog("浏览器已启动");
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns>true,登录成功，false失败</returns>
        private bool Login()
        {
            for (int i = 0; i < 2; i++)
            {
                driver.Navigate().GoToUrl("https://ui.ptlogin2.qq.com/cgi-bin/login?style=9&appid=1600000084&daid=0&s_url=http%3A%2F%2Fmcapp.z.qq.com%2Fnc%2Fcgi-bin%2Fwap_farm_index%3Fg_ut%3D3&low_login=0"); //网址
                Thread.Sleep(2000);

                IWebElement username = driver.FindElement(By.Id("u")); //用户名控件ID
                IWebElement password = driver.FindElement(By.Id("p")); //密码控件ID
                IWebElement login = driver.FindElement(By.Id("go")); //登录控件ID

                username.SendKeys("1642963395"); //填入账号
                Thread.Sleep(1500);
                password.SendKeys("yr18723750041.."); //填入密码
                Thread.Sleep(1500);
                login.Click(); //点击登录按钮
                Thread.Sleep(5000);

                if (TryFindElement(By.LinkText("个人中心"), out IWebElement personalCenter))
                {
                    Logger.WriteLog("登录成功");
                    return true;
                }
                try
                {

                    LoginFrame = driver.SwitchTo().Frame(driver.FindElement(By.Id("tcaptcha_iframe_dy")));
                    //driver.SwitchTo().DefaultContent(); 
                    if (TryFindElementInFrame(By.Id("instructionText"), out IWebElement instructionText, LoginFrame))
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            if (instructionText.Text.Contains("请选择最符合"))
                            {
                                Logger.WriteLog(instructionText.Text);
                                Logger.WriteLog("跳过此种验证方式，点击'我不会'");
                                TryFindElementInFrame(By.Id("agedText"), out IWebElement Iwont, LoginFrame);
                                Iwont.Click();
                                Thread.Sleep(2500);
                                continue;
                            }
                            if (instructionText.Text == "拖动下方滑块完成拼图")
                            {
                                //BatchDownloadImage(); //批量爬取下载验证图片，用于训练模型
                                Logger.WriteLog("登录出现滑块验证");
                                SliderVerification();
                                Thread.Sleep(2000);
                                driver.SwitchTo().DefaultContent();
                                //var phoneVerifyFrame = driver.SwitchTo().Frame(driver.FindElement(By.Id("verify")));
                                if (TryFindElement(By.Id("verify"), out IWebElement phoneVerify))
                                {
                                    Logger.WriteLog("手机号码验证");
                                    throw new Exception("出现手机验证，退出本次任务");
                                }
                                else
                                {
                                    if (TryFindElement(By.LinkText("个人中心"), out IWebElement home))
                                    {
                                        Logger.WriteLog("登录成功");
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    //return false;
                    throw e;
                }
            }
            Logger.WriteLog("登录失败");
            return false;
        }

        private void GetFarmInfo()
        {
            Logger.WriteLog("农场信息：");
            TryFindElement(By.ClassName("farm-info"), out IWebElement farm_info);
            string farmInfo = GetInfo(By.CssSelector("p.tabs-1"), farm_info);
            Logger.WriteLog(farmInfo);
        }

        private void GetPastureInfo()
        {
            TryFindElement(By.ClassName("farm-info"), out IWebElement farm_info);
            string farmInfo = GetInfo(By.CssSelector("p.tabs-1"), farm_info);
            Logger.WriteLog(farmInfo);
        }


        /// <summary>
        /// 农场
        /// </summary>
        private void Farm()
        {
            while (TryFindElement(By.ClassName("txt-warning"), out IWebElement warning))
            {
                TryFindElement(By.LinkText("我的农场"), out IWebElement back);
                back.Click();
                Logger.WriteLog("网络错误或繁忙，重新进入我的农场");
                Thread.Sleep(5000);
            }
            GetFarmInfo();
            if (TryFindElement(By.LinkText("浇水"), out IWebElement watering))
            {
                watering.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
            if (TryFindElement(By.LinkText("杀虫"), out IWebElement Insecticidal))
            {
                Insecticidal.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
            if (TryFindElement(By.LinkText("除草"), out IWebElement weed))
            {
                weed.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
            Logger.WriteLog("查找收获");
            if (TryFindElement(By.LinkText("收获"), out IWebElement harvest))
            {
                harvest.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
            Logger.WriteLog("查找铲除");
            if (TryFindElement(By.LinkText("铲除"), out IWebElement eradicate))
            {
                eradicate.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                if (TryFindElement(By.LinkText("播种"), out IWebElement sow))
                {
                    sow.Click();
                    Logger.WriteLog("有可播种的土地，开始播种");
                    Thread.Sleep(1500);

                    while (TryFindElement(By.LinkText("种植"), out _))
                    {
                        if (TryFindElements(By.ClassName("bg-alter"), out ReadOnlyCollection<IWebElement> zhongzis))
                        {
                            IWebElement zhongzi = null;
                            foreach (var item in zhongzis)
                            {
                                if (item.FindElement(By.ClassName("txt-fade")).Text.Contains("金"))
                                {
                                    continue;
                                }
                                else
                                {
                                    zhongzi = item;
                                    break;
                                }
                            }

                            if (zhongzi != null)
                            {
                                zhongzi.FindElement(By.LinkText("种植")).Click();
                                Thread.Sleep(1500);
                                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                            }
                            else
                            {
                                Logger.WriteLog("只有金种子或没有可种植的种子");
                                GoStorePurchaseSeeds();
                            }


                        }
                    }

                }
            }
            Logger.WriteLog("返回我的农场");
            TryFindElement(By.LinkText("我的农场"), out IWebElement back4);
            back4.Click();
            Logger.WriteLog("循环播种");
            while (TryFindElement(By.LinkText("播种"), out IWebElement sow2))
            {
                sow2.Click();
                Logger.WriteLog("有可播种的土地，开始播种");
                Thread.Sleep(1500);

                if (TryFindElement(By.XPath("/html/body/div[2]/div[1]/div"), out IWebElement BackpackInfo))
                {
                    var a = BackpackInfo.Text;
                    if (BackpackInfo.Text.Contains("你没有符合种植条件的种子"))
                    {
                        GoStorePurchaseSeeds();
                    }
                    else
                    {
                        while (TryFindElement(By.LinkText("种植"), out _))
                        {
                            if (TryFindElements(By.ClassName("bg-alter"), out ReadOnlyCollection<IWebElement> zhongzis))
                            {
                                IWebElement zhongzi = null;
                                foreach (var item in zhongzis)
                                {
                                    if (item.FindElement(By.ClassName("txt-fade")).Text.Contains("金"))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        zhongzi = item;
                                        break;
                                    }
                                }

                                if (zhongzi != null)
                                {
                                    zhongzi.FindElement(By.LinkText("种植")).Click();
                                    Thread.Sleep(1500);
                                    Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                                }
                                else
                                {
                                    Logger.WriteLog("只有金种子或没有可种植的种子");
                                    GoStorePurchaseSeeds();
                                }


                            }
                        }
                    }
                }

                TryFindElement(By.LinkText("我的农场"), out IWebElement back);
                back.Click();

            }
            TryFindElement(By.LinkText("我的农场"), out IWebElement back3);
            back3.Click();
            Thread.Sleep(1500);
            MyPond();


            if (TryFindElement(By.LinkText("签到"), out IWebElement SignIn))
            {
                SignIn.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                if (GetInfo(By.ClassName("txt-warning")).Contains("错误代码"))
                {
                    Logger.WriteLog(GetInfo(By.ClassName("txt-warning")));
                    TryFindElement(By.LinkText("我的农场"), out IWebElement back);
                    back.Click();
                }
            }
        }

        /// <summary>
        /// 去商店购买种子
        /// </summary>
        /// <param name="grassSeeds">默认购买牧草</param>
        private void GoStorePurchaseSeeds(bool grassSeeds = true)
        {
            TryFindElement(By.LinkText("去商店购买种子"), out IWebElement GoStore);
            Logger.WriteLog("没有种子, 去商店购买");
            GoStore.Click();
            Thread.Sleep(1500);
            IWebElement forageGrassSeeds = null;
            TryFindElement(By.LinkText("末页"), out IWebElement LastPage);
            LastPage.Click();
            Thread.Sleep(1000);
            while (true)
            {
                ReadOnlyCollection<IWebElement> SeedsInfos = driver.FindElements(By.ClassName("padding-3-0"));
                foreach (var item in SeedsInfos)
                {
                    if (item.Text.Contains("牧草"))
                    {
                        forageGrassSeeds = item;
                        break;
                    }
                }
                if (forageGrassSeeds != null)
                {
                    break;
                }
                TryFindElement(By.LinkText("上页"), out IWebElement PrevPage);
                PrevPage.Click();
                Thread.Sleep(1000);
            }

            if (forageGrassSeeds != null)
            {
                var purchase = forageGrassSeeds.FindElement(By.LinkText("购买"));
                purchase.Click();
                Thread.Sleep(1500);
                TryFindElement(By.Name("sb"), out IWebElement confirm); //确定
                confirm.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                TryFindElement(By.LinkText("去土地种植"), out IWebElement planting);
                planting.Click();
                Thread.Sleep(1500);
            }
        }

        /// <summary>
        /// 我的池塘
        /// </summary>
        private void MyPond(int retryCount = 0)
        {
            TryFindElement(By.LinkText("我的池塘"), out IWebElement fishPond);
            fishPond.Click();
            Logger.WriteLog("进入我的池塘");
            TryFindElement(By.XPath("/html"), out IWebElement html);
            if (html.Text.Contains("网络繁忙"))
            {
                Logger.WriteLog(html.Text);
                driver.Navigate().Back();
                if (retryCount < 3)
                {
                    MyPond(retryCount++);
                }
                return;
            }

            Thread.Sleep(1500);
            if (TryFindElement(By.LinkText("捞鱼"), out IWebElement Fishfishing))
            {
                Fishfishing.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
            if (TryFindElement(By.LinkText("养殖"), out IWebElement breed))
            {
                breed.Click();
                Thread.Sleep(1500);
                if (GetInfo(By.ClassName("module-content")).Contains("你没有可养殖的鱼苗了"))
                {
                    Logger.WriteLog("你没有可养殖的鱼苗了，到商店购买鱼苗");
                    TryFindElement(By.LinkText("到商店购买鱼苗"), out IWebElement BuyFry);
                    BuyFry.Click();
                    Thread.Sleep(1500);
                    TryFindElement(By.LinkText("购买"), out IWebElement Buy);
                    Buy.Click();
                    Thread.Sleep(1500);
                    Logger.WriteLog(GetInfo(By.XPath("/html/body/div[2]/div[2]/p")));
                    TryFindElement(By.Name("sb"), out IWebElement confirm); //确定
                    confirm.Click();
                    Thread.Sleep(1500);
                    Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                    TryFindElement(By.LinkText("去鱼塘养殖"), out IWebElement ToFishPond);
                    ToFishPond.Click();
                    Thread.Sleep(1500);
                }
                while (TryFindElement(By.LinkText("养殖"), out IWebElement breed2))
                {
                    breed2.Click();
                    Thread.Sleep(1500);
                    Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                }
            }
            TryFindElement(By.LinkText("我的农场"), out IWebElement back2);
            back2.Click();
            Thread.Sleep(1500);
        }


        /// <summary>
        /// 牧场
        /// </summary>
        private void Pasture()
        {
            TryFindElement(By.LinkText("QQ农场牧场版"), out IWebElement pasture);
            pasture.Click();
            Logger.WriteLog("进入我的牧场");
            Thread.Sleep(1500);
            GetPastureInfo();

            AddFeed();
            Production();
            Raise();

            if (TryFindElement(By.LinkText("清扫"), out IWebElement cleanPoop))
            {
                cleanPoop.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
            if (TryFindElement(By.LinkText("我的牧场"), out IWebElement backPasture))
            {
                backPasture.Click();
                Thread.Sleep(1500);
            }

            TryFindElement(By.LinkText("我的牧场"), out IWebElement backPasture3);
            backPasture3.Click();
            Thread.Sleep(1500);

            if (TryFindElement(By.LinkText("签到送好礼"), out IWebElement SignIn2))
            {
                SignIn2.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
        }


        /// <summary>
        /// 添加饲料
        /// </summary>
        void AddFeed()
        {
            if (TryFindElement(By.LinkText("添加"), out IWebElement addFeed))
            {
                addFeed.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("spacing-3")));
                if (TryFindElement(By.ClassName("btn-s"), out IWebElement makingFeed)) //制作饲料
                {
                    makingFeed.Click();
                    Thread.Sleep(1500);
                    Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                    if (TryFindElement(By.LinkText("我的牧场"), out IWebElement backPasture2))
                    {
                        backPasture2.Click();
                        Thread.Sleep(1500);
                    }
                }
            }
        }

        /// <summary>
        /// 生产
        /// </summary>
        void Production()
        {
            if (TryFindElement(By.LinkText("生产."), out IWebElement production))
            {
                production.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                Thread.Sleep(16000);
            }
            if (TryFindElement(By.LinkText("刷新"), out IWebElement refresh))
            {
                refresh.Click();
                Thread.Sleep(1500);
            }
            if (TryFindElement(By.LinkText("收获."), out IWebElement harvest2))
            {
                harvest2.Click();
                Thread.Sleep(1500);
                Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            }
            TryFindElement(By.LinkText("我的牧场"), out IWebElement backPasture3);
            backPasture3.Click();
            Thread.Sleep(1500);
        }

        /// <summary>
        /// 饲养
        /// </summary>
        void Raise()
        {
            while (TryFindElement(By.LinkText("饲养"), out IWebElement raise))
            {
                raise.Click();
                Logger.WriteLog("有可饲养的棚/窝");
                Thread.Sleep(1500);

                if (GetInfo(By.ClassName("txt-warning2")).Contains("请在商店购买"))
                {
                    string cubType = GetInfo(By.ClassName("txt-warning2")).Substring(9, 1);
                    PurchaseCubs(cubType);
                }
                else
                {
                    GetAnimalNumber(out int WoAnimalNum, out int PengAnimalNum);
                    if (WoAnimalNum < 10) //窝的动物数量小于10
                    {
                        var raiseElemt = GetFirstRaise(true);
                        if (raiseElemt == null)
                        {
                            TryFindElement(By.LinkText("商店"), out IWebElement store);
                            store.Click();
                            PurchaseCubs("窝");
                            continue;
                        }
                        raiseElemt.Click();
                        Thread.Sleep(1000);
                        Logger.WriteLog(GetInfo(By.ClassName("module-content")));
                        if (TryFindElement(By.Name("sb"), out IWebElement confirm)) //确定
                        {
                            confirm.Click();
                            Thread.Sleep(1000);
                            Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                        }
                    }
                    else if (PengAnimalNum < 10) //棚的动物数量小于10
                    {
                        var raiseElemt = GetFirstRaise(false);
                        if (raiseElemt == null)
                        {
                            TryFindElement(By.LinkText("商店"), out IWebElement store);
                            store.Click();
                            PurchaseCubs("棚");
                            continue;
                        }
                        raiseElemt.Click();
                        Thread.Sleep(1000);
                        Logger.WriteLog(GetInfo(By.ClassName("module-content")));
                        if (TryFindElement(By.Name("sb"), out IWebElement confirm)) //确定
                        {
                            confirm.Click();
                            Thread.Sleep(1000);
                            Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
                        }
                    }
                }


                TryFindElement(By.LinkText("我的牧场"), out IWebElement backPasture3);
                backPasture3.Click();
                Thread.Sleep(1500);
            }
        }

        //out int WoAnimalNum, out int PengAnimalNum
        //动物：窝(10/10) 棚(2/10)
        void GetAnimalNumber(out int WoAnimalNum, out int PengAnimalNum)
        {
            var Animal = driver.FindElement(By.XPath("/html/body/div[2]/div[1]/div/p[1]")).Text;
            Logger.WriteLog(Animal);
            var s1 = Animal.Split(' ')[0];
            var s2 = Animal.Split(' ')[1];
            string woAnimal_n = s1.Substring(s1.IndexOf('(') + 1, s1.IndexOf('/') - (s1.IndexOf('(') + 1));
            string woAnimal_s = s2.Substring(s2.IndexOf('(') + 1, s2.IndexOf('/') - (s2.IndexOf('(') + 1));
            WoAnimalNum = Convert.ToInt32(woAnimal_n);
            PengAnimalNum = Convert.ToInt32(woAnimal_s);
        }


        string[] ExcludedAnimals = new string[1] { "金兔子" };
        /// <summary>
        /// 获取第一个窝/棚的动物饲养 标签
        /// </summary>
        /// <param name="IsAnimalsLiveInNest">是窝的动物</param>
        /// <returns></returns>
        IWebElement GetFirstRaise(bool IsAnimalsLiveInNest)
        {
            string AnimalTag = "窝";
            if (!IsAnimalsLiveInNest)
                AnimalTag = "棚";
            IWebElement el = null;
            int index = -1;
            var MyAnimalInfo = driver.FindElement(By.XPath("/html/body/div[2]/div[3]/div[2]")).Text;
            var MyAnimalInfoList = MyAnimalInfo.Split('\n');
            for (int i = 0; i < MyAnimalInfoList.Length; i++)
            {
                if (MyAnimalInfoList[i].Contains(AnimalTag))
                {
                    //排除不可养殖的动物
                    if(ExcludedAnimals.Where(a => MyAnimalInfoList[i].Contains(a)).Count() != 0)
                    {
                        Logger.WriteLog($"{ExcludedAnimals} 不可养殖，跳过。");
                        continue;
                    }
                    Logger.WriteLog($"找到动物{MyAnimalInfoList[i]}。");
                    index = i;
                    break;
                }
            }
            if(index == -1)
            {
                Logger.WriteLog($"没有找到可养殖的动物。");
                return null;
            }

            ReadOnlyCollection<IWebElement> elements = driver.FindElements(By.LinkText("饲养"));
            if (elements.Count > 0)
            {
                el = elements[index];
            }
            return el;
        }

        /// <summary>
        /// 购买幼崽
        /// </summary>
        void PurchaseCubs(string cubType)
        {
            if (cubType == "窝")
            {
                GetFirstNestPurchase().Click();
                Thread.Sleep(1500);
            }
            else if (cubType == "棚")
            {
                GetFirstShedPurchase().Click();
                Thread.Sleep(1500);
            }
            else
            {
                return;
            }
            Logger.WriteLog(GetInfo(By.ClassName("farm-special")));
            TryFindElement(By.ClassName("btn-s"), out IWebElement confirmBtn);
            confirmBtn.Click();
            Thread.Sleep(1500);
            Logger.WriteLog(GetInfo(By.ClassName("txt-warning2")));
            TryFindElement(By.LinkText("我的牧场"), out IWebElement backPasture4);
            backPasture4.Click();
            Thread.Sleep(1500);
        }


        IWebElement GetFirstNestPurchase()
        {
            List<IWebElement> Elements = new List<IWebElement>();
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[1]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[2]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[3]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[4]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[5]")));

            foreach (var em in Elements)
            {
                string cubInfo = em.FindElements(By.TagName("p"))[0].Text;
                string typeAndLv = cubInfo.Substring(cubInfo.IndexOf('(')+1, cubInfo.IndexOf("级)") - cubInfo.IndexOf('('));
                if (typeAndLv.Split(',')[0] == "住窝")
                {
                    return em.FindElement(By.LinkText("购买"));
                }

            }
            return null;
        }

        IWebElement GetFirstShedPurchase()
        {
            List<IWebElement> Elements = new List<IWebElement>();
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[1]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[2]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[3]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[4]")));
            Elements.Add(driver.FindElement(By.XPath("/html/body/div[3]/div[2]/div[5]")));

            foreach (var em in Elements)
            {
                string cubInfo = em.FindElements(By.TagName("p"))[0].Text;
                string typeAndLv = cubInfo.Substring(cubInfo.IndexOf('(') + 1, cubInfo.IndexOf("级)") - cubInfo.IndexOf('('));
                if (typeAndLv.Split(',')[0] == "住棚")
                {
                    return em.FindElement(By.LinkText("购买"));
                }

            }
            return null;
        }

        /// <summary>
        /// 关闭浏览器
        /// </summary>
        private void Close()
        {
            //driver.Dispose();
            if (driver != null)
            {
                //driver.Close(); //关闭一个Tab页
                driver.Quit(); //关闭所有tab页，并会退出msedgedriver服务进程
            }
            if (driverService != null)
            {
                driverService.Dispose();
            }

            //string pName = "msedgedriver";
            //Process[] processes = Process.GetProcessesByName(pName);//在所有已启动的进程中查找需要的进程；
            //foreach (var p in processes)
            //{
            //    p.Kill();
            //}

            Logger.WriteLog("任务结束，关闭浏览器\n");
        }


        public bool TryFindElementInFrame(By by, out IWebElement element, IWebDriver parentFrame)
        {
            ReadOnlyCollection<IWebElement> elements = parentFrame.FindElements(by);
            if (elements.Count > 0)
            {
                element = elements[0];
                return true;
            }
            else
            {
                element = null;
                return false;
            }

        }

        #region 滑块验证

        /// <summary>
        /// 下载图片
        /// </summary>
        void DownloadImage()
        {
            try
            {
                TryFindElementInFrame(By.Id("slideBg"), out IWebElement slideBg, LoginFrame);
                var bgImg = slideBg.GetCssValue("background-image");
                Match match = Regex.Match(bgImg, "url\\(\"(.*?)\"\\)");
                if (match.Success)
                {
                    Logger.WriteLog($"开始下载验证图片");
                    string imgURL = match.Groups[1].Value;
                    HttpDownload(imgURL, $"{AppDomain.CurrentDomain.BaseDirectory}VerifyBack.png");
                    Logger.WriteLog($"下载完成");
                }
                else
                {
                    Logger.WriteLog($"获取图片下载地址 ,格式匹配失败");
                    Logger.WriteLog($"地址：{bgImg}");
                }
        
            }
            catch (Exception e)
            {
                Logger.WriteLog($"下载图片出错{e}");
            }
        }

        /// <summary>
        /// 通过ML.Net机器学习模型获取验证图片缺口位置
        /// </summary>
        public bool GetGapPosByMLNet(out double horizontalPos)
        {
            horizontalPos = 0;
            // Create single instance of sample data from first line of dataset for model input.
            var image = MLImage.CreateFromFile($"{AppDomain.CurrentDomain.BaseDirectory}VerifyBack.png");
            Slider.ModelInput sampleData = new Slider.ModelInput()
            {
                Image = image,
            };
            // Make a single prediction on the sample data and print results.
            var predictionResult = Slider.Predict(sampleData);
            Console.WriteLine("\n\nPredicted Boxes:\n");
            if (predictionResult.PredictedBoundingBoxes == null)
            {
                Logger.WriteLog($"预测失败");
                return false;
            }
            Logger.WriteLog($"预测完成");
            var boxes =
                predictionResult.PredictedBoundingBoxes.Chunk(4)
                    .Select(x => new { XTop = x[0], YTop = x[1], XBottom = x[2], YBottom = x[3] })
                    .Zip(predictionResult.Score, (a, b) => new { Box = a, Score = b });

            foreach (var item in boxes)
            {
                Logger.WriteLog($"XTop: {item.Box.XTop},YTop: {item.Box.YTop},XBottom: {item.Box.XBottom},YBottom: {item.Box.YBottom}, Score: {item.Score}");
            }
            var maxScoreBox = boxes.OrderByDescending(b => b.Score).First();
            horizontalPos = maxScoreBox.Box.XTop + (maxScoreBox.Box.XBottom - maxScoreBox.Box.XTop) / 2;
            Logger.WriteLog($"缺口水平像素值:{horizontalPos}");
            return true; 

        }

        void SliderVerification()
        {
            double pos = 0;
            int i = 0;
            for (; i < 50; i++)
            {
                DownloadImage();
                if (GetGapPosByMLNet(out pos))
                {
                    break;
                }
                else
                {

                    Logger.WriteLog($"刷新新的验证图片");
                    TryFindElementInFrame(By.Id("reload"), out IWebElement reload, LoginFrame);
                    reload.Click();
                    Thread.Sleep(2000);
                }
            }
            if(pos == 0 && i == 49)
            {
                Logger.WriteLog($"50次预测没有成功，结束本次任务！");
                throw new Exception("图片缺口预测失败");
            }

            var sliderWidth = 65.77;
            var sliderLeft = 22.76;
            //672 /340 = pos / x;
            var zoomXPos = 340 * pos / 672;
            var horDistance = zoomXPos - sliderLeft - (sliderWidth/2);
            //找到滑块元素
            TryFindElementInFrame(By.ClassName("tc-slider-normal"), out IWebElement slide, LoginFrame);
    
            var action = new Actions(LoginFrame);
            //模仿人工滑动
            action.ClickAndHold(slide).Perform(); //点击并按住滑块元素
            Logger.WriteLog("按住");
            action.MoveByOffset((int)horDistance, 0).Perform();
            Logger.WriteLog("拖动");
            driver.GetScreenshot().SaveAsFile($"{AppDomain.CurrentDomain.BaseDirectory}MoveSliderScreenshot.png");
            action.Release().Perform();
            Logger.WriteLog("释放");

            Thread.Sleep(1000);

        }
        #endregion

        /// <summary>
        /// 寻找第一个匹配的节点
        /// </summary>
        /// <param name="by"></param>
        /// <param name="element">找到节点并赋值给该对象，没有则为null</param>
        /// <returns>true，成功找到节点；反之为 false</returns>
        public bool TryFindElement(By by, out IWebElement element)
        {
            ReadOnlyCollection<IWebElement> elements = driver.FindElements(by);
            if (elements.Count > 0)
            {
                element = elements[0];
                return true;
            }
            else
            {
                element = null;
                return false;
            }
        }

        /// <summary>
        /// 寻找匹配的所有节点
        /// </summary>
        /// <param name="by"></param>
        /// <param name="element">找到节点并赋值给该对象，没有则为null</param>
        /// <returns>true，成功找到节点；反之为 false</returns>
        public bool TryFindElements(By by, out ReadOnlyCollection<IWebElement> elements)
        {
            elements = driver.FindElements(by);
            if (elements.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public string GetInfo(By by, IWebElement parentElement = null)
        {
            ReadOnlyCollection<IWebElement> elements;
            if (parentElement == null)
            {
                 elements = driver.FindElements(by);
            }
            else
            {
                elements = parentElement.FindElements(by);
            }
            string info = "";
            if (elements.Count > 0)
            {
                foreach (var em in elements)
                {
                    info += em.Text + "; ";
                }
            }
            else
            {
                Logger.WriteLog($"GetInfo({by}) 没有找到此节点.");
            }
            return info;
        }

        public void Dispose()
        {
        }
    }


    /// <summary>
    /// 定时任务类
    /// </summary>
    class ScheduledTask
    {

        public List<TimerObject> TimerList = new List<TimerObject>();
        public void StartExecuteTask()
        {
            TimerList.Add(CreateDailyScheduledTask(0, 0));
            TimerList.Add(CreateDailyScheduledTask(3, 0));
            TimerList.Add(CreateDailyScheduledTask(6, 0));
            TimerList.Add(CreateDailyScheduledTask(9, 0));
            TimerList.Add(CreateDailyScheduledTask(12, 0));
            TimerList.Add(CreateDailyScheduledTask(15, 0));
            TimerList.Add(CreateDailyScheduledTask(18, 0));
            TimerList.Add(CreateDailyScheduledTask(21, 0));

            //TimerList.Add(CreateDailyScheduledTask(20, 3));
        }


        private TimerObject CreateDailyScheduledTask(int hour, double min)
        {
            Thread.Sleep(50);

            double time = hour + (min / 60f);
            DateTime now = DateTime.Now;
            DateTime oneOClock = DateTime.Today.AddHours(time);
            if (now > oneOClock)
                oneOClock = oneOClock.AddDays(1.0);

            var timeState = new TimeState()
            {
                SetTime = time,
                TimeID = now,
            };
            int waitTime = (int)((oneOClock - now).TotalMilliseconds);
            TimerCallback timerDelegate = new TimerCallback(StartScheduledTask);
            var t = new Timer(timerDelegate, timeState, waitTime, Timeout.Infinite);
            TimerObject timerObject = new TimerObject()
            {
                TimeID = now,
                sTimer = t,
            };
            return timerObject;
        }

        //要执行的任务
        private void StartScheduledTask(object state)
        {
            //执行功能...
            AutomatedSelenium selenium = new AutomatedSelenium();
            selenium.StartTask();
            selenium = null;

            //再次设定
            var timeState = state as TimeState;
            //var time = Convert.ToDouble(state);
            int hour = (int)timeState.SetTime;
            var min = Convert.ToInt32((timeState.SetTime - hour) * 60);
            TimerList.Add(CreateDailyScheduledTask(hour, min));

            var timerObj = TimerList.Where(t => t.TimeID == timeState.TimeID).FirstOrDefault();
            if (timerObj != null)
                TimerList.Remove(timerObj);
            //Logger.WriteLog("TimerList数量:"+ TimerList.Count);
        }
    }


    public class TimeState
    {
        public double SetTime { get; set; }
        public DateTime TimeID { get; set; }
    }

    public class TimerObject
    {
        public DateTime TimeID { get; set; }

        public Timer sTimer { get; set; }

    }





    /// <summary>
    /// 日志
    /// </summary>
    public class Logger
    {
        public EdgeDriver driver { get; set; }

        public Logger(EdgeDriver _driver)
        {
            driver = _driver;
        }

        private static string LogPath = AppDomain.CurrentDomain.BaseDirectory + "Log.txt";

        private int logCount = 0;

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="strInfo"></param>
        public void WriteLog(string strInfo)
        {
            //string strPath = ConfigurationManager.AppSettings["FilePath"];
            FileExist(LogPath);
            using (StreamWriter sw = new StreamWriter(LogPath, true))
            {
                sw.WriteLine(DateTime.Now.ToString() + "-------------------" + strInfo);
                sw.Dispose();
                sw.Close();
            }

            if (string.IsNullOrWhiteSpace(strInfo))
            {
                DirExist($"{AppDomain.CurrentDomain.BaseDirectory}DebugScreenshot\\");

                if(logCount < 5)
                {
                    driver.GetScreenshot().SaveAsFile($"{AppDomain.CurrentDomain.BaseDirectory}DebugScreenshot\\{DateTime.Now.ToString("yy-MM-dd HHmmss")}.png");
                    var debugHtmlPath = $"{AppDomain.CurrentDomain.BaseDirectory}DebugScreenshot\\{DateTime.Now.ToString("yy-MM-dd HHmmss")}.html";
                    FileExist(debugHtmlPath);
                    using (StreamWriter sw = new StreamWriter(debugHtmlPath, true))
                    {
                        sw.WriteLine(driver.PageSource);
                        sw.Dispose();
                        sw.Close();
                    }
                    logCount++;
                }
            }
            else
            {
                if (!strInfo.Contains("没有找到此节点"))
                {
                    logCount = 0;
                }

                if (strInfo.Contains("异常"))
                {
                    try
                    {
                        if(driver != null)
                        {
                            driver.GetScreenshot().SaveAsFile($"{AppDomain.CurrentDomain.BaseDirectory}DebugScreenshot\\{DateTime.Now.ToString("异常-yy-MM-dd HHmmss")}.png");
                            var debugHtmlPath = $"{AppDomain.CurrentDomain.BaseDirectory}DebugScreenshot\\{DateTime.Now.ToString("异常-yy-MM-dd HHmmss")}.html";
                            FileExist(debugHtmlPath);
                            using (StreamWriter sw = new StreamWriter(debugHtmlPath, true))
                            {
                                sw.WriteLine(driver.PageSource);
                                sw.Dispose();
                                sw.Close();
                            }
                        }
                    }
                    catch(Exception e)
                    {

                    }
                   
                }
            }

        }

        private void FileExist(string fileName)
        {
            //判断文件是否存在
            if (!File.Exists(fileName))
            {
                //创建文件
                try
                {
                    var fileStream = File.Create(fileName);
                    fileStream.Dispose();
                    fileStream.Close();
                }
                catch (Exception e)
                {
                    //Logger.WriteLog(e.Message);
                }
            }
        }


        private void DirExist(string pathName)
        {
            //判断文件是否存在
            if (!Directory.Exists(pathName))
            {
                //创建文件
                try
                {
                    Directory.CreateDirectory(pathName);
                }
                catch (Exception e)
                {
                    WriteLog("创建目录失败：" + e.Message);
                }
            }
        }

    }













    public class Model 
    { 
        public int id { get; set; }


        public static void Test()
        {
            var a = new Model() { id = 1 };
            var b1 = new Model() { id = 2 };
            var b2 = new Model() { id = 2 };
            var d1 = new Model() { id = 4 };
            var d2 = new Model() { id = 4 };
            var c = new Model() { id = 3 };
            List<Model> ListA = new List<Model>();
            ListA.Add(a);
            ListA.Add(b1);
            ListA.Add(b1);
            ListA.Add(d1);

            List<Model> ListB = new List<Model>();
            ListB.Add(b2);
            ListB.Add(b1);
            ListB.Add(d2);
            ListB.Add(c);
            var list1 = ListA.Union(ListB).ToList();
            var list2 = ListA.Concat(ListB).ToList();
            var list3 = ListA.Intersect(ListB, new DistinctModuleHelper()).ToList();


            var  list = ListA.Union(ListB).Except(ListA.Intersect(ListB, new DistinctModuleHelper()), new DistinctModuleHelper()).ToList();
        }
    }

    public class DistinctModuleHelper : IEqualityComparer<Model>
    {
        public bool Equals(Model x, Model y)
        {
            return x.id == y.id;
        }

        public int GetHashCode(Model m)
        {
            if (m == null)
                return 0;
            return m.id.GetHashCode();
        }
    }

}
