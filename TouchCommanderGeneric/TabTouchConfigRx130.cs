using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WideBoxLib;

namespace TouchCommanderGenericNamespace
{
    public struct KeyDataRow
    {
        public byte key_id;
        public bool updated;

        public CheckBox ckEnable;
        public Label lbSensorID;
        public Label lbActSensorID;
        public ComboBox cbDriveFreq;
        public Label lbSDPAVal;
        public Label lbSSDIVVal;
        public NumericUpDown numSNUMVal;
        public NumericUpDown numSnsOffsetSet;
        public Label lbSnsOffsetGetVal;
        public Label lbPrmCntVal;
        public Label lbSecCntVal;
        public Label lbRawCntVal;
        public Label lbSnsCntVal;
        public Label lbDeltaVal;
        public NumericUpDown numFingerThreshold;
        public NumericUpDown numHysteresis;
        public NumericUpDown numBtnFilter;
    }

    public class ButtonConfigGroup : GroupBox
    {
        Button btnEnable;
        Label lbSensor;
        Label lbActSensor;
        Label lbDriveFreq;
        Label lbSDPA;
        Label lbSSDIV;
        Label lbSNUM;
        Label lbSnsOffsetSet;
        Label lbSnsOffsetGet;
        Label lbPrmCnt;
        Label lbSecCnt;
        Label lbRawCnt;
        Label lbSnsCnt;
        Label lbDelta;
        Label lbFingerThreshold;
        Label lbHysteresis;
        Label lbBtnFilter;

        private List<string> lstDriveFreq = new List<string> { "0.5 MHz", "1 MHz", "2 MHz", "3 MHz", "4 MHz" };
        public KeyDataRow[] keyData;

        private BackgroundWorker taskTuning;
        private int slave_id;
        private TouchConfigTabPage cfgView;

        private int tuning_keyid;

        public ButtonConfigGroup(int slave_id, TouchConfigTabPage view)
        {
            this.slave_id = slave_id;
            this.Text = String.Format("Slave {0} button config", slave_id);
            this.cfgView = view;

            tuning_keyid = -1;

            btnEnable = new Button();
            btnEnable.Location = new Point(20, 25);
            btnEnable.Size = new Size(70, 25);
            btnEnable.Text = "START";
            btnEnable.BackColor = Color.Khaki;
            btnEnable.Font = new Font(btnEnable.Font.Name, btnEnable.Font.Size + 2.0F, FontStyle.Bold, btnEnable.Font.Unit);
            btnEnable.Click += new EventHandler(this.btnEnable_Click);
            this.Controls.Add(btnEnable);

            lbSensor = new Label();
            lbSensor.Location = new Point(btnEnable.Location.X + btnEnable.Size.Width + 10, 25);
            lbSensor.Size = new Size(80, 20);
            lbSensor.Text = "Sns ID";
            lbSensor.TextAlign = ContentAlignment.MiddleCenter;
            lbSensor.Font = new Font(lbSensor.Font.Name, lbSensor.Font.Size + 2.0F, FontStyle.Bold, lbSensor.Font.Unit);
            this.Controls.Add(lbSensor);

            lbActSensor = new Label();
            lbActSensor.Location = new Point(lbSensor.Location.X + lbSensor.Size.Width + 10, 25);
            lbActSensor.Size = new Size(80, 20);
            lbActSensor.Text = "Act Sns";
            lbActSensor.TextAlign = ContentAlignment.MiddleCenter;
            lbActSensor.Font = new Font(lbActSensor.Font.Name, lbActSensor.Font.Size + 2.0F, FontStyle.Bold, lbActSensor.Font.Unit);
            this.Controls.Add(lbActSensor);

            lbDriveFreq = new Label();
            lbDriveFreq.Location = new Point(lbActSensor.Location.X + lbActSensor.Size.Width + 10, 25);
            lbDriveFreq.Size = new Size(80, 20);
            lbDriveFreq.Text = "Drive Freq";
            lbDriveFreq.Font = new Font(lbDriveFreq.Font.Name, lbDriveFreq.Font.Size + 2.0F, FontStyle.Bold, lbDriveFreq.Font.Unit);
            this.Controls.Add(lbDriveFreq);

            lbSDPA = new Label();
            lbSDPA.Location = new Point(lbDriveFreq.Location.X + lbDriveFreq.Size.Width + 10, 25);
            lbSDPA.Size = new Size(50, 20);
            lbSDPA.Text = "SDPA";
            lbSDPA.Font = new Font(lbSDPA.Font.Name, lbSDPA.Font.Size + 2.0F, FontStyle.Bold, lbSDPA.Font.Unit);
            this.Controls.Add(lbSDPA);

            lbSSDIV = new Label();
            lbSSDIV.Location = new Point(lbSDPA.Location.X + lbSDPA.Size.Width + 10, 25);
            lbSSDIV.Size = new Size(50, 20);
            lbSSDIV.Text = "SSDIV";
            lbSSDIV.Font = new Font(lbSSDIV.Font.Name, lbSSDIV.Font.Size + 2.0F, FontStyle.Bold, lbSSDIV.Font.Unit);
            this.Controls.Add(lbSSDIV);

            lbSNUM = new Label();
            lbSNUM.Location = new Point(lbSSDIV.Location.X + lbSSDIV.Size.Width + 10, 25);
            lbSNUM.Size = new Size(50, 20);
            lbSNUM.Text = "SNUM";
            lbSNUM.Font = new Font(lbSNUM.Font.Name, lbSNUM.Font.Size + 2.0F, FontStyle.Bold, lbSNUM.Font.Unit);
            this.Controls.Add(lbSNUM);

            lbSnsOffsetSet = new Label();
            lbSnsOffsetSet.Location = new Point(lbSNUM.Location.X + lbSNUM.Size.Width + 10, 25);
            lbSnsOffsetSet.Size = new Size(80, 20);
            lbSnsOffsetSet.Text = "Sns Set";
            lbSnsOffsetSet.Font = new Font(lbSnsOffsetSet.Font.Name, lbSnsOffsetSet.Font.Size + 2.0F, FontStyle.Bold, lbSnsOffsetSet.Font.Unit);
            this.Controls.Add(lbSnsOffsetSet);

            lbSnsOffsetGet = new Label();
            lbSnsOffsetGet.Location = new Point(lbSnsOffsetSet.Location.X + lbSnsOffsetSet.Size.Width + 10, 25);
            lbSnsOffsetGet.Size = new Size(80, 20);
            lbSnsOffsetGet.Text = "Sns Get";
            lbSnsOffsetGet.Font = new Font(lbSnsOffsetGet.Font.Name, lbSnsOffsetGet.Font.Size + 2.0F, FontStyle.Bold, lbSnsOffsetGet.Font.Unit);
            this.Controls.Add(lbSnsOffsetGet);

            lbPrmCnt = new Label();
            lbPrmCnt.Location = new Point(lbSnsOffsetGet.Location.X + lbSnsOffsetGet.Size.Width + 10, 25);
            lbPrmCnt.Size = new Size(80, 20);
            lbPrmCnt.Text = "Prm Cnt";
            lbPrmCnt.Font = new Font(lbPrmCnt.Font.Name, lbPrmCnt.Font.Size + 2.0F, FontStyle.Bold, lbPrmCnt.Font.Unit);
            this.Controls.Add(lbPrmCnt);

            lbSecCnt = new Label();
            lbSecCnt.Location = new Point(lbPrmCnt.Location.X + lbPrmCnt.Size.Width + 10, 25);
            lbSecCnt.Size = new Size(80, 20);
            lbSecCnt.Text = "Sec Cnt";
            lbSecCnt.Font = new Font(lbSecCnt.Font.Name, lbSecCnt.Font.Size + 2.0F, FontStyle.Bold, lbSecCnt.Font.Unit);
            this.Controls.Add(lbSecCnt);

            lbRawCnt = new Label();
            lbRawCnt.Location = new Point(lbSecCnt.Location.X + lbSecCnt.Size.Width + 10, 25);
            lbRawCnt.Size = new Size(80, 20);
            lbRawCnt.Text = "RAW Cnt";
            lbRawCnt.Font = new Font(lbRawCnt.Font.Name, lbRawCnt.Font.Size + 2.0F, FontStyle.Bold, lbRawCnt.Font.Unit);
            this.Controls.Add(lbRawCnt);

            lbSnsCnt = new Label();
            lbSnsCnt.Location = new Point(lbRawCnt.Location.X + lbRawCnt.Size.Width + 10, 25);
            lbSnsCnt.Size = new Size(80, 20);
            lbSnsCnt.Text = "REF Cnt";
            lbSnsCnt.Font = new Font(lbSnsCnt.Font.Name, lbSnsCnt.Font.Size + 2.0F, FontStyle.Bold, lbSnsCnt.Font.Unit);
            this.Controls.Add(lbSnsCnt);

            lbDelta = new Label();
            lbDelta.Location = new Point(lbSnsCnt.Location.X + lbSnsCnt.Size.Width + 10, 25);
            lbDelta.Size = new Size(50, 20);
            lbDelta.Text = "Delta";
            lbDelta.Font = new Font(lbSnsCnt.Font.Name, lbDelta.Font.Size + 2.0F, FontStyle.Bold, lbDelta.Font.Unit);
            this.Controls.Add(lbDelta);

            lbFingerThreshold = new Label();
            lbFingerThreshold.Location = new Point(lbDelta.Location.X + lbDelta.Size.Width + 10, 25);
            lbFingerThreshold.Size = new Size(80, 20);
            lbFingerThreshold.Text = "Threshold";
            lbFingerThreshold.Font = new Font(lbFingerThreshold.Font.Name, lbFingerThreshold.Font.Size + 2.0F, FontStyle.Bold, lbFingerThreshold.Font.Unit);
            this.Controls.Add(lbFingerThreshold);

            lbHysteresis = new Label();
            lbHysteresis.Location = new Point(lbFingerThreshold.Location.X + lbFingerThreshold.Size.Width + 10, 25);
            lbHysteresis.Size = new Size(80, 20);
            lbHysteresis.Text = "Hysteresis";
            lbHysteresis.Font = new Font(lbHysteresis.Font.Name, lbHysteresis.Font.Size + 2.0F, FontStyle.Bold, lbHysteresis.Font.Unit);
            this.Controls.Add(lbHysteresis);

            lbBtnFilter = new Label();
            lbBtnFilter.Location = new Point(lbHysteresis.Location.X + lbHysteresis.Size.Width + 10, 25);
            lbBtnFilter.Size = new Size(80, 20);
            lbBtnFilter.Text = "Btn Filter";
            lbBtnFilter.Font = new Font(lbBtnFilter.Font.Name, lbBtnFilter.Font.Size + 2.0F, FontStyle.Bold, lbBtnFilter.Font.Unit);
            this.Controls.Add(lbBtnFilter);

            keyData = new KeyDataRow[24];

            // Get PCLKB frequency to set correct default SDPA value
            RX130TouchDevConfigTabPage dev_cfg_view = (RX130TouchDevConfigTabPage)this.cfgView.getDevCfgViewForSlave(slave_id);
            int pclk_freq = dev_cfg_view.getPCLKBFreq();

            // Set correct SDPA for 4 MHz drive frequency based on PCLKB
            // For 16 MHz PCLKB: SDPA = 1 (16/(2×2) = 4 MHz)
            // For 32 MHz PCLKB: SDPA = 3 (32/(4×2) = 4 MHz)
            string default_sdpa = (pclk_freq == 16) ? "1" : "3";
            int y_location = btnEnable.Location.Y + btnEnable.Size.Height;
            for (int i = 0; i < 24; i++)
            {
                int indx = i;
                keyData[i].updated = false;
                keyData[i].key_id = (byte)i;

                keyData[i].ckEnable = new CheckBox();
                keyData[i].ckEnable.Location = new Point(btnEnable.Location.X + 20, y_location + 10);
                keyData[i].ckEnable.Size = new Size(30, 20);
                keyData[i].ckEnable.Checked = false;
                keyData[i].ckEnable.CheckAlign = ContentAlignment.MiddleCenter;
                keyData[i].ckEnable.Font = new Font(keyData[i].ckEnable.Font.Name, keyData[i].ckEnable.Font.Size + 2.0F, FontStyle.Bold, keyData[i].ckEnable.Font.Unit);
                keyData[i].ckEnable.CheckedChanged += delegate (object sender, EventArgs e) { ckEnable_CheckedChanged(sender, e, indx); };
                this.Controls.Add(keyData[i].ckEnable);

                keyData[i].lbSensorID = new Label();
                keyData[i].lbSensorID.Location = new Point(lbSensor.Location.X, y_location + 10);
                keyData[i].lbSensorID.Size = new Size(80, 20);
                keyData[i].lbSensorID.Text = String.Format("Sns {0}", i);
                keyData[i].lbSensorID.TextAlign = ContentAlignment.MiddleCenter;
                keyData[i].lbSensorID.Font = new Font(keyData[i].lbSensorID.Font.Name, keyData[i].lbSensorID.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbSensorID.Font.Unit);
                this.Controls.Add(keyData[i].lbSensorID);

                keyData[i].lbActSensorID = new Label();
                keyData[i].lbActSensorID.Location = new Point(lbActSensor.Location.X, y_location + 10);
                keyData[i].lbActSensorID.Size = new Size(80, 20);
                keyData[i].lbActSensorID.Text = String.Format("NA", i);
                keyData[i].lbActSensorID.TextAlign = ContentAlignment.MiddleCenter;
                keyData[i].lbActSensorID.Font = new Font(keyData[i].lbActSensorID.Font.Name, keyData[i].lbActSensorID.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbActSensorID.Font.Unit);
                this.Controls.Add(keyData[i].lbActSensorID);

                keyData[i].cbDriveFreq = new ComboBox();
                keyData[i].cbDriveFreq.Location = new Point(lbDriveFreq.Location.X, y_location + 10);
                keyData[i].cbDriveFreq.Size = new Size(80, 20);
                foreach (string s in lstDriveFreq)
                {
                    keyData[i].cbDriveFreq.Items.Add(s);
                }
                keyData[i].cbDriveFreq.SelectedIndex = 4;
                keyData[i].cbDriveFreq.Font = new Font(keyData[i].cbDriveFreq.Font.Name, keyData[i].cbDriveFreq.Font.Size + 2.0F, FontStyle.Bold, keyData[i].cbDriveFreq.Font.Unit);
                keyData[i].cbDriveFreq.SelectedIndexChanged += delegate (object sender, EventArgs e) { cbDriveFreq_SelectedIndexChanged(sender, e, indx); };
                this.Controls.Add(keyData[i].cbDriveFreq);

                keyData[i].lbSDPAVal = new Label();
                keyData[i].lbSDPAVal.Location = new Point(lbSDPA.Location.X, y_location + 10);
                keyData[i].lbSDPAVal.Size = new Size(50, 20);
                keyData[i].lbSDPAVal.Text = default_sdpa;
                keyData[i].lbSDPAVal.TextAlign = ContentAlignment.MiddleCenter;
                keyData[i].lbSDPAVal.Font = new Font(keyData[i].lbSDPAVal.Font.Name, keyData[i].lbSDPAVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbSDPAVal.Font.Unit);
                this.Controls.Add(keyData[i].lbSDPAVal);


                keyData[i].lbSSDIVVal = new Label();
                keyData[i].lbSSDIVVal.Location = new Point(lbSSDIV.Location.X, y_location + 10);
                keyData[i].lbSSDIVVal.Size = new Size(50, 20);
                keyData[i].lbSSDIVVal.Text = "0";
                keyData[i].lbSSDIVVal.TextAlign = ContentAlignment.MiddleCenter;
                keyData[i].lbSSDIVVal.Font = new Font(keyData[i].lbSSDIVVal.Font.Name, keyData[i].lbSSDIVVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbSSDIVVal.Font.Unit);
                this.Controls.Add(keyData[i].lbSSDIVVal);

                keyData[i].numSNUMVal = new NumericUpDown();
                keyData[i].numSNUMVal.Location = new Point(lbSNUM.Location.X, y_location + 10);
                keyData[i].numSNUMVal.Size = new Size(50, 20);
                keyData[i].numSNUMVal.Minimum = 0;
                keyData[i].numSNUMVal.Maximum = 255; //0x3F;
                keyData[i].numSNUMVal.Value = 7;
                keyData[i].numSNUMVal.Font = new Font(keyData[i].numSNUMVal.Font.Name, keyData[i].numSNUMVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].numSNUMVal.Font.Unit);
                keyData[i].numSNUMVal.ValueChanged += delegate (object sender, EventArgs e) { numUpDown_ValueChanged(sender, e, indx); };
                this.Controls.Add(keyData[i].numSNUMVal);

                keyData[i].numSnsOffsetSet = new NumericUpDown();
                keyData[i].numSnsOffsetSet.Location = new Point(lbSnsOffsetSet.Location.X + 20, y_location + 10);
                keyData[i].numSnsOffsetSet.Size = new Size(60, 20);
                keyData[i].numSnsOffsetSet.Minimum = 0;
                keyData[i].numSnsOffsetSet.Maximum = 65535;// 1023;
                keyData[i].numSnsOffsetSet.Value = 220;
                keyData[i].numSnsOffsetSet.Font = new Font(keyData[i].numSnsOffsetSet.Font.Name, keyData[i].numSnsOffsetSet.Font.Size + 2.0F, FontStyle.Bold, keyData[i].numSnsOffsetSet.Font.Unit);
                keyData[i].numSnsOffsetSet.ValueChanged += delegate (object sender, EventArgs e) { numUpDown_ValueChanged(sender, e, indx); };
                this.Controls.Add(keyData[i].numSnsOffsetSet);

                keyData[i].lbSnsOffsetGetVal = new Label();
                keyData[i].lbSnsOffsetGetVal.Location = new Point(lbSnsOffsetGet.Location.X + 10, y_location + 10);
                keyData[i].lbSnsOffsetGetVal.Size = new Size(60, 20);
                keyData[i].lbSnsOffsetGetVal.Text = "220";
                keyData[i].lbSnsOffsetGetVal.TextAlign = ContentAlignment.MiddleCenter;
                keyData[i].lbSnsOffsetGetVal.Font = new Font(keyData[i].lbSnsOffsetGetVal.Font.Name, keyData[i].lbSnsOffsetGetVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbSnsOffsetGetVal.Font.Unit);
                this.Controls.Add(keyData[i].lbSnsOffsetGetVal);

                keyData[i].lbPrmCntVal = new Label();
                keyData[i].lbPrmCntVal.Location = new Point(lbPrmCnt.Location.X, y_location + 10);
                keyData[i].lbPrmCntVal.Size = new Size(80, 20);
                keyData[i].lbPrmCntVal.Text = "1000";
                keyData[i].lbPrmCntVal.TextAlign = ContentAlignment.MiddleLeft;
                keyData[i].lbPrmCntVal.Font = new Font(keyData[i].lbPrmCntVal.Font.Name, keyData[i].lbPrmCntVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbPrmCntVal.Font.Unit);
                this.Controls.Add(keyData[i].lbPrmCntVal);

                keyData[i].lbSecCntVal = new Label();
                keyData[i].lbSecCntVal.Location = new Point(lbSecCnt.Location.X, y_location + 10);
                keyData[i].lbSecCntVal.Size = new Size(80, 20);
                keyData[i].lbSecCntVal.Text = "1000";
                keyData[i].lbSecCntVal.TextAlign = ContentAlignment.MiddleLeft;
                keyData[i].lbSecCntVal.Font = new Font(keyData[i].lbSecCntVal.Font.Name, keyData[i].lbSecCntVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbSecCntVal.Font.Unit);
                this.Controls.Add(keyData[i].lbSecCntVal);

                keyData[i].lbRawCntVal = new Label();
                keyData[i].lbRawCntVal.Location = new Point(lbRawCnt.Location.X + 5, y_location + 10);
                keyData[i].lbRawCntVal.Size = new Size(80, 20);
                keyData[i].lbRawCntVal.Text = "0";
                keyData[i].lbRawCntVal.TextAlign = ContentAlignment.MiddleLeft;
                keyData[i].lbRawCntVal.Font = new Font(keyData[i].lbRawCntVal.Font.Name, keyData[i].lbRawCntVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbRawCntVal.Font.Unit);
                this.Controls.Add(keyData[i].lbRawCntVal);

                keyData[i].lbSnsCntVal = new Label();
                keyData[i].lbSnsCntVal.Location = new Point(lbSnsCnt.Location.X + 5, y_location + 10);
                keyData[i].lbSnsCntVal.Size = new Size(80, 20);
                keyData[i].lbSnsCntVal.Text = "0";
                keyData[i].lbSnsCntVal.TextAlign = ContentAlignment.MiddleLeft;
                keyData[i].lbSnsCntVal.Font = new Font(keyData[i].lbSnsCntVal.Font.Name, keyData[i].lbSnsCntVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbSnsCntVal.Font.Unit);
                this.Controls.Add(keyData[i].lbSnsCntVal);

                keyData[i].lbDeltaVal = new Label();
                keyData[i].lbDeltaVal.Location = new Point(lbDelta.Location.X + 5, y_location + 10);
                keyData[i].lbDeltaVal.Size = new Size(50, 20);
                keyData[i].lbDeltaVal.Text = "0";
                keyData[i].lbDeltaVal.TextAlign = ContentAlignment.MiddleLeft;
                keyData[i].lbDeltaVal.Font = new Font(keyData[i].lbDeltaVal.Font.Name, keyData[i].lbDeltaVal.Font.Size + 2.0F, FontStyle.Bold, keyData[i].lbDeltaVal.Font.Unit);
                this.Controls.Add(keyData[i].lbDeltaVal);

                keyData[i].numFingerThreshold = new NumericUpDown();
                keyData[i].numFingerThreshold.Location = new Point(lbFingerThreshold.Location.X, y_location + 10);
                keyData[i].numFingerThreshold.Size = new Size(80, 20);
                keyData[i].numFingerThreshold.Minimum = 0;
                keyData[i].numFingerThreshold.Maximum = 65535;
                keyData[i].numFingerThreshold.Value = 1800;
                keyData[i].numFingerThreshold.Font = new Font(keyData[i].numFingerThreshold.Font.Name, keyData[i].numFingerThreshold.Font.Size + 2.0F, FontStyle.Bold, keyData[i].numFingerThreshold.Font.Unit);
                keyData[i].numFingerThreshold.ValueChanged += delegate (object sender, EventArgs e) { numUpDown_ValueChanged(sender, e, indx); };
                this.Controls.Add(keyData[i].numFingerThreshold);

                keyData[i].numHysteresis = new NumericUpDown();
                keyData[i].numHysteresis.Location = new Point(lbHysteresis.Location.X, y_location + 10);
                keyData[i].numHysteresis.Size = new Size(80, 20);
                keyData[i].numHysteresis.Minimum = 0;
                keyData[i].numHysteresis.Maximum = 65535;
                keyData[i].numHysteresis.Value = 100;
                keyData[i].numHysteresis.Font = new Font(keyData[i].numHysteresis.Font.Name, keyData[i].numHysteresis.Font.Size + 2.0F, FontStyle.Bold, keyData[i].numHysteresis.Font.Unit);
                keyData[i].numHysteresis.ValueChanged += delegate (object sender, EventArgs e) { numUpDown_ValueChanged(sender, e, indx); };
                this.Controls.Add(keyData[i].numHysteresis);

                keyData[i].numBtnFilter = new NumericUpDown();
                keyData[i].numBtnFilter.Location = new Point(lbBtnFilter.Location.X, y_location + 10);
                keyData[i].numBtnFilter.Size = new Size(80, 20);
                keyData[i].numBtnFilter.Value = 100;
                keyData[i].numBtnFilter.Minimum = 0;
                keyData[i].numBtnFilter.Maximum = 65535;
                keyData[i].numBtnFilter.Font = new Font(keyData[i].numBtnFilter.Font.Name, keyData[i].numBtnFilter.Font.Size + 2.0F, FontStyle.Bold, keyData[i].numBtnFilter.Font.Unit);
                keyData[i].numBtnFilter.ValueChanged += delegate (object sender, EventArgs e) { numUpDown_ValueChanged(sender, e, indx); };
                this.Controls.Add(keyData[i].numBtnFilter);

                y_location = keyData[i].lbSensorID.Location.Y + keyData[i].lbSensorID.Size.Height;
            }

            taskTuning = new BackgroundWorker();
            taskTuning.DoWork += taskTuning_DoWork;
            taskTuning.WorkerSupportsCancellation = true;
        }

        private bool sendKeyDataUpdates()
        {
            bool key_update_sent = false;
            bool paused = false;
            for (int i = 0; i < 24; i++)
            {
                if ((IsKeyEnabled(i) == true) && (IsKeySelected(i) == true) && (IsKeyDataUpdated(i) == true))
                {
                    if (paused == false)
                    {
                        cfgView.sendPauseKeyScanningCmd();
                        paused = true;
                    }
                    cfgView.sendUpdateKeyDataCmd((byte)i);
                    key_update_sent = true;
                    Console.WriteLine("[W] Sending Key[{0}] data", i);
                    ClearKeyDataUpdate(i);
                }
            }
            if (paused == true)
            {
                cfgView.sendResumeKeyScanningCmd();
                paused = false;
            }
            return key_update_sent;
        }

        private void sendReadRequest()
        {
            for (int i = 0; i < 24; i++)
            {
                if ((IsKeyEnabled(i) == true) && (IsKeySelected(i) == true))
                {
                    cfgView.sendRequestCounterDataCmd((byte)i);
                    break;
                }
            }
        }

        private bool readKeyData()
        {
            bool bKeyRead = false;
            for (int i = 0; i < 24; i++)
            {
                if ((IsKeyEnabled(i) == true) && (IsKeySelected(i) == true))
                {
                    cfgView.sendReadCounterDataCmd();
                    bKeyRead = true;
                    break;
                }
            }
            return bKeyRead;
        }

        private void taskTuning_DoWork(object sender, DoWorkEventArgs e)
        {
            bool bReadRequestSent = false;
            byte key_id = 0;
            BackgroundWorker worker = (BackgroundWorker)sender;

            Console.WriteLine("Started tuning");

            while (worker.CancellationPending == false)
            {
                if (sendKeyDataUpdates() == true)
                {
                    bReadRequestSent = false;
                }
                if (bReadRequestSent == false)
                {
                    sendReadRequest();
                    bReadRequestSent = true;
                }
                if (readKeyData() == false)
                {
                    bReadRequestSent = false;
                }
                Thread.Sleep(500);
            }
            Console.WriteLine("Completed tuning");
        }

        private void btnEnable_Click(Object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            if (btn.Text == "START")
            {
                if (IsAnyKeyEnabled() == true)
                {
                    if (IsAnyKeySelected() == true)
                    {
                        btn.Text = "STOP";
                        btn.BackColor = Color.GreenYellow;
                        taskTuning.RunWorkerAsync();
                    }
                    else
                    {
                        MessageBox.Show("ERROR: No keys are selected for monitoring");
                    }
                }
                else
                {
                    MessageBox.Show("ERROR: No keys are enabled. \nLoad Configuration data from file or from device and enable required keys using the global configuration tab");
                }
            }
            else
            {
                btn.Text = "START";
                btn.BackColor = Color.Khaki;
                taskTuning.CancelAsync();
                cfgView.sendClearQueue();
            }
        }

        private void ckEnable_CheckedChanged(Object sender, EventArgs e, int key_id)
        {
            CheckBox ckbox = (CheckBox)sender;

            for (int i = 0; i < 24; i++)
            {
                if ((ckbox.Checked == true) && key_id != i)
                {
                    keyData[i].ckEnable.Checked = false;
                    keyData[i].ckEnable.Enabled = false;
                    keyData[i].updated = true;
                    tuning_keyid = key_id;
                }
                else
                {
                    keyData[i].ckEnable.Enabled = true;
                }
            }
            Console.WriteLine("{0} key [{1}]", ckbox.Checked == true ? "Checked" : "Unchecked", key_id);
        }

        private void cbDriveFreq_SelectedIndexChanged(object sender, System.EventArgs e, int key_id)
        {
            ComboBox cbDriveFreq = (ComboBox)sender;
            keyData[key_id].updated = true;
            //TouchConfigTabPage cfg_view = (TouchConfigTabPage)this.cfgView.control.GetView("Touch Config");
            RX130TouchDevConfigTabPage dev_cfg_view = (RX130TouchDevConfigTabPage)this.cfgView.getDevCfgViewForSlave(slave_id);
            int pclk_freq = dev_cfg_view.getPCLKBFreq();

            //{ "0.5 MHz", "1 MHz", "2 MHz", "3 MHz", "4 MHz" };
            if (cbDriveFreq.Text == "0.5 MHz")
            {
                if (pclk_freq == 16)
                {
                    keyData[key_id].lbSDPAVal.Text = "15";
                    keyData[key_id].lbSSDIVVal.Text = "7";
                    keyData[key_id].numSNUMVal.Value = 1;
                }
                else if (pclk_freq == 32)
                {
                    keyData[key_id].lbSDPAVal.Text = "31";
                    keyData[key_id].lbSSDIVVal.Text = "7";
                    keyData[key_id].numSNUMVal.Value = 1;
                }
            }
            else if (cbDriveFreq.Text == "1 MHz")
            {
                if (pclk_freq == 16)
                {
                    keyData[key_id].lbSDPAVal.Text = "7";
                    keyData[key_id].lbSSDIVVal.Text = "3";
                    keyData[key_id].numSNUMVal.Value = 1;
                }
                else if (pclk_freq == 32)
                {
                    keyData[key_id].lbSDPAVal.Text = "15";
                    keyData[key_id].lbSSDIVVal.Text = "3";
                    keyData[key_id].numSNUMVal.Value = 1;
                }
            }
            else if (cbDriveFreq.Text == "2 MHz")
            {
                if (pclk_freq == 16)
                {
                    keyData[key_id].lbSDPAVal.Text = "3";
                    keyData[key_id].lbSSDIVVal.Text = "1";
                    keyData[key_id].numSNUMVal.Value = 3;
                }
                else if (pclk_freq == 32)
                {
                    keyData[key_id].lbSDPAVal.Text = "7";
                    keyData[key_id].lbSSDIVVal.Text = "1";
                    keyData[key_id].numSNUMVal.Value = 3;
                }
            }
            else if (cbDriveFreq.Text == "3 MHz")
            {
                if (pclk_freq == 16)
                {
                    keyData[key_id].lbSDPAVal.Text = "2";
                    keyData[key_id].lbSSDIVVal.Text = "1";
                    keyData[key_id].numSNUMVal.Value = 6;
                }
                else if (pclk_freq == 32)
                {
                    keyData[key_id].lbSDPAVal.Text = "4";
                    keyData[key_id].lbSSDIVVal.Text = "1";
                    keyData[key_id].numSNUMVal.Value = 6;
                }
            }
            else if (cbDriveFreq.Text == "4 MHz")
            {
                if (pclk_freq == 16)
                {
                    keyData[key_id].lbSDPAVal.Text = "1";
                    keyData[key_id].lbSSDIVVal.Text = "0";
                    keyData[key_id].numSNUMVal.Value = 7;
                }
                else if (pclk_freq == 32)
                {
                    keyData[key_id].lbSDPAVal.Text = "3";
                    keyData[key_id].lbSSDIVVal.Text = "0";
                    keyData[key_id].numSNUMVal.Value = 7;
                }
            }
            else
            {
                if (pclk_freq == 16)
                {
                    keyData[key_id].lbSDPAVal.Text = "3";
                    keyData[key_id].lbSSDIVVal.Text = "1";
                    keyData[key_id].numSNUMVal.Value = 3;
                }
                else if (pclk_freq == 32)
                {
                    keyData[key_id].lbSDPAVal.Text = "7";
                    keyData[key_id].lbSSDIVVal.Text = "1";
                    keyData[key_id].numSNUMVal.Value = 3;
                }
            }
        }

        private void numUpDown_ValueChanged(object sender, System.EventArgs e, int key_id)
        {
            keyData[key_id].updated = true;
        }

        private bool IsAnyKeyEnabled()
        {
            for (int i = 0; i < 24; i++)
            {
                if (keyData[i].ckEnable.Enabled == true)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsKeyEnabled(int keyid)
        {
            return keyData[keyid].ckEnable.Enabled;
        }

        private bool IsKeySelected(int keyid)
        {
            return keyData[keyid].ckEnable.Checked;
        }

        private bool IsKeyDataUpdated(int keyid)
        {
            return keyData[keyid].updated;
        }

        public void SetKeyDataUpdate(int keyid)
        {
            keyData[keyid].updated = true;
        }

        private void ClearKeyDataUpdate(int keyid)
        {
            keyData[keyid].updated = false;
        }

        private bool IsAnyKeySelected()
        {
            tuning_keyid = -1;
            for (int i = 0; i < 24; i++)
            {
                if (keyData[i].ckEnable.Checked == true)
                {
                    tuning_keyid = i;
                    return true;
                }
            }
            return false;
        }

        public int GetCurrentTuningKey()
        {
            return tuning_keyid;
        }
    }

    public class SensorButton : Button
    {
        private int Id;
        public bool Selected;
        public SensorButton(int id)
        {
            Id = id;
            Selected = false;
        }

        public int GetBtnId()
        {
            return Id;
        }
    }

    public class RX130TouchDevConfigTabPage : TouchCommanderTabBase
    {
        private bool bViewInitComplete;

        GroupBox gbGlobalConfig;
        Label lbHeartBeatRate;
        NumericUpDown numHeartBeatRate;
        Label lbClassBEnable;
        CheckBox ckClassBEnable;
        Label lbConfigWPart;
        TextBox txtConfigWPart;
        Label lbConfigVersion;
        NumericUpDown numConfigVersionMajor;
        NumericUpDown numConfigVersionMinor;
        NumericUpDown numConfigVersionValid;
        Label lbConfigNumber;
        NumericUpDown numConfigNumber;
        Label lbTuningEnable;
        CheckBox ckTuningEnable;

        GroupBox gbHwConfig;
        Label lbInvalidSensorConnection;
        ComboBox cbInvalidSensorConnection;
        private List<string> lstInvalidSensorConnection = new List<string> { "Connect to GND", "Connect to Shield", "Hign - Z" };
        Label lbHostIntPolarity;
        ComboBox cbHostIntPolarity;
        private List<string> lstHostIntPolarity = new List<string> { "Active Low", "Active High" };
        Label lbHostIntDriveMode;
        ComboBox cbHostIntDriveMode;
        private List<string> lstHostIntDriveMode = new List<string> { "Strong Drive", "Open Drain Drive" };
        Label lbHostIntTimeout;
        NumericUpDown numHostIntTimeout;
        Label lbRefreshInterval;
        NumericUpDown numRefreshInterval;
        Label lbI2CAddrHWSelection;
        CheckBox ckI2CAddrHWSelection;
        Label lbI2CAddrSWSelection;
        NumericUpDown numI2CAddrSWSelection;
        Label lbI2CInactivityTimeout;
        NumericUpDown numI2CInactivityTimeout;

        GroupBox gbFilterConfig;
        Label lbMovingAvgFilter;
        CheckBox ckMovingAvgFilter;
        Label lbPerSwitchThreshold;
        CheckBox ckPerSwitchThreshold;
        Label lbJudgeMultiFreq;
        CheckBox ckJudgeMultiFreq;
        Label lbFilterCommonThresh;
        NumericUpDown numFilterCommonThresh;
        Label lbFilterPointsToTrigger;
        NumericUpDown numFilterPointsToTrigger;
        Label lbFilterTimeoutSamples;
        NumericUpDown numFilterTimeoutSamples;

        GroupBox gbCTSUConfig;
        Label lbCTSUNonMeasuredChannel;
        NumericUpDown numCTSUNonMeasuredChannel;
        Label lbMovingAvgNumber;
        NumericUpDown numMovingAvgNumber;
        Label lbCTSUTransPSUSelect;
        NumericUpDown numCTSUTransPSUSelect;
        Label lbCTSUPSUCapacityAdj;
        NumericUpDown numCTSUPSUCapacityAdj;
        Label lbNumTxChannels;
        NumericUpDown numNumTxChannels;
        Label lbNumRxChannels;
        NumericUpDown numNumRxChannels;
        Label lbSelfTargetValueOffset;
        NumericUpDown numSelfTargetValueOffset;
        Label lbMutualTargetValueOffset;
        NumericUpDown numMutualTargetValueOffset;
        Label lbSensorAutoResetTimeout;
        NumericUpDown numSensorAutoResetTimeout;

        GroupBox gbBtnEnable;
        public SensorButton[] btnEnable;
        GroupBox gbLowPowerBtnEnable;
        public SensorButton[] btnLPEnable;

        GroupBox gbDebugConfig;
        Label lbCapsenseDebug;
        //CheckBox ckCapsenseDebug;
        ComboBox cbUartMode;
        private List<string> lstUartMode = new List<string> { "Disabled", "Debug Mode", "Tuning Mode" };
        //Label lbEnableUartTuning;
        //CheckBox ckEnableUartTuning;
        Label lbUartLogSelection;
        ComboBox cbUartLogSelection;
        private List<string> lstUartLogSelection = new List<string> { "Print 24 Keys", "Per Switch Print", "Key Press for Noise Testing" };
        Button btnEnableI2cTuningMode;
        Button btnRestartTouch;

        GroupBox gbOtherConfig;
        Label lbOnFreq;
        public NumericUpDown numOnFreq;
        Label lbOffFreq;
        public NumericUpDown numOffFreq;
        Label lbDriftFreq;
        public NumericUpDown numDriftFreq;
        Label lbCancelDriftFreq;
        public NumericUpDown numCancelDriftFreq;

        GroupBox gbBaselineReset;
        Label lbResetSensID;
        NumericUpDown numResetSensID;
        Button btnResetBaseline;

        GroupBox gbPCLKBConfig;
        Label lbPCLKBConfig;
        ComboBox cbPCLKBConfig;
        private List<string> lstPCLKBCongig = new List<string> { "16 MHz", "32 MHz" };

        GroupBox gbLimitConfig;
        Label lbFirstUL;
        public NumericUpDown numFirstUL;
        Label lbFirstLL;
        public NumericUpDown numFirstLL;
        Label lbSecondUL;
        public NumericUpDown numSecondUL;
        Label lbSecondLL;
        public NumericUpDown numSecondLL;

        private int slave_id;
        private bool mutual_type;
        private String dev_type;
        private int pclkb_freq;

        public RX130TouchDevConfigTabPage(TouchCommanderController c, int slave_id, String device_type, bool mutual_type) : base(c)
        {
            this.slave_id = slave_id;
            this.mutual_type = mutual_type;
            this.Text = String.Format("Slave {0}: RX130 Global Config", slave_id);

            if (device_type == "Rx130INT")
            {
                this.pclkb_freq = 32;
                //!this.pclkb_freq = 16;
            }
            else
            {
                this.pclkb_freq = 16;
            }


            bViewInitComplete = false;
            dev_type = device_type;
            initView();
        }

        private void initView()
        {
            gbGlobalConfig = new GroupBox();
            gbGlobalConfig.Location = new Point(15, 10);
            gbGlobalConfig.Size = new Size(300, 330);
            gbGlobalConfig.Text = "Global Config";
            gbGlobalConfig.Font = new Font(gbGlobalConfig.Font.Name, gbGlobalConfig.Font.Size + 2.0F, gbGlobalConfig.Font.Style, gbGlobalConfig.Font.Unit);
            this.Controls.Add(gbGlobalConfig);

            lbHeartBeatRate = new Label();
            lbHeartBeatRate.Location = new Point(10, 25);
            lbHeartBeatRate.Size = new Size(140, 40);
            lbHeartBeatRate.Text = "Heartbeat Rate (ms):";
            lbHeartBeatRate.Font = new Font(lbHeartBeatRate.Font.Name, lbHeartBeatRate.Font.Size + 2.0F, FontStyle.Bold, lbHeartBeatRate.Font.Unit);
            gbGlobalConfig.Controls.Add(lbHeartBeatRate);

            numHeartBeatRate = new NumericUpDown();
            numHeartBeatRate.Location = new Point(lbHeartBeatRate.Location.X + lbHeartBeatRate.Size.Width + 20, 20);
            numHeartBeatRate.Size = new Size(80, 40);
            numHeartBeatRate.Minimum = 0;
            numHeartBeatRate.Maximum = 30000;
            numHeartBeatRate.Value = 1000;
            numHeartBeatRate.Font = new Font(numHeartBeatRate.Font.Name, numHeartBeatRate.Font.Size + 2.0F, FontStyle.Bold, numHeartBeatRate.Font.Unit);
            gbGlobalConfig.Controls.Add(numHeartBeatRate);

            lbClassBEnable = new Label();
            lbClassBEnable.Location = new Point(lbHeartBeatRate.Location.X, lbHeartBeatRate.Location.Y + lbHeartBeatRate.Size.Height + 10);
            lbClassBEnable.Size = new Size(150, 30);
            lbClassBEnable.Text = "Enable Class B: ";
            lbClassBEnable.Font = new Font(lbClassBEnable.Font.Name, lbClassBEnable.Font.Size + 2.0F, FontStyle.Bold, lbClassBEnable.Font.Unit);
            gbGlobalConfig.Controls.Add(lbClassBEnable);

            ckClassBEnable = new CheckBox();
            //ckClassBEnable.Location = new Point(lbClassBEnable.Size.Width + 30, lbHeartBeatRate.Location.Y + lbHeartBeatRate.Size.Height + 5);
            ckClassBEnable.Location = new Point(lbClassBEnable.Location.X + lbClassBEnable.Size.Width + 30, lbClassBEnable.Location.Y - 5);
            ckClassBEnable.Size = new Size(30, 30);
            ckClassBEnable.Checked = false;
            gbGlobalConfig.Controls.Add(ckClassBEnable);

            lbRefreshInterval = new Label();
            lbRefreshInterval.Location = new Point(lbClassBEnable.Location.X, lbClassBEnable.Location.Y + lbClassBEnable.Size.Height + 10);
            lbRefreshInterval.Size = new Size(140, 40);
            lbRefreshInterval.Text = "Refresh Interval Rate: ";
            lbRefreshInterval.Font = new Font(lbRefreshInterval.Font.Name, lbRefreshInterval.Font.Size + 2.0F, FontStyle.Bold, lbRefreshInterval.Font.Unit);
            gbGlobalConfig.Controls.Add(lbRefreshInterval);

            numRefreshInterval = new NumericUpDown();
            numRefreshInterval.Location = new Point(lbRefreshInterval.Location.X + lbRefreshInterval.Size.Width + 20, lbRefreshInterval.Location.Y - 5);
            numRefreshInterval.Size = new Size(80, 40);
            numRefreshInterval.Value = 20;
            numRefreshInterval.Font = new Font(numRefreshInterval.Font.Name, numRefreshInterval.Font.Size + 2.0F, FontStyle.Bold, numRefreshInterval.Font.Unit);
            gbGlobalConfig.Controls.Add(numRefreshInterval);

            lbConfigWPart = new Label();
            lbConfigWPart.Location = new Point(lbRefreshInterval.Location.X, lbRefreshInterval.Location.Y + lbRefreshInterval.Size.Height + 10);
            lbConfigWPart.Size = new Size(140, 40);
            lbConfigWPart.Text = "Touch Config WPart: ";
            lbConfigWPart.Font = new Font(lbConfigWPart.Font.Name, lbConfigWPart.Font.Size + 2.0F, FontStyle.Bold, lbConfigWPart.Font.Unit);
            gbGlobalConfig.Controls.Add(lbConfigWPart);

            txtConfigWPart = new TextBox();
            txtConfigWPart.Location = new Point(lbConfigWPart.Location.X + lbConfigWPart.Size.Width + 20, lbConfigWPart.Location.Y - 5);
            txtConfigWPart.Size = new Size(100, 40);
            txtConfigWPart.Text = "W00000000";
            txtConfigWPart.MaxLength = 9;
            txtConfigWPart.Font = new Font(txtConfigWPart.Font.Name, txtConfigWPart.Font.Size + 2.0F, FontStyle.Bold, txtConfigWPart.Font.Unit);
            gbGlobalConfig.Controls.Add(txtConfigWPart);

            lbConfigVersion = new Label();
            lbConfigVersion.Location = new Point(lbConfigWPart.Location.X, lbConfigWPart.Location.Y + lbConfigWPart.Size.Height + 10);
            lbConfigVersion.Size = new Size(110, 40);
            lbConfigVersion.Text = "Touch Cfg Ver: ";
            lbConfigVersion.Font = new Font(lbConfigVersion.Font.Name, lbConfigVersion.Font.Size + 2.0F, FontStyle.Bold, lbConfigVersion.Font.Unit);
            gbGlobalConfig.Controls.Add(lbConfigVersion);

            numConfigVersionMajor = new NumericUpDown();
            numConfigVersionMajor.Location = new Point(lbConfigVersion.Location.X + lbConfigVersion.Size.Width + 20, lbConfigVersion.Location.Y - 5);
            numConfigVersionMajor.Size = new Size(45, 30);
            numConfigVersionMajor.Value = 0;
            numConfigVersionMajor.Font = new Font(numConfigVersionMajor.Font.Name, numConfigVersionMajor.Font.Size + 2.0F, FontStyle.Bold, numConfigVersionMajor.Font.Unit);
            numConfigVersionMajor.Maximum = 255;
            gbGlobalConfig.Controls.Add(numConfigVersionMajor);

            numConfigVersionMinor = new NumericUpDown();
            numConfigVersionMinor.Location = new Point(numConfigVersionMajor.Location.X + numConfigVersionMajor.Size.Width + 10, numConfigVersionMajor.Location.Y);
            numConfigVersionMinor.Size = new Size(45, 30);
            numConfigVersionMinor.Value = 0;
            numConfigVersionMinor.Font = new Font(numConfigVersionMinor.Font.Name, numConfigVersionMinor.Font.Size + 2.0F, FontStyle.Bold, numConfigVersionMinor.Font.Unit);
            numConfigVersionMinor.Maximum = 255;
            gbGlobalConfig.Controls.Add(numConfigVersionMinor);

            numConfigVersionValid = new NumericUpDown();
            numConfigVersionValid.Location = new Point(numConfigVersionMinor.Location.X + numConfigVersionMinor.Size.Width + 10, numConfigVersionMinor.Location.Y);
            numConfigVersionValid.Size = new Size(45, 30);
            numConfigVersionValid.Value = 0;
            numConfigVersionValid.Font = new Font(numConfigVersionValid.Font.Name, numConfigVersionValid.Font.Size + 2.0F, FontStyle.Bold, numConfigVersionValid.Font.Unit);
            numConfigVersionValid.Maximum = 255;
            gbGlobalConfig.Controls.Add(numConfigVersionValid);

            lbConfigNumber = new Label();
            lbConfigNumber.Location = new Point(lbConfigVersion.Location.X, lbConfigVersion.Location.Y + lbConfigVersion.Size.Height + 10);
            lbConfigNumber.Size = new Size(150, 20);
            lbConfigNumber.Text = "Config Number: ";
            lbConfigNumber.Font = new Font(lbConfigNumber.Font.Name, lbConfigNumber.Font.Size + 2.0F, FontStyle.Bold, lbConfigNumber.Font.Unit);
            gbGlobalConfig.Controls.Add(lbConfigNumber);

            numConfigNumber = new NumericUpDown();
            numConfigNumber.Location = new Point(lbConfigNumber.Location.X + lbConfigNumber.Size.Width + 10, lbConfigNumber.Location.Y);
            numConfigNumber.Size = new Size(45, 30);
            numConfigNumber.Value = 1;
            numConfigNumber.Font = new Font(numConfigNumber.Font.Name, numConfigNumber.Font.Size + 2.0F, FontStyle.Bold, numConfigNumber.Font.Unit);
            numConfigNumber.Maximum = 255;
            gbGlobalConfig.Controls.Add(numConfigNumber);

            lbTuningEnable = new Label();
            lbTuningEnable.Location = new Point(lbConfigNumber.Location.X, lbConfigNumber.Location.Y + lbConfigNumber.Size.Height + 10);
            lbTuningEnable.Size = new Size(150, 30);
            lbTuningEnable.Text = "Tuning Enable: ";
            lbTuningEnable.Font = new Font(lbTuningEnable.Font.Name, lbTuningEnable.Font.Size + 2.0F, FontStyle.Bold, lbTuningEnable.Font.Unit);
            gbGlobalConfig.Controls.Add(lbTuningEnable);

            ckTuningEnable = new CheckBox();
            ckTuningEnable.Location = new Point(lbTuningEnable.Location.X + lbTuningEnable.Size.Width + 30, lbTuningEnable.Location.Y - 5);
            ckTuningEnable.Size = new Size(30, 30);
            ckTuningEnable.Checked = true;
            ckTuningEnable.Enabled = false;
            gbGlobalConfig.Controls.Add(ckTuningEnable);

            gbFilterConfig = new GroupBox();
            //gbFilterConfig.Location = new Point(gbGlobalConfig.Location.X, gbGlobalConfig.Location.Y + gbGlobalConfig.Size.Height + 15);
            gbFilterConfig.Location = new Point(gbGlobalConfig.Location.X + gbGlobalConfig.Size.Width + 10, gbGlobalConfig.Location.Y);
            gbFilterConfig.Size = new Size(300, 240);
            gbFilterConfig.Text = "Filter Config";
            gbFilterConfig.Font = new Font(gbFilterConfig.Font.Name, gbFilterConfig.Font.Size + 2.0F, gbFilterConfig.Font.Style, gbFilterConfig.Font.Unit);
            if (dev_type == "Rx130INT")
            {
                gbFilterConfig.Enabled = false;
            }
            this.Controls.Add(gbFilterConfig);

            lbMovingAvgFilter = new Label();
            lbMovingAvgFilter.Location = new Point(10, 25);
            lbMovingAvgFilter.Size = new Size(200, 40);
            lbMovingAvgFilter.Text = "Enable Moving Avg Filter: ";
            lbMovingAvgFilter.Font = new Font(lbMovingAvgFilter.Font.Name, lbMovingAvgFilter.Font.Size + 2.0F, FontStyle.Bold, lbMovingAvgFilter.Font.Unit);
            gbFilterConfig.Controls.Add(lbMovingAvgFilter);

            ckMovingAvgFilter = new CheckBox();
            ckMovingAvgFilter.Location = new Point(lbMovingAvgFilter.Size.Width + 30, 20);
            ckMovingAvgFilter.Size = new Size(30, 30);
            ckMovingAvgFilter.Checked = true;
            gbFilterConfig.Controls.Add(ckMovingAvgFilter);

            lbPerSwitchThreshold = new Label();
            lbPerSwitchThreshold.Location = new Point(10, lbMovingAvgFilter.Location.X + lbMovingAvgFilter.Size.Height + 20);
            lbPerSwitchThreshold.Size = new Size(200, 40);
            lbPerSwitchThreshold.Text = "Enable Per Switch Threshold: ";
            lbPerSwitchThreshold.Font = new Font(lbPerSwitchThreshold.Font.Name, lbPerSwitchThreshold.Font.Size + 2.0F, FontStyle.Bold, lbPerSwitchThreshold.Font.Unit);
            gbFilterConfig.Controls.Add(lbPerSwitchThreshold);

            ckPerSwitchThreshold = new CheckBox();
            ckPerSwitchThreshold.Location = new Point(lbPerSwitchThreshold.Size.Width + 30, lbPerSwitchThreshold.Size.Height + 25);
            ckPerSwitchThreshold.Size = new Size(30, 30);
            ckPerSwitchThreshold.Checked = true;
            gbFilterConfig.Controls.Add(ckPerSwitchThreshold);


            lbJudgeMultiFreq = new Label();
            lbJudgeMultiFreq.Location = new Point(lbPerSwitchThreshold.Location.X, lbPerSwitchThreshold.Location.Y + lbPerSwitchThreshold.Size.Height + 10);
            lbJudgeMultiFreq.Size = new Size(200, 40);
            lbJudgeMultiFreq.Text = "Enable Judge Multi Freq: ";
            lbJudgeMultiFreq.Font = new Font(lbJudgeMultiFreq.Font.Name, lbJudgeMultiFreq.Font.Size + 2.0F, FontStyle.Bold, lbJudgeMultiFreq.Font.Unit);
            //gbFilterConfig.Controls.Add(lbJudgeMultiFreq);

            ckJudgeMultiFreq = new CheckBox();
            ckJudgeMultiFreq.Location = new Point(lbJudgeMultiFreq.Location.X + lbJudgeMultiFreq.Size.Width + 15, lbJudgeMultiFreq.Location.Y);
            ckJudgeMultiFreq.Size = new Size(30, 30);
            ckJudgeMultiFreq.Checked = false;
            //gbFilterConfig.Controls.Add(ckJudgeMultiFreq);

            lbFilterCommonThresh = new Label();
            lbFilterCommonThresh.Location = new Point(lbPerSwitchThreshold.Location.X, lbPerSwitchThreshold.Location.Y + lbPerSwitchThreshold.Size.Height + 10);
            lbFilterCommonThresh.Size = new Size(200, 30);
            lbFilterCommonThresh.Text = "Filter Common Thresh: ";
            lbFilterCommonThresh.Font = new Font(lbFilterCommonThresh.Font.Name, lbFilterCommonThresh.Font.Size + 2.0F, FontStyle.Bold, lbFilterCommonThresh.Font.Unit);
            gbFilterConfig.Controls.Add(lbFilterCommonThresh);

            numFilterCommonThresh = new NumericUpDown();
            numFilterCommonThresh.Location = new Point(lbFilterCommonThresh.Location.X + lbFilterCommonThresh.Size.Width + 10, lbFilterCommonThresh.Location.Y);
            numFilterCommonThresh.Size = new Size(50, 30);
            numFilterCommonThresh.Maximum = 255;
            numFilterCommonThresh.Value = 0x4B;
            numFilterCommonThresh.Font = new Font(numFilterCommonThresh.Font.Name, numFilterCommonThresh.Font.Size + 2.0F, FontStyle.Bold, numFilterCommonThresh.Font.Unit);
            gbFilterConfig.Controls.Add(numFilterCommonThresh);

            lbFilterPointsToTrigger = new Label();
            lbFilterPointsToTrigger.Location = new Point(lbFilterCommonThresh.Location.X, lbFilterCommonThresh.Location.Y + lbFilterCommonThresh.Size.Height + 10);
            lbFilterPointsToTrigger.Size = new Size(200, 30);
            lbFilterPointsToTrigger.Text = "Filter Points to Trig: ";
            lbFilterPointsToTrigger.Font = new Font(lbFilterPointsToTrigger.Font.Name, lbFilterPointsToTrigger.Font.Size + 2.0F, FontStyle.Bold, lbFilterPointsToTrigger.Font.Unit);
            gbFilterConfig.Controls.Add(lbFilterPointsToTrigger);

            numFilterPointsToTrigger = new NumericUpDown();
            numFilterPointsToTrigger.Location = new Point(lbFilterPointsToTrigger.Location.X + lbFilterPointsToTrigger.Size.Width + 10, lbFilterPointsToTrigger.Location.Y);
            numFilterPointsToTrigger.Size = new Size(50, 30);
            numFilterPointsToTrigger.Maximum = 255;
            numFilterPointsToTrigger.Value = 4;
            numFilterPointsToTrigger.Font = new Font(numFilterPointsToTrigger.Font.Name, numFilterPointsToTrigger.Font.Size + 2.0F, FontStyle.Bold, numFilterPointsToTrigger.Font.Unit);
            gbFilterConfig.Controls.Add(numFilterPointsToTrigger);

            lbFilterTimeoutSamples = new Label();
            lbFilterTimeoutSamples.Location = new Point(lbFilterPointsToTrigger.Location.X, lbFilterPointsToTrigger.Location.Y + lbFilterPointsToTrigger.Size.Height + 10);
            lbFilterTimeoutSamples.Size = new Size(200, 30);
            lbFilterTimeoutSamples.Text = "Filter Timeout Samples: ";
            lbFilterTimeoutSamples.Font = new Font(lbFilterTimeoutSamples.Font.Name, lbFilterTimeoutSamples.Font.Size + 2.0F, FontStyle.Bold, lbFilterTimeoutSamples.Font.Unit);
            gbFilterConfig.Controls.Add(lbFilterTimeoutSamples);

            numFilterTimeoutSamples = new NumericUpDown();
            numFilterTimeoutSamples.Location = new Point(lbFilterTimeoutSamples.Location.X + lbFilterTimeoutSamples.Size.Width + 10, lbFilterTimeoutSamples.Location.Y);
            numFilterTimeoutSamples.Size = new Size(50, 30);
            numFilterTimeoutSamples.Maximum = 255;
            numFilterTimeoutSamples.Value = 0x2A;
            numFilterTimeoutSamples.Font = new Font(numFilterTimeoutSamples.Font.Name, numFilterTimeoutSamples.Font.Size + 2.0F, FontStyle.Bold, numFilterTimeoutSamples.Font.Unit);
            gbFilterConfig.Controls.Add(numFilterTimeoutSamples);

            gbCTSUConfig = new GroupBox();
            gbCTSUConfig.Location = new Point(gbFilterConfig.Location.X, gbFilterConfig.Location.Y + gbFilterConfig.Size.Height + 15);
            gbCTSUConfig.Size = new Size(300, 350);
            gbCTSUConfig.Text = "CTSU Config";
            gbCTSUConfig.Font = new Font(gbCTSUConfig.Font.Name, gbCTSUConfig.Font.Size + 2.0F, gbCTSUConfig.Font.Style, gbCTSUConfig.Font.Unit);
            this.Controls.Add(gbCTSUConfig);

            lbCTSUNonMeasuredChannel = new Label();
            lbCTSUNonMeasuredChannel.Location = new Point(10, 25);
            lbCTSUNonMeasuredChannel.Size = new Size(180, 40);
            lbCTSUNonMeasuredChannel.Text = "Non Measured Channel: ";
            lbCTSUNonMeasuredChannel.Font = new Font(lbCTSUNonMeasuredChannel.Font.Name, lbCTSUNonMeasuredChannel.Font.Size + 2.0F, FontStyle.Bold, lbCTSUNonMeasuredChannel.Font.Unit);
            gbCTSUConfig.Controls.Add(lbCTSUNonMeasuredChannel);

            numCTSUNonMeasuredChannel = new NumericUpDown();
            numCTSUNonMeasuredChannel.Location = new Point(lbCTSUNonMeasuredChannel.Location.X + lbCTSUNonMeasuredChannel.Size.Width + 10, lbCTSUNonMeasuredChannel.Location.Y);
            numCTSUNonMeasuredChannel.Size = new Size(55, 30);
            numCTSUNonMeasuredChannel.Value = 0;
            numCTSUNonMeasuredChannel.Enabled = false;
            numCTSUNonMeasuredChannel.Font = new Font(numCTSUNonMeasuredChannel.Font.Name, numCTSUNonMeasuredChannel.Font.Size + 2.0F, FontStyle.Bold, numCTSUNonMeasuredChannel.Font.Unit);
            gbCTSUConfig.Controls.Add(numCTSUNonMeasuredChannel);

            lbMovingAvgNumber = new Label();
            lbMovingAvgNumber.Location = new Point(lbCTSUNonMeasuredChannel.Location.X, lbCTSUNonMeasuredChannel.Location.Y + lbCTSUNonMeasuredChannel.Size.Height + 10);
            lbMovingAvgNumber.Size = new Size(180, 40);
            lbMovingAvgNumber.Text = "CTSU Moving Avg Number: ";
            lbMovingAvgNumber.Font = new Font(lbMovingAvgNumber.Font.Name, lbMovingAvgNumber.Font.Size + 2.0F, FontStyle.Bold, lbMovingAvgNumber.Font.Unit);
            gbCTSUConfig.Controls.Add(lbMovingAvgNumber);

            numMovingAvgNumber = new NumericUpDown();
            numMovingAvgNumber.Location = new Point(lbMovingAvgNumber.Location.X + lbMovingAvgNumber.Size.Width + 10, lbMovingAvgNumber.Location.Y);
            numMovingAvgNumber.Size = new Size(55, 30);
            numMovingAvgNumber.Value = 4;
            numMovingAvgNumber.Font = new Font(numMovingAvgNumber.Font.Name, numMovingAvgNumber.Font.Size + 2.0F, FontStyle.Bold, numMovingAvgNumber.Font.Unit);
            gbCTSUConfig.Controls.Add(numMovingAvgNumber);

            lbCTSUTransPSUSelect = new Label();
            lbCTSUTransPSUSelect.Location = new Point(lbMovingAvgNumber.Location.X, lbMovingAvgNumber.Location.Y + lbMovingAvgNumber.Size.Height + 10);
            lbCTSUTransPSUSelect.Size = new Size(180, 40);
            lbCTSUTransPSUSelect.Text = "Transmission PSU Select: ";
            lbCTSUTransPSUSelect.Font = new Font(lbCTSUTransPSUSelect.Font.Name, lbCTSUTransPSUSelect.Font.Size + 2.0F, FontStyle.Bold, lbCTSUTransPSUSelect.Font.Unit);
            //gbCTSUConfig.Controls.Add(lbCTSUTransPSUSelect);

            numCTSUTransPSUSelect = new NumericUpDown();
            numCTSUTransPSUSelect.Location = new Point(lbCTSUTransPSUSelect.Location.X + lbCTSUTransPSUSelect.Size.Width + 10, lbCTSUTransPSUSelect.Location.Y);
            numCTSUTransPSUSelect.Size = new Size(55, 30);
            numCTSUTransPSUSelect.Maximum = 65535;
            numCTSUTransPSUSelect.Value = 0;
            numCTSUTransPSUSelect.Font = new Font(numCTSUTransPSUSelect.Font.Name, numCTSUTransPSUSelect.Font.Size + 2.0F, FontStyle.Bold, numCTSUTransPSUSelect.Font.Unit);
            //gbCTSUConfig.Controls.Add(numCTSUTransPSUSelect);

            lbCTSUPSUCapacityAdj = new Label();
            lbCTSUPSUCapacityAdj.Location = new Point(lbCTSUTransPSUSelect.Location.X, lbCTSUTransPSUSelect.Location.Y + lbCTSUTransPSUSelect.Size.Height + 10);
            lbCTSUPSUCapacityAdj.Size = new Size(180, 30);
            lbCTSUPSUCapacityAdj.Text = "PSU Capacity Adjust: ";
            lbCTSUPSUCapacityAdj.Font = new Font(lbCTSUPSUCapacityAdj.Font.Name, lbCTSUPSUCapacityAdj.Font.Size + 2.0F, FontStyle.Bold, lbCTSUPSUCapacityAdj.Font.Unit);
            //gbCTSUConfig.Controls.Add(lbCTSUPSUCapacityAdj);

            numCTSUPSUCapacityAdj = new NumericUpDown();
            numCTSUPSUCapacityAdj.Location = new Point(lbCTSUPSUCapacityAdj.Location.X + lbCTSUPSUCapacityAdj.Size.Width + 10, lbCTSUPSUCapacityAdj.Location.Y);
            numCTSUPSUCapacityAdj.Size = new Size(55, 30);
            numCTSUPSUCapacityAdj.Maximum = 65535;
            numCTSUPSUCapacityAdj.Value = 0x100;
            numCTSUPSUCapacityAdj.Font = new Font(numCTSUPSUCapacityAdj.Font.Name, numCTSUPSUCapacityAdj.Font.Size + 2.0F, FontStyle.Bold, numCTSUPSUCapacityAdj.Font.Unit);
            //gbCTSUConfig.Controls.Add(numCTSUPSUCapacityAdj);

            lbNumTxChannels = new Label();
            lbNumTxChannels.Location = new Point(lbMovingAvgNumber.Location.X, lbMovingAvgNumber.Location.Y + lbMovingAvgNumber.Size.Height + 10);
            lbNumTxChannels.Size = new Size(180, 30);
            lbNumTxChannels.Text = "Number of TX Lines: ";
            lbNumTxChannels.Font = new Font(lbNumTxChannels.Font.Name, lbNumTxChannels.Font.Size + 2.0F, FontStyle.Bold, lbNumTxChannels.Font.Unit);
            gbCTSUConfig.Controls.Add(lbNumTxChannels);

            numNumTxChannels = new NumericUpDown();
            numNumTxChannels.Location = new Point(lbNumTxChannels.Location.X + lbNumTxChannels.Size.Width + 10, lbNumTxChannels.Location.Y);
            numNumTxChannels.Size = new Size(55, 30);
            numNumTxChannels.Maximum = 255;
            numNumTxChannels.Value = this.mutual_type == true ? 4 : 0;
            numNumTxChannels.Enabled = dev_type == "Rx130INT" ? true : false;
            numNumTxChannels.Font = new Font(numNumTxChannels.Font.Name, numNumTxChannels.Font.Size + 2.0F, FontStyle.Bold, numNumTxChannels.Font.Unit);
            gbCTSUConfig.Controls.Add(numNumTxChannels);

            lbNumRxChannels = new Label();
            lbNumRxChannels.Location = new Point(lbNumTxChannels.Location.X, lbNumTxChannels.Location.Y + lbNumTxChannels.Size.Height + 10);
            lbNumRxChannels.Size = new Size(180, 30);
            lbNumRxChannels.Text = "Number of RX Lines: ";
            lbNumRxChannels.Font = new Font(lbNumRxChannels.Font.Name, lbNumRxChannels.Font.Size + 2.0F, FontStyle.Bold, lbNumRxChannels.Font.Unit);
            gbCTSUConfig.Controls.Add(lbNumRxChannels);

            numNumRxChannels = new NumericUpDown();
            numNumRxChannels.Location = new Point(lbNumRxChannels.Location.X + lbNumRxChannels.Size.Width + 10, lbNumRxChannels.Location.Y);
            numNumRxChannels.Size = new Size(55, 30);
            numNumRxChannels.Enabled = dev_type == "Rx130INT" ? true : false;
            numNumRxChannels.Maximum = 255;
            numNumRxChannels.Value = this.mutual_type == true ? 6 : 0;
            numNumRxChannels.Font = new Font(numNumRxChannels.Font.Name, numNumRxChannels.Font.Size + 2.0F, FontStyle.Bold, numNumRxChannels.Font.Unit);
            gbCTSUConfig.Controls.Add(numNumRxChannels);

            lbSelfTargetValueOffset = new Label();
            lbSelfTargetValueOffset.Location = new Point(lbNumRxChannels.Location.X, lbNumRxChannels.Location.Y + lbNumRxChannels.Size.Height + 10);
            lbSelfTargetValueOffset.Size = new Size(180, 40);
            lbSelfTargetValueOffset.Text = "Self Target Value Offset: ";
            lbSelfTargetValueOffset.Font = new Font(lbSelfTargetValueOffset.Font.Name, lbSelfTargetValueOffset.Font.Size + 2.0F, FontStyle.Bold, lbSelfTargetValueOffset.Font.Unit);
            gbCTSUConfig.Controls.Add(lbSelfTargetValueOffset);

            numSelfTargetValueOffset = new NumericUpDown();
            numSelfTargetValueOffset.Location = new Point(lbSelfTargetValueOffset.Location.X + lbSelfTargetValueOffset.Size.Width + 10, lbSelfTargetValueOffset.Location.Y);
            numSelfTargetValueOffset.Size = new Size(80, 30);
            numSelfTargetValueOffset.Maximum = 65535;
            numSelfTargetValueOffset.Value = 15360;
            numSelfTargetValueOffset.Enabled = false;
            numSelfTargetValueOffset.Font = new Font(numSelfTargetValueOffset.Font.Name, numSelfTargetValueOffset.Font.Size + 2.0F, FontStyle.Bold, numSelfTargetValueOffset.Font.Unit);
            gbCTSUConfig.Controls.Add(numSelfTargetValueOffset);

            lbMutualTargetValueOffset = new Label();
            lbMutualTargetValueOffset.Location = new Point(lbSelfTargetValueOffset.Location.X, lbSelfTargetValueOffset.Location.Y + lbSelfTargetValueOffset.Size.Height + 10);
            lbMutualTargetValueOffset.Size = new Size(180, 40);
            lbMutualTargetValueOffset.Text = "Mutual Target Value Offset: ";
            lbMutualTargetValueOffset.Font = new Font(lbMutualTargetValueOffset.Font.Name, lbMutualTargetValueOffset.Font.Size + 2.0F, FontStyle.Bold, lbMutualTargetValueOffset.Font.Unit);
            gbCTSUConfig.Controls.Add(lbMutualTargetValueOffset);

            numMutualTargetValueOffset = new NumericUpDown();
            numMutualTargetValueOffset.Location = new Point(lbMutualTargetValueOffset.Location.X + lbMutualTargetValueOffset.Size.Width + 10, lbMutualTargetValueOffset.Location.Y);
            numMutualTargetValueOffset.Size = new Size(80, 30);
            numMutualTargetValueOffset.Maximum = 65535;
            numMutualTargetValueOffset.Value = 10240;
            numMutualTargetValueOffset.Enabled = false;
            numMutualTargetValueOffset.Font = new Font(numMutualTargetValueOffset.Font.Name, numMutualTargetValueOffset.Font.Size + 2.0F, FontStyle.Bold, numMutualTargetValueOffset.Font.Unit);
            gbCTSUConfig.Controls.Add(numMutualTargetValueOffset);

            lbSensorAutoResetTimeout = new Label();
            lbSensorAutoResetTimeout.Location = new Point(lbMutualTargetValueOffset.Location.X, lbMutualTargetValueOffset.Location.Y + lbMutualTargetValueOffset.Size.Height + 10);
            lbSensorAutoResetTimeout.Size = new Size(180, 30);
            lbSensorAutoResetTimeout.Text = "SNS AutoResetTimeout (ms):";
            lbSensorAutoResetTimeout.Font = new Font(lbSensorAutoResetTimeout.Font.Name, lbSensorAutoResetTimeout.Font.Size + 2.0F, FontStyle.Bold, lbSensorAutoResetTimeout.Font.Unit);
            gbCTSUConfig.Controls.Add(lbSensorAutoResetTimeout);

            numSensorAutoResetTimeout = new NumericUpDown();
            numSensorAutoResetTimeout.Location = new Point(lbSensorAutoResetTimeout.Location.X + lbSensorAutoResetTimeout.Size.Width + 10, lbSensorAutoResetTimeout.Location.Y);
            numSensorAutoResetTimeout.Size = new Size(80, 30);
            numSensorAutoResetTimeout.Maximum = 60000;
            numSensorAutoResetTimeout.Value = 20000; //(600 * 100ms)
            numSensorAutoResetTimeout.Font = new Font(numSensorAutoResetTimeout.Font.Name, numSensorAutoResetTimeout.Font.Size + 2.0F, FontStyle.Bold, numSensorAutoResetTimeout.Font.Unit);
            gbCTSUConfig.Controls.Add(numSensorAutoResetTimeout);


            gbHwConfig = new GroupBox();
            //gbHwConfig.Location = new Point(gbFilterConfig.Location.X, gbFilterConfig.Location.Y + gbFilterConfig.Size.Height + 15);
            gbHwConfig.Location = new Point(gbGlobalConfig.Location.X, gbGlobalConfig.Location.Y + gbGlobalConfig.Size.Height + 15);
            gbHwConfig.Size = new Size(300, 320);
            gbHwConfig.Text = "Hardware Config";
            gbHwConfig.Font = new Font(gbHwConfig.Font.Name, gbHwConfig.Font.Size + 2.0F, gbHwConfig.Font.Style, gbHwConfig.Font.Unit);
            gbHwConfig.Enabled = this.dev_type != "Rx130INT" ? true : false;
            this.Controls.Add(gbHwConfig);

            lbInvalidSensorConnection = new Label();
            lbInvalidSensorConnection.Location = new Point(10, 25);
            lbInvalidSensorConnection.Size = new Size(150, 40);
            lbInvalidSensorConnection.Text = "Inactive SNS Conn: ";
            lbInvalidSensorConnection.Font = new Font(lbInvalidSensorConnection.Font.Name, lbInvalidSensorConnection.Font.Size + 2.0F, FontStyle.Bold, lbInvalidSensorConnection.Font.Unit);
            gbHwConfig.Controls.Add(lbInvalidSensorConnection);

            cbInvalidSensorConnection = new ComboBox();
            cbInvalidSensorConnection.Location = new Point(lbInvalidSensorConnection.Location.X + lbInvalidSensorConnection.Size.Width, lbInvalidSensorConnection.Location.Y);
            cbInvalidSensorConnection.Size = new Size(120, 50);
            cbInvalidSensorConnection.Font = new Font(cbInvalidSensorConnection.Font.Name, cbInvalidSensorConnection.Font.Size + 2.0F, cbInvalidSensorConnection.Font.Style, cbInvalidSensorConnection.Font.Unit);
            foreach (string s in lstInvalidSensorConnection)
            {
                cbInvalidSensorConnection.Items.Add(s);
            }
            cbInvalidSensorConnection.SelectedIndex = 0;
            cbInvalidSensorConnection.Enabled = false;
            gbHwConfig.Controls.Add(cbInvalidSensorConnection);

            lbHostIntPolarity = new Label();
            lbHostIntPolarity.Location = new Point(10, lbInvalidSensorConnection.Location.Y + lbInvalidSensorConnection.Size.Height + 10);
            lbHostIntPolarity.Size = new Size(130, 40);
            lbHostIntPolarity.Text = "Host INT Polarity: ";
            lbHostIntPolarity.Font = new Font(lbHostIntPolarity.Font.Name, lbHostIntPolarity.Font.Size + 2.0F, FontStyle.Bold, lbHostIntPolarity.Font.Unit);
            gbHwConfig.Controls.Add(lbHostIntPolarity);

            cbHostIntPolarity = new ComboBox();
            cbHostIntPolarity.Location = new Point(lbHostIntPolarity.Location.X + lbHostIntPolarity.Size.Width + 10, lbHostIntPolarity.Location.Y);
            cbHostIntPolarity.Size = new Size(130, 50);
            cbHostIntPolarity.Font = new Font(cbHostIntPolarity.Font.Name, cbHostIntPolarity.Font.Size + 2.0F, cbHostIntPolarity.Font.Style, cbHostIntPolarity.Font.Unit);
            foreach (string s in lstHostIntPolarity)
            {
                cbHostIntPolarity.Items.Add(s);
            }
            cbHostIntPolarity.SelectedIndex = 0;
            gbHwConfig.Controls.Add(cbHostIntPolarity);

            lbHostIntDriveMode = new Label();
            lbHostIntDriveMode.Location = new Point(10, lbHostIntPolarity.Location.Y + lbHostIntPolarity.Size.Height + 10);
            lbHostIntDriveMode.Size = new Size(130, 40);
            lbHostIntDriveMode.Text = "Host INT Drive: ";
            lbHostIntDriveMode.Font = new Font(lbHostIntDriveMode.Font.Name, lbHostIntDriveMode.Font.Size + 2.0F, FontStyle.Bold, lbHostIntDriveMode.Font.Unit);
            gbHwConfig.Controls.Add(lbHostIntDriveMode);

            cbHostIntDriveMode = new ComboBox();
            cbHostIntDriveMode.Location = new Point(lbHostIntDriveMode.Location.X + lbHostIntDriveMode.Size.Width + 10, lbHostIntDriveMode.Location.Y);
            cbHostIntDriveMode.Size = new Size(130, 50);
            cbHostIntDriveMode.Font = new Font(cbHostIntDriveMode.Font.Name, cbHostIntDriveMode.Font.Size + 2.0F, cbHostIntDriveMode.Font.Style, cbHostIntDriveMode.Font.Unit);
            foreach (string s in lstHostIntDriveMode)
            {
                cbHostIntDriveMode.Items.Add(s);
            }
            cbHostIntDriveMode.SelectedIndex = 0;
            gbHwConfig.Controls.Add(cbHostIntDriveMode);

            lbHostIntTimeout = new Label();
            lbHostIntTimeout.Location = new Point(10, lbHostIntDriveMode.Location.Y + lbHostIntDriveMode.Size.Height + 10);
            lbHostIntTimeout.Size = new Size(130, 40);
            lbHostIntTimeout.Text = "Host INT Timeout: ";
            lbHostIntTimeout.Font = new Font(lbHostIntTimeout.Font.Name, lbHostIntTimeout.Font.Size + 2.0F, FontStyle.Bold, lbHostIntTimeout.Font.Unit);
            gbHwConfig.Controls.Add(lbHostIntTimeout);

            numHostIntTimeout = new NumericUpDown();
            numHostIntTimeout.Location = new Point(lbHostIntTimeout.Location.X + lbHostIntTimeout.Size.Width + 10, lbHostIntTimeout.Location.Y);
            numHostIntTimeout.Size = new Size(130, 30);
            numHostIntTimeout.Minimum = 0;
            numHostIntTimeout.Maximum = 30000;
            numHostIntTimeout.Value = 1000;
            numHostIntTimeout.Font = new Font(numHostIntTimeout.Font.Name, numHostIntTimeout.Font.Size + 2.0F, FontStyle.Bold, numHostIntTimeout.Font.Unit);
            gbHwConfig.Controls.Add(numHostIntTimeout);

            lbI2CAddrHWSelection = new Label();
            lbI2CAddrHWSelection.Location = new Point(10, lbHostIntTimeout.Location.Y + lbHostIntTimeout.Size.Height + 10);
            lbI2CAddrHWSelection.Size = new Size(180, 40);
            lbI2CAddrHWSelection.Text = "Enable I2C HW Pin Select: ";
            lbI2CAddrHWSelection.Font = new Font(lbI2CAddrHWSelection.Font.Name, lbI2CAddrHWSelection.Font.Size + 2.0F, FontStyle.Bold, lbI2CAddrHWSelection.Font.Unit);
            gbHwConfig.Controls.Add(lbI2CAddrHWSelection);

            ckI2CAddrHWSelection = new CheckBox();
            ckI2CAddrHWSelection.Location = new Point(lbI2CAddrHWSelection.Location.X + lbI2CAddrHWSelection.Size.Width + 30, lbI2CAddrHWSelection.Location.Y - 6);
            ckI2CAddrHWSelection.Size = new Size(30, 30);
            ckI2CAddrHWSelection.Checked = false;
            gbHwConfig.Controls.Add(ckI2CAddrHWSelection);

            lbI2CAddrSWSelection = new Label();
            lbI2CAddrSWSelection.Location = new Point(10, lbI2CAddrHWSelection.Location.Y + lbI2CAddrHWSelection.Size.Height + 10);
            lbI2CAddrSWSelection.Size = new Size(150, 40);
            lbI2CAddrSWSelection.Text = "I2C Address Select: ";
            lbI2CAddrSWSelection.Font = new Font(lbI2CAddrSWSelection.Font.Name, lbI2CAddrSWSelection.Font.Size + 2.0F, FontStyle.Bold, lbI2CAddrSWSelection.Font.Unit);
            gbHwConfig.Controls.Add(lbI2CAddrSWSelection);

            numI2CAddrSWSelection = new NumericUpDown();
            numI2CAddrSWSelection.Location = new Point(lbI2CAddrSWSelection.Location.X + lbI2CAddrSWSelection.Size.Width + 10, lbI2CAddrSWSelection.Location.Y);
            numI2CAddrSWSelection.Size = new Size(100, 30);
            numI2CAddrSWSelection.Minimum = 0;
            numI2CAddrSWSelection.Maximum = 255;
            numI2CAddrSWSelection.Value = 8;
            if (ckI2CAddrHWSelection.Checked == true)
            {
                numI2CAddrSWSelection.Enabled = false;
                numI2CAddrSWSelection.Value = 0;
            }
            numI2CAddrSWSelection.Font = new Font(numI2CAddrSWSelection.Font.Name, numI2CAddrSWSelection.Font.Size + 2.0F, FontStyle.Bold, numI2CAddrSWSelection.Font.Unit);
            gbHwConfig.Controls.Add(numI2CAddrSWSelection);


            lbI2CInactivityTimeout = new Label();
            lbI2CInactivityTimeout.Location = new Point(10, lbI2CAddrSWSelection.Location.Y + lbI2CAddrSWSelection.Size.Height + 10);
            lbI2CInactivityTimeout.Size = new Size(150, 40);
            lbI2CInactivityTimeout.Text = "I2C Inactivity Timeout: ";
            lbI2CInactivityTimeout.Font = new Font(lbI2CInactivityTimeout.Font.Name, lbI2CInactivityTimeout.Font.Size + 2.0F, FontStyle.Bold, lbI2CInactivityTimeout.Font.Unit);
            gbHwConfig.Controls.Add(lbI2CInactivityTimeout);

            numI2CInactivityTimeout = new NumericUpDown();
            numI2CInactivityTimeout.Location = new Point(lbI2CInactivityTimeout.Location.X + lbI2CInactivityTimeout.Size.Width + 10, lbI2CInactivityTimeout.Location.Y);
            numI2CInactivityTimeout.Size = new Size(100, 30);
            numI2CInactivityTimeout.Minimum = 0;
            numI2CInactivityTimeout.Maximum = 10000;
            numI2CInactivityTimeout.Value = 600;  //600 * 100ms)
            numI2CInactivityTimeout.Font = new Font(numI2CInactivityTimeout.Font.Name, numI2CInactivityTimeout.Font.Size + 2.0F, FontStyle.Bold, numI2CInactivityTimeout.Font.Unit);
            gbHwConfig.Controls.Add(numI2CInactivityTimeout);

            gbPCLKBConfig = new GroupBox();
            gbPCLKBConfig = new GroupBox();
            gbPCLKBConfig.Location = new Point(gbHwConfig.Location.X, gbHwConfig.Location.Y + gbHwConfig.Size.Height + 10);
            gbPCLKBConfig.Size = new Size(300, 80);
            gbPCLKBConfig.Text = "PCLKB Frequency";
            gbPCLKBConfig.Font = new Font(gbPCLKBConfig.Font.Name, gbPCLKBConfig.Font.Size + 2.0F, gbPCLKBConfig.Font.Style, gbPCLKBConfig.Font.Unit);
            this.Controls.Add(gbPCLKBConfig);

            lbPCLKBConfig = new Label();
            lbPCLKBConfig.Location = new Point(10, 20);
            lbPCLKBConfig.Size = new Size(150, 40);
            lbPCLKBConfig.Text = "PCLKB Frequency:";
            lbPCLKBConfig.Font = new Font(lbPCLKBConfig.Font.Name, lbPCLKBConfig.Font.Size + 2.0F, FontStyle.Bold, lbPCLKBConfig.Font.Unit);
            gbPCLKBConfig.Controls.Add(lbPCLKBConfig);

            cbPCLKBConfig = new ComboBox();
            cbPCLKBConfig.Location = new Point(lbPCLKBConfig.Location.X + lbPCLKBConfig.Size.Width + 10, lbPCLKBConfig.Location.Y);
            cbPCLKBConfig.Size = new Size(100, 50);
            cbPCLKBConfig.Font = new Font(cbPCLKBConfig.Font.Name, cbPCLKBConfig.Font.Size + 2.0F, cbPCLKBConfig.Font.Style, cbPCLKBConfig.Font.Unit);
            foreach (string s in lstPCLKBCongig)
            {
                cbPCLKBConfig.Items.Add(s);
            }
            if (dev_type == "Rx130INT")
            {
                cbPCLKBConfig.SelectedIndex = 1;
                // cbPCLKBConfig.SelectedIndex = 0;
            }
            else
            {
                cbPCLKBConfig.SelectedIndex = 1;
            }
            cbPCLKBConfig.SelectedIndexChanged += new System.EventHandler(cbPCLKBConfig_SelectedIndexChanged);
            gbPCLKBConfig.Controls.Add(cbPCLKBConfig);

            gbBtnEnable = new GroupBox();
            gbBtnEnable.Location = new Point(gbFilterConfig.Location.X + gbFilterConfig.Size.Width + 10, gbFilterConfig.Location.Y);
            gbBtnEnable.Size = new Size(590, 220);
            gbBtnEnable.Text = "Button Enable";
            gbBtnEnable.Font = new Font(gbBtnEnable.Font.Name, gbBtnEnable.Font.Size + 2.0F, gbBtnEnable.Font.Style, gbBtnEnable.Font.Unit);
            this.Controls.Add(gbBtnEnable);

            btnEnable = new SensorButton[24];

            int row = 0;
            int x = 0;

            for (int i = 0; i < 24; i++)
            {
                btnEnable[i] = new SensorButton(i);
                btnEnable[i].Location = new Point(20 + (x * 70), 30 + (row * 60));
                btnEnable[i].Size = new Size(60, 50);
                btnEnable[i].Text = String.Format("S{0}", i);
                btnEnable[i].Font = new Font(btnEnable[i].Font.Name, btnEnable[i].Font.Size + 2.0F, btnEnable[i].Font.Style, btnEnable[i].Font.Unit);
                btnEnable[i].Click += new EventHandler(this.btnEnable_Click);
                gbBtnEnable.Controls.Add(btnEnable[i]);
                if (i == 7 || i == 15)
                {
                    row++;
                    x = 0;
                }
                else
                {
                    x++;
                }
                if (this.mutual_type == true || ((i >= 0 && i <= 10)))
                {
                    btnEnable[i].Enabled = true;
                }
                else
                {
                    btnEnable[i].Enabled = false;
                }
            }

            gbLowPowerBtnEnable = new GroupBox();
            gbLowPowerBtnEnable.Location = new Point(gbBtnEnable.Location.X, gbBtnEnable.Location.Y + gbBtnEnable.Size.Height + 10);
            gbLowPowerBtnEnable.Size = new Size(590, 220);
            gbLowPowerBtnEnable.Text = "Low Power Enable";
            gbLowPowerBtnEnable.Font = new Font(gbLowPowerBtnEnable.Font.Name, gbLowPowerBtnEnable.Font.Size + 2.0F, gbLowPowerBtnEnable.Font.Style, gbLowPowerBtnEnable.Font.Unit);
            this.Controls.Add(gbLowPowerBtnEnable);

            btnLPEnable = new SensorButton[24];

            row = 0;
            x = 0;

            for (int i = 0; i < 24; i++)
            {
                btnLPEnable[i] = new SensorButton(i);
                btnLPEnable[i].Location = new Point(20 + (x * 70), 30 + (row * 60));
                btnLPEnable[i].Size = new Size(60, 50);
                btnLPEnable[i].Text = String.Format("S{0}", i);
                btnLPEnable[i].Enabled = false;
                btnLPEnable[i].Font = new Font(btnLPEnable[i].Font.Name, btnLPEnable[i].Font.Size + 2.0F, btnLPEnable[i].Font.Style, btnLPEnable[i].Font.Unit);
                btnLPEnable[i].Click += new EventHandler(this.btnLPMEnable_Click);
                gbLowPowerBtnEnable.Controls.Add(btnLPEnable[i]);
                if (i == 7 || i == 15)
                {
                    row++;
                    x = 0;
                }
                else
                {
                    x++;
                }
            }

            gbDebugConfig = new GroupBox();
            gbDebugConfig.Location = new Point(gbLowPowerBtnEnable.Location.X, gbLowPowerBtnEnable.Location.Y + gbLowPowerBtnEnable.Size.Height + 10);
            gbDebugConfig.Size = new Size(400, 175);
            gbDebugConfig.Text = "Debug Config";
            gbDebugConfig.Font = new Font(gbDebugConfig.Font.Name, gbDebugConfig.Font.Size + 2.0F, gbDebugConfig.Font.Style, gbDebugConfig.Font.Unit);
            this.Controls.Add(gbDebugConfig);

            lbCapsenseDebug = new Label();
            lbCapsenseDebug.Location = new Point(15, 30);
            lbCapsenseDebug.Size = new Size(150, 30);
            //lbCapsenseDebug.Text = "Capsense Debug: ";
            lbCapsenseDebug.Text = "UART Mode: ";
            lbCapsenseDebug.Font = new Font(lbCapsenseDebug.Font.Name, lbCapsenseDebug.Font.Size + 2.0F, FontStyle.Bold, lbCapsenseDebug.Font.Unit);
            gbDebugConfig.Controls.Add(lbCapsenseDebug);

            /*
            ckCapsenseDebug = new CheckBox();
            ckCapsenseDebug.Location = new Point(lbCapsenseDebug.Location.X + lbCapsenseDebug.Size.Width + 30, lbCapsenseDebug.Location.Y - 5);
            ckCapsenseDebug.Size = new Size(30, 30);
            ckCapsenseDebug.Checked = false;
            gbDebugConfig.Controls.Add(ckCapsenseDebug);
            */

            cbUartMode = new ComboBox();
            cbUartMode.Location = new Point(lbCapsenseDebug.Location.X + lbCapsenseDebug.Size.Width + 10, lbCapsenseDebug.Location.Y);
            cbUartMode.Size = new Size(200, 50);
            cbUartMode.Font = new Font(cbUartMode.Font.Name, cbUartMode.Font.Size + 2.0F, cbUartMode.Font.Style, cbUartMode.Font.Unit);
            foreach (string s in lstUartMode)
            {
                cbUartMode.Items.Add(s);
            }
            cbUartMode.SelectedIndex = 0;
            cbUartMode.SelectedIndexChanged += delegate (object sender, EventArgs e) { cbUartMode_SelectedIndexChanged(sender, e); };
            gbDebugConfig.Controls.Add(cbUartMode);

            /*
            lbEnableUartTuning = new Label();
            lbEnableUartTuning.Location = new Point(lbCapsenseDebug.Location.X, lbCapsenseDebug.Location.Y + lbCapsenseDebug.Size.Height + 10);
            lbEnableUartTuning.Size = new Size(150, 30);
            lbEnableUartTuning.Text = "Enable UART Tuning: ";
            lbEnableUartTuning.Font = new Font(lbEnableUartTuning.Font.Name, lbEnableUartTuning.Font.Size + 2.0F, FontStyle.Bold, lbEnableUartTuning.Font.Unit);
            //gbDebugConfig.Controls.Add(lbEnableUartTuning);

            ckEnableUartTuning = new CheckBox();
            ckEnableUartTuning.Location = new Point(lbEnableUartTuning.Location.X + lbEnableUartTuning.Size.Width + 30, lbEnableUartTuning.Location.Y - 5);
            ckEnableUartTuning.Size = new Size(80, 30);
            ckEnableUartTuning.Checked = false;
            //gbDebugConfig.Controls.Add(ckEnableUartTuning);
            */

            lbUartLogSelection = new Label();
            lbUartLogSelection.Location = new Point(lbCapsenseDebug.Location.X, lbCapsenseDebug.Location.Y + lbCapsenseDebug.Size.Height + 10);
            lbUartLogSelection.Size = new Size(150, 30);
            lbUartLogSelection.Text = "UART Log Type: ";
            lbUartLogSelection.Font = new Font(lbUartLogSelection.Font.Name, lbUartLogSelection.Font.Size + 2.0F, FontStyle.Bold, lbUartLogSelection.Font.Unit);
            gbDebugConfig.Controls.Add(lbUartLogSelection);

            cbUartLogSelection = new ComboBox();
            cbUartLogSelection.Location = new Point(lbUartLogSelection.Location.X + lbUartLogSelection.Size.Width + 10, lbUartLogSelection.Location.Y);
            cbUartLogSelection.Size = new Size(200, 50);
            cbUartLogSelection.Font = new Font(cbUartLogSelection.Font.Name, cbUartLogSelection.Font.Size + 2.0F, cbUartLogSelection.Font.Style, cbUartLogSelection.Font.Unit);
            foreach (string s in lstUartLogSelection)
            {
                cbUartLogSelection.Items.Add(s);
            }
            cbUartLogSelection.SelectedIndex = 0;
            if (cbUartMode.SelectedIndex == 0)
            {
                cbUartLogSelection.Enabled = false;
            }
            gbDebugConfig.Controls.Add(cbUartLogSelection);

            btnEnableI2cTuningMode = new Button();
            btnEnableI2cTuningMode.Location = new Point(lbUartLogSelection.Location.X, lbUartLogSelection.Location.Y + lbUartLogSelection.Size.Height + 20);
            btnEnableI2cTuningMode.Size = new Size(170, 40);
            btnEnableI2cTuningMode.Font = new Font(btnEnableI2cTuningMode.Font.Name, btnEnableI2cTuningMode.Font.Size + 2.0F, btnEnableI2cTuningMode.Font.Style, btnEnableI2cTuningMode.Font.Unit);
            btnEnableI2cTuningMode.Text = "Enable I2C Tuning";
            btnEnableI2cTuningMode.BackColor = Color.LightGreen;
            btnEnableI2cTuningMode.Visible = true;
            btnEnableI2cTuningMode.Click += new EventHandler(this.btnEnableI2cTuningMode_Click);
            gbDebugConfig.Controls.Add(btnEnableI2cTuningMode);

            btnRestartTouch = new Button();
            btnRestartTouch.Location = new Point(btnEnableI2cTuningMode.Location.X + lbUartLogSelection.Size.Width + 40, btnEnableI2cTuningMode.Location.Y);
            btnRestartTouch.Size = new Size(170, 40);
            btnRestartTouch.Font = new Font(btnRestartTouch.Font.Name, btnRestartTouch.Font.Size + 2.0F, btnRestartTouch.Font.Style, btnRestartTouch.Font.Unit);
            btnRestartTouch.Text = "Restart Touch";
            btnRestartTouch.BackColor = Color.LightGreen;
            btnRestartTouch.Visible = true;
            btnRestartTouch.Click += new EventHandler(this.btnRestartTouch_Click);
            gbDebugConfig.Controls.Add(btnRestartTouch);


            gbBaselineReset = new GroupBox();
            gbBaselineReset.Location = new Point(gbDebugConfig.Location.X + gbDebugConfig.Size.Width + 10, gbDebugConfig.Location.Y);
            gbBaselineReset.Size = new Size(180, 175);
            gbBaselineReset.Text = "Baseline Reset";
            gbBaselineReset.Font = new Font(gbBaselineReset.Font.Name, gbBaselineReset.Font.Size + 2.0F, gbBaselineReset.Font.Style, gbBaselineReset.Font.Unit);
            this.Controls.Add(gbBaselineReset);

            lbResetSensID = new Label();
            lbResetSensID.Location = new Point(15, 40);
            lbResetSensID.Size = new Size(70, 30);
            lbResetSensID.Text = "Sns ID: ";
            lbResetSensID.Font = new Font(lbResetSensID.Font.Name, lbResetSensID.Font.Size + 2.0F, FontStyle.Bold, lbResetSensID.Font.Unit);
            gbBaselineReset.Controls.Add(lbResetSensID);

            numResetSensID = new NumericUpDown();
            numResetSensID.Location = new Point(lbResetSensID.Location.X + lbResetSensID.Size.Width + 10, lbResetSensID.Location.Y);
            numResetSensID.Size = new Size(70, 30);
            numResetSensID.Minimum = 0;
            numResetSensID.Maximum = 23;
            numResetSensID.Value = 0;
            numResetSensID.Font = new Font(numResetSensID.Font.Name, numResetSensID.Font.Size + 2.0F, FontStyle.Bold, numResetSensID.Font.Unit);
            gbBaselineReset.Controls.Add(numResetSensID);

            btnResetBaseline = new Button();
            btnResetBaseline.Location = new Point(lbResetSensID.Location.X, lbResetSensID.Location.Y + lbResetSensID.Size.Height + 30);
            btnResetBaseline.Size = new Size(150, 40);
            btnResetBaseline.Font = new Font(btnResetBaseline.Font.Name, btnResetBaseline.Font.Size + 2.0F, btnResetBaseline.Font.Style, btnResetBaseline.Font.Unit);
            btnResetBaseline.Text = "Baseline Reset";
            btnResetBaseline.BackColor = Color.LightGreen;
            btnResetBaseline.Visible = true;
            btnResetBaseline.Click += new EventHandler(this.btnResetBaseline_Click);
            gbBaselineReset.Controls.Add(btnResetBaseline);

            gbOtherConfig = new GroupBox();
            gbOtherConfig.Location = new Point(gbCTSUConfig.Location.X, gbCTSUConfig.Location.Y + gbCTSUConfig.Size.Height + 10);
            gbOtherConfig.Size = new Size(300, 190);
            gbOtherConfig.Text = "Other Config";
            gbOtherConfig.Font = new Font(gbOtherConfig.Font.Name, gbOtherConfig.Font.Size + 2.0F, gbOtherConfig.Font.Style, gbOtherConfig.Font.Unit);
            this.Controls.Add(gbOtherConfig);

            lbOnFreq = new Label();
            lbOnFreq.Location = new Point(15, 30);
            lbOnFreq.Size = new Size(150, 30);
            lbOnFreq.Text = "On Frequency: ";
            lbOnFreq.Font = new Font(lbOnFreq.Font.Name, lbOnFreq.Font.Size + 2.0F, FontStyle.Bold, lbOnFreq.Font.Unit);
            gbOtherConfig.Controls.Add(lbOnFreq);

            numOnFreq = new NumericUpDown();
            numOnFreq.Location = new Point(lbOnFreq.Location.X + lbOnFreq.Size.Width + 10, lbOnFreq.Location.Y);
            numOnFreq.Size = new Size(80, 30);
            numOnFreq.Value = 3;
            numOnFreq.Font = new Font(numOnFreq.Font.Name, numOnFreq.Font.Size + 2.0F, FontStyle.Bold, numOnFreq.Font.Unit);
            gbOtherConfig.Controls.Add(numOnFreq);

            lbOffFreq = new Label();
            lbOffFreq.Location = new Point(lbOnFreq.Location.X, lbOnFreq.Location.Y + lbOnFreq.Size.Height + 10);
            lbOffFreq.Size = new Size(150, 30);
            lbOffFreq.Text = "Off Frequency: ";
            lbOffFreq.Font = new Font(lbOffFreq.Font.Name, lbOffFreq.Font.Size + 2.0F, FontStyle.Bold, lbOffFreq.Font.Unit);
            gbOtherConfig.Controls.Add(lbOffFreq);

            numOffFreq = new NumericUpDown();
            numOffFreq.Location = new Point(lbOffFreq.Location.X + lbOffFreq.Size.Width + 10, lbOffFreq.Location.Y);
            numOffFreq.Size = new Size(80, 30);
            numOffFreq.Value = 3;
            numOffFreq.Font = new Font(numOffFreq.Font.Name, numOffFreq.Font.Size + 2.0F, FontStyle.Bold, numOffFreq.Font.Unit);
            gbOtherConfig.Controls.Add(numOffFreq);

            lbDriftFreq = new Label();
            lbDriftFreq.Location = new Point(lbOffFreq.Location.X, lbOffFreq.Location.Y + lbOffFreq.Size.Height + 10);
            lbDriftFreq.Size = new Size(150, 30);
            lbDriftFreq.Text = "Drift Frequency: ";
            lbDriftFreq.Font = new Font(lbDriftFreq.Font.Name, lbDriftFreq.Font.Size + 2.0F, FontStyle.Bold, lbDriftFreq.Font.Unit);
            gbOtherConfig.Controls.Add(lbDriftFreq);

            numDriftFreq = new NumericUpDown();
            numDriftFreq.Location = new Point(lbDriftFreq.Location.X + lbDriftFreq.Size.Width + 10, lbDriftFreq.Location.Y);
            numDriftFreq.Size = new Size(80, 30);
            numDriftFreq.Maximum = 65535;
            numDriftFreq.Value = 600;
            numDriftFreq.Font = new Font(numDriftFreq.Font.Name, numDriftFreq.Font.Size + 2.0F, FontStyle.Bold, numDriftFreq.Font.Unit);
            gbOtherConfig.Controls.Add(numDriftFreq);

            lbCancelDriftFreq = new Label();
            lbCancelDriftFreq.Location = new Point(lbDriftFreq.Location.X, lbDriftFreq.Location.Y + lbDriftFreq.Size.Height + 10);
            lbCancelDriftFreq.Size = new Size(150, 30);
            lbCancelDriftFreq.Text = "Cancel Drift Freq: ";
            lbCancelDriftFreq.Font = new Font(lbCancelDriftFreq.Font.Name, lbCancelDriftFreq.Font.Size + 2.0F, FontStyle.Bold, lbCancelDriftFreq.Font.Unit);
            gbOtherConfig.Controls.Add(lbCancelDriftFreq);

            numCancelDriftFreq = new NumericUpDown();
            numCancelDriftFreq.Location = new Point(lbCancelDriftFreq.Location.X + lbCancelDriftFreq.Size.Width + 10, lbCancelDriftFreq.Location.Y);
            numCancelDriftFreq.Size = new Size(80, 30);
            numCancelDriftFreq.Maximum = 65535;
            numCancelDriftFreq.Value = 0;
            numCancelDriftFreq.Font = new Font(numCancelDriftFreq.Font.Name, numCancelDriftFreq.Font.Size + 2.0F, FontStyle.Bold, numCancelDriftFreq.Font.Unit);
            gbOtherConfig.Controls.Add(numCancelDriftFreq);

            gbLimitConfig = new GroupBox();
            gbLimitConfig.Location = new Point(gbBtnEnable.Location.X + gbBtnEnable.Size.Width + 10, gbBtnEnable.Location.Y);
            gbLimitConfig.Size = new Size(210, 220);
            gbLimitConfig.Text = "Limit Config";
            gbLimitConfig.Font = new Font(gbLimitConfig.Font.Name, gbLimitConfig.Font.Size + 2.0F, gbLimitConfig.Font.Style, gbLimitConfig.Font.Unit);
            this.Controls.Add(gbLimitConfig);

            lbFirstUL = new Label();
            lbFirstUL.Location = new Point(15, 30);
            lbFirstUL.Size = new Size(90, 30);
            lbFirstUL.Text = "First UL:";
            lbFirstUL.Font = new Font(lbFirstUL.Font.Name, lbFirstUL.Font.Size + 2.0F, FontStyle.Bold, lbFirstUL.Font.Unit);
            gbLimitConfig.Controls.Add(lbFirstUL);

            numFirstUL = new NumericUpDown();
            numFirstUL.Location = new Point(lbFirstUL.Location.X + lbFirstUL.Size.Width + 10, lbFirstUL.Location.Y);
            numFirstUL.Size = new Size(80, 30);
            numFirstUL.Maximum = 65535;
            numFirstUL.Value = 50000;
            numFirstUL.Font = new Font(numFirstUL.Font.Name, numFirstUL.Font.Size + 2.0F, FontStyle.Bold, numFirstUL.Font.Unit);
            gbLimitConfig.Controls.Add(numFirstUL);

            lbFirstLL = new Label();
            lbFirstLL.Location = new Point(lbFirstUL.Location.X, lbFirstUL.Location.Y + lbFirstUL.Size.Height + 10);
            lbFirstLL.Size = new Size(90, 30);
            lbFirstLL.Text = "First LL:";
            lbFirstLL.Font = new Font(lbFirstLL.Font.Name, lbFirstLL.Font.Size + 2.0F, FontStyle.Bold, lbFirstLL.Font.Unit);
            gbLimitConfig.Controls.Add(lbFirstLL);

            numFirstLL = new NumericUpDown();
            numFirstLL.Location = new Point(lbFirstLL.Location.X + lbFirstLL.Size.Width + 10, lbFirstLL.Location.Y);
            numFirstLL.Size = new Size(80, 30);
            numFirstLL.Maximum = 65535;
            numFirstLL.Value = 300;
            numFirstLL.Font = new Font(numFirstLL.Font.Name, numFirstLL.Font.Size + 2.0F, FontStyle.Bold, numFirstLL.Font.Unit);
            gbLimitConfig.Controls.Add(numFirstLL);

            lbSecondUL = new Label();
            lbSecondUL.Location = new Point(lbFirstLL.Location.X, lbFirstLL.Location.Y + lbFirstLL.Size.Height + 10);
            lbSecondUL.Size = new Size(95, 40);
            lbSecondUL.Text = "Second UL:";
            lbSecondUL.Font = new Font(lbSecondUL.Font.Name, lbSecondUL.Font.Size + 2.0F, FontStyle.Bold, lbSecondUL.Font.Unit);
            gbLimitConfig.Controls.Add(lbSecondUL);

            numSecondUL = new NumericUpDown();
            numSecondUL.Location = new Point(lbSecondUL.Location.X + lbSecondUL.Size.Width + 10, lbSecondUL.Location.Y);
            numSecondUL.Size = new Size(80, 30);
            numSecondUL.Maximum = 65535;
            numSecondUL.Value = 63488;
            numSecondUL.Font = new Font(numSecondUL.Font.Name, numSecondUL.Font.Size + 2.0F, FontStyle.Bold, numSecondUL.Font.Unit);
            gbLimitConfig.Controls.Add(numSecondUL);

            lbSecondLL = new Label();
            lbSecondLL.Location = new Point(lbSecondUL.Location.X, lbSecondUL.Location.Y + lbSecondUL.Size.Height + 10);
            lbSecondLL.Size = new Size(95, 40);
            lbSecondLL.Text = "Second LL:";
            lbSecondLL.Font = new Font(lbSecondLL.Font.Name, lbSecondLL.Font.Size + 2.0F, FontStyle.Bold, lbSecondLL.Font.Unit);
            gbLimitConfig.Controls.Add(lbSecondLL);

            numSecondLL = new NumericUpDown();
            numSecondLL.Location = new Point(lbSecondLL.Location.X + lbSecondLL.Size.Width + 10, lbSecondLL.Location.Y);
            numSecondLL.Size = new Size(80, 30);
            numSecondLL.Maximum = 65535;
            numSecondLL.Value = 7424;
            numSecondLL.Font = new Font(numSecondLL.Font.Name, numSecondLL.Font.Size + 2.0F, FontStyle.Bold, numSecondLL.Font.Unit);
            gbLimitConfig.Controls.Add(numSecondLL);
        }

        private void cbPCLKBConfig_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cbPCLKB = (ComboBox)sender;
            if (cbPCLKB.Text == "16 MHz")
            {
                pclkb_freq = 16;
            }
            else if (cbPCLKB.Text == "32 MHz")
            {
                pclkb_freq = 32;
            }
            else
            {
                pclkb_freq = 32;
            }
        }

        private void cbUartMode_SelectedIndexChanged(Object sender, EventArgs e)
        {
            ComboBox cbMode = (ComboBox)sender;
            if (cbMode.SelectedIndex == 0 || cbMode.SelectedIndex == 2)
            {
                cbUartLogSelection.Enabled = false;
                cbUartLogSelection.SelectedIndex = 0;
            }
            else
            {
                cbUartLogSelection.Enabled = true;
            }
        }

        private void btnEnable_Click(Object sender, EventArgs e)
        {
            SensorButton btn = (SensorButton)sender;
            int id = btn.GetBtnId();

            if (btn.Selected == false)
            {
                btn.Selected = true;
                btn.BackColor = Color.LightGreen;
                btnLPEnable[id].Enabled = true;
            }
            else
            {
                btn.Selected = false;
                btn.BackColor = default(Color);
                btnLPEnable[id].Enabled = false;
                btnLPEnable[id].Selected = false;
                btnLPEnable[id].BackColor = default(Color);
            }
            if (this.mutual_type == false)
            {
                int enabled_keys = 0;
                for (int i = 0; i < btnEnable.Length; i++)
                {
                    if (btnEnable[i].Selected == true)
                    {
                        enabled_keys++;
                    }
                }
                numNumRxChannels.Value = enabled_keys;
            }
        }

        private void btnLPMEnable_Click(Object sender, EventArgs e)
        {
            SensorButton btn = (SensorButton)sender;
            int id = btn.GetBtnId();

            if (btn.Selected == false)
            {
                btn.Selected = true;
                btn.BackColor = Color.LightGreen;
            }
            else
            {
                btn.Selected = false;
                btn.BackColor = default(Color);
            }
        }

        private void btnEnableI2cTuningMode_Click(Object sender, EventArgs e)
        {
            control.SendI2CCommand("Touch Config", (byte)this.slave_id, 0x0E, 0x1F); // Pause key scanning
            control.SendDelayCommand(1000);
            byte[] data = new Byte[1];
            data[0] = 0x05;
            control.SendI2CWriteCommand("", (byte)this.slave_id, "Secondary", 0x25F, data);
            control.SendI2CCommand("Touch Config", (byte)this.slave_id, 0x30, 0xC5); // Start I2C Tuning
            control.SendDelayCommand(1000);
            control.SendI2CCommand("Touch Config", (byte)this.slave_id, 0x0F, 0x2E); // Resume key scanning
            control.SendDelayCommand(1000);
        }

        private void btnRestartTouch_Click(Object sender, EventArgs e)
        {
            control.SendI2CCommand("Touch Config", (byte)this.slave_id, 0x0E, 0x1F); // Pause key scanning
            control.SendDelayCommand(1000);
            control.SendI2CCommand("Touch Config", (byte)this.slave_id, 0x0F, 0x2E); // Resume key scanning
            control.SendDelayCommand(1000);
        }

        private void btnResetBaseline_Click(Object sender, EventArgs e)
        {
            byte sns_id = (byte)numResetSensID.Value;
            control.SendI2CCommand("Touch Config", (byte)this.slave_id, 0x14, sns_id); // Baseline reset command
        }

        public override void updateView(Object data)
        {
            TouchConfigModel model = (TouchConfigModel)data;
        }
        public override void updateView(string section, Object data)
        {
            TouchConfigModel model = (TouchConfigModel)data;
            if (section.Length == 1)
            {
                int slave_id = int.Parse(section);
                byte[] dev_config_data = model.getDevConfigData(slave_id);
                byte[] cap_config_data = model.getCapConfigData(slave_id);

                bool[] widget_enable = new bool[40];
                bool[] lpwidget_enable = new bool[40];
                bool lpwidget_mismatch = false;

                numHeartBeatRate.Value = (UInt32)((dev_config_data[3] << 24) + (dev_config_data[2] << 16) + (dev_config_data[1] << 8) + dev_config_data[0]);
                ckMovingAvgFilter.Checked = (dev_config_data[4] & 0x01) == 0x01 ? true : false;
                ckPerSwitchThreshold.Checked = (dev_config_data[4] & 0x02) == 0x02 ? true : false;
                ckClassBEnable.Checked = (dev_config_data[5] & 0x08) == 0x08 ? true : false;
                int inactive_sensor = (dev_config_data[5] & 0x30) >> 4;
                if (inactive_sensor >= 0 && inactive_sensor <= 2)
                {
                    cbInvalidSensorConnection.SelectedIndex = inactive_sensor;
                }

                //dev_config_data[6] ; // unused pin state is unused for now

                cbHostIntDriveMode.SelectedIndex = dev_config_data[7] & 0x01;
                cbHostIntPolarity.SelectedIndex = (dev_config_data[7] & 0x02) >> 1;

                numHostIntTimeout.Value = (dev_config_data[9] << 8) + dev_config_data[8];

                //dev_config_data[10]; // reserved

                numCTSUNonMeasuredChannel.Value = dev_config_data[11];
                numRefreshInterval.Value = dev_config_data[12];

                ckI2CAddrHWSelection.Checked = (dev_config_data[13] & 0x01) == 0x01 ? true : false;
                numI2CAddrSWSelection.Value = dev_config_data[14];
                numMovingAvgNumber.Value = dev_config_data[15];
                txtConfigWPart.Text = String.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}", Convert.ToChar(dev_config_data[16]), Convert.ToChar(dev_config_data[17]),
                                        Convert.ToChar(dev_config_data[18]), Convert.ToChar(dev_config_data[19]), Convert.ToChar(dev_config_data[20]),
                                        Convert.ToChar(dev_config_data[21]), Convert.ToChar(dev_config_data[22]), Convert.ToChar(dev_config_data[23]), Convert.ToChar(dev_config_data[24]));
                numConfigVersionMajor.Value = dev_config_data[25];
                numConfigVersionMinor.Value = dev_config_data[26];
                numConfigVersionValid.Value = dev_config_data[27];

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        widget_enable[(i * 8) + j] = ((cap_config_data[i] >> j) & 0x01) == 0x01 ? true : false;
                        lpwidget_enable[(i * 8) + j] = ((dev_config_data[28 + i] >> j) & 0x01) == 0x01 ? true : false;
                    }
                }

                for (int i = 0; i < 24; i++)
                {
                    btnEnable[i].Selected = widget_enable[i];
                    if (widget_enable[i])
                    {
                        btnEnable[i].BackColor = Color.LightGreen;
                        btnLPEnable[i].Enabled = true;
                        btnLPEnable[i].Selected = lpwidget_enable[i];
                        if (lpwidget_enable[i])
                        {
                            btnLPEnable[i].BackColor = Color.LightGreen;
                        }
                    }
                    else
                    {
                        btnEnable[i].BackColor = default(Color);
                        btnLPEnable[i].Enabled = false;
                        btnLPEnable[i].BackColor = default(Color);
                        if (lpwidget_enable[i])
                        {
                            lpwidget_mismatch = true;
                        }
                    }
                }

                if (lpwidget_mismatch == true)
                {
                    MessageBox.Show("ERROR: Invalid LP key selection.\nLow power key selection in loaded touch config is not matching with the enabled keys.\nThe mismatched low power keys are ignored");
                }

                numCTSUTransPSUSelect.Value = (dev_config_data[34] << 8) + dev_config_data[33];
                //numCTSUPSUCapacityAdj.Value = (dev_config_data[36] << 8) + dev_config_data[35];
                numCTSUPSUCapacityAdj.Value = (dev_config_data[35] << 8) + dev_config_data[36];

                ckJudgeMultiFreq.Checked = (dev_config_data[37] & 0x01) == 0x01 ? true : false;

                numFilterCommonThresh.Value = dev_config_data[38];
                numFilterPointsToTrigger.Value = dev_config_data[39];
                numFilterTimeoutSamples.Value = dev_config_data[40];

                numNumRxChannels.Value = dev_config_data[41];
                numNumTxChannels.Value = dev_config_data[42];

                //dev_config_data[43]; // Reserved
                //dev_config_data[44]; // Reserved
                //dev_config_data[45]; // Reserved
                //dev_config_data[46]; // Reserved
                //dev_config_data[47]; // Reserved

                //dev_config_data[48]; // Reserved
                //dev_config_data[49]; // Reserved
                //dev_config_data[50]; // Reserved
                //dev_config_data[51]; // Reserved
                //dev_config_data[52]; // Reserved

                numConfigNumber.Value = dev_config_data[53];
                ckTuningEnable.Checked = (dev_config_data[54] & 0x01) == 0x01 ? true : false;

                //dev_config_data[55]; // Reserved
                //dev_config_data[56]; // Reserved
                //dev_config_data[57]; // Reserved
                //dev_config_data[58]; // Reserved

                //dev_config_data[59]; // Reserved
                //dev_config_data[60]; // Reserved
                //dev_config_data[61]; // Reserved
                //dev_config_data[62]; // Reserved

                //dev_config_data[63]; // Reserved
                //dev_config_data[64]; // Reserved
                //dev_config_data[65]; // Reserved
                //dev_config_data[66]; // Reserved

                //dev_config_data[67]; // Reserved
                //dev_config_data[68]; // Reserved
                //dev_config_data[69]; // Reserved
                //dev_config_data[70]; // Reserved

                //dev_config_data[71]; // Reserved
                //dev_config_data[72]; // Reserved
                //dev_config_data[73]; // Reserved
                //dev_config_data[74]; // Reserved

                //dev_config_data[75]; // Reserved
                //dev_config_data[76]; // Reserved
                //dev_config_data[77]; // Reserved
                //dev_config_data[78]; // Reserved

                numSelfTargetValueOffset.Value = (dev_config_data[80] << 8) + dev_config_data[79];
                numMutualTargetValueOffset.Value = (dev_config_data[82] << 8) + dev_config_data[81];

                if ((dev_config_data[83] & 0x01) == 0x00)
                {
                    cbUartMode.SelectedIndex = 0;
                    cbUartLogSelection.Enabled = false;
                    cbUartLogSelection.SelectedIndex = (dev_config_data[83] >> 2) & 0x03;
                }
                else if ((dev_config_data[83] & 0x01) == 0x01 && (dev_config_data[83] & 0x02) == 0x00)
                {
                    cbUartMode.SelectedIndex = 1;
                    cbUartLogSelection.Enabled = false;
                    cbUartLogSelection.SelectedIndex = (dev_config_data[83] >> 2) & 0x03;
                }
                else if ((dev_config_data[83] & 0x01) == 0x01 && (dev_config_data[83] & 0x02) == 0x02)
                {
                    cbUartMode.SelectedIndex = 2;
                    cbUartLogSelection.Enabled = true;
                    cbUartLogSelection.SelectedIndex = 0;
                }


                //ckCapsenseDebug.Checked = (dev_config_data[83] & 0x01) == 0x01 ? true : false;
                //ckEnableUartTuning.Checked = (dev_config_data[83] & 0x02) == 0x02 ? true : false;

                numI2CInactivityTimeout.Value = (dev_config_data[85] << 8) + dev_config_data[84];
                numSensorAutoResetTimeout.Value = (dev_config_data[87] << 8) + dev_config_data[86];
                numFirstUL.Value = (dev_config_data[89] << 8) + dev_config_data[88];
                numFirstLL.Value = (dev_config_data[91] << 8) + dev_config_data[90];
                numSecondUL.Value = (dev_config_data[93] << 8) + dev_config_data[92];
                numSecondLL.Value = (dev_config_data[95] << 8) + dev_config_data[94];
            }
        }

        public byte[] getDevConfigData(int slave_id)
        {
            //TouchCommanderTabBase cfg_view = control.GetView("Touch Config");
            TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");
            int dev_cfg_size = model.getDevConfigDataSize(slave_id);
            byte[] dev_cfg = new byte[dev_cfg_size];

            Array.Clear(dev_cfg, 0, dev_cfg_size);

            UInt32 hb_rate = (UInt32)(numHeartBeatRate.Value);

            for (int i = 0; i < 4; i++)
            {
                dev_cfg[i] = (byte)((hb_rate >> (8 * i)) & 0xFF);
            }

            if (ckMovingAvgFilter.Checked == true)
            {
                dev_cfg[4] |= 0x01;
            }
            if (ckPerSwitchThreshold.Checked == true)
            {
                dev_cfg[4] |= 0x02;
            }

            if (ckClassBEnable.Checked == true)
            {
                dev_cfg[5] |= 0x08;
            }
            //dev_cfg[5] |= (byte)((cbInvalidSensorConnection.SelectedIndex << 4) & 0x30);
            cbInvalidSensorConnection.Invoke(new Action(() =>
            {
                dev_cfg[5] |= (byte)cbInvalidSensorConnection.SelectedIndex;
            }));

            dev_cfg[6] = 0; //Reserved - Unused Pin State

            cbHostIntDriveMode.Invoke(new Action(() =>
            {
                dev_cfg[7] = (byte)cbHostIntDriveMode.SelectedIndex;
            }));

            cbHostIntPolarity.Invoke(new Action(() =>
            {
                dev_cfg[7] |= (byte)(cbHostIntPolarity.SelectedIndex << 1);
            }));

            dev_cfg[8] = (byte)((int)numHostIntTimeout.Value & 0xFF);
            dev_cfg[9] = (byte)((int)numHostIntTimeout.Value >> 8 & 0xFF);

            dev_cfg[10] = (byte)(this.mutual_type == true ? 3 : 1); // Reserved

            dev_cfg[11] = (byte)numCTSUNonMeasuredChannel.Value; // CTSU Non-measured channel output - For Future use

            dev_cfg[12] = (byte)numRefreshInterval.Value;

            dev_cfg[13] = (byte)(ckI2CAddrHWSelection.Checked == true ? 0x01 : 0x00);
            dev_cfg[14] = (byte)numI2CAddrSWSelection.Value;

            dev_cfg[15] = (byte)numMovingAvgNumber.Value;

            byte[] bytes = Encoding.ASCII.GetBytes(txtConfigWPart.Text);
            for (int i = 0; i < bytes.Length; i++)
            {
                dev_cfg[16 + i] = bytes[i];
            }

            dev_cfg[25] = (byte)numConfigVersionMajor.Value;
            dev_cfg[26] = (byte)numConfigVersionMinor.Value;
            dev_cfg[27] = (byte)numConfigVersionValid.Value;

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    dev_cfg[28 + j] |= (byte)(btnLPEnable[(j * 8) + i].Selected == true ? 0x01 << i : 0x00 << i);
                }
            }
            dev_cfg[31] = 0;
            dev_cfg[32] = 0;

            dev_cfg[33] = (byte)((int)numCTSUTransPSUSelect.Value & 0xFF);
            dev_cfg[34] = (byte)(((int)numCTSUTransPSUSelect.Value >> 8) & 0xFF);

            dev_cfg[35] = (byte)(((int)numCTSUPSUCapacityAdj.Value >> 8) & 0xFF);
            dev_cfg[36] = (byte)((int)numCTSUPSUCapacityAdj.Value & 0xFF);

            //dev_cfg[35] = (byte)((int)numCTSUPSUCapacityAdj.Value & 0xFF);
            //dev_cfg[36] = (byte)(((int)numCTSUPSUCapacityAdj.Value >> 8) & 0xFF);

            dev_cfg[37] = (byte)(ckJudgeMultiFreq.Checked == true ? 0x01 : 0x00);

            dev_cfg[38] = (byte)numFilterCommonThresh.Value;
            dev_cfg[39] = (byte)numFilterPointsToTrigger.Value;
            dev_cfg[40] = (byte)numFilterTimeoutSamples.Value;

            dev_cfg[41] = (byte)numNumRxChannels.Value;
            dev_cfg[42] = (byte)numNumTxChannels.Value;

            for (int i = 0; i < 10; i++)
            {
                dev_cfg[43 + i] = 0;
            }

            dev_cfg[53] = (byte)numConfigNumber.Value;
            dev_cfg[54] = (byte)(ckTuningEnable.Checked == true ? 0x01 : 0x00);

            for (int i = 0; i < 24; i++)
            {
                dev_cfg[55 + i] = 0;
            }

            dev_cfg[79] = (byte)((int)numSelfTargetValueOffset.Value & 0xFF);
            dev_cfg[80] = (byte)(((int)numSelfTargetValueOffset.Value >> 8) & 0xFF);

            dev_cfg[81] = (byte)((int)numMutualTargetValueOffset.Value & 0xFF);
            dev_cfg[82] = (byte)(((int)numMutualTargetValueOffset.Value >> 8) & 0xFF);

            //            dev_cfg[83] = (byte)(ckCapsenseDebug.Checked == true ? 0x01 : 0x00);
            //            dev_cfg[83] |= (byte)(ckEnableUartTuning.Checked == true ? 0x02 : 0x00);

            cbUartMode.Invoke(new Action(() =>
            {
                if (cbUartMode.SelectedIndex == 0)
                {
                    dev_cfg[83] = 0;
                }
                else if (cbUartMode.SelectedIndex == 1)
                {
                    dev_cfg[83] = 1;
                }
                else if (cbUartMode.SelectedIndex == 2)
                {
                    dev_cfg[83] = 3;
                }
            }));

            //dev_cfg[83] |= (byte)(cbUartLogSelection.SelectedIndex << 2);
            cbUartLogSelection.Invoke(new Action(() =>
            {
                dev_cfg[83] |= (byte)(cbUartLogSelection.SelectedIndex << 2);
            }));


            dev_cfg[84] = (byte)((int)numI2CInactivityTimeout.Value & 0xFF);
            dev_cfg[85] = (byte)(((int)numI2CInactivityTimeout.Value >> 8) & 0xFF);

            dev_cfg[86] = (byte)((int)numSensorAutoResetTimeout.Value & 0xFF);
            dev_cfg[87] = (byte)(((int)numSensorAutoResetTimeout.Value >> 8) & 0xFF);

            dev_cfg[88] = (byte)((int)numFirstUL.Value & 0xFF);
            dev_cfg[89] = (byte)(((int)numFirstUL.Value >> 8) & 0xFF);
            dev_cfg[90] = (byte)((int)numFirstLL.Value & 0xFF);
            dev_cfg[91] = (byte)(((int)numFirstLL.Value >> 8) & 0xFF);

            dev_cfg[92] = (byte)((int)numSecondUL.Value & 0xFF);
            dev_cfg[93] = (byte)(((int)numSecondUL.Value >> 8) & 0xFF);
            dev_cfg[94] = (byte)((int)numSecondLL.Value & 0xFF);
            dev_cfg[95] = (byte)(((int)numSecondLL.Value >> 8) & 0xFF);

            return dev_cfg;
        }

        public int getPCLKBFreq()
        {
            return pclkb_freq;
        }
    }

    public class RX130TouchCapConfigTabPage : TouchCommanderTabBase
    {
        ButtonConfigGroup btnCfgGroup;
        TouchConfigTabPage cfgView;
        int slave_id;
        string dev_type;
        bool mutual_type;

        public RX130TouchCapConfigTabPage(TouchCommanderController c, int slave_id, string device_type, bool mutual_type) : base(c)
        {
            this.slave_id = slave_id;
            this.Text = String.Format("Slave {0}: RX130 Button Config", slave_id);
            this.cfgView = (TouchConfigTabPage)control.GetView("Touch Config");
            this.dev_type = device_type;
            this.mutual_type = mutual_type;
            initView();
        }

        private void initView()
        {
            btnCfgGroup = new ButtonConfigGroup(this.slave_id, cfgView);
            btnCfgGroup.Location = new Point(10, 5);
            btnCfgGroup.Size = new Size(1430, 780);
            this.Controls.Add(btnCfgGroup);
        }

        public override void updateView(Object data)
        {
            TouchConfigModel model = (TouchConfigModel)data;
        }
        public override void updateView(string section, Object data)
        {
            TouchConfigModel model = (TouchConfigModel)data;

            if (section.Length == 1)
            {
                int slave_id = int.Parse(section);
                byte[] cap_config_data = model.getCapConfigData(slave_id);
                byte[] physical_Sensor_info = model.getPhysicalKeys(slave_id);

                bool[] widget_enable = new bool[24];
                TouchConfigTabPage cfg_view = (TouchConfigTabPage)control.GetView("Touch Config");
                RX130TouchDevConfigTabPage dev_cfg_view = (RX130TouchDevConfigTabPage)cfg_view.getDevCfgViewForSlave(slave_id);

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        widget_enable[(i * 8) + j] = ((cap_config_data[i] >> j) & 0x01) == 0x01 ? true : false;
                    }
                }

                for (int i = 0; i < 24; i++)
                {

                    dev_cfg_view.btnEnable[i].Selected = widget_enable[i];
                    if (widget_enable[i])
                    {
                        dev_cfg_view.btnEnable[i].BackColor = Color.LightGreen;
                    }
                    else
                    {
                        dev_cfg_view.btnEnable[i].BackColor = default(Color);
                    }

                    btnCfgGroup.keyData[i].numHysteresis.Value = cap_config_data[16 + (i * 15)] | (cap_config_data[16 + (i * 15) + 1] << 8);
                    btnCfgGroup.keyData[i].numFingerThreshold.Value = cap_config_data[18 + (i * 15)] | (cap_config_data[18 + (i * 15) + 1] << 8);

                    btnCfgGroup.keyData[i].numSnsOffsetSet.Value = cap_config_data[20 + (i * 15)] | (cap_config_data[20 + (i * 15) + 1] << 8); ;
                    btnCfgGroup.keyData[i].numSNUMVal.Value = cap_config_data[22 + (i * 15)];
                    btnCfgGroup.keyData[i].lbSDPAVal.Text = String.Format("{0}", cap_config_data[23 + (i * 15)]);
                    btnCfgGroup.keyData[i].lbSSDIVVal.Text = String.Format("{0}", cap_config_data[24 + (i * 15)]);
                    if (i < 11)
                    {
                        btnCfgGroup.keyData[i].lbActSensorID.Text = String.Format("TS{0}", physical_Sensor_info[i]);
                    }

                    btnCfgGroup.keyData[i].numBtnFilter.Value = cap_config_data[25 + (i * 15)];
                }



                dev_cfg_view.numOnFreq.Value = cap_config_data[8];
                dev_cfg_view.numOffFreq.Value = cap_config_data[9];

                dev_cfg_view.numDriftFreq.Value = (cap_config_data[11] << 8) + cap_config_data[10];
                dev_cfg_view.numCancelDriftFreq.Value = (cap_config_data[13] << 8) + cap_config_data[12];

                enableSelectedkeys();
            }
            else
            {
                List<String> section_elem = section.Split(',').ToList();

                if (section_elem[0] == "tuning_data")
                {
                    int slave_id = Int16.Parse(section_elem[1]);
                    int tuning_data_size = model.getTuningDataSize(slave_id);
                    byte[] tuning_data = model.getTuningData(slave_id);
                    UInt16 key_id = tuning_data[0];
                    int tuning_keyid = btnCfgGroup.GetCurrentTuningKey();

                    if (tuning_keyid == key_id)
                    {
                        if (mutual_type == true)
                        {
                            UInt16 secondary_cnt = (UInt16)(tuning_data[1] << 8 | tuning_data[2]);
                            btnCfgGroup.keyData[key_id].lbSecCntVal.Text = String.Format("{0}", secondary_cnt);

                            UInt16 primary_cnt = (UInt16)(tuning_data[3] << 8 | tuning_data[4]);
                            btnCfgGroup.keyData[key_id].lbPrmCntVal.Text = String.Format("{0}", primary_cnt);

                            UInt16 raw_cnt = (UInt16)(secondary_cnt - primary_cnt);
                            btnCfgGroup.keyData[key_id].lbRawCntVal.Text = String.Format("{0}", raw_cnt);

                            UInt16 ref_cnt = (UInt16)(tuning_data[5] << 8 | tuning_data[6]);
                            btnCfgGroup.keyData[key_id].lbSnsCntVal.Text = String.Format("{0}", ref_cnt);

                            //short delta_cnt = (short)Math.Abs(raw_cnt - ref_cnt);
                            short delta_cnt = (short)(raw_cnt - ref_cnt);
                            btnCfgGroup.keyData[key_id].lbDeltaVal.Text = String.Format("{0}", delta_cnt);
                        }
                        else
                        {
                            btnCfgGroup.keyData[key_id].lbSecCntVal.Text = String.Format("{0}", 0);
                            btnCfgGroup.keyData[key_id].lbPrmCntVal.Text = String.Format("{0}", 0);

                            UInt16 raw_cnt = (UInt16)(tuning_data[3] << 8 | tuning_data[4]);
                            btnCfgGroup.keyData[key_id].lbRawCntVal.Text = String.Format("{0}", raw_cnt);

                            UInt16 ref_cnt = (UInt16)(tuning_data[5] << 8 | tuning_data[6]);
                            btnCfgGroup.keyData[key_id].lbSnsCntVal.Text = String.Format("{0}", ref_cnt);

                            short delta_cnt = (short)(raw_cnt - ref_cnt);
                            btnCfgGroup.keyData[key_id].lbDeltaVal.Text = String.Format("{0}", delta_cnt);
                        }

                        UInt16 sns_offset_get_cnt = (UInt16)((tuning_data[15] << 8 | tuning_data[16]) & 0x03FF);
                        btnCfgGroup.keyData[key_id].lbSnsOffsetGetVal.Text = String.Format("{0}", sns_offset_get_cnt);
                    }
                    else if (tuning_keyid != -1)
                    {
                        btnCfgGroup.SetKeyDataUpdate(tuning_keyid);
                    }
                }
            }
        }

        public void enableSelectedkeys()
        {
            TouchConfigTabPage cfg_view = (TouchConfigTabPage)control.GetView("Touch Config");
            RX130TouchDevConfigTabPage dev_cfg_view = (RX130TouchDevConfigTabPage)cfg_view.getDevCfgViewForSlave(slave_id);

            for (int i = 0; i < 24; i++)
            {
                if (dev_cfg_view.btnEnable[i].Selected == true)
                {
                    btnCfgGroup.keyData[i].numHysteresis.Enabled = true;
                    btnCfgGroup.keyData[i].ckEnable.Enabled = true;
                    btnCfgGroup.keyData[i].lbSensorID.Enabled = true;
                    btnCfgGroup.keyData[i].cbDriveFreq.Enabled = true;
                    btnCfgGroup.keyData[i].lbSDPAVal.Enabled = true;
                    btnCfgGroup.keyData[i].lbSSDIVVal.Enabled = true;
                    btnCfgGroup.keyData[i].numSNUMVal.Enabled = true;
                    btnCfgGroup.keyData[i].numSnsOffsetSet.Enabled = true;
                    btnCfgGroup.keyData[i].lbSnsOffsetGetVal.Enabled = true;
                    btnCfgGroup.keyData[i].lbPrmCntVal.Enabled = true;
                    btnCfgGroup.keyData[i].lbSecCntVal.Enabled = true;
                    btnCfgGroup.keyData[i].lbSnsCntVal.Enabled = true;
                    btnCfgGroup.keyData[i].numFingerThreshold.Enabled = true;
                    btnCfgGroup.keyData[i].numHysteresis.Enabled = true;
                    btnCfgGroup.keyData[i].numBtnFilter.Enabled = true;
                }
                else
                {
                    btnCfgGroup.keyData[i].numHysteresis.Enabled = false;
                    btnCfgGroup.keyData[i].ckEnable.Enabled = false;
                    btnCfgGroup.keyData[i].lbSensorID.Enabled = false;
                    btnCfgGroup.keyData[i].cbDriveFreq.Enabled = false;
                    btnCfgGroup.keyData[i].lbSDPAVal.Enabled = false;
                    btnCfgGroup.keyData[i].lbSSDIVVal.Enabled = false;
                    btnCfgGroup.keyData[i].numSNUMVal.Enabled = false;
                    btnCfgGroup.keyData[i].numSnsOffsetSet.Enabled = false;
                    btnCfgGroup.keyData[i].lbSnsOffsetGetVal.Enabled = false;
                    btnCfgGroup.keyData[i].lbPrmCntVal.Enabled = false;
                    btnCfgGroup.keyData[i].lbSecCntVal.Enabled = false;
                    btnCfgGroup.keyData[i].lbSnsCntVal.Enabled = false;
                    btnCfgGroup.keyData[i].numFingerThreshold.Enabled = false;
                    btnCfgGroup.keyData[i].numHysteresis.Enabled = false;
                    btnCfgGroup.keyData[i].numBtnFilter.Enabled = false;
                }
                if (btnCfgGroup.keyData[i].lbSDPAVal.Text == "31")
                {
                    if (dev_cfg_view.getPCLKBFreq() == 16)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                    else if (dev_cfg_view.getPCLKBFreq() == 32)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "0.5 MHz";
                    }
                    else
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                }
                else if (btnCfgGroup.keyData[i].lbSDPAVal.Text == "15")
                {
                    if (dev_cfg_view.getPCLKBFreq() == 16)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "0.5 MHz";
                    }
                    else if (dev_cfg_view.getPCLKBFreq() == 32)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "1 MHz";
                    }
                    else
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                }
                else if (btnCfgGroup.keyData[i].lbSDPAVal.Text == "7")
                {
                    if (dev_cfg_view.getPCLKBFreq() == 16)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "1 MHz";
                    }
                    else if (dev_cfg_view.getPCLKBFreq() == 32)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "2 MHz";
                    }
                    else
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                }
                else if (btnCfgGroup.keyData[i].lbSDPAVal.Text == "4")
                {
                    if (dev_cfg_view.getPCLKBFreq() == 16)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "2 MHz";
                    }
                    else if (dev_cfg_view.getPCLKBFreq() == 32)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "3 MHz";
                    }
                    else
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                }
                else if (btnCfgGroup.keyData[i].lbSDPAVal.Text == "3")
                {
                    if (dev_cfg_view.getPCLKBFreq() == 16)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "2 MHz";
                    }
                    else if (dev_cfg_view.getPCLKBFreq() == 32)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "4 MHz";
                    }
                    else
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                }
                else if (btnCfgGroup.keyData[i].lbSDPAVal.Text == "1")   //GJMSACHIN
                {
                    if (dev_cfg_view.getPCLKBFreq() == 16)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "4 MHz";
                    }
                    else if (dev_cfg_view.getPCLKBFreq() == 32)
                    {
                        // btnCfgGroup.keyData[i].cbDriveFreq.Text = "4 MHz";
                    }
                    else
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                }

                else if (btnCfgGroup.keyData[i].lbSDPAVal.Text == "2")   //GJMSACHIN
                {
                    if (dev_cfg_view.getPCLKBFreq() == 16)
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "3 MHz";
                    }
                    else if (dev_cfg_view.getPCLKBFreq() == 32)
                    {
                        // btnCfgGroup.keyData[i].cbDriveFreq.Text = "4 MHz";
                    }
                    else
                    {
                        btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                    }
                }

                else
                {
                    btnCfgGroup.keyData[i].cbDriveFreq.Text = "Select";
                }
            }
        }

        public byte[] getCapConfigData(int slave_id)
        {
            TouchConfigTabPage cfg_view = (TouchConfigTabPage)control.GetView("Touch Config");
            RX130TouchDevConfigTabPage dev_cfg_view = (RX130TouchDevConfigTabPage)cfg_view.getDevCfgViewForSlave(slave_id);
            TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");

            int cap_cfg_size = model.getCapConfigDataSize(slave_id);
            byte[] cap_cfg = new byte[cap_cfg_size];
            int indx = 0;
            int bit_pos = 0;

            Array.Clear(cap_cfg, 0, cap_cfg_size);

            /*
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    cap_cfg[j] |= (byte)(dev_cfg_view.btnEnable[(j * 8) + i].Selected == true ? 0x01 << i : 0x00 << i);
                }
            }
            */

            for (int i = 0; i < 24; i++)
            {
                if (dev_cfg_view.btnEnable[i].Selected == true)
                {
                    cap_cfg[indx] = (byte)(cap_cfg[indx] | (1 << bit_pos));
                }
                //Console.WriteLine(String.Format("{0}, {1}", indx, bit_pos));
                bit_pos++;
                if (bit_pos == 8)
                {
                    bit_pos = 0;
                    indx++;
                }
            }
            cap_cfg[3] = 0;

            cap_cfg[4] = 0;
            cap_cfg[5] = 0;
            cap_cfg[6] = 0;
            cap_cfg[7] = 0;

            cap_cfg[8] = (byte)dev_cfg_view.numOnFreq.Value;
            cap_cfg[9] = (byte)dev_cfg_view.numOffFreq.Value;

            cap_cfg[10] = (byte)((int)dev_cfg_view.numDriftFreq.Value & 0xFF);
            cap_cfg[11] = (byte)(((int)dev_cfg_view.numDriftFreq.Value >> 8) & 0xFF);

            cap_cfg[12] = (byte)((int)dev_cfg_view.numCancelDriftFreq.Value & 0xFF);
            cap_cfg[13] = (byte)(((int)dev_cfg_view.numCancelDriftFreq.Value >> 8) & 0xFF);

            cap_cfg[14] = 0;
            cap_cfg[15] = 0;

            for (int i = 0; i < 24; i++)
            {
                cap_cfg[16 + (i * 15)] = (byte)((int)btnCfgGroup.keyData[i].numHysteresis.Value & 0xFF);
                cap_cfg[17 + (i * 15)] = (byte)(((int)btnCfgGroup.keyData[i].numHysteresis.Value >> 8) & 0xFF);
                cap_cfg[18 + (i * 15)] = (byte)((int)btnCfgGroup.keyData[i].numFingerThreshold.Value & 0xFF);
                cap_cfg[19 + (i * 15)] = (byte)(((int)btnCfgGroup.keyData[i].numFingerThreshold.Value >> 8) & 0xFF);
                cap_cfg[20 + (i * 15)] = (byte)((int)btnCfgGroup.keyData[i].numSnsOffsetSet.Value & 0xFF);
                cap_cfg[21 + (i * 15)] = (byte)(((int)btnCfgGroup.keyData[i].numSnsOffsetSet.Value >> 8) & 0xFF);
                cap_cfg[22 + (i * 15)] = (byte)btnCfgGroup.keyData[i].numSNUMVal.Value;
                cap_cfg[23 + (i * 15)] = (byte)Int32.Parse(btnCfgGroup.keyData[i].lbSDPAVal.Text);
                cap_cfg[24 + (i * 15)] = (byte)Int32.Parse(btnCfgGroup.keyData[i].lbSSDIVVal.Text);
                cap_cfg[25 + (i * 15)] = (byte)btnCfgGroup.keyData[i].numBtnFilter.Value;
            }

            //Console.WriteLine(String.Format("{0}, {1}, {2}", cap_cfg[0], cap_cfg[1], cap_cfg[2]));

            return cap_cfg;
        }
    }

    public class RX130TouchTuningTabPage : TouchCommanderTabBase
    {
        int slave_id;

        public RX130TouchTuningTabPage(TouchCommanderController c, int slave_id) : base(c)
        {
            this.slave_id = slave_id;
            //this.Text = String.Format("Slave {0}: RX130 Initial Tuning", slave_id);
        }

        public override void updateView(Object data)
        {
            TouchConfigModel model = (TouchConfigModel)data;
        }
        public override void updateView(string section, Object data)
        {
            TouchConfigModel model = (TouchConfigModel)data;
        }
    }
}