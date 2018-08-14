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

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchKey.Text))
            {
                MessageBox.Show(App.Current.FindResource("SearchBox.SearchKeyShouldNotContainsWhiteSpace") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            foreach (var p in SearchKey.Text)
            {
                if (ForbiddenChars.Contains(p))
                {
                    MessageBox.Show(App.Current.FindResource("SearchBox.SearchKeyShouldNotContainsWhiteSpace") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            if (SearchByWordRB.IsChecked == true)
            {
                if (SearchKey.Text.Length != 1)
                {
                    MessageBox.Show(App.Current.FindResource("SearchBox.SearchKeyShouldBeSingleCharacter") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                Action = SearchBoxAction.SearchByWord;
            }
            else if (SearchByRadicalRB.IsChecked == true)
            {
                if (SearchKey.Text.Length != 1)
                {
                    MessageBox.Show(App.Current.FindResource("SearchBox.SearchKeyShouldBeSingleCharacter") as string, App.Current.FindResource("General.Error") as string, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                Action = SearchBoxAction.SearchByRadical;
            }
            else if (SearchByMultipleRadicalRB.IsChecked == true)
            {
                Action = SearchBoxAction.SearchByMultipleRadical;
            }

            SearchKeyString = SearchKey.Text;

            this.Close();
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
