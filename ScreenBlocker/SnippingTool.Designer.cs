using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;

namespace ScreenBlocker
{
    /// <summary>
    /// Snipping tool to select area to block
    /// </summary>
    public sealed partial class SnippingTool : Form
    {
        public static event EventHandler Cancel;
        public static event EventHandler AreaSelected;
        public event EventHandler AreaSelectedInstance;
        public Image Image { get; set; }
        public static Rectangle SnippedPos { get; set; }

        private static SnippingTool[] _forms;
        private Rectangle _rectSelection;
        private System.Drawing.Point _pointStart;

        public SnippingTool(Image bmp, int x, int y, int width, int height)
        {
            InitializeComponent();
            Image = bmp;
            Opacity = .25;
            Height = 400;
            Width = 400;
            //BackgroundImageLayout = ImageLayout.Stretch;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            SetBounds(x, y, width, height);
            //WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            //TopMost = true;
        }

        private void OnCancel(EventArgs e)
        {
            Cancel?.Invoke(this, e);
        }

        private void OnAreaSelected(EventArgs e)
        {
            AreaSelected?.Invoke(this, e);
        }

        /// <summary>
        /// Closes all of the open forms (one per monitor)
        /// </summary>
        private void CloseForms()
        {
            for (int i = 0; i < _forms.Length; i++)
            {
                _forms[i].Dispose();
            }
        }

        public static void Snip()
        {
            var screens = ScreenHelper.GetMonitorsInfo();
            _forms = new SnippingTool[screens.Count];
            for (int i = 0; i < screens.Count; i++)
            {
                int hRes = screens[i].HorizontalResolution;
                int vRes = screens[i].VerticalResolution;
                int top = screens[i].MonitorArea.Top;
                int left = screens[i].MonitorArea.Left;
                var bmp = new Bitmap(hRes, vRes, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(left, top, 0, 0, bmp.Size);
                }
                _forms[i] = new SnippingTool(bmp, left, top, hRes, vRes);
                _forms[i].Show();
            }
        }

        public static void SnipForCoords(EventHandler handler)
        {
            var screens = ScreenHelper.GetMonitorsInfo();
            _forms = new SnippingTool[screens.Count];
            for (int i = 0; i < screens.Count; i++)
            {
                int hRes = screens[i].HorizontalResolution;
                int vRes = screens[i].VerticalResolution;
                int top = screens[i].MonitorArea.Top;
                int left = screens[i].MonitorArea.Left;
                var bmp = new Bitmap(hRes, vRes, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(left, top, 0, 0, bmp.Size);
                }
                _forms[i] = new SnippingTool(bmp,left, top, hRes, vRes);
                _forms[i].AreaSelectedInstance += handler;
                _forms[i].Show();
            }
        }

        #region Overrides
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Start the snip on mouse down
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            _pointStart = e.Location;
            _rectSelection = new Rectangle(e.Location, new System.Drawing.Size(0, 0));
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // Modify the selection on mouse move
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            int x1 = Math.Min(e.X, _pointStart.X);
            int y1 = Math.Min(e.Y, _pointStart.Y);
            int x2 = Math.Max(e.X, _pointStart.X);
            int y2 = Math.Max(e.Y, _pointStart.Y);
            _rectSelection = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // Complete the snip on mouse-up
            if (_rectSelection.Width <= 0 || _rectSelection.Height <= 0)
            {
                CloseForms();
                OnCancel(new EventArgs());
                return;
            }



            //TODO: Migrate all of this into a helper class


            var hScale = Image.Width / (double)Width;
            var vScale = Image.Height / (double)Height;

            //Where the rectangle is on the current monitor
            int monitor_X = (int)(_rectSelection.X * hScale);
            int monitor_Y = (int)(_rectSelection.Y * vScale);


            int width = (int)(_rectSelection.Width * hScale);
            int height = (int)(_rectSelection.Height * vScale);

            int pixelSnapLimit = 30;

            //Snap to Left 
            if (monitor_X < pixelSnapLimit)
            {
                //Add the extra width
                width += monitor_X;
                //Take it to the edge of the screen
                monitor_X = 0;
            }

            //Snap to Top
            if (monitor_Y < pixelSnapLimit)
            {
                //Add the extra height
                height += monitor_Y;
                //Take it to the edge of the screen
                monitor_Y = 0;
            }

            //Snap to Right
            if(monitor_X + width + pixelSnapLimit >= Width)
            {
                //Add the extra width
                width = Width - monitor_X;
            }

            //Snap to Bottom
            if (monitor_Y + height + pixelSnapLimit >= Height)
            {
                //Add the extra width
                height = Height - monitor_Y;
            }


            //Calculate where the rectangle is across all monitors
            int absolute_X = monitor_X + this.Location.X;
            int absolute_Y = monitor_Y + this.Location.Y;


            SnippedPos = new Rectangle(absolute_X, absolute_Y, width, height);

            CloseForms();
            OnAreaSelected(new EventArgs());
            AreaSelectedInstance.Invoke(this,new EventArgs());
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw the current selection
            using (Brush br = new SolidBrush(Color.FromArgb(120, Color.White)))
            {
                int x1 = _rectSelection.X;
                int x2 = _rectSelection.X + _rectSelection.Width;
                int y1 = _rectSelection.Y;
                int y2 = _rectSelection.Y + _rectSelection.Height;
                e.Graphics.FillRectangle(br, new Rectangle(0, 0, x1, Height));
                e.Graphics.FillRectangle(br, new Rectangle(x2, 0, Width - x2, Height));
                e.Graphics.FillRectangle(br, new Rectangle(x1, 0, x2 - x1, y1));
                e.Graphics.FillRectangle(br, new Rectangle(x1, y2, x2 - x1, Height - y2));
            }
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, _rectSelection);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Allow canceling the snip with the Escape key
            if (keyData == Keys.Escape)
            {
                Image = null;
                CloseForms();
                OnCancel(new EventArgs());
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion
    

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SnippingTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "SnippingTool";
            this.Text = "SnippingTool";
            this.Load += new System.EventHandler(this.SnippingTool_Load);
            this.ResumeLayout(false);

        }

        #endregion
    }
}