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
using System.Windows.Shapes;

namespace Chinese_Word_Analyzer
{
    public partial class SearchBox : Window
    {
        public SearchBox()
        {
            InitializeComponent();

            SearchKeyTextBox.Text = Properties.Settings.Default.SearchBoxKey;

            switch (Properties.Settings.Default.SearchBoxType)
            {
                case 2: SearchByRadicalRB.IsChecked = true; break;
                case 3: SearchByMultipleRadicalRB.IsChecked = true; break;
                default: SearchByWordRB.IsChecked = true; break;
            }

            SearchKeyTextBox.Focus();
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
            SaveSitting();
            this.Action = SearchBoxAction.None;
            this.Close();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchKeyTextBox.Text))
            {
                MessageBox.Show(App.Current.FindResource("SearchBox.SearchKeyShouldNotContainsWhiteSpace") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                SearchKeyTextBox.Focus();
                return;
            }
            foreach (var p in SearchKeyTextBox.Text)
            {
                if (ForbiddenChars.Contains(p))
                {
                    MessageBox.Show(App.Current.FindResource("SearchBox.SearchKeyShouldNotContainsWhiteSpace") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    SearchKeyTextBox.Focus();
                    return;
                }
            }

            if ((Action == SearchBoxAction.SearchByWord || Action == SearchBoxAction.SearchByRadical) && SearchKeyTextBox.Text.Length != 1)
            {
                MessageBox.Show(App.Current.FindResource("SearchBox.SearchKeyShouldBeSingleCharacter") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                SearchKeyTextBox.Focus();
                return;
            }

            SearchKeyString = SearchKeyTextBox.Text;

            SaveSitting();

            this.Close();
        }

        private void RBChecked(object sender, RoutedEventArgs e)
        {
            if (sender == SearchByWordRB)
            {
                Description.SetResourceReference(TextBlock.TextProperty, "SearchBox.SearchByWordDescription");
                Action = SearchBoxAction.SearchByWord;
            }
            else if (sender == SearchByRadicalRB)
            {
                Description.SetResourceReference(TextBlock.TextProperty, "SearchBox.SearchByRadicalDescription");
                Action = SearchBoxAction.SearchByRadical;
            }
            else if (sender == SearchByMultipleRadicalRB)
            {
                Description.SetResourceReference(TextBlock.TextProperty, "SearchBox.SearchByMultipleRadicalDescription");
                Action = SearchBoxAction.SearchByMultipleRadical;
            }
        }

        private void SaveSitting()
        {
            Properties.Settings.Default.SearchBoxKey = SearchKeyTextBox.Text;

            switch (Action)
            {
                case SearchBoxAction.SearchByRadical: Properties.Settings.Default.SearchBoxType = 2; break;
                case SearchBoxAction.SearchByMultipleRadical: Properties.Settings.Default.SearchBoxType = 3; break;
                default: Properties.Settings.Default.SearchBoxType = 1; break;
            }
        }

        public enum SearchBoxAction
        {
            None,
            SearchByWord,
            SearchByRadical,
            SearchByMultipleRadical
        }

        public SearchBoxAction Action { get; set; } = SearchBoxAction.None;
        public string SearchKeyString { get; set; }
        static private char[] ForbiddenChars = new char[] { ' ', '\0', '\t', '\r', '\n' };
    }
}
