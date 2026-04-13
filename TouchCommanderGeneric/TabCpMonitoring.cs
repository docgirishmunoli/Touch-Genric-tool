using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WideBoxLib;

namespace TouchCommanderGenericNamespace
{
    public class CpMonitoringModel : TouchCommanderModelBase
    {
        private byte[][] data;
        private int iNumOfSlave;
        private bool[] mutualType; /*Self Cap = False, Mutual Cap = True*/
        private byte current_slave;
        private bool api17_cp_reading;

        public CpMonitoringModel(TouchCommanderController c) : base(c)
        {
            this.iNumOfSlave = control.GetNumberOfSlaves();
            this.mutualType = new bool[this.iNumOfSlave];

            this.data = new byte[this.iNumOfSlave][];

            for (int i = 0; i < this.iNumOfSlave; i++)
            {
                this.mutualType[i] = control.GetSensingType((byte)i) == "Mutual" ? true : false;
                if (this.mutualType[i] == true)
                {
                    this.data[i] = new byte[20];
                }
                else
                {
                    this.data[i] = new byte[22];
                }
            }
        }

        public override void updateModel(Object d)
        {
            byte[] Payload = (byte[])d;

            if (api17_cp_reading == true)
            {
                if (Payload[0] == 2) // 1 - Request, 2 - Response, 255 - OFF
                {
                    this.current_slave = Payload[1];

                    if (this.mutualType[this.current_slave] == true)
                    {
                        Buffer.BlockCopy(Payload, 2, data[this.current_slave], 0, 20);
                    }
                    else
                    {
                        Buffer.BlockCopy(Payload, 2, data[this.current_slave], 0, 11);
                        Buffer.BlockCopy(Payload, 14, data[this.current_slave], 11, 11);
                    }
                }
            }
            else
            {
                int i2c_address = (Payload[1] >> 1) ^ 2;
                int bytes_read = Payload[5];

                this.current_slave = control.GetSlaveIdFromI2CAddress((byte)i2c_address);

                if (bytes_read == 20)
                {
                    Buffer.BlockCopy(Payload, 6, data[this.current_slave], 0, 20);
                }
                else if (bytes_read == 24)
                {
                    Buffer.BlockCopy(Payload, 6, data[this.current_slave], 0, 11);
                    Buffer.BlockCopy(Payload, 18, data[this.current_slave], 11, 11);
                }
            }
        }

        public override void updateModel(Object d, string section)
        {

        }

        public byte[] GetCpData(byte slave_id)
        {
            return data[slave_id];
        }

        public byte GetCurrentSlave()
        {
            return this.current_slave;
        }

        public void ClearData(byte slave_id)
        {
            Array.Clear(data[slave_id], 0, data[slave_id].Length);
        }

        public void SetCPReadingMode(bool mode)
        {
            api17_cp_reading = mode;
        }

    }

    class CpMeasurementBackgroundWorker : BackgroundWorker
    {
        public byte slave_id;
        public int interval;
        public bool running;

        public CpMeasurementBackgroundWorker(byte slave_id, int interval)
        {
            this.slave_id = slave_id;
            this.interval = interval;
        }
    }

    public class CpMonitoringTabPage : TouchCommanderTabBase
    {
        private bool bViewInitComplete;
        private bool bMonitoringStarted;
        private bool bMonitoringEnabled;
        private int iNumOfSlave;
        private bool[] mutualType; /*Self Cap = False, Mutual Cap = True*/
        private bool[] cpMonitoringSupported;
        cypress_cp_monitoring_widgets[] cp_monitoring;
        private byte api17_major;
        private byte api17_minor;
        private byte api17_validation;

        private ComboBox cbCPReuestType;
        private ComboBox cbSlaveSelect;
        private Button btnStart;
        private Button btnLogging;
        private Button btnLogBrowse;
        private Label lbLogTimer;
        private Label lbCpNotes;
        private Button btnClear;
        private Button btnCpCalGuide;

        private Stopwatch logStopWatch;
        private System.Timers.Timer timer;
        private Dictionary<byte, CpMeasurementBackgroundWorker> cp_cmd_sender_dict = new Dictionary<byte, CpMeasurementBackgroundWorker>();
        private delegate void SafeCallDelegate(string text);

        public CpMonitoringTabPage(TouchCommanderController c) : base(c)
        {
            this.Text = "CP Monitoring";
            this.bViewInitComplete = false;
            this.bMonitoringStarted = false;

            this.iNumOfSlave = control.GetNumberOfSlaves();
            this.mutualType = new bool[this.iNumOfSlave];
            this.cp_monitoring = new cypress_cp_monitoring_widgets[this.iNumOfSlave];

            control.GetAPI17GDMVersion(out api17_major, out api17_minor, out api17_validation);

            for (int i = 0; i < this.iNumOfSlave; i++)
            {
                string device_type = control.GetDeviceType((byte)i);
                string firmware_type = control.GetFirmwareType((byte)i);
                bMonitoringEnabled = device_type == "PSoC4000S" && firmware_type == "Class B" ? true : false;

                this.mutualType[i] = control.GetSensingType((byte)i) == "Mutual" ? true : false;                
                cp_monitoring[i] = new cypress_cp_monitoring_widgets((byte)i, this.mutualType[i], bMonitoringEnabled);
                this.Controls.Add(cp_monitoring[i]);
                cp_monitoring[i].enable_logging(false);
            }

            initView();
        }

        private void initView()
        {

            cbCPReuestType = new ComboBox();
            cbCPReuestType.Location = new Point(1200, 15);
            cbCPReuestType.Size = new Size(150, 30);
            cbCPReuestType.Font = new Font(cbCPReuestType.Font.Name, cbCPReuestType.Font.Size + 2.0F, cbCPReuestType.Font.Style, cbCPReuestType.Font.Unit);
            cbCPReuestType.Items.Add(String.Format("API17 Publish"));
            cbCPReuestType.Items.Add(String.Format("Direct Addr Read"));
            cbCPReuestType.Enabled = bMonitoringEnabled;
            if (api17_major >= 14)
            {
                cbCPReuestType.SelectedIndex = 0;
            }
            else
            {
                cbCPReuestType.SelectedIndex = 1;
                cbCPReuestType.Enabled = false;
            }
            this.Controls.Add(cbCPReuestType);

            cbSlaveSelect = new ComboBox();
            cbSlaveSelect.Location = new Point(1200, 60);
            cbSlaveSelect.Size = new Size(150, 30);
            cbSlaveSelect.Font = new Font(cbSlaveSelect.Font.Name, cbSlaveSelect.Font.Size + 2.0F, cbSlaveSelect.Font.Style, cbSlaveSelect.Font.Unit);
            for (int i = 0; i < this.iNumOfSlave; i++)
            {
                cbSlaveSelect.Items.Add(String.Format("Slave {0}", i));
            }
            cbSlaveSelect.SelectedIndex = 0;
            cbSlaveSelect.Enabled = bMonitoringEnabled;
            this.Controls.Add(cbSlaveSelect);

            btnStart = new Button();
            btnStart.Location = new Point(1200, 110);
            btnStart.Size = new Size(150, 30);
            btnStart.Font = new Font(btnStart.Font.Name, btnStart.Font.Size + 2.0F, btnStart.Font.Style, btnStart.Font.Unit);
            btnStart.Text = "Start";
            btnStart.BackColor = Color.Khaki;
            btnStart.Click += new EventHandler(this.btnStart_Click);
            btnStart.Enabled = bMonitoringEnabled;
            this.Controls.Add(btnStart);

            btnLogging = new Button();
            btnLogging.Location = new Point(1200, 160);
            btnLogging.Size = new Size(150, 30);
            btnLogging.Font = new Font(btnLogging.Font.Name, btnLogging.Font.Size + 2.0F, btnLogging.Font.Style, btnLogging.Font.Unit);
            btnLogging.Text = "Enable Logging";
            btnLogging.BackColor = Color.Khaki;
            btnLogging.Click += new EventHandler(this.btnLogging_Click);
            btnLogging.Enabled = bMonitoringEnabled;
            this.Controls.Add(btnLogging);

            lbLogTimer = new Label();
            lbLogTimer.Text = string.Format("Log Time: NA");
            lbLogTimer.Location = new Point(1200, 210);
            lbLogTimer.Font = new Font(lbLogTimer.Font.Name, lbLogTimer.Font.Size + 2.0F, lbLogTimer.Font.Style, lbLogTimer.Font.Unit);
            lbLogTimer.AutoSize = true;
            this.Controls.Add(lbLogTimer);

            btnLogBrowse = new Button();
            btnLogBrowse.Location = new Point(1200, 260);
            btnLogBrowse.Size = new Size(150, 30);
            btnLogBrowse.Font = new Font(btnLogBrowse.Font.Name, btnLogBrowse.Font.Size + 2.0F, btnLogBrowse.Font.Style, btnLogBrowse.Font.Unit);
            btnLogBrowse.Text = "Browse Logs";
            btnLogBrowse.BackColor = Color.Khaki;
            btnLogBrowse.Click += new EventHandler(this.btnLogBrowse_Click);
            btnLogBrowse.Enabled = bMonitoringEnabled;
            this.Controls.Add(btnLogBrowse);

            lbCpNotes = new Label();
            lbCpNotes.Location = new Point(1200, 310);
            lbCpNotes.Size = new Size(300, 140);
            lbCpNotes.Font = new Font(lbCpNotes.Font.Name, lbCpNotes.Font.Size + 4.0F, lbCpNotes.Font.Style, lbCpNotes.Font.Unit);
            lbCpNotes.Text = "Notes:\n\nShort to VDD: CP < CP Min\n\nShort to GND: CP > CP Max\n\nCP Min must be > 5uF";
            this.Controls.Add(lbCpNotes);

            btnClear = new Button();
            btnClear.Location = new Point(1200, 470);
            btnClear.Size = new Size(150, 30);
            btnClear.Font = new Font(btnClear.Font.Name, btnClear.Font.Size + 2.0F, btnClear.Font.Style, btnClear.Font.Unit);
            btnClear.Text = "Clear Data";
            btnClear.BackColor = Color.Khaki;
            btnClear.Click += new EventHandler(this.btnClear_Click);
            btnClear.Enabled = bMonitoringEnabled;
            this.Controls.Add(btnClear);

            btnCpCalGuide = new Button();
            btnCpCalGuide.Location = new Point(1200, 530);
            btnCpCalGuide.Size = new Size(150, 60);
            btnCpCalGuide.Font = new Font(btnCpCalGuide.Font.Name, btnCpCalGuide.Font.Size + 2.0F, btnCpCalGuide.Font.Style, btnCpCalGuide.Font.Unit);
            btnCpCalGuide.Text = "CP Calculation\nGuide";
            btnCpCalGuide.BackColor = Color.Khaki;
            btnCpCalGuide.Click += new EventHandler(this.btnCpCalGuide_Click);
            this.Controls.Add(btnCpCalGuide);

            logStopWatch = new Stopwatch();
            logStopWatch.Reset();

            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = false;

            bViewInitComplete = true;
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Raised: {0}", e.SignalTime);
            TimeSpan ts = logStopWatch.Elapsed;
            string elapsedTime = String.Format("Log Time: {0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

            UpdateLogTime(elapsedTime);
        }

        private void UpdateLogTime(string logtime)
        {
            if (this.lbLogTimer.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdateLogTime);
                this.lbLogTimer.Invoke(d, new object[] { logtime });
            }
            else
            {
                this.lbLogTimer.Text = logtime;
            }
        }

        private void btnStart_Click(Object sender, EventArgs e)
        {
            if (bMonitoringStarted == false)
            {
                bMonitoringStarted = true;
                btnLogging.Enabled = false;
                btnStart.Text = "Stop";
                btnStart.BackColor = Color.LightGreen;
                cp_monitoring[cbSlaveSelect.SelectedIndex].start_logging(true);
                logStopWatch.Restart();
                timer.Enabled = true;
                cbSlaveSelect.Enabled = false;
                btnClear.Enabled = false;
                cbCPReuestType.Enabled = false;

                if (cbCPReuestType.SelectedIndex == 0) // use API17 CP publish command
                {
                    CpMonitoringModel model = (CpMonitoringModel)control.GetModel("CP Monitoring");
                    model.SetCPReadingMode(true);
                    control.SendStartStopCPMonitoring(true, (byte)cbSlaveSelect.SelectedIndex);
                }
                else
                {
                    CpMonitoringModel model = (CpMonitoringModel)control.GetModel("CP Monitoring");
                    model.SetCPReadingMode(false);
                    start_cp_monitoring_reader((byte)cbSlaveSelect.SelectedIndex);
                }
            }
            else
            {
                if (cbCPReuestType.SelectedIndex == 0) // use API17 CP publish command
                {
                    control.SendStartStopCPMonitoring(false, (byte)cbSlaveSelect.SelectedIndex);
                }
                else
                {
                    stop_cp_monitoring_reader((byte)cbSlaveSelect.SelectedIndex);
                }

                bMonitoringStarted = false;
                btnLogging.Enabled = true;
                btnStart.Text = "Start";
                btnStart.BackColor = Color.Khaki;
                cp_monitoring[cbSlaveSelect.SelectedIndex].start_logging(false);
                logStopWatch.Stop();
                timer.Enabled = false;
                cbSlaveSelect.Enabled = true;
                btnClear.Enabled = true;
                cbCPReuestType.Enabled = api17_major >= 14 ? true : false;
            }
        }

        private void btnLogging_Click(Object sender, EventArgs e)
        {
            if (btnLogging.Text == "Enable Logging")
            {
                btnLogging.Text = "Disable Logging";
                btnLogging.BackColor = Color.LightGreen;
                for (int i = 0; i < this.iNumOfSlave; i++)
                {
                    cp_monitoring[i].enable_logging(true);
                }
            }
            else
            {
                btnLogging.Text = "Enable Logging";
                btnLogging.BackColor = Color.Khaki;
                for (int i = 0; i < this.iNumOfSlave; i++)
                {
                    cp_monitoring[i].enable_logging(false);
                }
            }
        }

        private void btnLogBrowse_Click(Object sender, EventArgs e)
        {
            string LogFolderPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = LogFolderPath;
            openFileDialog1.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm;*.csv";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result != DialogResult.OK)
            {
                return;
            }
            string file = openFileDialog1.FileName;
            try
            {
                FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read);
                stream.Close();
            }
            catch
            {
                MessageBox.Show("File is being used by Touch Commander. Stop logging before accessing the file.");

                return;
            }
            string text = File.ReadAllText(file);
            if (result == DialogResult.OK && file != null) // Test result.
            {
                System.Diagnostics.Process.Start(file);
            }
        }

        private void btnClear_Click(Object sender, EventArgs e)
        {
            CpMonitoringModel model = (CpMonitoringModel)control.GetModel("CP Monitoring");
            model.ClearData((byte)cbSlaveSelect.SelectedIndex);
            cp_monitoring[cbSlaveSelect.SelectedIndex].clear_data();
        }

        private void btnCpCalGuide_Click(Object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://drive.google.com/drive/folders/1IIsTKz14RBROyev6Z_BR6bzKo0oSB_wV?usp=sharing");
            }
            catch
            {
                MessageBox.Show(this, "Unable to open the link");
            }
        }

        private void start_cp_monitoring_reader(byte slaveid)
        {
            if (cp_cmd_sender_dict.ContainsKey(slaveid) == false)
            {
                CpMeasurementBackgroundWorker cp_cmd_sender = new CpMeasurementBackgroundWorker(slaveid, 1000);
                cp_cmd_sender.DoWork += cp_monitoring_reader_DoWork;
                cp_cmd_sender.WorkerSupportsCancellation = true;

                cp_cmd_sender_dict.Add(slaveid, cp_cmd_sender);

                if (cp_cmd_sender.IsBusy != true)
                {
                    cp_cmd_sender.running = true;
                    cp_cmd_sender.RunWorkerAsync();
                }
            }
        }

        private void stop_cp_monitoring_reader(byte slaveid)
        {
            if (cp_cmd_sender_dict.ContainsKey(slaveid) == true)
            {
                cp_cmd_sender_dict[slaveid].running = false;
                cp_cmd_sender_dict[slaveid].CancelAsync();
                cp_cmd_sender_dict[slaveid].Dispose();
                cp_cmd_sender_dict.Remove(slaveid);
            }
        }

        private void cp_monitoring_reader_DoWork(object sender, DoWorkEventArgs e)
        {
            CpMeasurementBackgroundWorker worker = (CpMeasurementBackgroundWorker)sender;
            ushort offset = 0;
            byte bytes_to_read = 0;

            if (mutualType[worker.slave_id] == false)
            {
                offset = 0x01C4;
                bytes_to_read = 24;
            }
            else
            {
                offset = 0x0350;
                bytes_to_read = 20;
            }

            while (worker.running == true)
            {
                control.SendI2CReadCommand("CP Monitoring", worker.slave_id, "Secondary", offset, bytes_to_read);
                Thread.Sleep(worker.interval);
            }
        }

        public override void updateView(Object data)
        {
            if (data == null) return;

            CpMonitoringModel model = (CpMonitoringModel)data;
            byte slave_id = model.GetCurrentSlave();
            byte[] cp_data = model.GetCpData(slave_id);
            cp_monitoring[slave_id].set_monitoring_data(cp_data);
        }

        public override void updateView(string section, Object data)
        {
            if (data == null) return;

            CpMonitoringModel model = (CpMonitoringModel)data;
        }
    }

    public class cypress_cp_monitoring_widgets : GroupBox
    {
        Label[] cpValLabel;
        Label[] cpValues;

        Label[] ioStatLabel;
        Label[] io2ioShortLabel;
        Label[] short2GndLabel;
        Label[] short2VddLabel;

        Label lbCpNotSupported;

        int item_cnt = 0;
        int slaveID = 0;
        bool mutualType = false;
        StreamWriter logstream;
        bool log_enabled = false;
        bool monitoring_started = false;

        public cypress_cp_monitoring_widgets(byte slaveID, bool mutualType, bool enabled)
        {
            this.slaveID = slaveID;
            this.mutualType = mutualType;
            this.Name = "cpMonitoringGroupBox";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Location = new Point(10, 10 + (slaveID * 410));
            this.AutoSize = true;

            if (enabled == false)
            {
                this.Text = string.Format("Slave ID: {0}", slaveID);
                lbCpNotSupported = new Label();
                lbCpNotSupported.Text = "\nCP Monitoring is not supported for this Slave device\n\nIt is supported for PSoC4000S Self and Mutual, Class A devices only.        ";
                lbCpNotSupported.Location = new Point(30, 30);
                lbCpNotSupported.AutoSize = true;
                this.Controls.Add(lbCpNotSupported);
                return;
            }

            if (mutualType == true) // Mutual
            {
                item_cnt = 11;
                this.Text = string.Format("Slave ID: {0} - Mutual CP Monitoring", slaveID);

                cpValLabel = new Label[item_cnt];
                cpValLabel[0] = new Label();
                cpValLabel[0].Text = "TX/RX CP List";
                cpValLabel[0].Location = new Point(30, 30);
                cpValLabel[0].AutoSize = true;
                this.Controls.Add(cpValLabel[0]);

                ioStatLabel = new Label[item_cnt];
                ioStatLabel[0] = new Label();
                ioStatLabel[0].Text = "IO Status List";
                ioStatLabel[0].Location = new Point(400, 30);
                ioStatLabel[0].AutoSize = true;
                this.Controls.Add(ioStatLabel[0]);

                for (int i = 1; i < 7; i++)
                {
                    cpValLabel[i] = new Label();
                    cpValLabel[i].Text = string.Format("TX {0}", i - 1); ;
                    cpValLabel[i].Location = new Point(60, 30 + (i * 30));
                    cpValLabel[i].AutoSize = true;
                    this.Controls.Add(cpValLabel[i]);

                    ioStatLabel[i] = new Label();
                    ioStatLabel[i].Text = string.Format("TX {0} IO", i - 1);
                    ioStatLabel[i].Location = new Point(420, 30 + (i * 30));
                    ioStatLabel[i].AutoSize = true;
                    this.Controls.Add(ioStatLabel[i]);
                }
                for (int i = 7; i < item_cnt; i++)
                {
                    cpValLabel[i] = new Label();
                    cpValLabel[i].Text = string.Format("RX {0}", i - 7);
                    cpValLabel[i].Location = new Point(60, 30 + (i * 30));
                    cpValLabel[i].AutoSize = true;
                    this.Controls.Add(cpValLabel[i]);

                    ioStatLabel[i] = new Label();
                    ioStatLabel[i].Text = string.Format("RX {0} IO", i - 7);
                    ioStatLabel[i].Location = new Point(420, 30 + (i * 30));
                    ioStatLabel[i].AutoSize = true;
                    this.Controls.Add(ioStatLabel[i]);
                }
            }
            else
            {
                item_cnt = 12;
                this.Text = string.Format("Slave ID: {0} - Self CP Monitoring", slaveID);

                cpValLabel = new Label[item_cnt];
                cpValLabel[0] = new Label();
                cpValLabel[0].Text = "Sensor CP List";
                cpValLabel[0].Location = new Point(30, 30);
                cpValLabel[0].AutoSize = true;
                this.Controls.Add(cpValLabel[0]);

                ioStatLabel = new Label[item_cnt];
                ioStatLabel[0] = new Label();
                ioStatLabel[0].Text = "Sensor IO Status List";
                ioStatLabel[0].Location = new Point(400, 30);
                ioStatLabel[0].AutoSize = true;
                this.Controls.Add(ioStatLabel[0]);

                for (int i = 1; i < item_cnt; i++)
                {
                    cpValLabel[i] = new Label();
                    cpValLabel[i].Text = string.Format("Sensor {0} CP", i - 1);
                    cpValLabel[i].Location = new Point(35, 30 + (i * 30));
                    cpValLabel[0].AutoSize = true;
                    this.Controls.Add(cpValLabel[i]);

                    ioStatLabel[i] = new Label();
                    ioStatLabel[i].Text = string.Format("Sensor {0} IO", i - 1);
                    ioStatLabel[i].Location = new Point(420, 30 + (i * 30));
                    ioStatLabel[i].AutoSize = true;
                    this.Controls.Add(ioStatLabel[i]);
                }
            }

            cpValues = new Label[item_cnt];
            cpValues[0] = new Label();
            cpValues[0].Text = "CP Values";
            cpValues[0].Location = new Point(210, 30);
            cpValues[0].AutoSize = true;
            this.Controls.Add(cpValues[0]);

            io2ioShortLabel = new Label[item_cnt];
            io2ioShortLabel[0] = new Label();
            io2ioShortLabel[0].Text = "IO to IO Short";
            io2ioShortLabel[0].Location = new Point(600, 30);
            io2ioShortLabel[0].AutoSize = true;
            this.Controls.Add(io2ioShortLabel[0]);

            short2GndLabel = new Label[item_cnt];
            short2GndLabel[0] = new Label();
            short2GndLabel[0].Text = "Sensort to Gnd Short";
            short2GndLabel[0].Location = new Point(750, 30);
            short2GndLabel[0].AutoSize = true;
            this.Controls.Add(short2GndLabel[0]);

            short2VddLabel = new Label[item_cnt];
            short2VddLabel[0] = new Label();
            short2VddLabel[0].Text = "Sensor to Vdd Short   ";
            short2VddLabel[0].Location = new Point(950, 30);
            short2VddLabel[0].AutoSize = true;
            this.Controls.Add(short2VddLabel[0]);

            for (int i = 1; i < item_cnt; i++)
            {
                cpValues[i] = new Label();
                cpValues[i].Text = "0";
                cpValues[i].Location = new Point(230, 30 + (i * 30));
                cpValues[i].AutoSize = true;
                this.Controls.Add(cpValues[i]);

                io2ioShortLabel[i] = new Label();
                io2ioShortLabel[i].Text = "0";
                io2ioShortLabel[i].Location = new Point(640, 30 + (i * 30));
                io2ioShortLabel[i].AutoSize = true;
                this.Controls.Add(io2ioShortLabel[i]);

                short2GndLabel[i] = new Label();
                short2GndLabel[i].Text = "0";
                short2GndLabel[i].Location = new Point(810, 30 + (i * 30));
                short2GndLabel[i].AutoSize = true;
                this.Controls.Add(short2GndLabel[i]);

                short2VddLabel[i] = new Label();
                short2VddLabel[i].Text = "0";
                short2VddLabel[i].Location = new Point(1010, 30 + (i * 30));
                short2VddLabel[i].AutoSize = true;
                this.Controls.Add(short2VddLabel[i]);
            }
        }

        public void clear_data()
        {
            for (int i = 1; i < this.item_cnt; i++)
            {
                cpValues[i].Text = "0";
                io2ioShortLabel[i].Text = "0";
                short2GndLabel[i].Text = "0";
                short2VddLabel[i].Text = "0";
            }
        }

        public void set_monitoring_data(byte[] data)
        {
            int expected_len = (this.item_cnt - 1) * 2;
            if (data.Length == expected_len)
            {
                string logstr = string.Format("{0:yyyy-MM-dd_hh-mm-ss-fff}", DateTime.Now);
                for (int i = 1; i < this.item_cnt; i++)
                {
                    cpValues[i].Text = string.Format("{0}", data[i - 1]);
                    int iostat = data[i + (this.item_cnt - 1) - 1];
                    io2ioShortLabel[i].Text = string.Format("{0}", iostat & 0x04);
                    short2GndLabel[i].Text = string.Format("{0}", iostat & 0x02);
                    short2VddLabel[i].Text = string.Format("{0}", iostat & 0x01);
                    logstr += "," + cpValues[i].Text + "," + string.Format("{0}", iostat);
                }
                log_row(logstr);
            }
            else
            {
                //ERROR: Unexpected Payload length received.
                Console.WriteLine("Error: Unexpected Payload length received, Ignoring the payload\n");
            }
        }

        private void log_row(string logstr)
        {
            if (log_enabled == true && monitoring_started == true)
            {
                this.logstream.Write("{0}\r", logstr);
                this.logstream.Flush();
            }
        }

        public void start_logging(bool start)
        {
            if (log_enabled == true)
            {
                if (start == true)
                {
                    string logheader = "Timestamp";
                    string TouchLogFolderPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string FileName = string.Format("CypressCPMonitoringLog_Slave{0}-{1:yyyy-MM-dd_hh-mm-ss-fff}.csv", this.slaveID, DateTime.Now);
                    this.logstream = File.AppendText(TouchLogFolderPath + "\\" + FileName);

                    if (this.mutualType == true)
                    {
                        logheader += ",TX0_Cp,TX0_IO,TX1_Cp,TX1_IO,TX2_Cp,TX2_IO,TX3_Cp,TX3_IO,TX4_Cp,TX4_IO,TX5_Cp,TX5_IO,RX0_Cp,RX0_IO,RX1_Cp,RX1_IO,RX2_Cp,RX2_IO,RX3_Cp,RX3_IO\n";
                    }
                    else
                    {
                        logheader += ",btn0_Cp,btn0_IO,btn1_Cp,btn1_IO,btn2_Cp,btn2_IO,btn3_Cp,btn3_IO,btn4_Cp,btn4_IO,btn5_Cp,btn5_IO,btn6_Cp,btn6_IO,btn7_Cp,btn7_IO,btn8_Cp,btn8_IO,btn9_Cp,btn9_IO,btn10_Cp,btn10_IO\n";
                    }

                    this.logstream.Write(logheader);
                    this.logstream.Flush();
                    monitoring_started = true;
                }
                else
                {
                    monitoring_started = false;
                    this.logstream.Close();
                }
            }
        }

        public void enable_logging(bool enable)
        {
            log_enabled = enable;
        }
    }
}
