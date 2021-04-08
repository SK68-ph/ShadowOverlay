using Memory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
using System.Windows.Threading;


namespace ShadowOverlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mem mem = new Mem();
        string vbeAddr = "";
        public MainWindow()
        {
            InitializeComponent();
            init();
            this.Top = 30;
            this.Left = 565;

            SetVBE();
        }

        async void init()
        {
            int pId = mem.GetProcIdFromName("dota2.exe");
            if (mem.OpenProcess(pId))
            {
                IntPtr baseAddr;
                long baseAddrSz = 0x60A000;
                if (!(mem.modules.TryGetValue("engine2.dll", out baseAddr)))
                {
                    Debug.WriteLine("Base module not found");
                }
                string[] patterns = { "?? ?? ?? ?? ?? 02 00 00 02 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? 02 00 00", "?? ?? ?? ?? ?? 01 00 00 02 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? 01 00 00" };
                IEnumerable<long> AoBScanResults = await mem.AoBScan(baseAddr.ToInt64(), (baseAddr.ToInt64() + baseAddrSz), patterns[0], true, false);
                if (AoBScanResults.Count() == 0)
                {
                    AoBScanResults = await mem.AoBScan(baseAddr.ToInt64(), (baseAddr.ToInt64() + baseAddrSz), patterns[1], true, false);
                }
                UIntPtr vbeAddress = mem.GetCode(AoBScanResults.FirstOrDefault().ToString("X") + ",0x0,0x28,0x38,0x70,0x170,0x0,0x1E4");
                vbeAddr = vbeAddress.ToString("X");
            }
        }

        // Draw VBE Status.
        public void SetVBE()
        {
            //Initiate Timer 
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            // Display VBE Result at 0.1 interval
            timer.Tick += (o, e) =>
            {
                int vbe = mem.ReadInt(vbeAddr);
                if (vbe == 14) // Visible by enemy
                {
                    txtVBE.Text = "Visible";
                    txtVBE.Foreground = Brushes.LightGreen;
                }
                else if (vbe >= 6 && vbe <= 10) // Not visible by enemy
                {
                    txtVBE.Text = "";
                    txtVBE.Foreground = Brushes.Gray;
                }
                else
                {
                    txtVBE.Text = "Waiting";
                    txtVBE.Foreground = Brushes.Gray;
                }
            };
            timer.IsEnabled = true;
        }


        // Close handle before closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mem.CloseProcess();
        }
    }
}
