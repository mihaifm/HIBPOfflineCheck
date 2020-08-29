using KeePass.App;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HIBPOfflineCheck
{
    public partial class CreateBloomFilter : Form
    {
        private HIBPOfflineCheckExt ext;

        private CancellationTokenSource cancellationTokenSource;
        private Stopwatch stopwatch;
        private System.Windows.Forms.Timer timer;

        public CreateBloomFilter(HIBPOfflineCheckExt ext)
        {
            InitializeComponent();

            this.ext = ext;
            textBoxInput.Text = ext.Prov.PluginOptions.HIBPFileName;
            Icon = AppIcons.Default;
            cancellationTokenSource = new CancellationTokenSource();
            stopwatch = new Stopwatch();
            timer = new System.Windows.Forms.Timer();

            timer.Tick += TimerTick;
            timer.Interval = 1000;
        }

        private void buttonSelectInput_Click(object sender, EventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                string file = openDialog.FileName;
                textBoxInput.Text = file;
            }
        }

        private void buttonSelectOutput_Click(object sender, EventArgs e)
        {
            string defaultFileName = "HIBPBloomFilter.bin";

            var openDialog = new SaveFileDialog();
            openDialog.Filter = "All Files (*.*)|*.*";
            openDialog.FileName = defaultFileName;

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                string file = openDialog.FileName;
                textBoxOutput.Text = file;
            }
        }

        private void CreateBloomFilter_FormClosed(object sender, FormClosedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
            Close();
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {
            if (textBoxOutput.Text == string.Empty)
            {
                MessageBox.Show("Please specify an output file", "Output file not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var inputFile = textBoxInput.Text;
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException();
            }

            Bloom();
        }

        private void BloomWorker(IProgress<int> progress, CancellationToken token)
        {
            progress.Report(1);

            var lineCount = 0;
            using (var reader = File.OpenText(textBoxInput.Text))
            {
                while (reader.ReadLine() != null)
                {
                    lineCount++;

                    if (lineCount % (1024 * 1024) == 0)
                    {
                        if (token.IsCancellationRequested)
                            return;
                    }
                }
            }

            //lineCount = 551509767;

            var currentLine = 0;
            var progressTick = lineCount / 100;

            BloomFilter bloomFilter = new BloomFilter(lineCount, 0.001F);

            progress.Report(5);

            var inputFile = textBoxInput.Text;

            using (var fs = File.OpenRead(inputFile))
            using (var sr = new StreamReader(fs))
            {
                while (sr.EndOfStream == false)
                {
                    var line = sr.ReadLine().Substring(0, 40);
                    bloomFilter.Add(line);
                    currentLine++;

                    if (currentLine % progressTick == 0)
                    {
                        progress.Report(5 + (int)(((double)currentLine) / lineCount * 90));

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
            }

            bloomFilter.Save(textBoxOutput.Text);

            progress.Report(100);
        }

        private async void Bloom()
        {
            progressBar.Show();

            stopwatch.Start();
            timer.Start();

            var progress = new Progress<int>(percent =>
            {
                progressBar.Value = percent;
            });

            CancellationToken token = cancellationTokenSource.Token;

            await Task.Run(() => BloomWorker(progress, token));

            labelInfo.Text = "Bloom filter successfully generated in: " + stopwatch.Elapsed.ToString("hh\\:mm\\:ss");
            timer.Stop();
            stopwatch.Stop();
            stopwatch.Reset();
            progressBar.Value = 0;
            progressBar.Hide();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            labelInfo.Text = "Elapsed time: " + stopwatch.Elapsed.ToString("hh\\:mm\\:ss");
        }
    }
}
