using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace ScreenBlocker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties
        /// <summary>
        /// Tracks if the UI should be fullscreen
        /// </summary>
        private bool _fullscreen = false;

        /// <summary>
        /// Tracks if the window should force itself on top of other windows
        /// </summary>
        private bool _forceOnTop = true;

        /// <summary>
        /// Reference to the fullscreen menu item
        /// </summary>
        private readonly System.Windows.Controls.MenuItem _fullscreenMenuItem;

        /// <summary>
        /// Reference to the on top menu item
        /// </summary>
        private readonly System.Windows.Controls.MenuItem _onTopMenuItem;

        //eference to the main window
        private readonly MainWindow _mainWindow;

        /// <summary>
        /// Used to save which monitor the window is on on shutdown so we can restore later
        /// </summary>
        private double _x_offset = 0;

        /// <summary>
        /// The horizontal scale of the monitor
        /// </summary>
        double xScale = 1.0;

        /// <summary>
        /// The vertical scale of the monitor
        /// </summary>
        double yScale = 1.0;

        /// <summary>
        /// Use for the border when hovering
        /// </summary>
        readonly double hoverBoderThickness = 3.0;

        #endregion Properties

        /// <summary>
        /// Constructor for the main window.  
        /// </summary>
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

            //Add window loaded event handler
            Loaded += MainWindow_Loaded;

            //Start window hidden
            _mainWindow.Visibility = System.Windows.Visibility.Hidden;
            _mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            //Open the snipping form passing in our event handler
            SnippingTool.SnipForCoords(this.Snipper_AreaSelected);
        }


        #region Event Handlers

        /// <summary>
        /// Callback for when the Snipper completes selecting an area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Snipper_AreaSelected(object sender, EventArgs e)
        {
            _mainWindow.Visibility = Visibility.Visible;
            _mainWindow.Height = SnippingTool.SnippedPos.Height / yScale;
            _mainWindow.Width = SnippingTool.SnippedPos.Width / xScale;
            _mainWindow.Left = SnippingTool.SnippedPos.Left / xScale;
            _mainWindow.Top = SnippingTool.SnippedPos.Top / yScale;

            _mainWindow.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handler for when the window loads.  This scales the rectangle.
        /// The reason we do this here rather than in <see cref="Snipper_AreaSelected(object, EventArgs)"/>
        /// is because the UI is not rendered yet then which throws an exception
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            xScale = PresentationSource.FromVisual(_mainWindow).CompositionTarget.TransformToDevice.M11;
            yScale = PresentationSource.FromVisual(_mainWindow).CompositionTarget.TransformToDevice.M22;

            _mainWindow.Height = SnippingTool.SnippedPos.Height / yScale;
            _mainWindow.Width = SnippingTool.SnippedPos.Width / xScale;
            _mainWindow.Left = SnippingTool.SnippedPos.Left / xScale;
            _mainWindow.Top = SnippingTool.SnippedPos.Top / yScale;
        }

        /// <summary>
        /// Mouse down handler to enable dragging the window around
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
            else if(e.ChangedButton == MouseButton.Middle)
                SetForceOnTop(!_forceOnTop);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }

        /// <summary>
        /// When the user's mouse enters the window highlight the border
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseEnter(object sender, EventArgs e)
        {
            this.BorderThickness = new Thickness(hoverBoderThickness);
        }

        /// <summary>
        /// When the user's mouse enters the window remove highlight on the border
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeave(object sender, EventArgs e)
        {
            this.BorderThickness = new Thickness(0.0);
        }

        #endregion Event Handlers

        #region Power Handler

        /// <summary>
        /// Power change event handler to help restore the proper window position on the correct monitor
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
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

        #endregion Power Handler


        #region Menu Handlers

        /// <summary>
        /// Handler for when the resnip button is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReSnip_OnClick(object sender, RoutedEventArgs e)
        {
            _mainWindow.Visibility = System.Windows.Visibility.Hidden;
            SnippingTool.SnipForCoords(this.Snipper_AreaSelected);

            //Invalidate the UI so it redraws so the on load handler fires to adjust scale
            InvalidateVisual();
        }


        /// <summary>
        /// Handler for the fullscreen button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Fullscreen_OnClick(object sender, RoutedEventArgs e)
        {
            SetFullscreen(!_fullscreen);
        }

        /// <summary>
        /// Handler for the force on top button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTop_OnClick(object sender, RoutedEventArgs e)
        {
            SetForceOnTop(!_forceOnTop);
        }

        /// <summary>
        /// Handler for the new window button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewWindow_OnClick(object sender, RoutedEventArgs e)
        {
            var screenBlockPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(screenBlockPath);
        }

        /// <summary>
        /// Handler for the close window button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseWindow_OnClick(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Handler for the close all windows button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseAllWindows_OnClick(object sender, RoutedEventArgs e)
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


        #endregion Button Handlers

        #region Helpers

        /// <summary>
        /// Sets the force on top value and updates the UI and other required elements
        /// </summary>
        /// <param name="state">Desired state for <see cref="_forceOnTop"/></param>
        private void SetForceOnTop(bool state)
        {
            _forceOnTop = state;
            _mainWindow.Topmost = _forceOnTop;
            _onTopMenuItem.IsChecked = this._forceOnTop;
        }

        /// <summary>
        /// Sets the fullscreen value and updates the UI and other required elements
        /// </summary>
        /// <param name="state">Desired state for <see cref="_fullscreen"/></param>
        private void SetFullscreen(bool state)
        {
            _fullscreen = state;
            _mainWindow.WindowState = _fullscreen ? WindowState.Maximized : WindowState.Normal;
            _fullscreenMenuItem.IsChecked = this._fullscreen;
        }

        #endregion Helpers
    }
}
