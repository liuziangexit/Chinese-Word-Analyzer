using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Chinese_Word_Analyzer
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            IconRemover.RemoveIcon(this);
        }

        private void SourceAndLicensePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/liuziangexit/Chinese-Word-Analyzer");
            }
            catch (Exception)
            {
                try
                {
                    System.Diagnostics.Process.Start("IExplore.exe", "https://github.com/liuziangexit/Chinese-Word-Analyzer");
                }
                catch (Exception) { }
            }
        }
    }
}
