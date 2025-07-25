using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using NationalInstruments.DAQmx;
using NI=NationalInstruments.DAQmx;

namespace USB6501_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NationalInstruments.DAQmx.Task _daqTask;  // <- Eindeutig!
        private DigitalSingleChannelWriter _writer;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                MessageBox.Show("Läuft bereits.");
                return;
            }

            if (!int.TryParse(HighTimeTextBox.Text, out int highMs) || highMs < 0 ||
                !int.TryParse(LowTimeTextBox.Text, out int lowMs) || lowMs < 0 ||
                !int.TryParse(RepeatCountTextBox.Text, out int repeatCount) || repeatCount < 0)
            {
                MessageBox.Show("Ungültige Eingabe.");
                return;
            }

            try
            {
                _daqTask = new NationalInstruments.DAQmx.Task();
                _daqTask.DOChannels.CreateChannel("Dev1/port0/line0", "", ChannelLineGrouping.OneChannelForEachLine);

                _writer = new DigitalSingleChannelWriter(_daqTask.Stream);
                _cts = new CancellationTokenSource();

                await ToggleOutputAsync(highMs, lowMs, repeatCount, _cts.Token);
            }
            catch (DaqException ex)
            {
                MessageBox.Show("DAQ Fehler: " + ex.Message);
                _daqTask?.Dispose();
                _daqTask = null;
                _cts = null;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = null;

            _writer?.WriteSingleSampleSingleLine(true, false); // LOW
            _daqTask?.Dispose();
            _daqTask = null;
        }

        private async System.Threading.Tasks.Task ToggleOutputAsync(int highMs, int lowMs, int repeatCount, CancellationToken token)
        {
            int count = 0;

            while (!token.IsCancellationRequested)
            {
                _writer.WriteSingleSampleSingleLine(true, true);
                await System.Threading.Tasks.Task.Delay(highMs, token);

                _writer.WriteSingleSampleSingleLine(true, false);
                await System.Threading.Tasks.Task.Delay(lowMs, token);

                if (repeatCount > 0)
                {
                    count++;
                    if (count >= repeatCount)
                        break;
                }
            }

            _writer?.WriteSingleSampleSingleLine(true, false);
            _daqTask?.Dispose();
            _daqTask = null;
            _cts = null;
        }
    }
}
