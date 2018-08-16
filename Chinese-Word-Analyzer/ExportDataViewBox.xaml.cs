using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Chinese_Word_Analyzer
{
    /// <summary>
    /// ExportDataViewBox.xaml 的交互逻辑
    /// </summary>
    public partial class ExportDataViewBox : Window
    {
        public ExportDataViewBox()
        {
            InitializeComponent();

            SeparateColumnsByTextBox.Text = Properties.Settings.Default.ExportDataViewBoxSeparateColumnsBy;
            SeparateRowsByTextBox.Text = Properties.Settings.Default.ExportDataViewBoxSeparateRowsBy;
            WithHeaderCheckBox.IsChecked = Properties.Settings.Default.ExportDataViewBoxWithHeader;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            IconRemover.RemoveIcon(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            IconRemover.SetWindowLong(hwnd, IconRemover.GWL_STYLE, IconRemover.GetWindowLong(hwnd, IconRemover.GWL_STYLE) & ~IconRemover.WS_SYSMENU);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.ExportDataViewBoxSeparateColumnsBy = SeparateColumnsByTextBox.Text;
            Properties.Settings.Default.ExportDataViewBoxSeparateRowsBy = SeparateRowsByTextBox.Text;
            Properties.Settings.Default.ExportDataViewBoxWithHeader = WithHeaderCheckBox.IsChecked == true;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OkButton.Focus();
                OkButtonClick(OkButton, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton.Focus();
                CancelButtonClick(CancelButton, new RoutedEventArgs());
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            SeparateRowsBy = null;
            SeparateColumnsBy = null;
            ExportTo = null;
            WithHeader = false;
            Close();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            SeparateRowsBy = SeparateRowsByTextBox.Text;
            if (SeparateRowsBy == null)
                SeparateRowsBy = "";
            SeparateRowsBy = Regex.Unescape(SeparateRowsBy);

            SeparateColumnsBy = SeparateColumnsByTextBox.Text;
            if (SeparateColumnsBy == null)
                SeparateColumnsBy = "";
            SeparateColumnsBy = Regex.Unescape(SeparateColumnsBy);

            Microsoft.Win32.SaveFileDialog box = new Microsoft.Win32.SaveFileDialog
            {
                Title = App.Current.FindResource("ExportDataView.SaveFileDialog.Title") as string,
                DefaultExt = ".txt",
                Filter = App.Current.FindResource("FileTypes.Txt") as string + "|*.txt|" + App.Current.FindResource("FileTypes.All") + "|*.*",
                DereferenceLinks = true
            };
            if (box.ShowDialog() != true)
                return;

            ExportTo = box.FileName;

            if (WithHeaderCheckBox.IsChecked == true)
                WithHeader = true;

            Close();
        }
        
        public string SeparateRowsBy { get; set; }
        public string SeparateColumnsBy { get; set; }
        public string ExportTo { get; set; }
        public bool WithHeader { get; set; } = false;
    }
}
