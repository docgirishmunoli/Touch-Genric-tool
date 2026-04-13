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
    public enum update_data
    {
        DELTA,
        MAX_DELTA,
        RAW_DATA,
        REF_DATA,
        KEY_STATUS,
    }

    public struct enable_data_monitoring
    {
        public bool raw_enable;
        public bool baseline_enable;
        public bool delta_enable;
        public bool maxdelta_enable;
        public bool keystatus_enable;
    }

    public class DataMonitoringModel : TouchCommanderModelBase
    {
        public struct monitoring_data
        {
            public byte[] logicalid;
            public byte[] keyid;
            public UInt16[] delta;
            public UInt16[] maxdelta;
            public UInt16[] rawdata;
            public UInt16[] refdata;
            public bool[] keystatus;
            public int hbcounter;
            public int keycounter;
        }

        public struct monitoring_start_cmd_list
        {
            public byte cmd;
            public byte crc;
        }

        private byte current_slave;
        private update_data current_update_data;

        private byte no_of_slaves;
        private byte[][] keymap;
        private byte[][] sorted_keymap;
        private monitoring_data[] key_data;

        private byte[] echo_response_count;
        private monitoring_start_cmd_list[] start_cmd_list;
        private byte[] start_cmd_indx;

        private bool bMonitoring;
        private enable_data_monitoring[] mon_param_enabled;
        private int keys_per_read_request;
        private int[] raw_start_address;
        private int[] raw_end_address;
        private int[] ref_start_address;
        private int[] ref_end_address;
        private int[] delta_start_address;
        private int[] delta_end_address;
        private int[] max_min_delta_start_address;
        private int[] max_min_delta_end_address;

        public DataMonitoringModel(TouchCommanderController c) : base(c)
        {
            current_slave = 0;
            current_update_data = update_data.DELTA;
            no_of_slaves = control.GetNumberOfSlaves();
            byte[][] km = control.GetKeyMapping();

            keymap = new byte[no_of_slaves][];
            sorted_keymap = new byte[no_of_slaves][];
            for (int i = 0; i < no_of_slaves; i++)
            {
                keymap[i] = new byte[km[i].Count()];
                sorted_keymap[i] = new byte[km[i].Count()];
                Array.Copy(km[i], keymap[i], km[i].Count());
                Array.Copy(km[i], sorted_keymap[i], km[i].Count());
                Array.Sort(sorted_keymap[i]);
            }

            echo_response_count = new byte[no_of_slaves];
            key_data = new monitoring_data[no_of_slaves];
            for (int i = 0; i < no_of_slaves; i++)
            {
                byte num_keys = (byte)keymap[i].Count();
                key_data[i].logicalid = new byte[num_keys];
                key_data[i].keyid = new byte[num_keys];
                Array.Copy(keymap[i], key_data[i].keyid, num_keys);
                key_data[i].delta = new UInt16[num_keys];
                key_data[i].maxdelta = new UInt16[num_keys];
                key_data[i].rawdata = new UInt16[num_keys];
                key_data[i].refdata = new UInt16[num_keys];
                key_data[i].keystatus = new bool[num_keys];
                echo_response_count[i] = 0;
            }

            start_cmd_list = new monitoring_start_cmd_list[]
             {
                new monitoring_start_cmd_list() { cmd = 0x21, crc = 0xB7 },
                new monitoring_start_cmd_list() { cmd = 0x16, crc = 0xE5 },
                new monitoring_start_cmd_list() { cmd = 0x18, crc = 0xFA },
                new monitoring_start_cmd_list() { cmd = 0x1B, crc = 0xA9 },
             };
            start_cmd_indx = new byte[] { 0, 0 };

            enable_monitoring(false);
            mon_param_enabled = new enable_data_monitoring[no_of_slaves];
            keys_per_read_request = 24;

            raw_start_address = new int[no_of_slaves];
            raw_end_address = new int[no_of_slaves];
            ref_start_address = new int[no_of_slaves];
            ref_end_address = new int[no_of_slaves];
            delta_start_address = new int[no_of_slaves];
            delta_end_address = new int[no_of_slaves];
            max_min_delta_start_address = new int[no_of_slaves];
            max_min_delta_end_address = new int[no_of_slaves];
            for (int i = 0; i < no_of_slaves; i++)
            {
                if (control.GetDeviceType((byte)i) == "Rx130INT" && control.GetSensingType((byte)i) == "Self")
                {
                    raw_start_address[i] = 0x0056;
                    raw_end_address[i] = 0x006B;
                    ref_start_address[i] = 0x006C;
                    ref_end_address[i] = 0x0081;
                    delta_start_address[i] = 0x002A;
                    delta_end_address[i] = 0x003F;
                    max_min_delta_start_address[i] = 0x0040;
                    max_min_delta_end_address[i] = 0x0055;
                }
                else
                {
                    raw_start_address[i] = 0x008A;
                    raw_end_address[i] = 0x00B9;
                    ref_start_address[i] = 0x00BA;
                    ref_end_address[i] = 0x00E9;
                    delta_start_address[i] = 0x002A;
                    delta_end_address[i] = 0x0059;
                    max_min_delta_start_address[i] = 0x005A;
                    max_min_delta_end_address[i] = 0x0089;
                }
            }
        }

        public override void updateModel(Object d)
        {
            byte[] data = (byte[])d;
            UInt16 offset = (UInt16)((data[2] << 8) | data[3]);
            byte slave_id = control.GetSlaveIdFromI2CAddress((byte)(data[1] >> 1));
            current_slave = slave_id;

            if (data[4] == 0 && (offset >= delta_start_address[slave_id] && offset <= delta_end_address[slave_id]))
            {
                int skip = (offset - delta_start_address[slave_id]) / 2;
                int data_for_num_keys = (data.Length - 6) / 2;
                for (int i = 0; i < data_for_num_keys; i++)
                {
                    byte physical_keyid = get_physical_key_mapping(current_slave, (byte)(skip + i));
                    key_data[current_slave].delta[physical_keyid] = (UInt16)((data[6 + (i * 2) + 1] << 8) | data[6 + (i * 2)]);
                }
                current_update_data = update_data.DELTA;
            }
            else if (data[4] == 0 && (offset >= max_min_delta_start_address[slave_id] && offset <= max_min_delta_end_address[slave_id]))
            {
                int skip = (offset - max_min_delta_start_address[slave_id]) / 2;
                int data_for_num_keys = (data.Length - 6) / 2;
                for (int i = 0; i < data_for_num_keys; i++)
                {
                    byte physical_keyid = get_physical_key_mapping(current_slave, (byte)(skip + i));
                    key_data[current_slave].maxdelta[physical_keyid] = (UInt16)((data[6 + (i * 2) + 1] << 8) | data[6 + (i * 2)]);
                }
                current_update_data = update_data.MAX_DELTA;
            }
            else if (data[4] == 0 && (offset >= raw_start_address[slave_id] && offset <= raw_end_address[slave_id]))
            {
                int skip = (offset - raw_start_address[slave_id]) / 2;
                int data_for_num_keys = (data.Length - 6) / 2;
                for (int i = 0; i < data_for_num_keys; i++)
                {
                    byte physical_keyid = get_physical_key_mapping(current_slave, (byte)(skip + i));
                    key_data[current_slave].rawdata[physical_keyid] = (UInt16)((data[6 + (i * 2) + 1] << 8) | data[6 + (i * 2)]);
                }
                current_update_data = update_data.RAW_DATA;
            }
            else if (data[4] == 0 && (offset >= ref_start_address[slave_id] && offset <= ref_end_address[slave_id]))
            {
                int skip = (offset - ref_start_address[slave_id]) / 2;
                int data_for_num_keys = (data.Length - 6) / 2;
                for (int i = 0; i < data_for_num_keys; i++)
                {
                    byte physical_keyid = get_physical_key_mapping(current_slave, (byte)(skip + i));
                    key_data[current_slave].refdata[physical_keyid] = (UInt16)((data[6 + (i * 2) + 1] << 8) | data[6 + (i * 2)]);
                }
                current_update_data = update_data.REF_DATA;
            }
            else if (data.Length == 30)
            {
                //Console.WriteLine(String.Format("Received 30 Bytes: {0}, {1}, {2}, {3}, {4}, {5}", data[0], data[1], data[2], data[3], data[4], data[5]));
            }
            else if (data.Length == 18)
            {
                //Console.WriteLine(String.Format("Received 18 Bytes: {0}, {1}, {2}, {3}, {4}, {5}", data[0], data[1], data[2], data[3], data[4], data[5]));
                if (data[3] == 0x0E) //Key Status
                {
                    for (int i = 0; i < keymap[slave_id].Count(); i++)
                    {
                        int keyid = keymap[slave_id][i];

                        if (keyid < 8)
                        {
                            int status = (data[6] >> keyid) & 1;
                            key_data[slave_id].keystatus[i] = status == 1 ? true : false;
                        }
                        else if (keyid < 16)
                        {
                            int status = (data[7] >> (keyid - 8)) & 1;
                            key_data[slave_id].keystatus[i] = status == 1 ? true : false;
                        }
                        else if (keyid < 24)
                        {
                            int status = (data[8] >> (keyid - 16)) & 1;
                            key_data[slave_id].keystatus[i] = status == 1 ? true : false;
                        }
                    }
                    current_update_data = update_data.KEY_STATUS;
                    QueueDataMonitoringCmds(slave_id);
                }
            }
            else if (data.Length == 6)
            {
                //Console.WriteLine(String.Format("{0}, {1}, {2}, {3}, {4}, {5}", data[0], data[1], data[2], data[3], data[4], data[5]));
            }
            else if (data.Length == 9)
            {
                //Console.WriteLine(String.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}",
                //                                data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8]));

                if (slave_id < no_of_slaves)
                {
                    if ((data[6] == start_cmd_list[start_cmd_indx[slave_id]].cmd) && (data[8] == 0))
                    {
                        //start_cmd_indx[slave_id]++;
                        start_cmd_indx[slave_id] = get_next_start_cmd_index(slave_id, start_cmd_indx[slave_id]);
                        if (start_cmd_indx[slave_id] > start_cmd_list.Count())
                        {
                            start_cmd_indx[slave_id] = 0;
                            QueueDataMonitoringCmds(slave_id);
                            return;
                        }
                    }
                    QueueStartMonitoringCmds(slave_id, start_cmd_list[start_cmd_indx[slave_id]].cmd, start_cmd_list[start_cmd_indx[slave_id]].crc);
                }
            }
            else
            {
                Console.WriteLine(String.Format("Received [{0}] bytes", data.Length));
            }
        }

        public override void updateModel(Object d, string section)
        {
            byte[] data = (byte[])d;

            if (section == "Heart Beat" && data.Length == (2 + (4 * no_of_slaves)))
            {
                for (int i = 0; i < no_of_slaves; i++)
                {
                    key_data[i].hbcounter = (data[2 + (4 * i) + 1] << 8) | data[2 + (4 * i)];
                    key_data[i].keycounter = (data[4 + (4 * i) + 1] << 8) | data[4 + (4 * i)];
                }
            }
        }

        public void StartDataMonitoring()
        {
            enable_monitoring(true);
            for (byte i = 0; i < no_of_slaves; i++)
            {
                start_cmd_indx[i] = get_next_start_cmd_index(i);
                if (start_cmd_indx[i] < start_cmd_list.Count())
                {
                    QueueStartMonitoringCmds(i, start_cmd_list[start_cmd_indx[i]].cmd, start_cmd_list[start_cmd_indx[i]].crc);
                }
                /*else
                {
                    QueueDataMonitoringCmds(i);
                }
                */
            }
        }

        public void StopDataMonitoring()
        {
            QueueStopMonitoringCmds();
        }

        private void QueueStartMonitoringCmds(byte slaveid, byte cmd, byte crc)
        {
            control.SendI2CCommand("Data Monitoring", slaveid, cmd, crc);
            control.SendI2CReadCommand("Data Monitoring", slaveid, "Primary", (UInt16)(0x0003), 3);
        }

        private void QueueStopMonitoringCmds()
        {
            control.ClearCommandQueue();

            for (byte i = 0; i < no_of_slaves; i++)
            {
                if (control.GetActualDeviceType((byte)i) != "RX130")
                {
                    control.SendI2CCommand("Data Monitoring", i, 0x0B, 0xEA);
                }
            }

            enable_monitoring(false);
        }

        private void QueueCommands(byte slaveid, byte num_keys, UInt16 start_address)
        {
            int num_cmds_to_queue = (num_keys / keys_per_read_request) + 1;

            for (int i = 0; i < num_cmds_to_queue; i++)
            {
                if (num_keys - (i * keys_per_read_request) < keys_per_read_request)
                {
                    control.SendI2CReadCommand("Data Monitoring", slaveid, "Primary", (UInt16)(start_address + (i * keys_per_read_request * 2)),
                        (byte)((num_keys - (i * keys_per_read_request)) * 2));
                }
                else
                {
                    control.SendI2CReadCommand("Data Monitoring", slaveid, "Primary", (UInt16)(start_address + (i * keys_per_read_request * 2)),
                        (byte)(keys_per_read_request * 2));
                }
            }
        }

        private void QueueDataMonitoringCmds(byte slaveid)
        {
            if (is_monitoring_enabled() == true)
            {
                byte num_keys = (byte)keymap[slaveid].Count();

                if (mon_param_enabled[slaveid].delta_enable == true)
                {
                    QueueCommands(slaveid, num_keys, (ushort)delta_start_address[slaveid]);
                }

                if (mon_param_enabled[slaveid].maxdelta_enable == true)
                {
                    QueueCommands(slaveid, num_keys, (ushort)max_min_delta_start_address[slaveid]);
                }

                if (mon_param_enabled[slaveid].raw_enable == true)
                {
                    QueueCommands(slaveid, num_keys, (ushort)raw_start_address[slaveid]);
                }

                if (mon_param_enabled[slaveid].baseline_enable == true)
                {
                    QueueCommands(slaveid, num_keys, (ushort)ref_start_address[slaveid]);
                }

                if (mon_param_enabled[slaveid].keystatus_enable == true)
                {
                    control.SendI2CReadCommand("Data Monitoring", slaveid, "Primary", (UInt16)(0x000E), 12);
                }
            }
        }

        private byte get_physical_key_mapping(byte slaveid, byte position)
        {
            byte physical_keyid = sorted_keymap[slaveid][position];
            int indx = Array.IndexOf(keymap[slaveid], physical_keyid);
            return (byte)indx;
        }

        private bool is_monitoring_enabled()
        {
            return bMonitoring;
        }

        private void enable_monitoring(bool enable)
        {
            bMonitoring = enable;
        }

        private byte get_next_start_cmd_index(byte slaveid)
        {
            return get_next_start_cmd_index(slaveid, 255);
        }
        private byte get_next_start_cmd_index(byte slaveid, byte current_indx)
        {
            byte temp_indx = (byte)(current_indx == 255 ? 0 : current_indx + 1);

            while (temp_indx < start_cmd_list.Count())
            {
                if (temp_indx == 0 && mon_param_enabled[slaveid].delta_enable == true)
                    return temp_indx;
                else if (temp_indx == 1 && mon_param_enabled[slaveid].maxdelta_enable == true)
                    return temp_indx;
                else if (temp_indx == 2 && mon_param_enabled[slaveid].raw_enable == true)
                    return temp_indx;
                else if (temp_indx == 3 && mon_param_enabled[slaveid].baseline_enable == true)
                    return temp_indx;

                temp_indx++;
            }
            return 255;
        }

        public byte GetKeysPerSlave(byte slaveid)
        {
            return (byte)(key_data[slaveid].keyid.Length);
        }

        public byte[] GetPhysicalKeyId(byte slaveid)
        {
            return key_data[slaveid].keyid;
        }

        public UInt16[] GetDelta(byte slaveid)
        {
            return key_data[slaveid].delta;
        }

        public UInt16[] GetMaxDelta(byte slaveid)
        {
            return key_data[slaveid].maxdelta;
        }

        public UInt16[] GetRaw(byte slaveid)
        {
            return key_data[slaveid].rawdata;
        }

        public UInt16[] GetBaseline(byte slaveid)
        {
            return key_data[slaveid].refdata;
        }

        public bool[] GetKeyStatus(byte slaveid)
        {
            return key_data[slaveid].keystatus;
        }

        public int GetHearBeatCounter(byte slaveid)
        {
            return key_data[slaveid].hbcounter;
        }

        public int GetKeyPressCounter(byte slaveid)
        {
            return key_data[slaveid].keycounter;
        }

        public byte GetCurrentSlave()
        {
            return current_slave;
        }

        public update_data GetUpdateDataField()
        {
            return current_update_data;
        }

        public void set_enable_data_monitoring(byte slaveid, enable_data_monitoring enable)
        {
            mon_param_enabled[slaveid] = enable;
        }

        public void set_num_keys_per_request_data(int num)
        {
            keys_per_read_request = num;
        }
    }

    public class DataMonitoringTabPage : TouchCommanderTabBase
    {
        public struct monitoring_data_row
        {
            public Label lbLogicalSensorId;
            public Label lbSensorId;
            public Label lbRaw;
            public Label lbBaseline;
            public Label lbDelta;
            // public Label lbMaxdeltaFW;      // HIDDEN - Not needed in UI
            public Label lbMaxdelta;           // Tool-calculated Max Delta (renamed from lbMaxdelta)
            public Label lbKeystatus;
        };

        public struct monitoring_data_enable
        {
            public int slaveid;
            public CheckBox cbRaw;
            public CheckBox cbBaseline;
            public CheckBox cbDelta;
            public CheckBox cbMaxdelta;         // Tool Max Delta checkbox (only one needed)
            //public CheckBox cbKeystatus;
        }

        public struct monitoring_view
        {
            public GroupBox gbSlaveId;
            public Label lbHeartBeatKeyCounter;
            public monitoring_data_enable data_enable;
            public monitoring_data_row[] sensor_data;
            public TableLayoutPanel tableview;
        };

        private byte no_of_slaves;
        private bool bViewInitComplete;
        private bool bMonitoringStarted;
        private DataMonitoringModel model;

        private monitoring_view[] monitoring_views;
        private enable_data_monitoring[] mon_param_enabled;
        private byte[] keys_per_slave;

        private Button btnStart;
        private Button btnLogging;
        private Button btnLogBrowse;
        private Button btnClearMaxDelta;
        private Label lbLogTimer;
        private Label lbSelNumKeysToRequest;
        private ComboBox cbSelNumKeysToRequest;
        private bool bLoggingEnabled;
        private StreamWriter logstream;
        private Stopwatch logStopWatch;
        private System.Timers.Timer timer;
        private delegate void SafeCallDelegate(string text);

        private Dictionary<int, Dictionary<int, ushort>> maxDeltaValues;
        // =====================================================================
        // OPTIMIZATION: Previous value arrays for change detection
        // =====================================================================
        private UInt16[][] prevDelta;
        private UInt16[][] prevMaxDelta;
        private UInt16[][] prevRaw;
        private UInt16[][] prevRef;
        private bool[][] prevKeyStatus;

        public DataMonitoringTabPage(TouchCommanderController c) : base(c)
        {
            this.Text = "Touch Data Monitoring";
            this.bViewInitComplete = false;
            // Initialize UI-side Max Delta storage
            maxDeltaValues = new Dictionary<int, Dictionary<int, ushort>>();
            for (int i = 0; i < no_of_slaves; i++)
            {
                maxDeltaValues[i] = new Dictionary<int, ushort>();
                for (int j = 0; j < 24; j++) // Max 24 sensors per slave
                {
                    maxDeltaValues[i][j] = 0;
                }
            }
            model = (DataMonitoringModel)control.GetModel("Data Monitoring");
            no_of_slaves = control.GetNumberOfSlaves();
            keys_per_slave = new byte[no_of_slaves];
            bool enable_view = true;

            for (int i = 0; i < no_of_slaves; i++)
            {
                String device_type = control.GetDeviceType((byte)i);
                String sensing_type = control.GetSensingType((byte)i);

                if (device_type == "Rx130INT" && sensing_type == "Self")
                {
                    //Do nothing
                }
                else if ((device_type != "PSoC4000S" && device_type != "PSoC4100S") || sensing_type != "Mutual")
                {
                    enable_view = false;
                    //                    MessageBox.Show(String.Format("WARNING!!!: Touch Data Monitoring View is not yet implemented for [ {0}, {1} ] combination", device_type, sensing_type),
                    //                            "WARNING!!!");
                    Label lbwarning = new Label();
                    lbwarning.Location = new Point(10, 10);
                    lbwarning.Size = new Size(800, 30);
                    lbwarning.Text = String.Format("Touch Data Monitoring View is not yet implemented for [ {0}, {1} ] combination", device_type, sensing_type);
                    this.Controls.Add(lbwarning);
                    break;
                }
                keys_per_slave[i] = model.GetKeysPerSlave((byte)i);
            }

            if (enable_view == true)
            {
                initView();
                bLoggingEnabled = false;
                this.logstream = null;

                logStopWatch = new Stopwatch();
                logStopWatch.Reset();

                timer = new System.Timers.Timer();
                timer.Interval = 1000;
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = false;
            }
        }

        private void initView()
        {
            btnStart = new Button();
            btnStart.Location = new Point(20, 10);
            btnStart.Size = new Size(150, 30);
            btnStart.Font = new Font(btnStart.Font.Name, btnStart.Font.Size + 2.0F, btnStart.Font.Style, btnStart.Font.Unit);
            btnStart.Text = "Start";
            btnStart.BackColor = Color.Khaki;
            btnStart.Click += new EventHandler(this.btnStart_Click);
            this.Controls.Add(btnStart);

            btnLogging = new Button();
            btnLogging.Location = new Point(200, 10);
            btnLogging.Size = new Size(150, 30);
            btnLogging.Font = new Font(btnLogging.Font.Name, btnLogging.Font.Size + 2.0F, btnLogging.Font.Style, btnLogging.Font.Unit);
            btnLogging.Text = "Enable Logging";
            btnLogging.BackColor = Color.Khaki;
            btnLogging.Click += new EventHandler(this.btnLogging_Click);
            this.Controls.Add(btnLogging);

            lbLogTimer = new Label();
            lbLogTimer.Text = "Log Time: NA";
            lbLogTimer.Location = new Point(370, 18);
            lbLogTimer.Font = new Font(lbLogTimer.Font.Name, lbLogTimer.Font.Size + 2.0F, lbLogTimer.Font.Style, lbLogTimer.Font.Unit);
            lbLogTimer.AutoSize = true;
            this.Controls.Add(lbLogTimer);

            // Clear Max Delta Button
            btnClearMaxDelta = new Button();
            btnClearMaxDelta.Location = new Point(730, 10);
            btnClearMaxDelta.Size = new Size(150, 30);
            btnClearMaxDelta.Font = new Font(btnClearMaxDelta.Font.Name, btnClearMaxDelta.Font.Size + 2.0F, btnClearMaxDelta.Font.Style, btnClearMaxDelta.Font.Unit);
            btnClearMaxDelta.Text = "Clear Max Delta";
            btnClearMaxDelta.BackColor = Color.LightCoral;
            btnClearMaxDelta.Click += new EventHandler(this.btnClearMaxDelta_Click);
            this.Controls.Add(btnClearMaxDelta);

            btnLogBrowse = new Button();
            btnLogBrowse.Location = new Point(570, 10);
            btnLogBrowse.Size = new Size(150, 30);
            btnLogBrowse.Font = new Font(btnLogBrowse.Font.Name, btnLogBrowse.Font.Size + 2.0F, btnLogBrowse.Font.Style, btnLogBrowse.Font.Unit);
            btnLogBrowse.Text = "Browse Logs";
            btnLogBrowse.BackColor = Color.Khaki;
            btnLogBrowse.Click += new EventHandler(this.btnLogBrowse_Click);
            this.Controls.Add(btnLogBrowse);

            lbSelNumKeysToRequest = new Label();
            lbSelNumKeysToRequest.Text = "Number of keys to request data for in single read command";
            lbSelNumKeysToRequest.Location = new Point(770, 18);
            lbSelNumKeysToRequest.Font = new Font(lbSelNumKeysToRequest.Font.Name, lbSelNumKeysToRequest.Font.Size + 2.0F,
                                                   lbSelNumKeysToRequest.Font.Style, lbSelNumKeysToRequest.Font.Unit);
            lbSelNumKeysToRequest.AutoSize = true;
            this.Controls.Add(lbSelNumKeysToRequest);

            cbSelNumKeysToRequest = new ComboBox();
            cbSelNumKeysToRequest.Location = new Point(1180, 15);
            cbSelNumKeysToRequest.Size = new Size(200, 10);
            cbSelNumKeysToRequest.Font = new Font(cbSelNumKeysToRequest.Font.Name, cbSelNumKeysToRequest.Font.Size + 2.0F,
                                                   cbSelNumKeysToRequest.Font.Style, cbSelNumKeysToRequest.Font.Unit);
            cbSelNumKeysToRequest.AutoSize = true;
            cbSelNumKeysToRequest.Items.Add("Upto 24 Keys");
            cbSelNumKeysToRequest.Items.Add("Upto 12 Keys");
            cbSelNumKeysToRequest.SelectedIndex = cbSelNumKeysToRequest.Items.IndexOf("Upto 24 Keys");
            this.cbSelNumKeysToRequest.SelectedIndexChanged += new System.EventHandler(cbSelNumKeysToRequest_SelectedIndexChanged);
            this.Controls.Add(cbSelNumKeysToRequest);
            model.set_num_keys_per_request_data(24);

            monitoring_views = new monitoring_view[no_of_slaves];
            mon_param_enabled = new enable_data_monitoring[no_of_slaves];

            byte logical_id_offset = 0;

            for (int i = 0; i < no_of_slaves; i++)
            {
                monitoring_views[i].data_enable = new monitoring_data_enable();
                monitoring_views[i].sensor_data = new monitoring_data_row[24];

                monitoring_views[i].gbSlaveId = new GroupBox();
                monitoring_views[i].gbSlaveId.Location = new Point(20 + (720 * i), 50);
                monitoring_views[i].gbSlaveId.Size = new Size(700, 730);
                monitoring_views[i].gbSlaveId.Text = String.Format("Slave {0}", i);
                monitoring_views[i].gbSlaveId.Font = new Font(monitoring_views[i].gbSlaveId.Font.Name, monitoring_views[i].gbSlaveId.Font.Size + 2.0F,
                                                                monitoring_views[i].gbSlaveId.Font.Style, monitoring_views[i].gbSlaveId.Font.Unit);
                this.Controls.Add(monitoring_views[i].gbSlaveId);

                monitoring_views[i].lbHeartBeatKeyCounter = new Label();
                monitoring_views[i].lbHeartBeatKeyCounter.Location = new Point(40, 40);
                monitoring_views[i].lbHeartBeatKeyCounter.Size = new Size(500, 30);
                monitoring_views[i].lbHeartBeatKeyCounter.Text = String.Format("Heart Beat Counter [ {0} ]          Key Press Counter [ {1} ]", 0, 0);
                monitoring_views[i].gbSlaveId.Controls.Add(monitoring_views[i].lbHeartBeatKeyCounter);

                monitoring_views[i].tableview = new TableLayoutPanel();
                monitoring_views[i].tableview.Location = new Point(40, 70);
                monitoring_views[i].tableview.Size = new Size(600, 630);
                monitoring_views[i].tableview.ColumnCount = 7;
                monitoring_views[i].tableview.RowCount = 1;
                //monitoring_views[i].tableview.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
                monitoring_views[i].tableview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));  // Logical Id
                monitoring_views[i].tableview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));  // Sensor Id
                monitoring_views[i].tableview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));  // Raw
                monitoring_views[i].tableview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));  // Ref
                monitoring_views[i].tableview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));  // Delta
                monitoring_views[i].tableview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));  // MaxDelta
                monitoring_views[i].tableview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));  // Status

                monitoring_views[i].tableview.RowStyles.Add(new RowStyle(SizeType.AutoSize, 80F));
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "", TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "", TextAlign = ContentAlignment.MiddleCenter }, 1, 0);

                int slaveid = i;
                monitoring_views[i].data_enable.cbRaw = new CheckBox() { CheckAlign = ContentAlignment.MiddleCenter, Checked = true };
                monitoring_views[i].data_enable.cbRaw.CheckedChanged += delegate (object sender, EventArgs e)
                { CheckBox_CheckedChanged(sender, e, "Raw", slaveid); };
                monitoring_views[i].tableview.Controls.Add(monitoring_views[i].data_enable.cbRaw, 2, 0);
                mon_param_enabled[i].raw_enable = monitoring_views[i].data_enable.cbRaw.Checked;

                monitoring_views[i].data_enable.cbBaseline = new CheckBox() { CheckAlign = ContentAlignment.MiddleCenter, Checked = true };
                monitoring_views[i].data_enable.cbBaseline.CheckedChanged += delegate (object sender, EventArgs e)
                { CheckBox_CheckedChanged(sender, e, "Baseline", slaveid); };
                monitoring_views[i].tableview.Controls.Add(monitoring_views[i].data_enable.cbBaseline, 3, 0);
                mon_param_enabled[i].baseline_enable = monitoring_views[i].data_enable.cbBaseline.Checked;

                monitoring_views[i].data_enable.cbDelta = new CheckBox() { CheckAlign = ContentAlignment.MiddleCenter, Checked = true };
                monitoring_views[i].data_enable.cbDelta.CheckedChanged += delegate (object sender, EventArgs e)
                { CheckBox_CheckedChanged(sender, e, "Delta", slaveid); };
                monitoring_views[i].tableview.Controls.Add(monitoring_views[i].data_enable.cbDelta, 4, 0);
                mon_param_enabled[i].delta_enable = monitoring_views[i].data_enable.cbDelta.Checked;

                monitoring_views[i].data_enable.cbMaxdelta = new CheckBox() { CheckAlign = ContentAlignment.MiddleCenter, Checked = true };
                monitoring_views[i].data_enable.cbMaxdelta.CheckedChanged += delegate (object sender, EventArgs e)
                { CheckBox_CheckedChanged(sender, e, "Maxdelta", slaveid); };
                monitoring_views[i].tableview.Controls.Add(monitoring_views[i].data_enable.cbMaxdelta, 5, 0);

                // Add checkbox event handler for MaxΔ(Tool)
                monitoring_views[i].data_enable.cbMaxdelta.Tag = i;
                monitoring_views[i].data_enable.cbMaxdelta.CheckedChanged += delegate (object sender, EventArgs e)
                {
                    CheckBox cb = sender as CheckBox;
                    int slaveIndex = (int)cb.Tag;  // Use different variable name to avoid conflict

                    // Update background color of all MaxΔ(Tool) labels for this slave
                    for (int j = 0; j < monitoring_views[slaveIndex].sensor_data.Length; j++)
                    {
                        if (monitoring_views[slaveIndex].sensor_data[j].lbMaxdelta != null)
                        {
                            monitoring_views[slaveIndex].sensor_data[j].lbMaxdelta.BackColor = cb.Checked ? Color.White : Color.LightGray;
                        }
                    }
                };
                mon_param_enabled[i].maxdelta_enable = monitoring_views[i].data_enable.cbMaxdelta.Checked;
                /*
                monitoring_views[i].data_enable.cbKeystatus = new CheckBox() { CheckAlign = ContentAlignment.MiddleCenter, Checked = true };
                monitoring_views[i].data_enable.cbKeystatus.CheckedChanged += delegate (object sender, EventArgs e)
                { CheckBox_CheckedChanged(sender, e, "Keystatus", slaveid); };
                monitoring_views[i].tableview.Controls.Add(monitoring_views[i].data_enable.cbKeystatus, 6, 0);
                mon_param_enabled[i].keystatus_enable = monitoring_views[i].data_enable.cbKeystatus.Checked;
                */
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "", TextAlign = ContentAlignment.MiddleCenter }, 6, 0);
                mon_param_enabled[i].keystatus_enable = true;

                model.set_enable_data_monitoring((byte)i, mon_param_enabled[i]);

                monitoring_views[i].tableview.RowCount += 1;
                monitoring_views[i].tableview.RowStyles.Add(new RowStyle(SizeType.AutoSize, 80F));
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "Logical Id", TextAlign = ContentAlignment.MiddleCenter }, 0, 1);
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "Sensor Id", TextAlign = ContentAlignment.MiddleCenter }, 1, 1);
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "Raw", TextAlign = ContentAlignment.MiddleCenter }, 2, 1);
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "Ref", TextAlign = ContentAlignment.MiddleCenter }, 3, 1);
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "Delta", TextAlign = ContentAlignment.MiddleCenter }, 4, 1);
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "MaxDelta", TextAlign = ContentAlignment.MiddleCenter }, 5, 1);
                monitoring_views[i].tableview.Controls.Add(new Label() { Text = "Status", TextAlign = ContentAlignment.MiddleCenter }, 6, 1);

                byte[] physical_keyid = model.GetPhysicalKeyId((byte)i);

                for (int j = 0; j < physical_keyid.Length; j++)
                {
                    monitoring_views[i].tableview.RowCount += 1;
                    monitoring_views[i].tableview.RowStyles.Add(new RowStyle(SizeType.AutoSize, 80F));

                    monitoring_views[i].sensor_data[j].lbLogicalSensorId = new Label() { Text = String.Format("{0}", logical_id_offset + j), TextAlign = ContentAlignment.MiddleCenter };
                    monitoring_views[i].tableview.Controls.Add(monitoring_views[i].sensor_data[j].lbLogicalSensorId, 0, monitoring_views[i].tableview.RowCount - 1);

                    monitoring_views[i].sensor_data[j].lbSensorId = new Label() { Text = String.Format("{0}", physical_keyid[j]), TextAlign = ContentAlignment.MiddleCenter };
                    monitoring_views[i].tableview.Controls.Add(monitoring_views[i].sensor_data[j].lbSensorId, 1, monitoring_views[i].tableview.RowCount - 1);

                    monitoring_views[i].sensor_data[j].lbRaw = new Label() { Text = String.Format("0"), TextAlign = ContentAlignment.MiddleCenter };
                    monitoring_views[i].tableview.Controls.Add(monitoring_views[i].sensor_data[j].lbRaw, 2, monitoring_views[i].tableview.RowCount - 1);

                    monitoring_views[i].sensor_data[j].lbBaseline = new Label() { Text = String.Format("0"), TextAlign = ContentAlignment.MiddleCenter };
                    monitoring_views[i].tableview.Controls.Add(monitoring_views[i].sensor_data[j].lbBaseline, 3, monitoring_views[i].tableview.RowCount - 1);

                    monitoring_views[i].sensor_data[j].lbDelta = new Label() { Text = String.Format("0"), TextAlign = ContentAlignment.MiddleCenter };
                    monitoring_views[i].tableview.Controls.Add(monitoring_views[i].sensor_data[j].lbDelta, 4, monitoring_views[i].tableview.RowCount - 1);

                    // Max Delta (Tool-calculated from Delta)
                    monitoring_views[i].sensor_data[j].lbMaxdelta = new Label() { Text = String.Format("0"), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.White };
                    monitoring_views[i].tableview.Controls.Add(monitoring_views[i].sensor_data[j].lbMaxdelta, 5, monitoring_views[i].tableview.RowCount - 1);

                    monitoring_views[i].sensor_data[j].lbKeystatus = new Label() { Text = String.Format("OFF"), TextAlign = ContentAlignment.MiddleCenter };
                    monitoring_views[i].tableview.Controls.Add(monitoring_views[i].sensor_data[j].lbKeystatus, 6, monitoring_views[i].tableview.RowCount - 1);
                }
                monitoring_views[i].gbSlaveId.Controls.Add(monitoring_views[i].tableview);
                logical_id_offset += keys_per_slave[i];
            }

            // =====================================================================
            // OPTIMIZATION: Initialize previous value arrays for change detection
            // =====================================================================
            prevDelta = new UInt16[no_of_slaves][];
            prevMaxDelta = new UInt16[no_of_slaves][];
            prevRaw = new UInt16[no_of_slaves][];
            prevRef = new UInt16[no_of_slaves][];
            prevKeyStatus = new bool[no_of_slaves][];

            for (int i = 0; i < no_of_slaves; i++)
            {
                int keyCount = keys_per_slave[i];
                prevDelta[i] = new UInt16[keyCount];
                prevMaxDelta[i] = new UInt16[keyCount];
                prevRaw[i] = new UInt16[keyCount];
                prevRef[i] = new UInt16[keyCount];
                prevKeyStatus[i] = new bool[keyCount];
            }

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

        private void log_file_handler(bool start_logging)
        {
            if (start_logging)
            {
                string LogHeader = "TimeStamp";
                string LogFolderPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string LogFileName = string.Format("CypressMonitoringLog-{0:yyyy-MM-dd_HH-mm-ss-fff}.csv", DateTime.Now);
                string LogFilePath = System.IO.Path.Combine(LogFolderPath, LogFileName);
                this.logstream = File.AppendText(LogFilePath);

                for (int slaveid = 0; slaveid < no_of_slaves; slaveid++)
                {
                    for (int i = 0; i < model.GetKeysPerSlave((byte)slaveid); i++)
                    {
                        String LogHeaderFields = "";
                        if (monitoring_views[slaveid].data_enable.cbRaw.Checked)
                        {
                            LogHeaderFields += ",Raw";
                        }
                        if (monitoring_views[slaveid].data_enable.cbBaseline.Checked)
                        {
                            LogHeaderFields += ",Ref";
                        }
                        if (monitoring_views[slaveid].data_enable.cbDelta.Checked)
                        {
                            LogHeaderFields += ",Delta";
                        }
                        if (monitoring_views[slaveid].data_enable.cbMaxdelta.Checked)
                        {
                            LogHeaderFields += ",MaxDelta";
                        }
                        if (LogHeaderFields != "")
                        {
                            LogHeader += ",KeyNumber,SlaveId,ActualSensor-" + monitoring_views[slaveid].sensor_data[i].lbLogicalSensorId.Text;
                            LogHeader += LogHeaderFields;
                            LogHeader += ",KeyStatus";
                        }
                    }
                }
                LogHeader += "\n";
                this.logstream.Write(LogHeader);
                this.logstream.Flush();
                logStopWatch.Restart();
                timer.Enabled = true;
            }
            else
            {
                if (this.logstream != null)
                {
                    this.logstream.Close();
                    this.logstream = null;
                    logStopWatch.Stop();
                    timer.Enabled = false;
                }
            }
        }

        private void log_data_handler()
        {
            if (bLoggingEnabled)
            {
                string logstr = string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now);
                bool write_data = false;

                for (int slaveid = 0; slaveid < no_of_slaves; slaveid++)
                {
                    for (int i = 0; i < model.GetKeysPerSlave((byte)slaveid); i++)
                    {
                        String logFieldStr = "";

                        // Add KeyNumber, SlaveId, ActualSensor FIRST (previous format)
                        logFieldStr += string.Format(",{0},{1},{2}",
                            monitoring_views[slaveid].sensor_data[i].lbLogicalSensorId.Text,
                            slaveid,
                            monitoring_views[slaveid].sensor_data[i].lbSensorId.Text);

                        // Then add data fields
                        if (monitoring_views[slaveid].data_enable.cbRaw.Checked)
                        {
                            logFieldStr += "," + monitoring_views[slaveid].sensor_data[i].lbRaw.Text;
                        }
                        if (monitoring_views[slaveid].data_enable.cbBaseline.Checked)
                        {
                            logFieldStr += "," + monitoring_views[slaveid].sensor_data[i].lbBaseline.Text;
                        }
                        if (monitoring_views[slaveid].data_enable.cbDelta.Checked)
                        {
                            logFieldStr += "," + monitoring_views[slaveid].sensor_data[i].lbDelta.Text;
                        }
                        if (monitoring_views[slaveid].data_enable.cbMaxdelta.Checked)
                        {
                            logFieldStr += "," + monitoring_views[slaveid].sensor_data[i].lbMaxdelta.Text;
                        }

                        // KeyStatus as ON/OFF (not numbers)
                        string keyStatus = monitoring_views[slaveid].sensor_data[i].lbKeystatus.Text;
                        logFieldStr += "," + keyStatus;

                        if (logFieldStr != "")
                        {
                            logstr += logFieldStr;
                            write_data = true;
                        }
                    }
                }
                if (write_data && this.logstream != null)
                {
                    this.logstream.Write("{0}\n", logstr);
                    this.logstream.Flush();
                }
            }
        }

        private void btnStart_Click(Object sender, EventArgs e)
        {
            if (!bMonitoringStarted)
            {
                ClearMaxDelta();  // <----- ADD THIS LINE
                bMonitoringStarted = true;
                btnLogging.Enabled = false;
                btnStart.Text = "Stop";
                btnStart.BackColor = Color.LightGreen;
                model.StartDataMonitoring();
            }
            else
            {
                bMonitoringStarted = false;
                btnLogging.Enabled = true;
                btnStart.Text = "Start";
                btnStart.BackColor = Color.Khaki;
                model.StopDataMonitoring();
            }

            for (int i = 0; i < no_of_slaves; i++)
            {
                monitoring_views[i].data_enable.cbDelta.Enabled = !bMonitoringStarted;
                monitoring_views[i].data_enable.cbMaxdelta.Enabled = !bMonitoringStarted;
                monitoring_views[i].data_enable.cbMaxdelta.Enabled = !bMonitoringStarted;
                monitoring_views[i].data_enable.cbRaw.Enabled = !bMonitoringStarted;
                monitoring_views[i].data_enable.cbBaseline.Enabled = !bMonitoringStarted;
                //monitoring_views[i].data_enable.cbKeystatus.Enabled = !bMonitoringStarted;
            }
            if (bLoggingEnabled)
            {
                log_file_handler(bMonitoringStarted);
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

        private void cbSelNumKeysToRequest_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            string selectedEmployee = (string)comboBox.SelectedItem;

            if (selectedEmployee == "Upto 24 Keys")
            {
                model.set_num_keys_per_request_data(24);
            }
            else if (selectedEmployee == "Upto 12 Keys")
            {
                model.set_num_keys_per_request_data(12);
                MessageBox.Show("Reducing the number of keys to request in single command will result is slower updates of the data\n\n" +
                                "Select this option in case the project is configured to use a smaller WIN payload size (less than 60 bytes)");
            }
        }

        private void CheckBox_CheckedChanged(Object sender, EventArgs e, String name, int slaveid)
        {
            CheckBox cb = (CheckBox)sender;

            if (name == "Raw")
            {
                mon_param_enabled[slaveid].raw_enable = cb.Checked;
                for (int i = 0; i < keys_per_slave[slaveid]; i++)
                {
                    monitoring_views[slaveid].sensor_data[i].lbRaw.BackColor = cb.Checked ? default(Color) : Color.LightGray;
                }
            }
            else if (name == "Baseline")
            {
                mon_param_enabled[slaveid].baseline_enable = cb.Checked;
                for (int i = 0; i < keys_per_slave[slaveid]; i++)
                {
                    monitoring_views[slaveid].sensor_data[i].lbBaseline.BackColor = cb.Checked ? default(Color) : Color.LightGray;
                }
            }
            else if (name == "Delta")
            {
                mon_param_enabled[slaveid].delta_enable = cb.Checked;
                for (int i = 0; i < keys_per_slave[slaveid]; i++)
                {
                    monitoring_views[slaveid].sensor_data[i].lbDelta.BackColor = cb.Checked ? default(Color) : Color.LightGray;
                }
            }
            else if (name == "Maxdelta")
            {
                mon_param_enabled[slaveid].maxdelta_enable = cb.Checked;
                for (int i = 0; i < keys_per_slave[slaveid]; i++)
                {
                    // lbMaxdeltaFW removed - using single MaxDelta column
                    monitoring_views[slaveid].sensor_data[i].lbMaxdelta.BackColor = cb.Checked ? Color.White : Color.LightGray;
                }
            }
            else if (name == "Keystatus")
            {
                mon_param_enabled[slaveid].keystatus_enable = cb.Checked;
                for (int i = 0; i < keys_per_slave[slaveid]; i++)
                {
                    monitoring_views[slaveid].sensor_data[i].lbKeystatus.BackColor = cb.Checked ? default(Color) : Color.LightGray;
                }
            }

            model.set_enable_data_monitoring((byte)slaveid, mon_param_enabled[slaveid]);
        }

        // =====================================================================
        // OPTIMIZED updateView() - with change detection for faster updates
        // =====================================================================
        public override void updateView(Object data)
        {
            if (!bViewInitComplete || data == null) return;

            DataMonitoringModel model = (DataMonitoringModel)data;
            byte current_slave = model.GetCurrentSlave();

            // Validate slave index
            if (current_slave >= no_of_slaves) return;

            update_data current_update_data = model.GetUpdateDataField();

            // Get all data arrays once
            UInt16[] delta = model.GetDelta(current_slave);
            UInt16[] maxdelta = model.GetMaxDelta(current_slave);
            UInt16[] rawdata = model.GetRaw(current_slave);
            UInt16[] refdata = model.GetBaseline(current_slave);
            bool[] keystatus = model.GetKeyStatus(current_slave);

            int keyCount = keys_per_slave[current_slave];

            for (int i = 0; i < keyCount; i++)
            {
                switch (current_update_data)
                {
                    case update_data.DELTA:
                        if (monitoring_views[current_slave].data_enable.cbDelta.Checked)
                        {
                            ushort currentDelta = delta[i];

                            // Update Delta display
                            if (currentDelta != prevDelta[current_slave][i])
                            {
                                monitoring_views[current_slave].sensor_data[i].lbDelta.Text = String.Format("{0}", currentDelta);
                                prevDelta[current_slave][i] = currentDelta;
                            }

                            // Track and display Tool Max Delta directly here (simplified!)
                            try
                            {
                                // Initialize if needed
                                if (!maxDeltaValues.ContainsKey(current_slave))
                                {
                                    maxDeltaValues[current_slave] = new Dictionary<int, ushort>();
                                }
                                if (!maxDeltaValues[current_slave].ContainsKey(i))
                                {
                                    maxDeltaValues[current_slave][i] = 0;
                                }

                                // Track maximum (always track, regardless of checkbox)
                                if (currentDelta > maxDeltaValues[current_slave][i])
                                {
                                    maxDeltaValues[current_slave][i] = currentDelta;
                                }

                                // Display only if checkbox is checked
                                if (monitoring_views[current_slave].data_enable.cbMaxdelta.Checked)
                                {
                                    ushort toolMaxDelta = maxDeltaValues[current_slave][i];
                                    monitoring_views[current_slave].sensor_data[i].lbMaxdelta.Text = String.Format("{0}", toolMaxDelta);

                                    if (toolMaxDelta > 0)
                                        monitoring_views[current_slave].sensor_data[i].lbMaxdelta.BackColor = Color.LightYellow;
                                    else
                                        monitoring_views[current_slave].sensor_data[i].lbMaxdelta.BackColor = Color.White;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error updating Tool MaxDelta: {ex.Message}");
                            }
                        }
                        break;

                    case update_data.MAX_DELTA:
                        // MAX_DELTA from firmware is ignored
                        // Tool-side Max Delta is calculated in DELTA case
                        break;

                    case update_data.RAW_DATA:
                        if (monitoring_views[current_slave].data_enable.cbRaw.Checked)
                        {
                            if (rawdata[i] != prevRaw[current_slave][i])
                            {
                                monitoring_views[current_slave].sensor_data[i].lbRaw.Text = String.Format("{0}", rawdata[i]);
                                prevRaw[current_slave][i] = rawdata[i];
                            }
                        }
                        break;

                    case update_data.REF_DATA:
                        if (monitoring_views[current_slave].data_enable.cbBaseline.Checked)
                        {
                            if (refdata[i] != prevRef[current_slave][i])
                            {
                                monitoring_views[current_slave].sensor_data[i].lbBaseline.Text = String.Format("{0}", refdata[i]);
                                prevRef[current_slave][i] = refdata[i];
                            }
                        }
                        break;

                    case update_data.KEY_STATUS:
                        //if (monitoring_views[current_slave].data_enable.cbKeystatus.Checked == true)
                        {
                            // Always update for immediate response (no previous value check)
                            monitoring_views[current_slave].sensor_data[i].lbKeystatus.Text = String.Format("{0}", keystatus[i] == true ? "ON" : "OFF");
                            monitoring_views[current_slave].sensor_data[i].lbKeystatus.BackColor = keystatus[i] == true ? Color.LightGreen : default(Color);
                            prevKeyStatus[current_slave][i] = keystatus[i];

                            if (i == (keyCount - 1))
                            {
                                log_data_handler();
                            }
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// Clear Max Delta Button Click Handler
        /// </summary>
        private void btnClearMaxDelta_Click(object sender, EventArgs e)
        {
            ClearMaxDelta();
            MessageBox.Show("Max Delta values have been cleared for all slaves!",
                          "Clear Max Delta",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Information);
        }

        /// <summary>
        /// Clear Max Delta - UI Side Implementation
        /// </summary>
        private void ClearMaxDelta()
        {
            try
            {
                // Clear UI-side dictionary
                for (int slave = 0; slave < no_of_slaves; slave++)
                {
                    if (maxDeltaValues.ContainsKey(slave))
                    {
                        foreach (var key in maxDeltaValues[slave].Keys.ToList())
                        {
                            maxDeltaValues[slave][key] = 0;
                        }
                    }

                    // CRITICAL: Also clear previous value array
                    if (prevMaxDelta != null && slave < prevMaxDelta.Length)
                    {
                        for (int i = 0; i < prevMaxDelta[slave].Length; i++)
                        {
                            prevMaxDelta[slave][i] = 0;
                        }
                    }
                }

                // Clear the UI display
                ClearMaxDeltaUI();

                System.Diagnostics.Debug.WriteLine("✓ Max Delta cleared (dictionary + prevMaxDelta + UI)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing Max Delta: {ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clear Max Delta UI Labels
        /// </summary>
        private void ClearMaxDeltaUI()
        {
            try
            {
                for (int slave = 0; slave < no_of_slaves; slave++)
                {
                    if (monitoring_views != null && monitoring_views[slave].sensor_data != null)
                    {
                        for (int sensor = 0; sensor < monitoring_views[slave].sensor_data.Length; sensor++)
                        {
                            // lbMaxdelta no longer exists - now using lbMaxdelta
                            // (Already handled in updated ClearMaxDeltaUI method)
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing UI: {ex.Message}");
            }
        }

        public override void updateView(string section, Object data)
        {
            if (!bViewInitComplete)
            {
                return;
            }

            if ((data != null) && (section != "Stop Monitoring"))
            {
                DataMonitoringModel model = (DataMonitoringModel)data;

                if (section == "Heart Beat")
                {
                    for (int i = 0; i < no_of_slaves; i++)
                    {
                        monitoring_views[i].lbHeartBeatKeyCounter.Text = String.Format("Heart Beat Counter [ {0} ]          Key Press Counter [ {1} ]",
                            model.GetHearBeatCounter((byte)i), model.GetKeyPressCounter((byte)i));
                    }
                }
            }
            else if (section == "Stop Monitoring")
            {
                if (bMonitoringStarted)
                {
                    bMonitoringStarted = false;
                    btnLogging.Enabled = true;
                    btnStart.Text = "Start";
                    btnStart.BackColor = Color.Khaki;
                    model.StopDataMonitoring();

                    for (int i = 0; i < no_of_slaves; i++)
                    {
                        monitoring_views[i].data_enable.cbDelta.Enabled = true;
                        monitoring_views[i].data_enable.cbMaxdelta.Enabled = true;
                        monitoring_views[i].data_enable.cbMaxdelta.Enabled = true;
                        monitoring_views[i].data_enable.cbRaw.Enabled = true;
                        monitoring_views[i].data_enable.cbBaseline.Enabled = true;
                        //monitoring_views[i].data_enable.cbKeystatus.Enabled = true;
                    }
                }

                if (bLoggingEnabled)
                {
                    log_file_handler(bMonitoringStarted);
                }
            }
        }
    }
}