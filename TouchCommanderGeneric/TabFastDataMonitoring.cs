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
using System.Windows.Forms;
using WideBoxLib;

namespace TouchCommanderGenericNamespace
{
    public class RawRefDeltaMonitoringData
    {
        public int raw_count;
        public int ref_count;
        public int threshold;
        public int delta;
        public int max_delta;
        public bool key_status;
        public byte key_id;
        public byte slave_id;
        public byte actual_key_id;
        public int heartbeat_cnt;
        public int keypress_cnt;

        public RawRefDeltaMonitoringData()
        {
            raw_count = 0;
            ref_count = 0;
            threshold = 0;
            delta = 0;
            max_delta = 0;
            key_status = false;
            key_id = 0;
            slave_id = 0;
            actual_key_id = 0;
            heartbeat_cnt = 0;
            keypress_cnt = 0;
        }
    }

    public struct heartbeat_data
    {
        public int hbcounter;
        public int keycounter;
    }

    public class FastDataMonitoringModel : TouchCommanderModelBase
    {
        heartbeat_data[] hb_data;
        byte iNumOfSlaves;
        private bool[] mutualType; /*Self Cap = False, Mutual Cap = True*/
        RawRefDeltaMonitoringData[] monitoring_data;

        public FastDataMonitoringModel(TouchCommanderController c) : base(c)
        {
            iNumOfSlaves = control.GetNumberOfSlaves();
            hb_data = new heartbeat_data[iNumOfSlaves];
            mutualType = new bool[this.iNumOfSlaves];

            for (int i = 0; i < iNumOfSlaves; i++)
            {
                mutualType[i] = control.GetSensingType((byte)i) == "Mutual" ? true : false;
            }
        }

        public override void updateModel(Object d)
        {
            byte[] Payload = (byte[])d;
            int payload_len = Payload.Length;
            int bytes_per_key = 9;
            int key_count = payload_len / bytes_per_key;

            monitoring_data = null;
            monitoring_data = new RawRefDeltaMonitoringData[key_count];

            for (int i = 0; i < key_count; i++)
            {
                monitoring_data[i] = new RawRefDeltaMonitoringData();

                monitoring_data[i].key_id = (byte)(0x3F & Payload[i * bytes_per_key]);
                monitoring_data[i].slave_id = (byte)((0x40 & Payload[i * bytes_per_key]) >> 6);
                byte stat = (byte)((0x80 & Payload[i * bytes_per_key]) >> 7);
                monitoring_data[i].key_status = stat == 1 ? true : false;

                monitoring_data[i].raw_count = (Payload[1 + (i * bytes_per_key)] << 8) + Payload[2 + (i * bytes_per_key)];
                monitoring_data[i].ref_count = (Payload[3 + (i * bytes_per_key)] << 8) + Payload[4 + (i * bytes_per_key)];

                monitoring_data[i].threshold = 0;

                monitoring_data[i].delta = (Payload[5 + (i * bytes_per_key)] << 8) + Payload[6 + (i * bytes_per_key)];
                monitoring_data[i].max_delta = (Payload[7 + (i * bytes_per_key)] << 8) + Payload[8 + (i * bytes_per_key)];

                monitoring_data[i].actual_key_id = control.GetActualKeyNumber(monitoring_data[i].key_id);
            }

        }

        public override void updateModel(Object d, string section)
        {
            byte[] data = (byte[])d;

            if (data.Length == (2 + (4 * iNumOfSlaves)) && section == "Heart Beat")
            {
                for (int i = 0; i < iNumOfSlaves; i++)
                {
                    hb_data[i].hbcounter = (data[2 + (4 * i) + 1] << 8) | data[2 + (4 * i)];
                    hb_data[i].keycounter = (data[4 + (4 * i) + 1] << 8) | data[4 + (4 * i)];
                }
            }
        }

        public heartbeat_data get_hb_data(byte slave_id)
        {
            return hb_data[slave_id];
        }

        public RawRefDeltaMonitoringData[] get_monitoring_data()
        {
            return monitoring_data;
        }
    }

    public class FastDataMonitoringTabPage : TouchCommanderTabBase
    {
        bool bViewInitComplete;
        bool bLoggingEnabled;
        bool bMonitoringStarted;
        byte iNumOfSlave;
        bool wait_for_valid_data;
        int total_num_keys;
        int log_delay;

        private byte api17_major;
        private byte api17_minor;
        private byte api17_validation;

        cypress_raw_ref_delta_monitoring_widgets[] raw_ref_delta_group;

        private Button btnStart;
        private Button btnLogging;
        private Button btnLogBrowse;
        private Button btnClear;
        private Label lbLogTimer;
        private Label lbNotSupported;

        private delegate void SafeCallDelegate(string text);
        Stopwatch LogstopWatch;
        System.Timers.Timer timer;

        StreamWriter logstream;

        public FastDataMonitoringTabPage(TouchCommanderController c) : base(c)
        {
            this.Text = "Fast Raw/Ref/Delta Monitoring";
            this.bViewInitComplete = false;
            this.bLoggingEnabled = false;
            this.bMonitoringStarted = false;

            this.iNumOfSlave = control.GetNumberOfSlaves();

            this.total_num_keys = 0;
            for (int i = 0; i < this.iNumOfSlave; i++)
            {
                total_num_keys += control.GetKeysPerSlave((byte)i);
            }
            

            control.GetAPI17GDMVersion(out api17_major, out api17_minor, out api17_validation);

            initView();
        }

        private void initView()
        {
            btnStart = new Button();
            btnStart.Location = new Point(1365, 10);
            btnStart.Size = new Size(150, 30);
            btnStart.Font = new Font(btnStart.Font.Name, btnStart.Font.Size + 2.0F, btnStart.Font.Style, btnStart.Font.Unit);
            btnStart.Text = "Start";
            btnStart.BackColor = Color.Khaki;
            btnStart.Click += new EventHandler(this.btnStart_Click);
            this.Controls.Add(btnStart);

            btnLogging = new Button();
            btnLogging.Location = new Point(1365, 60);
            btnLogging.Size = new Size(150, 30);
            btnLogging.Font = new Font(btnLogging.Font.Name, btnLogging.Font.Size + 2.0F, btnLogging.Font.Style, btnLogging.Font.Unit);
            btnLogging.Text = "Enable Logging";
            btnLogging.BackColor = Color.Khaki;
            btnLogging.Click += new EventHandler(this.btnLogging_Click);
            this.Controls.Add(btnLogging);

            lbLogTimer = new Label();
            lbLogTimer.Text = string.Format("LogTime: NA");
            lbLogTimer.Location = new Point(1365, 118);
            lbLogTimer.Font = new Font(lbLogTimer.Font.Name, lbLogTimer.Font.Size + 2.0F, lbLogTimer.Font.Style, lbLogTimer.Font.Unit);
            lbLogTimer.AutoSize = true;
            this.Controls.Add(lbLogTimer);

            LogstopWatch = new Stopwatch();
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = false;

            btnLogBrowse = new Button();
            btnLogBrowse.Location = new Point(1365, 170);
            btnLogBrowse.Size = new Size(150, 30);
            btnLogBrowse.Font = new Font(btnLogBrowse.Font.Name, btnLogBrowse.Font.Size + 2.0F, btnLogBrowse.Font.Style, btnLogBrowse.Font.Unit);
            btnLogBrowse.Text = "Browse Logs";
            btnLogBrowse.BackColor = Color.Khaki;
            btnLogBrowse.Click += new EventHandler(this.btnLogBrowse_Click);
            this.Controls.Add(btnLogBrowse);

            btnClear = new Button();
            btnClear.Location = new Point(1365, 230);
            btnClear.Size = new Size(150, 30);
            btnClear.Font = new Font(btnClear.Font.Name, btnClear.Font.Size + 2.0F, btnClear.Font.Style, btnClear.Font.Unit);
            btnClear.Text = "Clear Data";
            btnClear.BackColor = Color.Khaki;
            btnClear.Click += new EventHandler(this.btnClear_Click);
            this.Controls.Add(btnClear);

            if (api17_major >= 14)
            {
                raw_ref_delta_group = new cypress_raw_ref_delta_monitoring_widgets[iNumOfSlave];
                byte total_num_keys = 0;
                byte start_key_offset = 0;
                for (int i = 0; i < iNumOfSlave; i++)
                {
                    byte num_keys = control.GetKeysPerSlave((byte)i);
                    raw_ref_delta_group[i] = new cypress_raw_ref_delta_monitoring_widgets((byte)i, num_keys, start_key_offset);
                    raw_ref_delta_group[i].Visible = true;
                    this.Controls.Add(raw_ref_delta_group[i]);
                    total_num_keys += num_keys;
                    start_key_offset += (byte)total_num_keys;
                }
                bViewInitComplete = true;
            }
            else
            {
                btnStart.Enabled = false;
                btnLogging.Enabled = false;
                btnLogBrowse.Enabled = false;
                btnClear.Enabled = false;

                lbNotSupported = new Label();
                lbNotSupported.Text = string.Format("Fast RAW/REF/DElta monitoring requires API17 Modules version 14.0.0 and above.");
                lbNotSupported.Location = new Point(20, 20);
                lbNotSupported.Font = new Font(lbNotSupported.Font.Name, lbNotSupported.Font.Size + 4.0F, lbNotSupported.Font.Style, lbNotSupported.Font.Unit);
                lbNotSupported.AutoSize = true;
                this.Controls.Add(lbNotSupported);
            }
        }

        private void btnStart_Click(Object sender, EventArgs e)
        {
            if (bMonitoringStarted == false)
            {
                bMonitoringStarted = true;
                btnLogging.Enabled = false;
                btnClear.Enabled = false;
                btnStart.Text = "Stop";
                btnStart.BackColor = Color.LightGreen;
                control.SendStartStopRawRefDeltaPMonitoring(true);
                if (bLoggingEnabled == true)
                {
                    start_logging();
                }
                else
                {
                    lbLogTimer.Text = string.Format("LogTime: NA");
                }
            }
            else
            {
                bMonitoringStarted = false;
                btnLogging.Enabled = true;
                btnClear.Enabled = true;
                btnStart.Text = "Start";
                btnStart.BackColor = Color.Khaki;
                control.SendStartStopRawRefDeltaPMonitoring(false);
                if (bLoggingEnabled == true)
                {
                    stop_logging();
                }
            }
        }

        private void btnLogging_Click(Object sender, EventArgs e)
        {
            if (btnLogging.Text == "Enable Logging")
            {
                btnLogging.Text = "Disable Logging";
                btnLogging.BackColor = Color.LightGreen;
                bLoggingEnabled = true;
            }
            else
            {
                btnLogging.Text = "Enable Logging";
                btnLogging.BackColor = Color.Khaki;
                bLoggingEnabled = false;
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
            for (int i = 0; i < iNumOfSlave; i++)
            {
                raw_ref_delta_group[i].clear_data();
            }
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Raised: {0}", e.SignalTime);
            TimeSpan ts = LogstopWatch.Elapsed;
            string elapsedTime = String.Format("LogTime: {0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

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
                if (log_delay == 2) // wait for 2 seconds to start logging
                {
                    wait_for_valid_data = false;
                }
                else
                {
                    log_delay++;
                }
            }
        }
        
        private void start_logging()
        {
            string logheader = "TimeStamp";
            string TouchLogFolderPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string FileName = string.Format("CypressMonitoringLog-{0:yyyy-MM-dd_hh-mm-ss-fff}.csv", DateTime.Now);
            this.logstream = File.AppendText(TouchLogFolderPath + "\\" + FileName);

            for (int i = 0; i < this.total_num_keys; i++)
            {
                logheader += ",KeyNumber,SlaveId,ActualSensor-" + i + ",Raw,Ref,Delta,MaxDelta,KeyStatus";
            }

            logheader += "\n";
            this.logstream.Write(logheader);
            this.logstream.Flush();
            wait_for_valid_data = true;
            LogstopWatch.Reset();
            LogstopWatch.Start();
            timer.Enabled = true;
            log_delay = 0;
        }

        private void stop_logging()
        {
            this.logstream.Close();
            LogstopWatch.Stop();
            timer.Enabled = false;
        }

        private bool check_if_data_valid()
        {
            bool wait_for_valid_data = true;

            for (int i = 0; i < raw_ref_delta_group.Length; i++)
            {
                wait_for_valid_data = raw_ref_delta_group[i].can_start_logging() == true ? false : true;
                if (wait_for_valid_data == false)
                {
                    break;
                }
            }

            return wait_for_valid_data;
        }

        private void log_row()
        {
            /*
            if (wait_for_valid_data == true)
            {
                wait_for_valid_data = check_if_data_valid();
            }
            else
            */
            if (wait_for_valid_data == false)
            {
                string logstr = string.Format("{0:yyyy-MM-dd_hh-mm-ss-fff}", DateTime.Now);

                for (int i = 0; i < raw_ref_delta_group.Length; i++)
                {
                    logstr += raw_ref_delta_group[i].get_log_row();
                }

                this.logstream.Write("{0}\r", logstr);
                this.logstream.Flush();
            }
        }

        public override void updateView(Object data)
        {
            FastDataMonitoringModel model = (FastDataMonitoringModel)data;

            if (bViewInitComplete == false)
            {
                return;
            }

            if (data == null) return;

            RawRefDeltaMonitoringData[] get_monitoring_data = model.get_monitoring_data();

            for (int i = 0; i < get_monitoring_data.Length; i++)
            {
                raw_ref_delta_group[get_monitoring_data[i].slave_id].set_raw_ref_delta_status(get_monitoring_data[i].key_id, get_monitoring_data[i].raw_count,
                                                            get_monitoring_data[i].ref_count, get_monitoring_data[i].threshold, get_monitoring_data[i].delta,
                                                            get_monitoring_data[i].max_delta, get_monitoring_data[i].key_status, get_monitoring_data[i].actual_key_id);
                if (bLoggingEnabled == true && bMonitoringStarted == true)
                {
                    log_row();
                }
            }
        }

        public override void updateView(string section, Object data)
        {
            if (data == null) return;

            FastDataMonitoringModel model = (FastDataMonitoringModel)data;

            if (bViewInitComplete == false)
            {
                return;
            }
            if (section == "Heart Beat")
            {
                for (int i = 0; i < iNumOfSlave; i++)
                {
                    heartbeat_data hb_data = model.get_hb_data((byte)i);
                    raw_ref_delta_group[i].update_heartbeat_data((byte)i, hb_data.hbcounter, hb_data.keycounter);
                }
            }
        }
    }

    public class cypress_raw_ref_delta_monitoring_widgets : GroupBox
    {
        Label[] lbKeyId;
        Label[] lbPhysicalKeyId;
        Label[] lbRaw;
        Label[] lbRef;
        //Label[] lbThresh;
        Label[] lbDelta;
        Label[] lbMaxDelta;
        Label[] lbKeyStatus;
        Label lbHeartBeat;
        Label lbKeyPress;
        //byte[] actual_key_id;

        byte keys_per_slave;
        byte slaveID;
        byte start_key_offset;

        public cypress_raw_ref_delta_monitoring_widgets(byte slaveID, byte keys_per_slave, byte start_key_offset)
        {
            this.keys_per_slave = keys_per_slave;
            this.slaveID = slaveID;
            this.Name = "RawRefDeltaMonitoringGroupBox";
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Location = new Point(20 + (slaveID * 680), 10);
            this.AutoSize = true;
            //this.Size = new Size(650, 500);
            this.Text = string.Format("Slave {0}", slaveID);
            this.start_key_offset = start_key_offset;

            //actual_key_id = new byte[keys_per_slave];
            //Array.Clear(actual_key_id, 0, actual_key_id.Length);

            lbHeartBeat = new Label();
            lbHeartBeat.Text = string.Format("Heart Beat Count = 0");
            lbHeartBeat.Location = new Point(20, 30);
            lbHeartBeat.AutoSize = true;
            this.Controls.Add(lbHeartBeat);

            lbKeyPress = new Label();
            lbKeyPress.Text = string.Format("Slave {0} Key Press Count = 0", slaveID);
            lbKeyPress.Location = new Point(300, 30);
            lbKeyPress.AutoSize = true;
            this.Controls.Add(lbKeyPress);

            lbKeyId = new Label[keys_per_slave + 1];
            lbKeyId[0] = new Label();
            lbKeyId[0].Text = "KeyID";
            lbKeyId[0].Location = new Point(20, 65);
            lbKeyId[0].AutoSize = true;
            this.Controls.Add(lbKeyId[0]);

            lbPhysicalKeyId = new Label[keys_per_slave + 1];
            lbPhysicalKeyId[0] = new Label();
            lbPhysicalKeyId[0].Text = "Sensor ID";
            lbPhysicalKeyId[0].Location = new Point(100, 65);
            lbPhysicalKeyId[0].AutoSize = true;
            this.Controls.Add(lbPhysicalKeyId[0]);

            lbRaw = new Label[keys_per_slave + 1];
            lbRaw[0] = new Label();
            lbRaw[0].Text = "RAW Cnt";
            lbRaw[0].Location = new Point(190, 65); //100
            lbRaw[0].AutoSize = true;
            this.Controls.Add(lbRaw[0]);

            lbRef = new Label[keys_per_slave + 1];
            lbRef[0] = new Label();
            lbRef[0].Text = "REF Cnt";
            lbRef[0].Location = new Point(280, 65); //190
            lbRef[0].AutoSize = true;
            this.Controls.Add(lbRef[0]);
            /*
            lbThresh = new Label[keys_per_slave + 1];
            lbThresh[0] = new Label();
            lbThresh[0].Text = "Threshold";
            lbThresh[0].Location = new Point(280, 65);
            lbThresh[0].AutoSize = true;
            this.Controls.Add(lbThresh[0]);
            */
            lbDelta = new Label[keys_per_slave + 1];
            lbDelta[0] = new Label();
            lbDelta[0].Text = "Delta";
            lbDelta[0].Location = new Point(370, 65); //300
            lbDelta[0].AutoSize = true;
            this.Controls.Add(lbDelta[0]);

            lbMaxDelta = new Label[keys_per_slave + 1];
            lbMaxDelta[0] = new Label();
            lbMaxDelta[0].Text = "Max Delta";
            lbMaxDelta[0].Location = new Point(460, 65); //460
            lbMaxDelta[0].AutoSize = true;
            this.Controls.Add(lbMaxDelta[0]);

            lbKeyStatus = new Label[keys_per_slave + 1];
            lbKeyStatus[0] = new Label();
            lbKeyStatus[0].Text = "Key Status";
            lbKeyStatus[0].Location = new Point(560, 65); //560
            lbKeyStatus[0].AutoSize = true;
            this.Controls.Add(lbKeyStatus[0]);

            for (int i = 0; i < keys_per_slave; i++)
            {
                int indx = i + 1;

                lbKeyId[indx] = new Label();
                lbKeyId[indx].Text = "NA";
                lbKeyId[indx].Location = new Point(20, 65 + (indx * 29));
                lbKeyId[indx].Size = new Size(50, 30);
                lbKeyId[indx].TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(lbKeyId[indx]);

                lbPhysicalKeyId[indx] = new Label();
                lbPhysicalKeyId[indx].Text = "0";
                lbPhysicalKeyId[indx].Location = new Point(100, 65 + (indx * 29));
                lbPhysicalKeyId[indx].Size = new Size(50, 30);
                lbPhysicalKeyId[indx].TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(lbPhysicalKeyId[indx]);

                lbRaw[indx] = new Label();
                lbRaw[indx].Text = "0";
                lbRaw[indx].Location = new Point(190, 65 + (indx * 29)); //100
                lbRaw[indx].Size = new Size(60, 30);
                lbRaw[indx].TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(lbRaw[indx]);

                lbRef[indx] = new Label();
                lbRef[indx].Text = "0";
                lbRef[indx].Location = new Point(280, 65 + (indx * 29)); //190
                lbRef[indx].Size = new Size(60, 30);
                lbRef[indx].TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(lbRef[indx]);
/*
                lbThresh[indx] = new Label();
                lbThresh[indx].Text = "0";
                lbThresh[indx].Location = new Point(280, 65 + (indx * 29));
                lbThresh[indx].Size = new Size(80, 30);
                lbThresh[indx].TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(lbThresh[indx]);
*/
                lbDelta[indx] = new Label();
                lbDelta[indx].Text = "0";
                lbDelta[indx].Location = new Point(370, 65 + (indx * 29)); //380
                lbDelta[indx].Size = new Size(50, 30);
                lbDelta[indx].TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(lbDelta[indx]);

                lbMaxDelta[indx] = new Label();
                lbMaxDelta[indx].Text = "0";
                lbMaxDelta[indx].Location = new Point(460, 65 + (indx * 29)); //460
                lbMaxDelta[indx].Size = new Size(80, 30);
                lbMaxDelta[indx].TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(lbMaxDelta[indx]);

                lbKeyStatus[indx] = new Label();
                lbKeyStatus[indx].Text = "OFF";
                lbKeyStatus[indx].Location = new Point(570, 65 + (indx * 29)); //570
                lbKeyStatus[indx].Size = new Size(60, 30);
                lbKeyStatus[indx].TextAlign = ContentAlignment.MiddleCenter;
                lbKeyStatus[indx].BackColor = Color.Transparent;
                this.Controls.Add(lbKeyStatus[indx]);
            }
        }

        public void set_raw_ref_delta_status(int key_id, int raw_count, int ref_count, int threshold, int delta, int max_delta, bool key_status, byte actual_keyid)
        {
            int indx = (key_id - start_key_offset) + 1;
            lbKeyId[indx].Text = string.Format("{0}", key_id);
            lbRaw[indx].Text = string.Format("{0}", raw_count);
            lbRef[indx].Text = string.Format("{0}", ref_count);
            //lbThresh[indx].Text = string.Format("{0}", threshold);
            lbDelta[indx].Text = string.Format("{0}", delta);
            lbMaxDelta[indx].Text = string.Format("{0}", max_delta);

            if (key_status == true)
            {
                lbKeyStatus[indx].Text = "ON";
                lbKeyStatus[indx].BackColor = Color.GreenYellow;
            }
            else
            {
                lbKeyStatus[indx].Text = "OFF";
                lbKeyStatus[indx].BackColor = Color.Transparent;
            }

            lbPhysicalKeyId[indx].Text = string.Format("{0}", actual_keyid);
            //actual_key_id[key_id - start_key_offset] = actual_keyid;
        }

        public void clear_data()
        {
            for (int i = 0; i < this.keys_per_slave; i++)
            {
                lbKeyId[i + 1].Text = "NA";
                lbPhysicalKeyId[i + 1].Text = "0";
                lbRaw[i + 1].Text = "0";
                lbRef[i + 1].Text = "0";
                //lbThresh[i + 1].Text = "0";
                lbDelta[i + 1].Text = "0";
                lbMaxDelta[i + 1].Text = "0";
                lbKeyStatus[i + 1].Text = "OFF";
                lbKeyStatus[i + 1].BackColor = Color.Transparent;
                //actual_key_id[i] = 0;
            }
            lbHeartBeat.Text = string.Format("Heart Beat Count = 0");
            lbKeyPress.Text = string.Format("Slave {0} Key Press Count = 0", slaveID);
        }

        public bool can_start_logging()
        {
            bool can_start_logging = true;

            /*
            for (int i = 0; i < this.keys_per_slave; i++)
            {
                if (String.Compare(lbKeyId[i + 1].Text, "NA") == 0)
                {
                    can_start_logging = false;
                    break;
                }
            } */

            if (String.Compare(lbKeyId[keys_per_slave - 1].Text, "NA") == 0)
            {
                can_start_logging = false;
            }

            return can_start_logging;
        }

        public string get_log_row()
        {
            string log_str = "";

            for (int i = 0; i < this.keys_per_slave; i++)
            {
                //",KeyNumber,SlaveId,Raw,Ref,//Threshold,MaxDelta,KeyStatus";
                log_str += "," + lbKeyId[i + 1].Text;
                log_str += "," + slaveID;
                log_str += "," + lbPhysicalKeyId[i + 1].Text;
                log_str += "," + lbRaw[i + 1].Text;
                log_str += "," + lbRef[i + 1].Text;
                //log_str += "," + lbThresh[i + 1].Text;
                log_str += "," + lbDelta[i + 1].Text;
                log_str += "," + lbMaxDelta[i + 1].Text;
                log_str += "," + lbKeyStatus[i + 1].Text;
            }

            return log_str;
        }

        public byte get_key_offset()
        {
            return this.start_key_offset;
        }

        public void update_heartbeat_data(byte slaveID, int hb_count, int key_count)
        {
            lbHeartBeat.Text = string.Format("Heart Beat Count = {0}", hb_count);
            lbKeyPress.Text = string.Format("Slave {0} Key Press Count = {1}", slaveID, key_count);
        }
    }
}
