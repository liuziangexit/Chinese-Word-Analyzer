﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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

            if (box.ShowDialog(this) != true)
            {
                MessageBox.Show(App.Current.FindResource("OpenDataSource.OpenFileDialog.NotSelected") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            StoreAndDisplayDataSource(LoadDataSource(box.FileName));
        }

        private void ApplicationCommandsSave(object sender, ExecutedRoutedEventArgs e)
        {
            if (DataView.Items.Count == 0)
                return;

            var box = new ExportDataViewBox()
            {
                Owner = this
            };
            box.ShowDialog();

            if (box.SeparateRowsBy == null || box.SeparateColumnsBy == null || box.ExportTo == null)
                return;

            try
            {
                File.WriteAllText(box.ExportTo,
                    DeserializationDataView(DataView.View as GridView, DataView.ItemsSource as IList, box.SeparateRowsBy, box.SeparateColumnsBy, box.WithHeader),
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void ApplicationCommandsFind(object sender, ExecutedRoutedEventArgs e)
        {
            if (DataView.Items.Count == 0)
                return;

            var box = new SearchBox
            {
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

            void UpdateInterfaceWhenHasResult() => StatusRadicalCountText.SetResourceReference(TextBlock.TextProperty, "StatusBar.Unavailable");
            void UpdateInterfaceWhenNoResult()
            {
                var NoResultTextBlock = new TextBlock();
                NoResultTextBlock.SetResourceReference(TextBlock.TextProperty, "DataView.NoResult");
                RefreshDataView(null, new List<ListViewItem> { new ListViewItem { Content = NoResultTextBlock, HorizontalAlignment = HorizontalAlignment.Center } },
                    () => {
                        StatusRadicalCountText.SetResourceReference(TextBlock.TextProperty, "StatusBar.Unavailable");
                        StatusCharCountText.SetResourceReference(TextBlock.TextProperty, "StatusBar.Unavailable");
                    });
            }
            switch (box.Action)
            {
                case SearchBox.SearchBoxAction.SearchByWord: SearchByWord(box.SearchKeyString[0], UpdateInterfaceWhenHasResult, UpdateInterfaceWhenNoResult); break;
                case SearchBox.SearchBoxAction.SearchByRadical: SearchByRadical(box.SearchKeyString[0], UpdateInterfaceWhenHasResult, UpdateInterfaceWhenNoResult); break;
                case SearchBox.SearchBoxAction.SearchByMultipleRadical: SearchByMultipleRadical(box.SearchKeyString, UpdateInterfaceWhenHasResult, UpdateInterfaceWhenNoResult); break;
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

        private void ClearDataViewMenuItemClick(object sender, RoutedEventArgs e)
        {
            DataView.View = null;
            DataView.ItemsSource = null;
            Radical2Chars.Clear();
            Char2Radicals.Clear();
            ResetStatusText();
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

        private void RefreshDataView(GridView View, IList ItemsSource, Action UpdateInterfaceFunc)
        {
            DataView.View = View;
            DataView.ItemsSource = ItemsSource;
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

            var InputRadical2CharsList = InputRadical2Chars.ToList();
            RefreshDataView(View,
                (from kv in InputRadical2CharsList
                 orderby kv.Value.Length descending
                 select kv).ToList(),
                UpdateInterfaceFunc);
        }

        private string DeserializationDataView(GridView View, IList ItemsSource, string SeparateRowsBy, string SeparateColumnsBy, bool IncludeHeader)
        {
            var ColumnValuePath = (from ColumnInfo in View.Columns
                                   select (ColumnInfo.DisplayMemberBinding as Binding).Path).ToArray();
            var ObjType = ItemsSource.GetType().GetGenericArguments()[0];
            var Builder = new StringBuilder();

            if (IncludeHeader)
            {
                for (int i = 0; i < View.Columns.Count; i++)
                {
                    Builder.Append(((View.Columns[i].Header as GridViewColumnHeader).Content as TextBlock).Text.Replace(" ", ""));
                    if (i != ColumnValuePath.Length - 1)
                        Builder.Append(SeparateColumnsBy);
                }
                if (ItemsSource.Count != 0)
                    Builder.Append(SeparateRowsBy);
            }

            for (int i = 0; i < ItemsSource.Count; i++)
            {
                for (int i2 = 0; i2 < ColumnValuePath.Length; i2++)
                {
                    bool ShouldBreak = false;
                    var Paths = ColumnValuePath[i2].Path.Split('.');
                    var Obj = ItemsSource[i];
                    foreach (String part in Paths)
                    {
                        if (part.IndexOf('[') == -1)
                        {
                            Obj = Obj.GetType().GetProperty(part).GetValue(Obj, null);
                        }
                        else
                        {
                            IList ListObj = ObjType.GetProperty(part.Substring(0, part.IndexOf('['))).GetValue(Obj, null) as IList;
                            var Index = int.Parse(part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1));
                            if (Index > ListObj.Count - 2)
                                ShouldBreak = true;
                            Obj = ListObj[Index];
                        }
                    }
                    Builder.Append(Obj);
                    if (ShouldBreak)
                        break;
                    if (i2 != ColumnValuePath.Length - 1)
                        Builder.Append(SeparateColumnsBy);
                }
                if (i != ItemsSource.Count - 1)
                    Builder.Append(SeparateRowsBy);
            }
            return Builder.ToString();
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
                else
                {
                    IntersectedChars = null;
                    break;
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
            var C2R = new Dictionary<char, List<string>>();
            string[] FileContent = File.ReadAllLines(FileName, Encoding.UTF8);
            try
            {
                foreach (var p in FileContent)
                {
                    string[] cols = p.Split('\t');
                    var addMe = new KeyValuePair<char, List<string>>(cols[0][0], new List<string>(cols.Length - 1));
                    for (int i = 1; i < cols.Length; i++)
                        addMe.Value.Add(cols[i].Split(' ').Aggregate((current, next) => current + next));
                    C2R[addMe.Key] = addMe.Value;
                }
            }
            catch (Exception ex)
            {
                C2R = null;
                MessageBox.Show(ex.Message, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return new Tuple<Dictionary<char, List<string>>, Dictionary<char, string>>(null, null);
            }

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
