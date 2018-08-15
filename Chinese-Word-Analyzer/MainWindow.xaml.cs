using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

        //视图

        protected override void OnSourceInitialized(EventArgs e)
        {
            IconRemover.RemoveIcon(this);
        }

        private void ApplicationCommandsOpen(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog box = new Microsoft.Win32.OpenFileDialog
            {
                Title = App.Current.FindResource("OpenDataSource.OpenFileDialog.Title") as string,
                DefaultExt = ".txt",
                Filter = App.Current.FindResource("FileTypes.Txt") as string + "|*.txt|" + App.Current.FindResource("FileTypes.All") + "|*.*",

                DereferenceLinks = true,
                Multiselect = false
            };

            Nullable<bool> isSelected = box.ShowDialog(this);
            if (isSelected != true)
            {
                MessageBox.Show(App.Current.FindResource("OpenDataSource.OpenFileDialog.NotSelected") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            StoreAndDisplayDataSource(LoadDataSource(box.FileName));
        }

        private void ApplicationCommandsFind(object sender, ExecutedRoutedEventArgs e)
        {
            var box = new SearchBox
            {
                Title = App.Current.FindResource("SearchBox.Title") as string,
                Owner = this
            };
            box.ShowDialog();
            if (box.Action == SearchBox.SearchBoxAction.None
                || string.IsNullOrEmpty(box.SearchKeyString)
                || string.IsNullOrWhiteSpace(box.SearchKeyString))
                return;

            if (Char2Radicals == null || Radical2Chars == null)
                return;

            ResetStatusText();

            void SetStatusRadicalCountTextBlockAsUnavailableFunc() => StatusRadicalCountText.SetResourceReference(TextBlock.TextProperty, "StatusBar.Unavailable");
            void UpdateDataViewToEmpty() => RefreshDataView(null, null, SetStatusRadicalCountTextBlockAsUnavailableFunc);
            switch (box.Action)
            {
                case SearchBox.SearchBoxAction.SearchByWord: SearchByWord(box.SearchKeyString[0], SetStatusRadicalCountTextBlockAsUnavailableFunc, UpdateDataViewToEmpty); break;
                case SearchBox.SearchBoxAction.SearchByRadical: SearchByRadical(box.SearchKeyString[0], SetStatusRadicalCountTextBlockAsUnavailableFunc, UpdateDataViewToEmpty); break;
                case SearchBox.SearchBoxAction.SearchByMultipleRadical: SearchByMultipleRadical(box.SearchKeyString, SetStatusRadicalCountTextBlockAsUnavailableFunc, UpdateDataViewToEmpty); break;
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void LanguageControlClick(object sender, RoutedEventArgs e)
        {
            RefreshLanguageMenuAndLanguageSetting(ResetLanguageResource(GetLanguage((sender as MenuItem).Header as string)));
        }

        private void UseSystemLanguageClick(object sender, RoutedEventArgs e)
        {
            RefreshLanguageMenuAndLanguageSetting(ResetLanguageResource(GetLanguage(GetSystemLanguageResourceKey())));
        }

        private void ClearSearchResultMenuItemClick(object sender, RoutedEventArgs e)
        {
            ClearSearchResult();
        }

        private void DisplayRadicalsByNumberOfReferencesMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (Char2Radicals == null || Radical2Chars == null)
                return;
            ResetStatusText();
            RefreshDataViewWithRadicalsOrderByCharCount(Radical2Chars, () => StatusCharCountText.SetResourceReference(TextBlock.TextProperty, "StatusBar.Unavailable"));
        }

        private void AboutMenuItemClick(object sender, RoutedEventArgs e)
        {
            var box = new About() { Owner = this };
            box.ShowDialog();
        }

        //视图-接口

        private void ResetStatusText()
        {
            StatusRadicalCountText.Text = Radical2Chars.Keys.Count.ToString();
            StatusCharCountText.SetBinding(TextBlock.TextProperty, new Binding { ElementName = "DataView", Path = new PropertyPath("Items.Count") });
        }

        private void ClearSearchResult()
        {
            if (Char2Radicals != null && Radical2Chars != null)
                RefreshDataViewWithChars(Char2Radicals, ResetStatusText);
        }

        private void RefreshDataView(GridView View, IEnumerable ItemSource, Action UpdateInterfaceFunc)
        {
            DataView.View = View;
            DataView.ItemsSource = ItemSource;
            UpdateInterfaceFunc();
        }

        private void RefreshDataViewWithChars(Dictionary<char, List<string>> InputChar2Radicals, Action UpdateInterfaceFunc)
        {
            var View = new GridView();

            if (InputChar2Radicals.Count != 0)
            {
                //first header
                var Header0Text = new TextBlock { Margin = new Thickness { Right = 30 } };
                Header0Text.SetResourceReference(TextBlock.TextProperty, "DataView.Header0");
                View.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = Header0Text }, DisplayMemberBinding = new Binding("Key") });

                //rest of headers
                int ColumnCountNeed = InputChar2Radicals.Values.Max(list => list.Count);
                if (ColumnCountNeed > 0)
                {
                    for (int i = 0; i < ColumnCountNeed; i++)
                    {
                        var HeaderNText = new TextBlock { Margin = new Thickness { Right = 30 } };
                        HeaderNText.SetResourceReference(TextBlock.TextProperty, "DataView.HeaderN");
                        View.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = HeaderNText }, DisplayMemberBinding = new Binding("Value[" + i.ToString() + "]") });
                    }
                }
            }

            RefreshDataView(View, InputChar2Radicals.ToList(), UpdateInterfaceFunc);
        }

        private void RefreshDataViewWithRadicalsOrderByCharCount(Dictionary<char, string> InputRadical2Chars, Action UpdateInterfaceFunc)
        {
            var View = new GridView();

            if (InputRadical2Chars.Count != 0)
            {
                var Header0Text = new TextBlock { Margin = new Thickness { Right = 30 } };
                Header0Text.SetResourceReference(TextBlock.TextProperty, "DataView.Radicals.Header0");
                View.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = Header0Text }, DisplayMemberBinding = new Binding("Key") });

                var Header1Text = new TextBlock { Margin = new Thickness { Right = 30 } };
                Header1Text.SetResourceReference(TextBlock.TextProperty, "DataView.Radicals.Header1");
                View.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = Header1Text }, DisplayMemberBinding = new Binding("Value.Length") });

                var Header2Text = new TextBlock { Margin = new Thickness { Right = 30 } };
                Header2Text.SetResourceReference(TextBlock.TextProperty, "DataView.Radicals.Header2");
                View.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = Header2Text, HorizontalContentAlignment = HorizontalAlignment.Left }, DisplayMemberBinding = new Binding("Value") });
            }

            RefreshDataView(View, InputRadical2Chars.ToList().OrderByDescending(kv => kv.Value.Length), UpdateInterfaceFunc);
        }

        //控制器-主要功能

        private void SearchByWord(char Word, Action UpdateStatusRadicalCountTextFunc, Action UpdateDataViewToEmpty)
        {
            if (Char2Radicals.ContainsKey(Word))
                RefreshDataViewWithChars(new Dictionary<char, List<string>> { { Word, Char2Radicals[Word] } }, UpdateStatusRadicalCountTextFunc);
            else
                UpdateDataViewToEmpty();
        }

        private void SearchByRadical(char Radical, Action UpdateStatusRadicalCountTextFunc, Action UpdateDataViewToEmpty)
        {
            if (Radical2Chars.ContainsKey(Radical))
                RefreshDataViewWithChars(DoSearchByRadical(Radical, Radical2Chars), UpdateStatusRadicalCountTextFunc);
            else
                UpdateDataViewToEmpty();
        }

        private void SearchByMultipleRadical(string Radicals, Action UpdateStatusRadicalCountTextFunc, Action UpdateDataViewToEmpty)
        {
            IEnumerable<char> IntersectedChars = null;

            foreach (var Radical in Radicals)
            {
                if (Radical2Chars.TryGetValue(Radical, out string Chars))
                {
                    if (IntersectedChars == null)
                    {
                        IntersectedChars = Chars.ToList();
                        continue;
                    }
                    IntersectedChars = IntersectedChars.Intersect(Chars.ToList());
                }
            }

            if (IntersectedChars == null)
            {
                UpdateDataViewToEmpty();
                return;
            }

            Dictionary<char, List<string>> result = new Dictionary<char, List<string>>();
            foreach (var Char in IntersectedChars)
                result.Add(Char, Char2Radicals[Char]);
            RefreshDataViewWithChars(result, UpdateStatusRadicalCountTextFunc);
        }

        private Dictionary<char, List<string>> DoSearchByRadical(char Radical, Dictionary<char, string> Source)
        {
            var Result = new Dictionary<char, List<string>>();
            var Chars = Source[Radical];
            foreach (var Character in Chars)
                Result.Add(Character, Char2Radicals[Character]);
            return Result;
        }

        //数据-接口

        private Tuple<Dictionary<char, List<string>>, Dictionary<char, string>> LoadDataSource(string FileName)
        {
            var Data = new ChineseWordDataSource();
            try
            {
                Data.Load(FileName);
            }
            catch (Exception ex)
            {
                Data = null;
                MessageBox.Show(ex.Message, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return new Tuple<Dictionary<char, List<string>>, Dictionary<char, string>>(null, null);
            }

            var C2R = new Dictionary<char, List<string>>();
            foreach (var p in Data.WordDetails)
                C2R[p.Word] = p.Radicals;

            var R2C = new Dictionary<char, string>();
            foreach (var kv in C2R)
            {
                foreach (var Radicals in kv.Value)
                {
                    foreach (var Ch in Radicals)
                    {
                        if (!R2C.ContainsKey(Ch))
                            R2C.Add(Ch, "");
                        if (!R2C[Ch].Contains(kv.Key))
                            R2C[Ch] += kv.Key;
                    }
                }
            }

            return new Tuple<Dictionary<char, List<string>>, Dictionary<char, string>>(C2R, R2C);
        }

        private void StoreAndDisplayDataSource(Tuple<Dictionary<char, List<string>>, Dictionary<char, string>> result)
        {
            Char2Radicals = result.Item1;
            Radical2Chars = result.Item2;

            ClearSearchResult();
        }

        //多语言(控制器&数据)

        private void LoadRegionCodes()
        {
            App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("LanguageResource/regioncodes.xaml", UriKind.Relative) });
        }

        private void LoadLanguagesAndBuildLanguageMenu()
        {
            var LanguageResourceConfig = new ResourceDictionary() { Source = new Uri("LanguageResource/languages.xaml", UriKind.Relative) };
            foreach (var key in LanguageResourceConfig.Keys)
            {
                var addMe = new MenuItem
                {
                    Header = key
                };
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
            return new Tuple<string, ResourceDictionary>(LanguageResourceKey, new ResourceDictionary() { Source = new Uri(App.Current.FindResource(LanguageResourceKey) as string, UriKind.Relative) });
        }

        private static Tuple<string, ResourceDictionary> GetSettedOrSystemLanguage()
        {
            string LanguageResourceKey = Properties.Settings.Default.LanguageResourceKey as string;
            if (string.IsNullOrEmpty(LanguageResourceKey))
                LanguageResourceKey = GetSystemLanguageResourceKey();
            return GetLanguage(LanguageResourceKey);
        }

        private string ResetLanguageResource(Tuple<string, ResourceDictionary> NewLanguage)
        {
            if (CurrentLanguageResource != null)
                App.Current.Resources.MergedDictionaries.Remove(CurrentLanguageResource);
            App.Current.Resources.MergedDictionaries.Add(NewLanguage.Item2);

            CurrentLanguageResource = NewLanguage.Item2;
            return NewLanguage.Item1;
        }

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

        private ResourceDictionary CurrentLanguageResource { get; set; }//当前使用的语言字典

        private Dictionary<char, List<string>> Char2Radicals;//汉字对部首
        private Dictionary<char, string> Radical2Chars;//部首对汉字
    }
}
