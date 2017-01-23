
namespace ProcessKiller
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Drawing;
    using System.Diagnostics;
    using System.Windows.Forms;
    using ProcessKiller.Properties;

    class TimerButton : Button
    {
        private readonly Stopwatch _watch;
        private DateTime _startTime;
        private bool _started;
        private bool _alertCreated;
        private readonly Color _timerDefaultTextColor;
        private readonly Color _timerAlertTextColor;
        private readonly Color _timerRunningBackColor;
        private readonly Color _timerDefaultBackColor;

        public  TimeSpan CountDownTime, AlertThreshold;

        public TimerButton(TimeSpan countDown, TimeSpan alertThreshold, Color timerDefaultTextColor, Color timerAlertTextColor, Color timerRunningBackColor, Color timerDefaultBackColor)
        {
            SetStyle(ControlStyles.Selectable, false);
            CountDownTime = countDown;
            AlertThreshold = alertThreshold;
            _timerDefaultTextColor = timerDefaultTextColor;
            _timerAlertTextColor = timerAlertTextColor;
            _timerRunningBackColor = timerRunningBackColor;
            _timerDefaultBackColor = timerDefaultBackColor;
           _watch = new Stopwatch();
        }

        public void StartTimer()
        {
            _started = true;
            _startTime = DateTime.Now;
            _watch.Start();
            this.BackColor = _timerRunningBackColor;

            Task.Run(() =>
            {
                while (_watch.IsRunning)
                {
                    var remaining = CountDownTime - (DateTime.Now - _startTime);

                    if (!_alertCreated && remaining.TotalSeconds < AlertThreshold.TotalSeconds)
                    {
                        _alertCreated = true;
                        var player = new System.Media.SoundPlayer(Resources.alert);
                        player.Play();

                        this.ForeColor = _timerAlertTextColor;
                    }

                    if (remaining.TotalSeconds < 0 )
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke(new MethodInvoker(() =>
                            {
                                this.Text = 0.ToString("0");
                            }));
                        }
                        return;
                    }
                    else if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            this.Text = remaining.TotalSeconds.ToString("0");
                        }));
                    }

                    Thread.Sleep(100);
                }
            });
        }

        public void StopTimer()
        {
            _started = false;
            _alertCreated = false;
            _watch.Stop();
            this.Text = ((int)CountDownTime.TotalSeconds).ToString();
            this.ForeColor = _timerDefaultTextColor;
            this.BackColor = _timerDefaultBackColor;
        }

        public bool Started()
        {
            return _started;
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

        public void Reset()
        {
            _started = false;
            _alertCreated = false;
            _watch.Stop();
            this.Text = ((int) CountDownTime.TotalSeconds).ToString();
            this.ForeColor = _timerDefaultTextColor;
            this.BackColor = _timerDefaultBackColor;
        }
    }
}
