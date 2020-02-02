using owdl.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace owdl
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ItemsControl_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (VisualTreeHelper.GetChildrenCount(itemsControl) > 0)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(itemsControl, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            viewModel.StopSniffer.Execute(null);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ((INotifyCollectionChanged)itemsControl.Items).CollectionChanged += ItemsControl_CollectionChanged; ;

            viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            viewModel.InitializeAndStartSniffer.Execute(null);
        }

        private void ItemsControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(sender.ToString());
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var lbi = (ListBoxItem)sender;

            bool result = Uri.TryCreate(lbi.Content.ToString(), UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (result)
            {
                System.Diagnostics.Process.Start(lbi.Content.ToString());
            }
        }
    }
}
