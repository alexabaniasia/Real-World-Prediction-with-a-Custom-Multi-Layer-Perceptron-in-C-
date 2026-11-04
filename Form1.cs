using Backprop;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Backpropagation
{
    public partial class Form1 : Form
    {
        NeuralNet nn;
        Bitmap loaded;
        List<double[]> data = new List<double[]>();
        List<int> labels = new List<int>();
        string[] classes;
        int size = 32;
        int hidden = 32;

        public Form1()
        {
            InitializeComponent();
        }

        double[] Pixels(Bitmap b)
        {
            Bitmap s = new Bitmap(b, size, size);
            double[] p = new double[size * size * 3];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    Color c = s.GetPixel(x, y);
                    int k = (x * size + y) * 3;
                    p[k] = c.R / 255.0;
                    p[k + 1] = c.G / 255.0;
                    p[k + 2] = c.B / 255.0;
                }
            return p;
        }

        void Feed(double[] p)
        {
            for (int i = 0; i < p.Length; i++)
                nn.setInputs(i, p[i]);
        }

        // load image
        private void button7_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            loaded = new Bitmap(openFileDialog1.FileName);
            pictureBox1.Image = loaded;
        }

        // load dataset
        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            if (fb.ShowDialog() != DialogResult.OK) return;

            string[] dirs = Directory.GetDirectories(fb.SelectedPath);
            classes = new string[dirs.Length];
            data.Clear();
            labels.Clear();

            for (int c = 0; c < dirs.Length; c++)
            {
                classes[c] = Path.GetFileName(dirs[c]);
                foreach (string f in Directory.GetFiles(dirs[c]))
                {
                    data.Add(Pixels(new Bitmap(f)));
                    labels.Add(c);
                }
            }
            nn = new NeuralNet(size * size * 3, hidden, classes.Length);
            MessageBox.Show("Loaded " + data.Count + " images in " + classes.Length + " classes.");
        }

        // train
        double[][] configs = {
            new double[]{16, 0.1, 100},
            new double[]{32, 0.1, 100},
            new double[]{32, 0.2, 100},
            new double[]{32, 0.2, 200},
            new double[]{64, 0.2, 200}
        };

        private void button6_Click(object sender, EventArgs e)
        {
            int total = 0;
            foreach (double[] cfg in configs) total += (int)cfg[2];

            progressBar1.Minimum = 0;
            progressBar1.Maximum = total;
            progressBar1.Value = 0;

            Thread t = new Thread(new ThreadStart(Train));
            t.IsBackground = true;
            t.Start();
        }

        void Train()
        {
            string tag = size + "x" + size + "_" + data.Count + "img";
            string logFile = "log_" + tag + ".csv";
            string tuningFile = "tuning_" + tag + ".csv";
            string resultsFile = "results_" + tag + ".txt";

            Random r = new Random();

            var idx = Enumerable.Range(0, data.Count).OrderBy(x => r.Next()).ToList();
            int cut = idx.Count * 8 / 10;
            var train = idx.Take(cut).ToList();
            var test = idx.Skip(cut).ToList();

            File.WriteAllText(logFile, "hidden,lr,epoch,train_mse\n");
            File.WriteAllText(tuningFile, "hidden,lr,epochs,test_accuracy\n");
            double bestAcc = -1; int bestHidden = hidden;
            int done = 0;

            foreach (double[] cfg in configs)
            {
                int h = (int)cfg[0]; double lr = cfg[1]; int epochs = (int)cfg[2];
                NeuralNet.LRPOUT = lr; NeuralNet.LRPIN = lr;
                nn = new NeuralNet(size * size * 3, h, classes.Length);

                for (int epoch = 1; epoch <= epochs; epoch++)
                {
                    for (int n = 0; n < train.Count; n++)
                    {
                        int i = train[r.Next(train.Count)];
                        Feed(data[i]);
                        for (int c = 0; c < classes.Length; c++)
                            nn.setDesiredOutput(c, c == labels[i] ? 1.0 : 0.0);
                        nn.learn();
                    }
                    if (epoch % 10 == 0)
                        File.AppendAllText(logFile, h + "," + lr + "," + epoch + "," + Mse(train) + "\n");

                    done++;
                    progressBar1.Invoke(new Action(() => progressBar1.Value = done));
                }

                int[,] m = Confusion(test);
                double acc = Accuracy(m);
                File.AppendAllText(tuningFile, h + "," + lr + "," + epochs + "," + acc + "\n");
                if (acc > bestAcc) { bestAcc = acc; bestHidden = h; nn.saveWeights("best.tmp"); }
            }

            hidden = bestHidden;
            nn = new NeuralNet(size * size * 3, hidden, classes.Length);
            nn.loadWeights("best.tmp");

            int[,] cm = Confusion(test);
            string report = "Best hidden = " + hidden + "\nTest accuracy = " + (Accuracy(cm) * 100).ToString("F1") + "%\n";
            for (int c = 0; c < classes.Length; c++)
            {
                int tp = cm[c, c], predicted = 0, actual = 0;
                for (int k = 0; k < classes.Length; k++) { predicted += cm[k, c]; actual += cm[c, k]; }
                report += classes[c] + ": precision " + (predicted == 0 ? 0 : (double)tp / predicted).ToString("F2")
                        + ", recall " + (actual == 0 ? 0 : (double)tp / actual).ToString("F2") + "\n";
            }
            File.WriteAllText(resultsFile, report);
            MessageBox.Show(report);
        }

        double Mse(List<int> set)
        {
            double sum = 0;
            foreach (int i in set)
            {
                Feed(data[i]); nn.run();
                for (int c = 0; c < classes.Length; c++)
                {
                    double d = (c == labels[i] ? 1.0 : 0.0) - nn.getOuputData(c);
                    sum += d * d;
                }
            }
            return sum / (set.Count * classes.Length);
        }

        int[,] Confusion(List<int> set)
        {
            int[,] m = new int[classes.Length, classes.Length];
            foreach (int i in set)
            {
                Feed(data[i]); nn.run();
                int best = 0;
                for (int c = 1; c < classes.Length; c++)
                    if (nn.getOuputData(c) > nn.getOuputData(best)) best = c;
                m[labels[i], best]++;
            }
            return m;
        }

        double Accuracy(int[,] m)
        {
            int ok = 0, total = 0;
            for (int a = 0; a < classes.Length; a++)
                for (int p = 0; p < classes.Length; p++)
                {
                    total += m[a, p];
                    if (a == p) ok += m[a, p];
                }
            return (double)ok / total;
        }

        // classify
        private void button5_Click(object sender, EventArgs e)
        {
            Feed(Pixels(loaded));
            nn.run();

            int best = 0;
            for (int c = 1; c < classes.Length; c++)
                if (nn.getOuputData(c) > nn.getOuputData(best))
                    best = c;

            textBox4.Text = classes[best];
        }

        // save
        private void button8_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "weights_" + size + "x" + size + "_h" + hidden + "_" + (data.Count / classes.Length) + "img";
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
            nn.saveWeights(saveFileDialog1.FileName);
            File.WriteAllLines(saveFileDialog1.FileName + ".classes.txt", classes);
        }

        // load weights
        private void button9_Click(object sender, EventArgs e)
        {
            openFileDialog2.ShowDialog();
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            classes = File.ReadAllLines(openFileDialog2.FileName + ".classes.txt");
            nn = new NeuralNet(size * size * 3, hidden, classes.Length);
            nn.loadWeights(openFileDialog2.FileName);
        }
    }
}