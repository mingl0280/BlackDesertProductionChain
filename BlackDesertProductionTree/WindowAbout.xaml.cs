using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reflection;
using System.Diagnostics;

namespace BlackDesertProductionTree
{
    /// <summary>
    /// WindowAbout.xaml 的交互逻辑
    /// </summary>
    public partial class WindowAbout : Window
    {
        public WindowAbout()
        {
            InitializeComponent();
            lblVersion.Content = Assembly.GetExecutingAssembly().GetName().Version;
            lblOpenSourceAnnounce.Text = "项目使用了Newtonsoft.Json (https://www.newtonsoft.com/json)\r\n和 System.Data.SQLite (https://system.data.sqlite.org/)";
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LblLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "http://bd.youxidudu.com/";
            p.Start();
        }

        private void WndAbout_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
                BtnOK.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        private class AnnounceText
        {
            public override string ToString()
            {
                return "";
            }
        }
    }

    
}
