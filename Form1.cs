using NAudio.Gui;
using NAudio.Wave;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoundEditor {
    public partial class MainForm : Form {
        private Label _debugLabel;
        private WaveViewer _waveViewer;
        private Panel _tracker;

        private WaveOutEvent _outputDevice;
        private AudioFileReader _audioFileReader;

        private string _fileName;
        private int _zoomFactor;

        private bool _playing;

        public MainForm() {
            InitializeComponent();
            Setup();
            CenterToScreen();
        }

        private void Setup() {
            Text = "SoundEditor";
            BackColor = Color.DarkGray;
            Width = 1000;
            Height = 400;
            AllowDrop = true;
            DragEnter += new DragEventHandler(MainForm_DragEnter);
            DragDrop += new DragEventHandler(MainForm_DragDrop);
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            CreateDebugLabel();
            CreateWaveVisualizer();
        }

        private void CreateDebugLabel() {
            _debugLabel = new Label();
            _debugLabel.Dock = DockStyle.Top;
            _debugLabel.Height = 50;
            _debugLabel.ForeColor = Color.White;
            _debugLabel.BackColor = Color.Black;
            _debugLabel.TextAlign = ContentAlignment.MiddleCenter;
            _debugLabel.Font = new Font(_debugLabel.Font, FontStyle.Bold);
            _debugLabel.Text = "DEBUG";

            Controls.Add(_debugLabel);
        }

        private void CreateWaveVisualizer() {
            _waveViewer = new WaveViewer();
            _waveViewer.Anchor = AnchorStyles.None;
            _waveViewer.Height = 200;
            _waveViewer.Width = Width;
            _waveViewer.Location = new Point(0, Height / 2 - _waveViewer.Height / 2);
            _waveViewer.Padding = new Padding(0, 0, 0, 50);
            _waveViewer.BackColor = Color.CornflowerBlue;
            _waveViewer.MouseWheel += WaveViewer_MouseWheel;
            
            Controls.Add(_waveViewer);

            _tracker = new Panel();
            _tracker.BackColor = Color.White;
            _tracker.Height = _waveViewer.Height;
            _tracker.Width = 1;

            _waveViewer.Controls.Add(_tracker);
        }

        private void Log(string log) {
            _debugLabel.Text = log;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e) {
            BackColor = Color.WhiteSmoke;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e) {
            BackColor = Color.Black;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0) {
                if (GetFileType(files[0]) == AudioFileType.None)
                    return;

                LoadFile(files[0]);
            }
        }

        private void LoadFile(string path) {
            if (GetFileType(path) == AudioFileType.None)
                return;

            if (_outputDevice == null) {
                _outputDevice = new WaveOutEvent();
                _outputDevice.PlaybackStopped += OnPlaybackStopped;
            }

            _fileName = path;

            ShowWaveForm();
            PlaySound();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e) {
            _playing = false;
            Log("END");
        }

        private void PlaySound() {
            if (GetFileType(_fileName) == AudioFileType.None)
                return;

            _outputDevice.Stop();
            _audioFileReader = new AudioFileReader(_fileName);
            _outputDevice.Init(_audioFileReader);
            _outputDevice.Play();

            _playing = true;
            UpdateTrackerPosition();

            Log($"Playing > {_fileName}");
        }

        private void ShowWaveForm() {
            AudioFileType type = GetFileType(_fileName);

            if (type == AudioFileType.mp3)
                _waveViewer.WaveStream = new Mp3FileReader(_fileName);
            else if (type == AudioFileType.wav)
                _waveViewer.WaveStream = new WaveFileReader(_fileName);
            else
                return;

            _zoomFactor = 1;
            UpdateWaveForm();
        }

        private void UpdateWaveForm() {
            _waveViewer.SamplesPerPixel = (int)_waveViewer.WaveStream.TotalTime.TotalMilliseconds;
            _waveViewer.SamplesPerPixel /= _zoomFactor;
        }

        private async void UpdateTrackerPosition() {
            if (!_playing)
                return;

            double timeRatio = _audioFileReader.CurrentTime.TotalMilliseconds / _audioFileReader.TotalTime.TotalMilliseconds;
            double lengthRatio = 
            _tracker.Location = new Point((int)Math.Round(_waveViewer.Width * timeRatio), 0);

            Log(_audioFileReader.CurrentTime.ToString());
            await Task.Delay(10);
            UpdateTrackerPosition();
        }

        private AudioFileType GetFileType(string path) {
            string ext = Path.GetExtension(path);

            switch (ext) {
                case ".mp3":
                    return AudioFileType.mp3;

                case ".wav":
                    return AudioFileType.wav;
            }

            Log("this file is not a .wav nor a .mp3");
            return AudioFileType.None;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Space)
                PlaySound();
        }

        private void WaveViewer_MouseWheel(object sender, MouseEventArgs e) {
            if (e.Delta > 0)
                _zoomFactor *= 2;
            else
                _zoomFactor /= 2;

            if (_zoomFactor < 1)
                _zoomFactor = 1;

            UpdateWaveForm();

            Log($"Zoom factor : {_zoomFactor}");
        }

        private void MainForm_Paint(object sender, PaintEventArgs e) {

        }
    }
}
