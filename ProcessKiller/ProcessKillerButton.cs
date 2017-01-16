
namespace ProcessKiller
{
    using System.Drawing;
    using System.Windows.Forms;

    class ProcessKillerButton : Button
    {
        public ProcessButton ProcessButton;
        private readonly Color _highlightColor;
        private readonly Color _unhighlightColor;
        private readonly Color _disableColor;
        private bool _isHighlighted;
        private readonly string _defaultText;
        private readonly string _highlightedText;
        private readonly string _killingMessage;

        public ProcessKillerButton(ProcessButton processButton, Color highlightColor, Color unhighlightColor, Color disableColor, string defaultText, string highlightedText, string killingMessage)
        {
            SetStyle(ControlStyles.Selectable, false);
            ProcessButton = processButton;
            _highlightColor = highlightColor;
            _unhighlightColor = unhighlightColor;
            _disableColor = disableColor;
            _defaultText = defaultText;
            _highlightedText = highlightedText;
            _killingMessage = killingMessage;
        }

        public void KillProcess()
        {
            this.Enabled = false;
            this.BackColor = _disableColor;
            this.Text = _killingMessage;
            ProcessButton.KillProcess();
        }

        public void Highlight()
        {
            if (!Enabled) return;
            _isHighlighted = true;
            this.Text = _highlightedText;
            this.ForeColor = Color.White;
            this.BackColor = _highlightColor;
        }

        public void Unhighlight()
        {
            if (!Enabled) return;
            _isHighlighted = false;
            this.Text = _defaultText;
            this.ForeColor = Color.Black;
            this.BackColor = _unhighlightColor;
        }

        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        protected override bool ShowFocusCues => false;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw Border using color specified in Flat Appearance
            Pen pen = new Pen(Color.Gray, 1);
            Rectangle rectangle = new Rectangle(0, 0, Size.Width - 1, Size.Height - 1);
            e.Graphics.DrawRectangle(pen, rectangle);
        }
    }
}
