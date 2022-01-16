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
        private MenuItem _fullscreenMenuItem;
        private MenuItem _onTopMenuItem;
        private MainWindow _mainWindow;
        public MainWindow()
        {
            InitializeComponent();
            //Setup the local pointers for easy access later
            _mainWindow = GetWindow(this) as MainWindow;
            _onTopMenuItem = _mainWindow.ContextMenu.Items[0] as MenuItem;
            _fullscreenMenuItem = this.ContextMenu.Items[1] as MenuItem;

            //Setup the default states
            _onTopMenuItem.IsChecked = this._forceOnTop;
            _fullscreenMenuItem.IsChecked = this._fullscreen;
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

    }
}
