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
    public partial class MainWindow : Window
    {
        private NationalInstruments.DAQmx.Task _daqTask;
        private DigitalSingleChannelWriter _writer;
        private CancellationTokenSource _cts;
        private System.Threading.Tasks.Task _toggleTask;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                MessageBox.Show("Der Vorgang läuft bereits.");
                return;
            }

            if (!int.TryParse(HighTimeTextBox.Text, out int highMs) || highMs < 0 ||
                !int.TryParse(LowTimeTextBox.Text, out int lowMs) || lowMs < 0 ||
                !int.TryParse(RepeatCountTextBox.Text, out int repeatCount) || repeatCount < 0)
            {
                MessageBox.Show("Bitte gültige Zahlen eingeben.");
                return;
            }

            try
            {
                _cts = new CancellationTokenSource();

                _daqTask = new NationalInstruments.DAQmx.Task();
                _daqTask.DOChannels.CreateChannel("Dev1/port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines);

                _writer = new DigitalSingleChannelWriter(_daqTask.Stream);

                _toggleTask = ToggleOutputAsync(highMs, lowMs, repeatCount, _cts.Token);
                await _toggleTask;
            }
            catch (TaskCanceledException)
            {
                // Erwarten wir – nichts tun
            }
            catch (DaqException ex)
            {
                MessageBox.Show("DAQ Fehler: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Allgemeiner Fehler: " + ex.Message);
            }
            finally
            {
                try
                {
                    if (_daqTask != null && _writer != null)
                    {
                        bool[] low = Enumerable.Repeat(false, 8).ToArray();
                        _writer.WriteSingleSampleMultiLine(true, low);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Rücksetzen: " + ex.Message);
                }

                _writer = null;
                _daqTask?.Dispose();
                _daqTask = null;
                _cts?.Dispose();
                _cts = null;
                _toggleTask = null;
            }
        }

        private async System.Threading.Tasks.Task ToggleOutputAsync(int highMs, int lowMs, int repeatCount, CancellationToken token)
        {
            //bool[] high = Enumerable.Repeat(true, 8).ToArray();
            //bool[] low = Enumerable.Repeat(false, 8).ToArray();

            bool[] high = new bool[8] { false, true, false, true, false, true, false, true };
            bool[] low = new bool[8] { true, false, true, false, true, false, true, false };

            for (int i = 0; repeatCount == 0 || i < repeatCount; i++)
            {
                token.ThrowIfCancellationRequested();
                _writer.WriteSingleSampleMultiLine(true, high);
                await System.Threading.Tasks.Task.Delay(highMs, token);

                token.ThrowIfCancellationRequested();
                _writer.WriteSingleSampleMultiLine(true, low);
                await System.Threading.Tasks.Task.Delay(lowMs, token);
            }
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();

                try
                {
                    if (_toggleTask != null)
                        await _toggleTask;
                }
                catch (TaskCanceledException)
                {
                    // Ignorieren
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Stoppen: " + ex.Message);
                }
            }
        }
    }
}