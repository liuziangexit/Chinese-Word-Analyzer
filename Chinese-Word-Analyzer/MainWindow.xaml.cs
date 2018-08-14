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

        protected override void OnSourceInitialized(EventArgs e)
        {
            IconRemover.RemoveIcon(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            IconRemover.SetWindowLong(hwnd, IconRemover.GWL_STYLE, IconRemover.GetWindowLong(hwnd, IconRemover.GWL_STYLE) & ~IconRemover.WS_SYSMENU);
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
            box.Show();
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            this.Dispatcher.Invoke((Action)delegate ()
            {
                OpenDataSourceMenuItem.IsEnabled = false;
                AnalysisMenuItem.IsEnabled = false;
            });

            switch ((e.Argument as BackgroundWorkArg).Type)
            {
                case BackgroundWorkArg.WorkType.LoadDataSource:
                    e.Result = new BackgroundWorkArg() { Arg = DoLoadDataSourceWork((e.Argument as BackgroundWorkArg).Arg as string, i => Worker.ReportProgress(i)) };
                    break;
            }

            (e.Result as BackgroundWorkArg).Type = (e.Argument as BackgroundWorkArg).Type;
        }

        void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.StatusProgressBar.Value = e.ProgressPercentage;
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //enable controls
            OpenDataSourceMenuItem.IsEnabled = true;
            AnalysisMenuItem.IsEnabled = true;

            //process result
            switch ((e.Result as BackgroundWorkArg).Type)
            {
                case BackgroundWorkArg.WorkType.LoadDataSource:
                    CompleteLoadDataSourceWork((e.Result as BackgroundWorkArg).Arg as Tuple<Dictionary<char, List<string>>, Dictionary<char, string>>);
                    break;
            }

            //reset the progress bar and status text
            this.StatusProgressBar.Value = 0;
            StatusText.SetResourceReference(TextBlock.TextProperty, "StatusBar.Ready");
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
            return new Tuple<string, ResourceDictionary>(LanguageResourceKey, new ResourceDictionary() { Source = new Uri(App.Current.FindResource(LanguageResourceKey) as string, UriKind.Relative) });
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

            if (Char2Radicals != null && Radical2Chars != null)
            {
                //update DataView                                                
                StatusText.SetResourceReference(TextBlock.TextProperty, "StatusBar.ParsingDataSource3");

                //construct new ListView
                var Grid = new GridView();

                //first header
                var Header0Text = new TextBlock { Margin = new Thickness { Right = 30 } };
                Header0Text.SetResourceReference(TextBlock.TextProperty, "DataView.Header0");
                Grid.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = Header0Text }, DisplayMemberBinding = new Binding("Word") });

                //rest of headers
                int ColumnCountNeed = Char2Radicals.Values.Max(list => list.Count);
                if (ColumnCountNeed > 0)
                {
                    for (int i = 0; i < ColumnCountNeed; i++)
                    {
                        var HeaderNText = new TextBlock { Margin = new Thickness { Right = 30 } };
                        HeaderNText.SetResourceReference(TextBlock.TextProperty, "DataView.HeaderN");
                        Grid.Columns.Add(new GridViewColumn { Header = new GridViewColumnHeader { Content = HeaderNText }, Width = 75, DisplayMemberBinding = new Binding("Radicals[" + i.ToString() + "]") });
                    }
                }

                //assign the view
                DataView.View = Grid;

                //fill ListView with data
                var ItemSource = new List<ChineseWordDataSource.WordDetail>(Char2Radicals.Count);
                foreach (var p in Char2Radicals)
                    ItemSource.Add(new ChineseWordDataSource.WordDetail() { Word = p.Key, Radicals = p.Value });
                DataView.ItemsSource = ItemSource;
                StatusRadicalCountText.Text = " " + Radical2Chars.Keys.Count.ToString();
            }
        }

        //编辑菜单里面加“查看数据源”，以便移除搜索结果，重新显示所有数据
        //部首频率分析，拿部首对汉字的hashmap来搞个value.size()排个序就好了

        public ResourceDictionary CurrentLanguageResource { get; private set; }
        private BackgroundWorker Worker = new BackgroundWorker();

        private Dictionary<char, List<string>> Char2Radicals;//汉字对部首
        private Dictionary<char, string> Radical2Chars;//部首对汉字
    }
}
