using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Audio.Filters;
using Recorder.Recorder;
using Recorder.MFCC;
using System.Collections.Generic;
//using Oracle.DataAccess.Client;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace Recorder
{
    /// <summary>
    ///   Speaker Identification application.
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        /// <summary>
        /// Data of the opened audio file, contains:
        ///     1. signal data
        ///     2. sample rate
        ///     3. signal length in ms
        /// </summary>
        private AudioSignal signal = null;
        Sequence seq = null;

        private string path;
        int choose = 0;
        private Recorder.Encoder encoder;
        private Recorder.Decoder decoder;
        List<Sequence> sequences;
        List<Sequence> trainsequences1;
        List<Sequence> trainsequences2;
        List<Sequence> trainsequences3;
        List<Sequence> trainSequences;
        List<Sequence> testsequences;
        List<User> userstest;
        List<User> users;
        private bool isRecorded;

        public MainForm()
        {
            InitializeComponent();
            sequences = new List<Sequence>();
            trainSequences = new List<Sequence>();
            trainsequences1 = new List<Sequence>();
            trainsequences2 = new List<Sequence>();
            trainsequences3 = new List<Sequence>();

            loadSequences();
            users = new List<User>();
            userstest = new List<User>();
            // Configure the wavechart
            chart.SimpleMode = true;
            chart.AddWaveform("wave", Color.Green, 1, false);
            updateButtons();
        }


        /// <summary>
        ///   Starts recording audio from the sound card
        /// </summary>
        /// 
        private void btnRecord_Click(object sender, EventArgs e)
        {
            isRecorded = true;
            this.encoder = new Recorder.Encoder(source_NewFrame, source_AudioSourceError);
            this.encoder.Start();
            updateButtons();
        }

        /// <summary>
        ///   Plays the recorded audio stream.
        /// </summary>
        /// 
        private void btnPlay_Click(object sender, EventArgs e)
        {
            InitializeDecoder();
            // Configure the track bar so the cursor
            // can show the proper current position
            if (trackBar1.Value < this.decoder.frames)
                this.decoder.Seek(trackBar1.Value);
            trackBar1.Maximum = this.decoder.samples;
            this.decoder.Start();
            updateButtons();
        }

        private void InitializeDecoder()
        {
            if (isRecorded)
            {
                // First, we rewind the stream
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                this.decoder = new Recorder.Decoder(this.encoder.stream, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
            else
            {
                this.decoder = new Recorder.Decoder(this.path, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
        }

        /// <summary>
        ///   Stops recording or playing a stream.
        /// </summary>
        /// 
        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This callback will be called when there is some error with the audio 
        ///   source. It can be used to route exceptions so they don't compromise 
        ///   the audio processing pipeline.
        /// </summary>
        /// 
        private void source_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   This method will be called whenever there is a new input audio frame 
        ///   to be processed. This would be the case for samples arriving at the 
        ///   computer's microphone
        /// </summary>
        /// 
        private void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            this.encoder.addNewFrame(eventArgs.Signal);
            updateWaveform(this.encoder.current, eventArgs.Signal.Length);
        }


        /// <summary>
        ///   This event will be triggered as soon as the audio starts playing in the 
        ///   computer speakers. It can be used to update the UI and to notify that soon
        ///   we will be requesting additional frames.
        /// </summary>
        /// 
        private void output_FramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            updateTrackbar(e.FrameIndex);

            if (e.FrameIndex + e.Count < this.decoder.frames)
            {
                int previous = this.decoder.Position;
                decoder.Seek(e.FrameIndex);

                Signal s = this.decoder.Decode(e.Count);
                decoder.Seek(previous);

                updateWaveform(s.ToFloat(), s.Length);
            }
        }

        /// <summary>
        ///   This event will be triggered when the output device finishes
        ///   playing the audio stream. Again we can use it to update the UI.
        /// </summary>
        /// 
        private void output_PlayingFinished(object sender, EventArgs e)
        {
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This event is triggered when the sound card needs more samples to be
        ///   played. When this happens, we have to feed it additional frames so it
        ///   can continue playing.
        /// </summary>
        /// 
        private void output_NewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            this.decoder.FillNewFrame(e);
        }


        void output_AudioOutputError(object sender, AudioOutputErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   Updates the audio display in the wave chart
        /// </summary>
        /// 
        private void updateWaveform(float[] samples, int length)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    chart.UpdateWaveform("wave", samples, length);
                }));
            }
            else
            {
                if (this.encoder != null) { chart.UpdateWaveform("wave", this.encoder.current, length); }
            }
        }

        /// <summary>
        ///   Updates the current position at the trackbar.
        /// </summary>
        /// 
        private void updateTrackbar(int value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
                }));
            }
            else
            {
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
            }
        }

        private void updateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(updateButtons));
                return;
            }

            if (this.encoder != null && this.encoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = false;
            }
            else if (this.decoder != null && this.decoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = this.path != null || this.encoder != null;
                btnIdentify.Enabled = true;
                btnPlay.Enabled = this.path != null || this.encoder != null;//stream != null;
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                trackBar1.Enabled = this.decoder != null;
                trackBar1.Value = 0;
            }
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.encoder != null)
            {
                Stream fileStream = saveFileDialog1.OpenFile();
                this.encoder.Save(fileStream);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (this.encoder != null) { lbLength.Text = String.Format("Length: {0:00.00} sec.", this.encoder.duration / 1000.0); }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                isRecorded = false;
                path = open.FileName;
                //Open the selected audio file
                signal = AudioOperations.OpenAudioFile(path);
                signal = AudioOperations.RemoveSilence(signal);
                seq = AudioOperations.ExtractFeatures(signal);
                for (int i = 0; i < seq.Frames.Length; i++)
                {
                    for (int j = 0; j < 13; j++)
                    {

                        if (double.IsNaN(seq.Frames[i].Features[j]) || double.IsInfinity(seq.Frames[i].Features[j]))
                            throw new Exception("NaN");
                    }
                }
                MessageBox.Show("File loaded successfully");
                MessageBox.Show("Length: " + signal.signalLengthInMilliSec / 1000.0);
                lbLength.Text = String.Format("Length: {0:00.00} sec.", signal.signalLengthInMilliSec / 1000.0);
                updateButtons();
                btnIdentify.Enabled = true;
            }
        }

        private void Stop()
        {
            if (this.encoder != null) { this.encoder.Stop(); }
            if (this.decoder != null) { this.decoder.Stop(); }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            nameLbl.Visible = true;
            nameBox.Visible = true;
            Submit.Visible = true;
            DTW_without_pruning.Visible = false;
            Beam_Search.Visible = false;
            Synchronous_Search.Visible = false;
            Sync_Beam_Search.Visible = false;
            DTW_with_pruning.Visible = false;
        }

        private void loadTrain1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.ShowDialog();
            users = TestcaseLoader.LoadTestcase1Training(fileDialog.FileName);
            foreach (User user in users)
            {
                foreach (AudioSignal signal in user.UserTemplates)
                {
                    Sequence newSeq = new Sequence();
                    newSeq.Name = user.UserName;
                    seq = AudioOperations.ExtractFeatures(signal);
                    for (int i = 0; i < seq.Frames.Length; i++)
                    {
                        for (int j = 0; j < 13; j++)
                        {
                            if (double.IsNaN(seq.Frames[i].Features[j]) || double.IsInfinity(seq.Frames[i].Features[j]))
                                throw new Exception("NaN");
                        }
                    }
                    sequences.Add(seq);
                }
            }
        }

        private void btnIdentify_Click(object sender, EventArgs e)
        {
            DTW_without_pruning.Visible = true;
            Beam_Search.Visible = true;
            Synchronous_Search.Visible = true;
            Sync_Beam_Search.Visible = true;
            DTW_with_pruning.Visible = true;
            nameBox.Visible = false;
            Submit.Visible = false;
            nameLbl.Visible = false;
            checkIsRecorded();
        }

        private void Submit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                MessageBox.Show("Please enter your name in the box before proceeding.");
                return;
            }
            string name = nameBox.Text;
            //if (usersData.ContainsKey(name))
            //{
            //    MessageBox.Show("This name has already been used. Please choose another one.");
            //    return;
            //}
            string filePath = "";
            if (isRecorded)
            {
                this.encoder.Stop();
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                signal = AudioOperations.createRecordedSignal(this.encoder.stream);
            }
            else
            {
                signal = AudioOperations.OpenAudioFile(this.path);
            }
            signal = AudioOperations.RemoveSilence(signal);
            seq = AudioOperations.ExtractFeatures(signal);
            seq.Name = name;
            for (int i = 0; i < seq.Frames.Length; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    if (double.IsNaN(seq.Frames[i].Features[j]) || double.IsInfinity(seq.Frames[i].Features[j]))
                        throw new Exception("NaN");
                }
            }
            string folderPath = "C:\\Users\\LP\\Downloads\\Speaker Identification Startup Code\\Speaker Identification Startup Code\\[TEMPLATE] SpeakerID\\[TEMPLATE] SpeakerID\\audio";
            string fileName = $"input_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            filePath = folderPath + "\\" + fileName;
            StringBuilder content = new StringBuilder();
            content.Append(seq.Name + ":");
            for (int j = 0; j < seq.Frames.Length; j++)
            {
                for (int i = 0; i < 13; i++)
                {
                    content.Append(seq.Frames[j].Features[i].ToString());
                    if (!(j == seq.Frames.Length - 1 && i == 12))
                        content.Append(",");
                }
            }
            File.WriteAllText(filePath, content.ToString());
            //usersData[name] = seq;
            sequences.Add(seq);
            MessageBox.Show("Your recod has been added successfully");
            updateButtons();
            encoder = null;
            decoder = null;
            signal = null;
            seq = null;
            path = null;
            isRecorded = false;
            chart.UpdateWaveform("wave", new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
            trackBar1.Value = 0;
            trackBar1.Maximum = 0;
            lbLength.Text = "Length: 00.00 sec.";
            btnAdd.Enabled = false;
            btnIdentify.Enabled = false;
            btnPlay.Enabled = false;
            btnStop.Enabled = false;
            btnRecord.Enabled = true;
            trackBar1.Enabled = false;
            btnAdd.Enabled = false;
            btnIdentify.Enabled = false;
            nameLbl.Visible = false;
            nameBox.Visible = false;
            Submit.Visible = false;
        }

        private void loadSequences()
        {
            string folderPath = "C:\\Users\\LP\\Downloads\\Speaker Identification Startup Code\\Speaker Identification Startup Code\\[TEMPLATE] SpeakerID\\[TEMPLATE] SpeakerID\\audio";
            string[] pathes = Directory.GetFiles(folderPath);

            for (int i = 0; i < pathes.Length; i++)
            {
                Sequence newSeq = new Sequence();
                StreamReader reader = new StreamReader(pathes[i]);
                string content = reader.ReadToEnd();
                string[] splitedContent = content.Split(':');
                newSeq.Name = splitedContent[0];
                string[] splitedFrames = splitedContent[1].Split(',');
                int framesNumber = (splitedFrames.Length) / 13;
                newSeq.Frames = new MFCCFrame[framesNumber];
                int index = 0;
                for (int j = 0; j < framesNumber; j++)
                {
                    newSeq.Frames[j] = new MFCCFrame();
                    newSeq.Frames[j].Features = new double[13];
                    for (int k = 0; k < 13; k++)
                    {
                        newSeq.Frames[j].Features[k] = Convert.ToDouble(splitedFrames[index]);
                        index++;
                    }
                }
                sequences.Add(newSeq);
            }
        }

        private void checkIsRecorded()
        {
            if (isRecorded)
            {
                this.encoder.Stop();
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                signal = AudioOperations.createRecordedSignal(this.encoder.stream);
            }
            else
            {
                signal = AudioOperations.OpenAudioFile(this.path);
            }
            signal = AudioOperations.RemoveSilence(signal);
            seq = AudioOperations.ExtractFeatures(signal);
            for (int i = 0; i < seq.Frames.Length; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    if (double.IsNaN(seq.Frames[i].Features[j]) || double.IsInfinity(seq.Frames[i].Features[j]))
                        throw new Exception("NaN");
                }
            }
        }

        private void DTW_without_pruning_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            double minCost = double.PositiveInfinity;
            double result = double.PositiveInfinity;
            Sequence requiredSeq = new Sequence();
            foreach (var sequence in sequences)
            {
                result = DTWFunctions.DTW_WithoutPrunning(seq, sequence);
                if (result < minCost)
                {
                    minCost = result;
                    requiredSeq = sequence;
                }
            }
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            MessageBox.Show("Time taken: " + elapsedTime.TotalMilliseconds + " ms");
            MessageBox.Show(minCost.ToString());
            MessageBox.Show(requiredSeq.Name);
            DTW_without_pruning.Visible = false;
            Beam_Search.Visible = false;
            Synchronous_Search.Visible = false;
            Sync_Beam_Search.Visible = false;
            DTW_with_pruning.Visible = false;
        }


        private void DTW_with_pruning_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            double minCost = double.PositiveInfinity;
            double result = double.PositiveInfinity;
            Sequence requiredSeq = new Sequence();
            foreach (var sequence in sequences)
            {
                result = DTWFunctions.DTW_with_pruning(seq, sequence,1111);
                if (result < minCost)
                {
                    minCost = result;
                    requiredSeq = sequence;
                }
            }
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            MessageBox.Show("Time taken: " + elapsedTime.TotalMilliseconds + " ms");
            MessageBox.Show(minCost.ToString());
            MessageBox.Show(requiredSeq.Name);
            DTW_without_pruning.Visible = false;
            Beam_Search.Visible = false;
            Synchronous_Search.Visible = false;
            Sync_Beam_Search.Visible = false;
            DTW_with_pruning.Visible = false;
        }

        private void Beam_Search_Click(object sender, EventArgs e)
        {

            checkIsRecorded();
            Stopwatch stopwatch = Stopwatch.StartNew();
            double minCost = double.PositiveInfinity;
            double result = double.PositiveInfinity;
            Sequence requiredSeq = new Sequence();


            foreach (var sequence in sequences)
            {
                //double width = 4 * Math.Max(seq.Frames.Count(), sequence.Frames.Count());
                result = DTWFunctions.DTW_WithBeamSearch(seq, sequence,1111);
                if (result < minCost)
                {
                    minCost = result;
                    requiredSeq = sequence;
                }
            }
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            MessageBox.Show("Time taken: " + elapsedTime.TotalMilliseconds + " ms");
            MessageBox.Show(minCost.ToString());
            MessageBox.Show(requiredSeq.Name);
            DTW_without_pruning.Visible = false;
            Beam_Search.Visible = false;
            Synchronous_Search.Visible = false;
            Sync_Beam_Search.Visible = false;
            DTW_with_pruning.Visible = false;
        }


        private void Sync_Beam_Search_Click(object sender, EventArgs e)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            //double minCost = double.PositiveInfinity;
            //double result = double.PositiveInfinity;
            //Sequence requiredSeq = new Sequence();
            //foreach (var sequence in sequences)
            //{
            //    result = DTWFunctions.Beam_Search(seq, sequence.Frames.ToList(), 333);
            //    if (result < minCost)
            //    {
            //        minCost = result;
            //        requiredSeq = sequence;
            //    }
            //}
            //stopwatch.Stop();
            //TimeSpan elapsedTime = stopwatch.Elapsed;
            //MessageBox.Show("Time taken: " + elapsedTime.TotalMilliseconds + " ms");
            //MessageBox.Show(minCost.ToString());
            //MessageBox.Show(requiredSeq.Name);
            //DTW_without_pruning.Visible = false;
            //Beam_Search.Visible = false;
            //Synchronous_Search.Visible = false;
            //Sync_Beam_Search.Visible = false;
            //DTW_with_pruning.Visible = false;
        }

        private void Synchronous_Search_Click(object sender, EventArgs e)
        {

        }

        private void loadTrain_Click(object sender, EventArgs e)
        {
            choose = 1;
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.ShowDialog();
            if (string.IsNullOrWhiteSpace(caseTxt.Text))
            {
                MessageBox.Show("Please enter number of test case");
                return;
            }
            int number = Convert.ToInt32(caseTxt.Text);
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (number)
            {
                case 1:
                    users = TestcaseLoader.LoadTestcase1Training(fileDialog.FileName);
                    break;
                case 2:
                    users = TestcaseLoader.LoadTestcase1Training(fileDialog.FileName);
                    break;
                case 3:
                    users = TestcaseLoader.LoadTestcase1Training(fileDialog.FileName);
                    break;
                default:
                    MessageBox.Show("Invalid test case number");
                    return;
            }
            foreach (User user in users)
            {
                foreach (AudioSignal signal in user.UserTemplates)
                {
                    //AudioSignal newSignal = new AudioSignal();
                    Sequence newSeq = new Sequence();
                    //newSignal = AudioOperations.RemoveSilence(signal);
                    newSeq = AudioOperations.ExtractFeatures(signal);
                    newSeq.Name = user.UserName;
                    //try
                    //{
                    for (int i = 0; i < newSeq.Frames.Length; i++)
                    {
                        for (int j = 0; j < 13; j++)
                        {
                            if (double.IsNaN(newSeq.Frames[i].Features[j]) || double.IsInfinity(newSeq.Frames[i].Features[j]))
                            {
                                newSeq.Frames[i].Features[j] = 0.0;
                            }
                            //throw new Exception("NaN");
                        }
                    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    MessageBox.Show("Feature extraction error: " + ex.Message);
                    //    return;
                    //}

                    trainSequences.Add(newSeq);
                }
            }
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            MessageBox.Show("Time taken: " + elapsedTime.TotalMinutes + " ms");
            MessageBox.Show("done");
            string folderPath = $"C:\\Users\\LP\\Downloads\\Speaker Identification Startup Code\\Speaker Identification Startup Code\\[TEMPLATE] SpeakerID\\[TEMPLATE] SpeakerID\\train{number}";
            string Path = "";
            string Name = $"train{number}.txt";
            Path = folderPath + "\\" + Name;
            StreamWriter writer = new StreamWriter(Path);
            int index = 0;
            foreach (Sequence trainseq in trainSequences)
            {
                writer.Write(trainseq.Name + ":");
                for (int j = 0; j < trainseq.Frames.Length; j++)
                {
                    for (int i = 0; i < 13; i++)
                    {
                        string value = trainseq.Frames[j].Features[i].ToString();
                        writer.Write(value);
                        if (!(j == trainseq.Frames.Length - 1 && i == 12))
                            writer.Write(",");
                    }
                }
                if (index < trainSequences.Count - 1)  
                    writer.Write("?");
            }
            writer.Close();
        }

        private void Test_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<string> names = new List<string>();
            //List<Sequence> match = new List<Sequence>();
            //switch (choose)
            //{
            //    case 1:
            //        match = trainSequences;
            //        break;
            //    case 2:
            //        match = trainsequences1;
            //        break;
            //    case 3:
            //        match = trainsequences2;
            //        break;
            //    case 4:
            //        match = trainsequences3;
            //        break;
            //}

            foreach (User user in userstest)
            {
                foreach (AudioSignal signal in user.UserTemplates)
                {
                    Sequence newSeq = new Sequence();
                    newSeq = AudioOperations.ExtractFeatures(signal);
                    newSeq.Name = user.UserName;
                    try
                    {
                        for (int i = 0; i < newSeq.Frames.Length; i++)
                        {
                            for (int j = 0; j < 13; j++)
                            {
                                if (double.IsNaN(newSeq.Frames[i].Features[j]) || double.IsInfinity(newSeq.Frames[i].Features[j]))
                                    throw new Exception("NaN");
                                //newSeq.Frames[i].Features[j] = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Feature extraction error: " + ex.Message);
                        return;
                    }
                    double minCost = double.PositiveInfinity;
                    double result = double.PositiveInfinity;
                    Sequence requiredSeq = null;
                    foreach (Sequence trainseq in trainSequences)
                    {
                        //result = DTWFunctions.DTW_WithoutPrunning(newSeq, trainseq);
                        //result = DTWFunctions.DTW_with_pruning(newSeq, trainseq, 23);
                        result = DTWFunctions.DTW_WithBeamSearch(newSeq, trainseq, 23);

                        if (result < minCost)
                        {
                            minCost = result;
                            requiredSeq = trainseq;
                        }
                    }
                    names.Add(requiredSeq.Name);
                }
            }

            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            MessageBox.Show("Time taken: " + elapsedTime.TotalMinutes + " ms");
            double accuracy = TestcaseLoader.CheckTestcaseAccuracy(userstest, names);
            MessageBox.Show("Error: " + accuracy.ToString() + "%");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.ShowDialog();
            if (string.IsNullOrWhiteSpace(caseTxt.Text))
            {
                MessageBox.Show("Please enter number of test case");
                return;
            }
            int number = Convert.ToInt32(caseTxt.Text);
            Stopwatch stopwatch = Stopwatch.StartNew();
            switch (number)
            {
                case 1:
                    userstest = TestcaseLoader.LoadTestcase1Testing(fileDialog.FileName);
                    break;
                case 2:
                    userstest = TestcaseLoader.LoadTestcase1Testing(fileDialog.FileName);
                    break;
                case 3:
                    userstest = TestcaseLoader.LoadTestcase1Testing(fileDialog.FileName);
                    break;
                default:
                    MessageBox.Show("Invalid test case number");
                    return;
            }
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            MessageBox.Show("Time taken: " + elapsedTime.TotalMinutes + " ms");
            MessageBox.Show("done");
        }
        private List<Sequence> loadTrainSequences(List<Sequence> train, int number)
        {

            string folderPath = $"C:\\Users\\LP\\Downloads\\Speaker Identification Startup Code\\Speaker Identification Startup Code\\[TEMPLATE] SpeakerID\\[TEMPLATE] SpeakerID\\train{number}";
            string fileName = folderPath + "\\" + $"train{number}.txt";
            StreamReader reader = new StreamReader(fileName);
            string content = reader.ReadToEnd();
            string[] sequenceSplit = content.Split('?');
            for (int i = 0; i < sequenceSplit.Length-1; i++)
            {
                Sequence newSeq = new Sequence();
                string[] splitName = sequenceSplit[i].Split(':');
                newSeq.Name = splitName[0];
                string[] splitedFrames = splitName[1].Split(',');
                int framesNumber = (splitedFrames.Length) / 13;
                newSeq.Frames = new MFCCFrame[framesNumber];
                int index = 0;
                for (int j = 0; j < framesNumber; j++)
                {
                    newSeq.Frames[j] = new MFCCFrame();
                    newSeq.Frames[j].Features = new double[13];
                    for (int k = 0; k < 13; k++)
                    {
                        newSeq.Frames[j].Features[k] = Convert.ToDouble(splitedFrames[index]);
                        index++;
                    }
                }
                train.Add(newSeq);
            }
            reader.Close();
            return train;
        }

        //private void button4_Click(object sender, EventArgs e)
        //{
        //    choose = 2;
        //    trainsequences1 = loadTrainSequences(trainsequences1, 1);
        //    if (trainsequences1 == null || trainsequences1.Count == 0)
        //    {
        //        MessageBox.Show("Error: Training sequences failed to load or are empty.");
        //        return;
        //    }
        //    else
        //    {
        //        MessageBox.Show("Training sequences loaded successfully.");
        //    }

        //}

        //private void case2_Click(object sender, EventArgs e)
        //{
        //    choose = 3;
        //    trainsequences2 = loadTrainSequences(trainsequences2, 2);
        //}

        //private void case3_Click(object sender, EventArgs e)
        //{
        //    choose = 4;
        //    trainsequences3 = loadTrainSequences(trainsequences3, 3);
        //}

        private void case1_Click(object sender, EventArgs e)
        {
            choose = 2;
            trainSequences = loadTrainSequences(trainSequences, 1);
            if (trainSequences == null || trainSequences.Count == 0)
            {
                MessageBox.Show("Error: Training sequences failed to load or are empty.");
                return;
            }
            else
            {
                MessageBox.Show("Training sequences loaded successfully.");
            }
        }

        private void case2_Click(object sender, EventArgs e)
        {
            choose = 3;
            trainsequences2 = loadTrainSequences(trainsequences2, 2);
        }

        private void case3_Click(object sender, EventArgs e)
        {
            choose = 4;
            trainsequences3 = loadTrainSequences(trainsequences3, 3);
        }
    }
}