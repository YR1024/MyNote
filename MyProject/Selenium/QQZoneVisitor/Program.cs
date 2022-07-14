using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QQZoneVisitor
{
    class Program
    {
        static EdgeDriver driver;
        static IWebDriver LoginFrame;
        static IWebDriver VCodeFrame;

        static void Main(string[] args)
        {
            var msedgedriverPath = AppDomain.CurrentDomain.BaseDirectory;
            EdgeDriverService driverService = EdgeDriverService.CreateDefaultService(msedgedriverPath); //此处为msedgedriver.exe的存放路径

            EdgeOptions options = new EdgeOptions();
            //options.AddArgument("--headless"); //浏览器静默模式启动

            driver = new EdgeDriver(driverService, options);

            //LoginQZone("1642963395", "yr18723750041..");
            //LoginOut();
            //LoginQZone("193589375", "yr18723750041..");
            //LoginOut();
            LoginQZone("192799479", "18723750041..");
            LoginOut();


            driver.Close();
            //Console.ReadLine();
        }



        static void LoginQZone(string user, string pwd)
        {
            driver.Navigate().GoToUrl("https://qzone.qq.com/");

            LoginFrame = driver.SwitchTo().Frame(driver.FindElement(By.Id("login_frame")));
            if (TryFindElementInFrame(By.Id("switcher_plogin"), out IWebElement userPwdLogin))
            {
                userPwdLogin.Click();
                Thread.Sleep(50);
            }

            TryFindElementInFrame(By.Id("u"), out IWebElement username); //用户名控件ID
            TryFindElementInFrame(By.Id("p"), out IWebElement password); //密码控件ID
            TryFindElementInFrame(By.Id("login_button"), out IWebElement login); //登录控件ID
            username.SendKeys(user); //填入账号
            password.SendKeys(pwd); //填入密码
            login.Click(); //点击登录按钮
            Thread.Sleep(500);


            if (TryFindElementInFrame(By.Id("tcaptcha_iframe"), out IWebElement vCodeIframe))
            {
                VCodeFrame = LoginFrame.SwitchTo().Frame(LoginFrame.FindElement(By.Id("tcaptcha_iframe")));
                SliderVerification();
                Thread.Sleep(50);
            }
            
            Thread.Sleep(2500);

        }

        static void LoginOut()
        {
            driver.Manage().Cookies.DeleteAllCookies();
            //TryFindElement(By.Id("tb_logout"), out IWebElement logout);
            //logout.Click();
            Thread.Sleep(1000);
        }

        static void SliderVerification()
        {
            //找到滑块元素
            var slide = VCodeFrame.FindElement(By.Id("tcaptcha_drag_button"));
            //var verifyContainer = driver.FindElement(By.CssSelector(".nc-lang-cnt"));
            //var width = verifyContainer.Size.Width;
            var action = new Actions(VCodeFrame);
            int offset = 0;
            //模仿人工滑动
            
            const int Offset = 30;
            while (true)
            {
                action.ClickAndHold(slide).Perform(); //点击并按住滑块元素
                action.MoveByOffset(Offset + 20, 0).Perform();
                action.Release().Perform();
                Thread.Sleep(1000);
            }
        }


        public static bool TryFindElement(By by, out IWebElement element)
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

        public static bool TryFindElementInFrame(By by, out IWebElement element)
        {
            ReadOnlyCollection<IWebElement> elements = LoginFrame.FindElements(by);
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

    }
}
