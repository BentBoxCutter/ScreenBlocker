using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScreenBlocker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _fullscreen = false;
        private bool _forceOnTop = true;
        private System.Windows.Controls.MenuItem _fullscreenMenuItem;
        private System.Windows.Controls.MenuItem _onTopMenuItem;
        private MainWindow _mainWindow;
        private double _x_offset = 0;

        public MainWindow()
        {
            
            InitializeComponent();
            //Setup the local pointers for easy access later
            _mainWindow = GetWindow(this) as MainWindow;
            _onTopMenuItem = _mainWindow.ContextMenu.Items[0] as System.Windows.Controls.MenuItem;
            _fullscreenMenuItem = this.ContextMenu.Items[1] as System.Windows.Controls.MenuItem;

            //Setup the default states
            _onTopMenuItem.IsChecked = this._forceOnTop;
            _fullscreenMenuItem.IsChecked = this._fullscreen;

            //Add event handler for sleep and wake so we can properly handle returning to the correct monitor
            SystemEvents.PowerModeChanged += OnPowerChange;

            //Start window hidden
            _mainWindow.Visibility = System.Windows.Visibility.Hidden;
            _mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            //Open the snipping form passing in our event handler
            SnippingTool.SnipForCoords(this.Snipper_AreaSelected);
        }

        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    //Give it 3 seconds to recognize the monitors
                    System.Threading.Thread.Sleep(3000);
                    _mainWindow.Left = _x_offset;
                    break;
                case PowerModes.Suspend:
                    //var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(GetWindow(this)).Handle);
                    _x_offset = _mainWindow.Left;
                    break;
            }
        }

        private void Snipper_AreaSelected(object sender, EventArgs e)
        {
            _mainWindow.Visibility = Visibility.Visible;
            _mainWindow.Height = SnippingTool.SnippedPos.Height;
            _mainWindow.Width = SnippingTool.SnippedPos.Width;
            _mainWindow.Left = SnippingTool.SnippedPos.Left;
            _mainWindow.Top = SnippingTool.SnippedPos.Top;
            
            _mainWindow.Visibility = Visibility.Visible;
        }

        private void reSnip_OnClick(object sender, RoutedEventArgs e)
        {
            _mainWindow.Visibility = System.Windows.Visibility.Hidden;
            SnippingTool.SnipForCoords(this.Snipper_AreaSelected);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        
        private void Fullscreen_OnClick(object sender, RoutedEventArgs e)
        {
            _mainWindow.WindowState = _fullscreen ? WindowState.Normal : WindowState.Maximized;

            _fullscreen = !_fullscreen;
            _fullscreenMenuItem.IsChecked = this._fullscreen;
        }

        private void onTop_OnClick(object sender, RoutedEventArgs e)
        {
            _forceOnTop = !_forceOnTop;
            _mainWindow.Topmost = _forceOnTop;
            _onTopMenuItem.IsChecked = this._forceOnTop;
        }

        private void newWindow_OnClick(object sender, RoutedEventArgs e)
        {
            var screenBlockPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(screenBlockPath);
        }

        private void closeWindow_OnClick(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void closeAllWindows_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process[] procs = null;

            try
            {
                procs = Process.GetProcessesByName("ScreenBlocker");
               
                foreach (Process proc in procs)
                {
                    //Kill the process if it is not the current process
                    if (proc.Id != Process.GetCurrentProcess().Id)
                    {
                        proc.Kill();
                    }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString()); 
            }
            finally
            {

            }
            //Finally kill the current process
            Process.GetCurrentProcess().Kill();
        }

    }
}
