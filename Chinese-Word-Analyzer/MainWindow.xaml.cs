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
            InitializeComponent();
            LoadRegionCodes();
            LoadLanguagesAndBuildLanguageMenu();
            RefreshLanguageMenuAndLanguageSetting(ResetLanguageResource(GetSettedOrSystemLanguage()));
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

        //load region codes to application dictionary
        private void LoadRegionCodes()
        {
            App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("LanguageResource/regioncodes.xaml", UriKind.Relative) });
        }

        //load language list to application dictionary and build language menu
        private void LoadLanguagesAndBuildLanguageMenu()
        {
            var LanguageResourceConfig = new ResourceDictionary() { Source = new Uri("LanguageResource/languages.xaml", UriKind.Relative) };
            foreach (var key in LanguageResourceConfig.Keys)
            {
                var addMe = new MenuItem();
                addMe.Header = key;
                addMe.Click += new RoutedEventHandler(this.LanguageControlClick);
                LanguageMenu.Items.Insert(0, addMe);
            }
            App.Current.Resources.MergedDictionaries.Add(LanguageResourceConfig);
        }

        private static string GetSystemLanguageResourceKey()
        {
            string LanguageResourceKey = App.Current.TryFindResource(System.Globalization.CultureInfo.InstalledUICulture.Name) as string;
            if (LanguageResourceKey == null)
                LanguageResourceKey = "Default";
            return LanguageResourceKey;
        }

        private static Tuple<string, ResourceDictionary> GetLanguage(string LanguageResourceKey)
        {
            return new Tuple<string, ResourceDictionary>(LanguageResourceKey, new ResourceDictionary() { Source = new Uri(App.Current.TryFindResource(LanguageResourceKey) as string, UriKind.Relative) });
        }

        //获得用户已设定的语言，如果用户没有指定，则使用默认语言(取决于操作系统语言)
        private static Tuple<string, ResourceDictionary> GetSettedOrSystemLanguage()
        {
            string LanguageResourceKey = Properties.Settings.Default.LanguageResourceKey as string;
            if (string.IsNullOrEmpty(LanguageResourceKey))
                LanguageResourceKey = GetSystemLanguageResourceKey();
            return GetLanguage(LanguageResourceKey);
        }

        //remove old language resource then add new language resource to application dictionary
        //returns language's DisplayName
        private string ResetLanguageResource(Tuple<string, ResourceDictionary> NewLanguage)
        {
            if (CurrentLanguageResource != null)
                App.Current.Resources.MergedDictionaries.Remove(CurrentLanguageResource);
            App.Current.Resources.MergedDictionaries.Add(NewLanguage.Item2);

            CurrentLanguageResource = NewLanguage.Item2;
            return NewLanguage.Item1;
        }

        //设置语言菜单中当前已选的语言，并设置程序Setting
        private void RefreshLanguageMenuAndLanguageSetting(string LanguageResourceKey)
        {
            foreach (object p in LanguageMenu.Items)
                if (p is MenuItem)
                    (p as MenuItem).IsChecked = false;

            foreach (object p in LanguageMenu.Items)
                if (p is MenuItem)
                    if ((p as MenuItem).Header.Equals(LanguageResourceKey))
                        (p as MenuItem).IsChecked = true;

            Properties.Settings.Default.LanguageResourceKey = LanguageResourceKey;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void LanguageControlClick(object sender, RoutedEventArgs e)
        {
            RefreshLanguageMenuAndLanguageSetting(ResetLanguageResource(GetLanguage((sender as MenuItem).Header as string)));
        }

        private void UseSystemLanguage_Click(object sender, RoutedEventArgs e)
        {
            RefreshLanguageMenuAndLanguageSetting(ResetLanguageResource(GetLanguage(GetSystemLanguageResourceKey())));
        }

        public ResourceDictionary CurrentLanguageResource { get; private set; }
    }
}
