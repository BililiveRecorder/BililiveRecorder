using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _m = null;
        protected override void OnExit(ExitEventArgs e) => _m?.ReleaseMutex();
        protected override void OnStartup(StartupEventArgs e)
        {

            if (Debugger.IsAttached || e.Args.Contains("-o") || e.Args.Contains("--offline")
#if DEBUG
                || true
#endif
                    )
            {
                return;
            }

            if (DateTime.Now > new DateTime(2019, 1, 4, 17, 0, 0))
            {
                MessageBox.Show("请使用最新版本\n官网： https://rec.danmuji.org/", "录播姬", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }
            else if (DateTime.Now > new DateTime(2018, 12, 1))
            {
                MessageBox.Show("此版本已不再维护更新。\n" +
                    "请在控制面板卸载此版本，使用官网上最新版本安装包重新安装。\n" +
                    "官网： https://rec.danmuji.org/ \n" +
                    "QQ群： 689636812", "录播姬", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                try
                {
                    _m = new Mutex(true, @"Global\BililiveRecorder.WPF", out bool b);
                    if (!b)
                    {
                        _m = null;
                        MessageBox.Show("录播姬已经在运行了", "", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        Current.Shutdown(2);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "启动时出现错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown(3);
                }
            }
            else
            {
                MessageBox.Show("请使用桌面上或开始菜单里的快捷方式打开", "你的打开方式不正确", MessageBoxButton.OK, MessageBoxImage.Warning);
                Current.Shutdown(1);
            }
        }


    }
}
