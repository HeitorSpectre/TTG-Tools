using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;

namespace TTG_Tools
{
    /// <summary>
    /// Small audio player for the Archive Unpacker preview: a waveform that doubles as the seek
    /// bar (click or drag anywhere), a smooth playhead, play/pause, stop and the elapsed/total time.
    /// Plays decoded 16-bit PCM through NAudio's WaveOutEvent.
    /// </summary>
    internal sealed class AudioPlayerPanel : UserControl
    {
        private readonly WavePanel wave = new WavePanel();
        private readonly Button playButton = new Button();
        private readonly Button stopButton = new Button();
        private readonly Label timeLabel = new Label();
        private readonly Timer timer = new Timer { Interval = 16 };

        private WaveOutEvent output;
        private RawSourceWaveStream stream;
        private bool seeking;

        private static readonly Color Played = Color.FromArgb(0, 120, 215);
        private static readonly Color Unplayed = Color.FromArgb(150, 150, 150);

        public AudioPlayerPanel()
        {
            DoubleBuffered = true;
            BackColor = SystemColors.Window;

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(6, 4, 6, 4) };
            playButton.Text = "▶";
            playButton.Width = 44;
            playButton.Dock = DockStyle.Left;
            playButton.Click += (s, e) => TogglePlay();
            stopButton.Text = "■";
            stopButton.Width = 44;
            stopButton.Dock = DockStyle.Left;
            stopButton.Click += (s, e) => Stop();
            timeLabel.Dock = DockStyle.Fill;
            timeLabel.TextAlign = ContentAlignment.MiddleLeft;
            timeLabel.Padding = new Padding(8, 0, 0, 0);
            timeLabel.Font = new Font("Consolas", 10f);

            bar.Controls.Add(timeLabel);
            bar.Controls.Add(stopButton);
            bar.Controls.Add(playButton);

            wave.Dock = DockStyle.Fill;
            wave.Owner = this;
            wave.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { seeking = true; SeekTo(e.X); } };
            wave.MouseMove += (s, e) => { if (seeking) SeekTo(e.X); };
            wave.MouseUp += (s, e) => seeking = false;

            Controls.Add(wave);
            Controls.Add(bar);

            timer.Tick += (s, e) => { UpdateTime(); wave.Invalidate(); };
        }

        public void LoadAudio(byte[] pcm, int sampleRate, int channels)
        {
            Unload();

            var format = new WaveFormat(sampleRate, 16, channels);
            stream = new RawSourceWaveStream(new MemoryStream(pcm, false), format);
            wave.SetPeaks(pcm, channels);

            output = new WaveOutEvent { DesiredLatency = 120 };
            output.Init(stream);
            output.PlaybackStopped += (s, e) =>
            {
                if (IsDisposed || stream == null) return;
                if (stream.Position >= stream.Length) stream.Position = 0;
                BeginInvoke(new Action(UpdateButtons));
            };

            UpdateButtons();
            UpdateTime();
            wave.Invalidate();
        }

        public void Unload()
        {
            timer.Stop();
            if (output != null) { output.Stop(); output.Dispose(); output = null; }
            if (stream != null) { stream.Dispose(); stream = null; }
            wave.SetPeaks(null, 1);
            UpdateButtons();
            UpdateTime();
        }

        public void TogglePlay()
        {
            if (output == null) return;

            if (output.PlaybackState == PlaybackState.Playing) output.Pause();
            else
            {
                if (stream.Position >= stream.Length) stream.Position = 0;
                output.Play();
            }

            UpdateButtons();
        }

        public void Stop()
        {
            if (output == null) return;
            output.Stop();
            stream.Position = 0;
            UpdateButtons();
            UpdateTime();
            wave.Invalidate();
        }

        private void SeekTo(int x)
        {
            if (stream == null || wave.Width <= 0) return;

            double frac = Math.Max(0, Math.Min(1, (double)x / wave.Width));
            long pos = (long)(stream.Length * frac);
            pos -= pos % stream.WaveFormat.BlockAlign;
            stream.Position = pos;
            UpdateTime();
            wave.Invalidate();
        }

        internal double Progress
        {
            get { return stream == null || stream.Length == 0 ? 0 : (double)stream.Position / stream.Length; }
        }

        private void UpdateButtons()
        {
            bool playing = output != null && output.PlaybackState == PlaybackState.Playing;
            playButton.Text = playing ? "❚❚" : "▶";
            playButton.Enabled = stopButton.Enabled = output != null;
            if (playing) timer.Start(); else timer.Stop();
        }

        private void UpdateTime()
        {
            if (stream == null) { timeLabel.Text = ""; return; }
            timeLabel.Text = Format(stream.CurrentTime) + " / " + Format(stream.TotalTime);
        }

        private static string Format(TimeSpan t)
        {
            return ((int)t.TotalMinutes) + ":" + t.Seconds.ToString("00") + "." + (t.Milliseconds / 100);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { Unload(); timer.Dispose(); }
            base.Dispose(disposing);
        }

        /// <summary>Waveform + playhead, drawn from min/max peaks per column bucket.</summary>
        private sealed class WavePanel : Panel
        {
            internal AudioPlayerPanel Owner;
            private float[] mins, maxs;
            private const int Buckets = 2048;

            public WavePanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                Cursor = Cursors.Hand;
            }

            public void SetPeaks(byte[] pcm, int channels)
            {
                mins = maxs = null;
                if (pcm == null || pcm.Length < 4) return;

                int frames = pcm.Length / (2 * channels);
                mins = new float[Buckets];
                maxs = new float[Buckets];

                for (int b = 0; b < Buckets; b++)
                {
                    int from = (int)((long)frames * b / Buckets);
                    int to = Math.Max(from + 1, (int)((long)frames * (b + 1) / Buckets));
                    float lo = 0, hi = 0;
                    int step = Math.Max(1, (to - from) / 256);

                    for (int f = from; f < to && f < frames; f += step)
                    {
                        int o = f * 2 * channels;
                        float v = (short)(pcm[o] | (pcm[o + 1] << 8)) / 32768f;
                        if (v < lo) lo = v;
                        if (v > hi) hi = v;
                    }

                    mins[b] = lo;
                    maxs[b] = hi;
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                int w = ClientSize.Width, h = ClientSize.Height;
                if (w <= 0 || h <= 0) return;

                float mid = h / 2f;
                int playX = Owner != null ? (int)(Owner.Progress * w) : 0;

                using (var axis = new Pen(Color.FromArgb(210, 210, 210)))
                    g.DrawLine(axis, 0, mid, w, mid);

                if (mins != null)
                {
                    using (var played = new Pen(Played))
                    using (var unplayed = new Pen(Unplayed))
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int b = (int)((long)x * Buckets / w);
                            float y1 = mid - maxs[b] * (mid - 4);
                            float y2 = mid - mins[b] * (mid - 4);
                            if (y2 - y1 < 1) y2 = y1 + 1;
                            g.DrawLine(x < playX ? played : unplayed, x, y1, x, y2);
                        }
                    }
                }

                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var head = new Pen(Color.FromArgb(220, 40, 40), 2f))
                    g.DrawLine(head, playX, 0, playX, h);
            }
        }
    }
}
