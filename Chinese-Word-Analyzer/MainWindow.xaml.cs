using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chinese_Word_Analyzer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ResetLanguageResource(GetDefaultLanguageResource());
            InitializeComponent();
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

        private void ResetLanguageResource(ResourceDictionary NewLanguageResource)
        {
            if (CurrentLanguageResource != null)
                App.Current.Resources.MergedDictionaries.Remove(CurrentLanguageResource);
            App.Current.Resources.MergedDictionaries.Add(NewLanguageResource);

            CurrentLanguageResource = NewLanguageResource;
        }

        private static ResourceDictionary GetDefaultLanguageResource()
        {
            string SystemLanguage = System.Globalization.CultureInfo.InstalledUICulture.Name;
            SystemLanguage = "en-US";

            if (SystemLanguage.Equals("zh-CN"))
                return new ResourceDictionary() { Source = new Uri(lang_zh_chs, UriKind.RelativeOrAbsolute) };
            if (SystemLanguage.IndexOf("zh") != -1)
                return new ResourceDictionary() { Source = new Uri(lang_zh_cht, UriKind.RelativeOrAbsolute) };
            return new ResourceDictionary() { Source = new Uri(lang_en_us, UriKind.RelativeOrAbsolute) };
        }

        private ResourceDictionary CurrentLanguageResource;

        const string lang_zh_chs = "LanguageResource/lang_zh-chs.xaml";
        const string lang_zh_cht = "LanguageResource/lang_zh-cht.xaml";
        const string lang_en_us = "LanguageResource/lang_en-us.xaml";
    }
}
