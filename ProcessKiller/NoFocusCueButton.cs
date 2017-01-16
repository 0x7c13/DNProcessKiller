

namespace ProcessKiller
{
    using System.Windows.Forms;
    using System.Drawing;

    public class NoFocusCueButton : Button
    {
        public NoFocusCueButton()
        {
            SetStyle(ControlStyles.Selectable, false);
        }

        protected override bool ShowFocusCues => false;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw Border using color specified in Flat Appearance
            Pen pen = new Pen(Color.Transparent, 1);
            Rectangle rectangle = new Rectangle(0, 0, Size.Width - 1, Size.Height - 1);
            e.Graphics.DrawRectangle(pen, rectangle);
        }
    }
}
