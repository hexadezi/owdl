using owdl.Model;
using owdl.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace owdl.ViewModel
{
    class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<string> Lines { get; } = new ObservableCollection<string>();
        readonly Sniffer sniffer = new Sniffer();
        public RelayCommand InitializeAndStartSniffer { get; private set; }
        public RelayCommand StartSniffer { get; private set; }
        public RelayCommand StopSniffer { get; private set; }
        public MainWindowViewModel()
        {
            InitializeAndStartSniffer = new RelayCommand(() => Task.Run(() => sniffer.InitializeAndStart()));
            StartSniffer = new RelayCommand(() => Task.Run(() => sniffer.Start()));
            StopSniffer = new RelayCommand(() => Task.Run(() => sniffer.Stop()));
            sniffer.OnLineAddition += Sniffer_OnLineAddition;
        }
        private void Sniffer_OnLineAddition(object sender, string e)
        {
            //Application.Current.Dispatcher.Invoke(() => { Lines.Add(e); });
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                Lines.Add(e);
            }));
        }
    }
}
