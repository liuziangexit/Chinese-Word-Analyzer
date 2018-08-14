using System;
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

            Worker.WorkerReportsProgress = true;
            Worker.DoWork += new DoWorkEventHandler(WorkerDoWork);
            Worker.ProgressChanged += new ProgressChangedEventHandler(WorkerProgressChanged);
            Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);
        }

        //视图

        protected override void OnSourceInitialized(EventArgs e)
        {
            IconRemover.RemoveIcon(this);
        }

        private void ApplicationCommandsOpen(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog box = new Microsoft.Win32.OpenFileDialog();

            box.Title = App.Current.FindResource("OpenDataSource.OpenFileDialog.Title") as string;
            box.DefaultExt = ".txt";
            box.Filter = App.Current.FindResource("FileTypes.Txt") as string + "|*.txt|" + App.Current.FindResource("FileTypes.All") + "|*.*";

            box.DereferenceLinks = true;
            box.Multiselect = false;

            Nullable<bool> isSelected = box.ShowDialog(this);
            if (isSelected != true)
            {
                MessageBox.Show(App.Current.FindResource("OpenDataSource.OpenFileDialog.NotSelected") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            Worker.RunWorkerAsync(new BackgroundWorkArg() { Arg = box.FileName, Type = BackgroundWorkArg.WorkType.LoadDataSource });
        }

        private void ApplicationCommandsFind(object sender, ExecutedRoutedEventArgs e)
        {
            var box = new SearchBox();
            box.Title = App.Current.FindResource("SearchBox.Title") as string;
            box.Owner = this;
            box.ShowDialog();
            if (box.Action == SearchBox.SearchBoxAction.None
                || string.IsNullOrEmpty(box.SearchKeyString)
                || string.IsNullOrWhiteSpace(box.SearchKeyString))
                return;

            if (Char2Radicals == null || Radical2Chars == null)
                return;

            Action<TextBlock> SetTextBlockAsUnavailableFunc = t => t.SetResourceReference(TextBlock.TextProperty, "StatusBar.Unavailable");
            Action UpdateDataViewToEmpty = () => RefreshDataView(new Dictionary<char, List<string>>(), SetTextBlockAsUnavailableFunc);
            switch (box.Action)
            {
                case SearchBox.SearchBoxAction.SearchByWord: SearchByWord(box.SearchKeyString[0], SetTextBlockAsUnavailableFunc, UpdateDataViewToEmpty); break;
                case SearchBox.SearchBoxAction.SearchByRadical: SearchByRadical(box.SearchKeyString[0], SetTextBlockAsUnavailableFunc, UpdateDataViewToEmpty); break;
                case SearchBox.SearchBoxAction.SearchByMultipleRadical: SearchByMultipleRadical(box.SearchKeyString, SetTextBlockAsUnavailableFunc, UpdateDataViewToEmpty); break;
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

        private void DataSourceInfoMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (Char2Radicals == null || Radical2Chars == null)
            {
                MessageBox.Show(App.Current.FindResource("File.DataSourceInfo.NoDataSource") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

        }

        private void ClearSearchResultMenuItemClick(object sender, RoutedEventArgs e)
        {
            ClearSearchResult();
        }

        //视图-接口

        private void ClearSearchResult()
        {
            if (Char2Radicals != null && Radical2Chars != null)
                RefreshDataView(Char2Radicals, t => t.Text = Radical2Chars.Keys.Count.ToString());
        }

        private void RefreshDataView(Dictionary<char, List<string>> InputChar2Radicals, Action<TextBlock> UpdateStatusRadicalCountTextFunc)
        {
            //construct new ListView
            var Grid = new GridView();

            if (InputChar2Radicals.Count != 0)
            {
                //first header
                var Header0Text = new TextBlock { Margin = new Thickness { Right = 30 } };
                Header0Text.SetResourceReference(TextBlock.TextProperty, "DataView.Header0");
                Grid.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = Header0Text }, DisplayMemberBinding = new Binding("Word") });

                //rest of headers
                int ColumnCountNeed = InputChar2Radicals.Values.Max(list => list.Count);
                if (ColumnCountNeed > 0)
                {
                    for (int i = 0; i < ColumnCountNeed; i++)
                    {
                        var HeaderNText = new TextBlock { Margin = new Thickness { Right = 30 } };
                        HeaderNText.SetResourceReference(TextBlock.TextProperty, "DataView.HeaderN");
                        Grid.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = HeaderNText }, Width = 75, DisplayMemberBinding = new Binding("Radicals[" + i.ToString() + "]") });
                    }
                }
            }

            //assign the view
            DataView.View = Grid;

            //fill ListView with data
            var ItemSource = new List<ChineseWordDataSource.WordDetail>(InputChar2Radicals.Count);
            foreach (var p in InputChar2Radicals)
                ItemSource.Add(new ChineseWordDataSource.WordDetail() { Word = p.Key, Radicals = p.Value });
            DataView.ItemsSource = ItemSource;
            UpdateStatusRadicalCountTextFunc(StatusRadicalCountText);
        }

        private void DisableNewBackgroundWorkEntranceThreadSafe()
        {
            this.Dispatcher.Invoke((Action)delegate ()
            {
                OpenDataSourceMenuItem.IsEnabled = false;
                SearchMenuItem.IsEnabled = false;
            });
        }

        private void SetStatusProgressBarValue(int Value)
        {
            StatusProgressBar.Value = Value;
        }

        private void SetStatusTextContent(string ResourceKey)
        {
            StatusText.SetResourceReference(TextBlock.TextProperty, ResourceKey);
        }

        private void EnableNewBackgroundWorkEntrance()
        {
            OpenDataSourceMenuItem.IsEnabled = true;
            SearchMenuItem.IsEnabled = true;
        }

        //控制器-主要功能

        private void SearchByWord(char Word, Action<TextBlock> UpdateStatusRadicalCountTextFunc, Action UpdateDataViewToEmpty)
        {
            if (Char2Radicals.ContainsKey(Word))
                RefreshDataView(new Dictionary<char, List<string>> { { Word, Char2Radicals[Word] } }, UpdateStatusRadicalCountTextFunc);
            else
                UpdateDataViewToEmpty();
        }

        private void SearchByRadical(char Radical, Action<TextBlock> UpdateStatusRadicalCountTextFunc, Action UpdateDataViewToEmpty)
        {
            if (Radical2Chars.ContainsKey(Radical))
                RefreshDataView(DoSearchByRadical(Radical, Radical2Chars), UpdateStatusRadicalCountTextFunc);
            else
                UpdateDataViewToEmpty();
        }

        private void SearchByMultipleRadical(string Radicals, Action<TextBlock> UpdateStatusRadicalCountTextFunc, Action UpdateDataViewToEmpty)
        {
            IEnumerable<char> IntersectedChars = null;

            foreach (var Radical in Radicals)
            {
                string Chars = null;
                if (Radical2Chars.TryGetValue(Radical, out Chars))
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
            RefreshDataView(result, UpdateStatusRadicalCountTextFunc);
        }

        private Dictionary<char, List<string>> DoSearchByRadical(char Radical, Dictionary<char, string> Source)
        {
            var Result = new Dictionary<char, List<string>>();
            var Chars = Source[Radical];
            foreach (var Character in Chars)
                Result.Add(Character, Char2Radicals[Character]);
            return Result;
        }

        //控制器-后台线程

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            DisableNewBackgroundWorkEntranceThreadSafe();

            var WorkResult = new BackgroundWorkArg { Type = (e.Argument as BackgroundWorkArg).Type };
            switch ((e.Argument as BackgroundWorkArg).Type)
            {
                case BackgroundWorkArg.WorkType.LoadDataSource:
                    WorkResult.Arg = DoLoadDataSourceWork((e.Argument as BackgroundWorkArg).Arg as string, i => Worker.ReportProgress(i));
                    break;
            }

            e.Result = WorkResult;
        }

        private void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SetStatusProgressBarValue(e.ProgressPercentage);
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            EnableNewBackgroundWorkEntrance();

            switch ((e.Result as BackgroundWorkArg).Type)
            {
                case BackgroundWorkArg.WorkType.LoadDataSource:
                    CompleteLoadDataSourceWork((e.Result as BackgroundWorkArg).Arg as Tuple<Dictionary<char, List<string>>, Dictionary<char, string>>);
                    break;
            }

            SetStatusProgressBarValue(0);
            SetStatusTextContent("StatusBar.Ready");
        }

        //数据-接口

        private Tuple<Dictionary<char, List<string>>, Dictionary<char, string>> DoLoadDataSourceWork(string FileName, Action<int> ReportProgress)
        {
            this.Dispatcher.Invoke((Action)delegate ()
            {
                StatusText.SetResourceReference(TextBlock.TextProperty, "StatusBar.ParsingDataSource1");
            });

            var Data = new ChineseWordDataSource();
            try
            {
                Data.load(FileName, ReportProgress);
            }
            catch (Exception ex)
            {
                Data = null;
                MessageBox.Show(ex.Message, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return new Tuple<Dictionary<char, List<string>>, Dictionary<char, string>>(null, null);
            }

            this.Dispatcher.Invoke((Action)delegate ()
            {
                StatusText.SetResourceReference(TextBlock.TextProperty, "StatusBar.ParsingDataSource2");
            });
            ReportProgress(0);

            int ProcessedCount = 0;
            var C2R = new Dictionary<char, List<string>>();
            foreach (var p in Data.WordDetails)
            {
                C2R[p.Word] = p.Radicals;
                ReportProgress((int)(((double)(++ProcessedCount) / ((double)Data.WordDetails.Count * 2)) * 100));
                //System.Threading.Thread.Sleep(1);
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
                ReportProgress((int)(((double)(++ProcessedCount) / ((double)Data.WordDetails.Count * 2)) * 100));
                //System.Threading.Thread.Sleep(1);
            }

            return new Tuple<Dictionary<char, List<string>>, Dictionary<char, string>>(C2R, R2C);
        }

        private void CompleteLoadDataSourceWork(Tuple<Dictionary<char, List<string>>, Dictionary<char, string>> result)
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

        //TODO:
        //按单个字搜索-ok，按单个笔画搜索，按多个笔画搜索
        //部首频率分析，拿部首对汉字的hashmap来搞个value.size()排个序就好了

        private ResourceDictionary CurrentLanguageResource { get; set; }//当前使用的语言字典
        private BackgroundWorker Worker = new BackgroundWorker();//后台线程

        private Dictionary<char, List<string>> Char2Radicals;//汉字对部首
        private Dictionary<char, string> Radical2Chars;//部首对汉字
    }
}
