using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using WideBoxLib;

namespace TouchCommanderGenericNamespace
{

    public class ConfigDataModel
    {
        public byte[] dev_cfg;
        public byte[] cap_cfg;
        public byte[] cfg_crc;
        private string chip_type;
        private string capsense_type;
        public byte[] phy_keys;
        private int dev_cfg_size;
        private int dev_cfg_data_size;
        private int dev_cfg_padding_size;
        private int cap_cfg_size;
        private int cap_cfg_data_size;
        private int cap_cfg_padding_size;

        private UInt16 dev_cfg_start_offset;
        private UInt16 dev_cfg_crc_offset;
        private UInt16 cap_cfg_start_offset;
        private UInt16 cap_cfg_crc_offset;

        private UInt16 dev_cfg_crc_from_device;
        private UInt16 cap_cfg_crc_from_device;

        private byte[] tuning_data;
        private int tuning_data_size;

        public ConfigDataModel(string chip_type, string capsense_type)
        {
            this.chip_type = chip_type;
            this.capsense_type = capsense_type;

            if (chip_type == "Rx130" || chip_type == "Rx130INT")
            {
                dev_cfg_size = 160;
                dev_cfg = new byte[160];
                dev_cfg_data_size = 160;// 88;
                dev_cfg_padding_size = 72;
                dev_cfg_start_offset = 0x020C;
                dev_cfg_crc_offset = 0x02AC;


                cap_cfg_size = 512;
                cap_cfg = new byte[512];
                cap_cfg_data_size = 512;// 376;
                cap_cfg_padding_size = 136;
                cap_cfg_start_offset = 0x000C;
                cap_cfg_crc_offset = 0x02AE;

                cfg_crc = new byte[4];

                tuning_data = new byte[256];
                tuning_data_size = 0;
            }
            else if (chip_type == "PSoC4000S" || chip_type == "PSoC4100S")
            {
            }
            else
            {
            }
        }

        public void updateFullModel(UInt16 offset_addr, byte[] data, int length)
        {
            System.Buffer.BlockCopy(data, 0, cap_cfg, 0, cap_cfg_size);
            System.Buffer.BlockCopy(data, cap_cfg_size, dev_cfg, 0, dev_cfg_size);
            System.Buffer.BlockCopy(data, cap_cfg_size + dev_cfg_size, cfg_crc, 0, 4);
        }

        public void updateDevModel(UInt16 offset_addr, byte[] data, int length)
        {
            System.Buffer.BlockCopy(data, 0, dev_cfg, offset_addr, length);
            ushort CrcData = CalculateCrc16(dev_cfg, (ushort)dev_cfg_data_size, CCITT16_DEFAULT_SEED);
            cfg_crc[0] = (byte)(CrcData & 0xFF);
            cfg_crc[1] = (byte)((CrcData >> 8) & 0xFF);
        }

        public void updateCapModel(UInt16 offset_addr, byte[] data, int length)
        {
            System.Buffer.BlockCopy(data, 0, cap_cfg, offset_addr, length);
            ushort CrcData = CalculateCrc16(cap_cfg, (ushort)cap_cfg_data_size, CCITT16_DEFAULT_SEED);
            cfg_crc[2] = (byte)(CrcData & 0xFF);
            cfg_crc[3] = (byte)((CrcData >> 8) & 0xFF);
        }

        public void updateFromDeviceCRC(byte[] data, int length)
        {
            dev_cfg_crc_from_device = (UInt16)((data[1] << 8) | (data[0]));
            cap_cfg_crc_from_device = (UInt16)((data[3] << 8) | (data[2]));
        }

        public void updateTuningData(byte[] data, int length)
        {
            System.Buffer.BlockCopy(data, 0, tuning_data, 0, length);
            tuning_data_size = length;
        }

        public byte[] getTuningData()
        {
            return tuning_data;
        }

        public int getTuningDataSize()
        {
            return tuning_data_size;
        }

        public UInt16 getDevCfgCRC()
        {
            UInt16 dev_cfg_crc = (UInt16)((cfg_crc[1] << 8) | cfg_crc[0]);
            return dev_cfg_crc;
        }
        public UInt16 getCapCfgCRC()
        {
            UInt16 cap_cfg_crc = (UInt16)((cfg_crc[3] << 8) | cfg_crc[2]);
            return cap_cfg_crc;
        }

        public UInt16 getDevCfgDeviceCRC()
        {
            return dev_cfg_crc_from_device;
        }

        public UInt16 getCapCfgDeviceCRC()
        {
            return cap_cfg_crc_from_device;
        }

        public int getDevCfgSize()
        {
            return dev_cfg_size;
        }

        public int getCapCfgSize()
        {
            return cap_cfg_size;
        }

        public int getDevCfgDataSize()
        {
            return dev_cfg_data_size;
        }

        public int getCapCfgDataSize()
        {
            return cap_cfg_data_size;
        }

        public UInt16 getDevCfgStartOffset()
        {
            return dev_cfg_start_offset;
        }

        public UInt16 getCapCfgStartOffset()
        {
            return cap_cfg_start_offset;
        }

        public UInt16 getDevCfgCRCOffset()
        {
            return dev_cfg_crc_offset;
        }

        public UInt16 getCapCfgCRCOffset()
        {
            return cap_cfg_crc_offset;
        }

        private const ushort CRC_BIT_WIDTH = 16;
        private const ushort CRC_BIT4_MASK = 0x0F;
        private const ushort CRC_BIT4_SHIFT = 0x04;
        private const ushort CCITT16_DEFAULT_SEED = 0xffff;
        private const ushort CCITT16_POLYNOM = 0x1021;

        private ushort Calc4BitsCRC(byte value, ushort remainder)
        {
            byte tableIndex;

            byte tempVar = ((byte)(CRC_BIT_WIDTH - CRC_BIT4_SHIFT));
            tableIndex = (byte)(((byte)(value & CRC_BIT4_MASK)) ^ ((byte)((remainder) >> tempVar)));
            remainder = (ushort)(((ushort)(CCITT16_POLYNOM * tableIndex)) ^ ((ushort)(remainder << CRC_BIT4_SHIFT)));
            return remainder;
        }

        private ushort CalculateCrc16(byte[] configuration, ushort numOfBytes, ushort seed)
        {
            uint messageIndex;
            byte byteValue;

            for (messageIndex = 0; messageIndex < numOfBytes; messageIndex++)
            {
                byteValue = configuration[messageIndex];
                seed = Calc4BitsCRC((byte)(byteValue >> CRC_BIT4_SHIFT), seed);
                seed = Calc4BitsCRC(byteValue, seed);
            }
            return (ushort)~seed;
        }
    }

    public class TouchConfigModel : TouchCommanderModelBase
    {
        private bool bModelCreated;
        private Dictionary<int, ConfigDataModel> cfgModelDict;
        private int no_of_slaves;
        private int[] hbcounter;
        private int[] keycounter;
        private bool[] devicebusy;
        private byte[] actual_sensor;

        public TouchConfigModel(TouchCommanderController c) : base(c)
        {
            cfgModelDict = new Dictionary<int, ConfigDataModel>();
            actual_sensor = new byte[24];

        }

        public override void updateModel(Object d)
        {
            byte[] data = (byte[])d;
        }

        public override void updateModel(Object d, string section)
        {
            byte[] data = (byte[])d;

            if (section == "hmi_connected" || section == "hmi_disconnected")
            {
                return;
            }

            int slave_id = data[0];
            UInt16 offset_addr = (UInt16)((data[1] << 8) | data[2]);
            UInt16 data_length = (UInt16)((data[3] << 8) | data[4]);

            if (section == "full")
            {
                Console.WriteLine("Updating full config for [{0}] slave starting from offset addr [{1}]\n", slave_id, offset_addr);
                byte[] cfg_data = new byte[data_length];
                System.Buffer.BlockCopy(data, 5, cfg_data, 0, data_length);
                cfgModelDict[slave_id].updateFullModel(offset_addr, cfg_data, data_length);
            }
            else if (section == "dev_cfg")
            {
                Console.WriteLine("Updating dev config for [{0}] slave starting from offset addr [{1}]\n", slave_id, offset_addr);
                byte[] cfg_data = new byte[data_length];
                System.Buffer.BlockCopy(data, 5, cfg_data, 0, data_length);
                cfgModelDict[slave_id].updateDevModel(offset_addr, cfg_data, data_length);
            }
            else if (section == "cap_cfg")
            {
                Console.WriteLine("Updating cap config for [{0}] slave starting from offset addr [{1}]\n", slave_id, offset_addr);
                byte[] cfg_data = new byte[data_length];
                System.Buffer.BlockCopy(data, 5, cfg_data, 0, data_length);
                cfgModelDict[slave_id].updateCapModel(offset_addr, cfg_data, data_length);
            }
            else if ((section == "i2c_dev_cfg") || (section == "i2c_cap_cfg") || (section == "validate_crc") || (section == "i2c_read_crc"))
            {
                byte primary_i2c_addr = (byte)((data[1] >> 1) ^ 0x02);
                bool i2c_write_op = data[4] == 1 ? true : false;

                if (i2c_write_op == false)
                {
                    slave_id = control.GetSlaveIdFromI2CAddress((byte)(primary_i2c_addr));
                    data_length = data[5];
                    byte[] cfg_data = new byte[data_length];
                    System.Buffer.BlockCopy(data, 6, cfg_data, 0, data_length);

                    if (section == "i2c_dev_cfg")
                    {
                        offset_addr = (UInt16)(((data[2] << 8) | data[3]) - this.getDevCfgStartOffset(slave_id));
                        cfgModelDict[slave_id].updateDevModel(offset_addr, cfg_data, data_length);
                    }
                    else if (section == "i2c_cap_cfg")
                    {
                        offset_addr = (UInt16)(((data[2] << 8) | data[3]) - this.getCapCfgStartOffset(slave_id));
                        cfgModelDict[slave_id].updateCapModel(offset_addr, cfg_data, data_length);
                    }
                    else if (section == "validate_crc" || section == "i2c_read_crc")
                    {
                        cfgModelDict[slave_id].updateFromDeviceCRC(cfg_data, data_length);
                    }
                }
            }

            else if (section == "Physical Key")
            {
                data_length = (UInt16)((data[4] << 8) | data[5]);
                if (data_length < 25)
                {
                    for (int i = 0; i < data_length; i++)
                    {
                        actual_sensor[i] = data[i + 6];
                    }
                }
                else
                {
                    Console.WriteLine("Something went wrong with data length actual sensor.");
                }
            }
            else if (section == "tuning_data")
            {
                byte primary_i2c_addr = (byte)(data[1] >> 1);
                bool i2c_write_op = data[4] == 1 ? true : false;

                if (i2c_write_op == false)
                {
                    slave_id = control.GetSlaveIdFromI2CAddress((byte)(primary_i2c_addr));
                    data_length = data[5];
                    byte[] tuning_data = new byte[data_length];
                    System.Buffer.BlockCopy(data, 6, tuning_data, 0, data_length);
                    if (cfgModelDict.ContainsKey(slave_id) == true)
                    {
                        cfgModelDict[slave_id].updateTuningData(tuning_data, data_length);
                    }
                }
            }
            else if (section == "Heart Beat")
            {
                if (no_of_slaves == 0)
                {
                    no_of_slaves = control.GetNumberOfSlaves();
                    hbcounter = new int[no_of_slaves];
                    keycounter = new int[no_of_slaves];
                    devicebusy = new bool[no_of_slaves];
                }

                if (data.Length == (2 + (4 * no_of_slaves)))
                {
                    for (int i = 0; i < no_of_slaves; i++)
                    {
                        if (hbcounter.Length == no_of_slaves)
                        {
                            hbcounter[i] = (data[2 + (4 * i) + 1] << 8) | data[2 + (4 * i)];
                            keycounter[i] = (data[4 + (4 * i) + 1] << 8) | data[4 + (4 * i)];
                        }
                    }
                }
            }
            else if (section == "State Check Before Start" || section == "State Check After End")
            {
                if (devicebusy != null)
                {
                    byte primary_i2c_addr = (byte)(data[1] >> 1);
                    slave_id = control.GetSlaveIdFromI2CAddress((byte)(primary_i2c_addr));
                    devicebusy[slave_id] = (data[6] == 0 && data[7] == 0) ? false : true;
                }
            }
        }

        public void createConfigModel(byte slaveid, string chip_type, string capsense_type)
        {
            if (cfgModelDict.ContainsKey(slaveid) == false)
            {
                ConfigDataModel cfgmodel;
                cfgmodel = new ConfigDataModel(chip_type, capsense_type);
                cfgModelDict.Add(slaveid, cfgmodel);
            }
        }

        public int getHearBeatCounter(byte slaveid)
        {
            if (hbcounter != null && hbcounter.Length != 0)
            {
                return hbcounter[slaveid];
            }
            else
            {
                return 0;
            }
        }

        public int getKeyPressCounter(byte slaveid)
        {
            return keycounter[slaveid];
        }

        public void clearConfigModel()
        {
            cfgModelDict.Clear();
        }

        public byte[] getDevConfigData(int slaveid)
        {
            return cfgModelDict[slaveid].dev_cfg;
        }
        public byte[] getCapConfigData(int slaveid)
        {
            return cfgModelDict[slaveid].cap_cfg;
        }

        public byte[] getTuningData(int slaveid)
        {
            return cfgModelDict[slaveid].getTuningData();
        }

        public byte[] getPhysicalKeys(int slaveid)
        {
            return actual_sensor;
        }

        public int getTuningDataSize(int slaveid)
        {
            return cfgModelDict[slaveid].getTuningDataSize();
        }

        public int getDevConfigSize(int slaveid)
        {
            return cfgModelDict[slaveid].getDevCfgSize();
        }

        public int getCapConfigSize(int slaveid)
        {
            return cfgModelDict[slaveid].getCapCfgSize();
        }

        public int getDevConfigDataSize(int slaveid)
        {
            return cfgModelDict[slaveid].getDevCfgDataSize();
        }

        public int getCapConfigDataSize(int slaveid)
        {
            return cfgModelDict[slaveid].getCapCfgDataSize();
        }

        public UInt16 getDevCfgStartOffset(int slaveid)
        {
            return cfgModelDict[slaveid].getDevCfgStartOffset();
        }

        public UInt16 getCapCfgStartOffset(int slaveid)
        {
            return cfgModelDict[slaveid].getCapCfgStartOffset();
        }

        public UInt16 getDevCfgCRCOffset(int slaveid)
        {
            return cfgModelDict[slaveid].getDevCfgCRCOffset();
        }

        public UInt16 getCapCfgCRCOffset(int slaveid)
        {
            return cfgModelDict[slaveid].getCapCfgCRCOffset();
        }

        public UInt16 getDevCfgCRC(int slaveid)
        {
            return cfgModelDict[slaveid].getDevCfgCRC();
        }
        public UInt16 getCapCfgCRC(int slaveid)
        {
            return cfgModelDict[slaveid].getCapCfgCRC();
        }

        public UInt16 getDevCfgDeviceCRC(int slaveid)
        {
            return cfgModelDict[slaveid].getDevCfgDeviceCRC();
        }

        public UInt16 getCapCfgDeviceCRC(int slaveid)
        {
            return cfgModelDict[slaveid].getCapCfgDeviceCRC();
        }

        public bool getDeviceBusyStatus(int slaveid)
        {
            return devicebusy[slaveid];
        }
    }

    // S19 File Generator Helper Class
    public static class S19FileGenerator
    {
        public static void SaveS19File(string fileName, byte[] data, UInt32 startAddress)
        {
            string s19FileName = Path.Combine(Path.GetDirectoryName(fileName),
                                             Path.GetFileNameWithoutExtension(fileName) + ".s19");

            using (StreamWriter writer = new StreamWriter(s19FileName, false, Encoding.ASCII))
            {
                // Write S3 data records (32-bit address) - 16 bytes per line
                int bytesPerLine = 16;
                UInt32 currentAddress = startAddress;
                int offset = 0;

                while (offset < data.Length)
                {
                    int bytesToWrite = Math.Min(bytesPerLine, data.Length - offset);
                    byte[] lineData = new byte[bytesToWrite];
                    Array.Copy(data, offset, lineData, 0, bytesToWrite);

                    string dataRecord = GenerateS3Record(currentAddress, lineData);
                    writer.WriteLine(dataRecord);

                    currentAddress += (UInt32)bytesToWrite;
                    offset += bytesToWrite;
                }

                // Write S7 termination record (32-bit address)
                string terminationRecord = GenerateS7Record(startAddress);
                writer.WriteLine(terminationRecord);
            }
        }

        private static string GenerateS3Record(UInt32 address, byte[] data)
        {
            // S3 record format: S3 + ByteCount + Address(8 hex digits) + Data + Checksum
            // ByteCount = data length + 5 (4 bytes address + 1 byte checksum)
            int byteCount = data.Length + 5;

            StringBuilder sb = new StringBuilder();
            sb.Append("S3");
            sb.Append(byteCount.ToString("X2"));
            sb.Append(address.ToString("X8"));

            foreach (byte b in data)
            {
                sb.Append(b.ToString("X2"));
            }

            byte checksum = CalculateChecksum(byteCount, address, data);
            sb.Append(checksum.ToString("X2"));

            return sb.ToString();
        }

        private static string GenerateS7Record(UInt32 startAddress)
        {
            // S7 record format: S7 + ByteCount + Address(8 hex digits) + Checksum
            // ByteCount = 5 (4 bytes address + 1 byte checksum)
            int byteCount = 5;

            StringBuilder sb = new StringBuilder();
            sb.Append("S7");
            sb.Append(byteCount.ToString("X2"));
            sb.Append(startAddress.ToString("X8"));

            byte checksum = CalculateChecksum(byteCount, startAddress, new byte[0]);
            sb.Append(checksum.ToString("X2"));

            return sb.ToString();
        }

        private static byte CalculateChecksum(int byteCount, UInt32 address, byte[] data)
        {
            // Checksum calculation: sum all bytes (count + address + data), then one's complement of LSB
            int sum = byteCount;

            // Add address bytes (4 bytes for 32-bit address)
            sum += (int)((address >> 24) & 0xFF);
            sum += (int)((address >> 16) & 0xFF);
            sum += (int)((address >> 8) & 0xFF);
            sum += (int)(address & 0xFF);

            // Add data bytes
            foreach (byte b in data)
            {
                sum += b;
            }

            // Return one's complement of the least significant byte
            return (byte)(~sum & 0xFF);
        }
    }

    public class TouchConfigTabPage : TouchCommanderTabBase
    {
        private bool bViewInitComplete;
        private Button btnLoadFromFile;
        private Button btnSaveToFile;
        private Button btnLoadFromDevice;
        private Button btnWriteToDevice;
        private Button btnReadFromDeviceFlash;
        private Button btnWriteToDeviceFlash;
        private Button btnClear;
        private Button btnSoftReset;
        private Label lbHeartBeatCnt;
        private Button btnFactoryDefault;
        private ComboBox cbSlaveIdSelect;
        private ComboBox cbTouchChipType;
        private ComboBox cbTouchSeningMethod;
        private Label lbProgress;
        private ProgressBar pbProgressBar;

        // S19 Generation Controls
        private CheckBox ckEnableS19Generation;
        private Label lbS19StartAddress;
        private TextBox txtS19StartAddress;

        private List<string> lstTouchChipTypes = new List<string> { "Rx130", "Rx130INT", "PSoC4000", "PSoC4000S", "PSoC4100S" };
        private List<string> lstTouchSeningMethods = new List<string> { "Self", "Mutual" };
        private int iNumberOfSlaves;
        private TabControl tcConfigTabs;
        private String toucHwType;
        private bool bMutualTouchType;
        private int iSelectedSlaveId;

        private Dictionary<int, TouchCommanderTabBase> devCfgViewDict;
        private Dictionary<int, TouchCommanderTabBase> capCfgViewDict;
        private Dictionary<int, TouchCommanderTabBase> tuningViewDict;

        public TouchConfigTabPage(TouchCommanderController c) : base(c)
        {
            this.Text = "Touch Configuration";
            this.bViewInitComplete = false;
            this.iNumberOfSlaves = 0;
            this.iSelectedSlaveId = 0;
            devCfgViewDict = new Dictionary<int, TouchCommanderTabBase>();
            capCfgViewDict = new Dictionary<int, TouchCommanderTabBase>();
            tuningViewDict = new Dictionary<int, TouchCommanderTabBase>();
        }

        private void initView(object data)
        {
            cbSlaveIdSelect = new ComboBox();
            cbSlaveIdSelect.Location = new Point(15, 15);
            cbSlaveIdSelect.Size = new Size(180, 50);
            cbSlaveIdSelect.Text = "Select the Slave Id";
            cbSlaveIdSelect.Font = new Font(cbSlaveIdSelect.Font.Name, cbSlaveIdSelect.Font.Size + 2.0F, cbSlaveIdSelect.Font.Style, cbSlaveIdSelect.Font.Unit);
            for (int i = 0; i < iNumberOfSlaves; i++)
            {
                cbSlaveIdSelect.Items.Add(String.Format("Slave {0}", i));
            }

            if (iNumberOfSlaves == 1)
            {
                cbSlaveIdSelect.Enabled = false;
            }
            cbSlaveIdSelect.SelectedIndex = 0;
            iSelectedSlaveId = cbSlaveIdSelect.SelectedIndex;
            cbSlaveIdSelect.SelectedIndexChanged += new System.EventHandler(cbSlaveIdSelect_SelectedIndexChanged);
            this.Controls.Add(cbSlaveIdSelect);

            cbTouchChipType = new ComboBox();
            cbTouchChipType.Location = new Point(cbSlaveIdSelect.Location.X, cbSlaveIdSelect.Location.Y + cbSlaveIdSelect.Size.Height + 20);
            cbTouchChipType.Size = new Size(180, 50);
            cbTouchChipType.Text = "Select the Touch Micro";
            cbTouchChipType.Font = new Font(cbTouchChipType.Font.Name, cbTouchChipType.Font.Size + 2.0F, cbTouchChipType.Font.Style, cbTouchChipType.Font.Unit);
            foreach (string s in lstTouchChipTypes)
            {
                cbTouchChipType.Items.Add(s);
            }
            cbTouchChipType.SelectedIndexChanged += new System.EventHandler(cbTouchChipType_SelectedIndexChanged);
            this.Controls.Add(cbTouchChipType);

            cbTouchSeningMethod = new ComboBox();
            cbTouchSeningMethod.Location = new Point(cbTouchChipType.Location.X, cbTouchChipType.Location.Y + cbTouchChipType.Size.Height + 20);
            cbTouchSeningMethod.Size = new Size(180, 50);
            cbTouchSeningMethod.Text = "Select Sensing Method";
            cbTouchSeningMethod.Font = new Font(cbTouchSeningMethod.Font.Name, cbTouchSeningMethod.Font.Size + 2.0F, cbTouchSeningMethod.Font.Style, cbTouchSeningMethod.Font.Unit);
            foreach (string s in lstTouchSeningMethods)
            {
                cbTouchSeningMethod.Items.Add(s);
            }
            cbTouchSeningMethod.Enabled = false;
            cbTouchSeningMethod.SelectedIndexChanged += new System.EventHandler(cbTouchSeningMethod_SelectedIndexChanged);
            this.Controls.Add(cbTouchSeningMethod);

            btnLoadFromFile = new Button();
            btnLoadFromFile.Location = new Point(cbTouchSeningMethod.Location.X, cbTouchSeningMethod.Location.Y + cbTouchSeningMethod.Size.Height + 20);
            btnLoadFromFile.Size = new Size(180, 50);
            btnLoadFromFile.Font = new Font(btnLoadFromFile.Font.Name, btnLoadFromFile.Font.Size + 2.0F, btnLoadFromFile.Font.Style, btnLoadFromFile.Font.Unit);
            btnLoadFromFile.Text = "Load Config From File";
            btnLoadFromFile.BackColor = Color.LightGreen;
            btnLoadFromFile.Visible = true;
            btnLoadFromFile.Click += new EventHandler(this.btnLoadFromFile_Click);
            this.Controls.Add(btnLoadFromFile);

            btnSaveToFile = new Button();
            btnSaveToFile.Location = new Point(btnLoadFromFile.Location.X, btnLoadFromFile.Location.Y + btnLoadFromFile.Size.Height + 20);
            btnSaveToFile.Size = new Size(180, 50);
            btnSaveToFile.Font = new Font(btnSaveToFile.Font.Name, btnSaveToFile.Font.Size + 2.0F, btnSaveToFile.Font.Style, btnSaveToFile.Font.Unit);
            btnSaveToFile.Text = "Save Config To File";
            btnSaveToFile.BackColor = Color.LightGreen;
            btnSaveToFile.Visible = true;
            btnSaveToFile.Click += new EventHandler(this.btnSaveToFile_Click);
            this.Controls.Add(btnSaveToFile);

            // S19 Generation Checkbox
            ckEnableS19Generation = new CheckBox();
            ckEnableS19Generation.Location = new Point(btnSaveToFile.Location.X, btnSaveToFile.Location.Y + btnSaveToFile.Size.Height + 10);
            ckEnableS19Generation.Size = new Size(180, 25);
            ckEnableS19Generation.Text = "Generate S19 File";
            ckEnableS19Generation.Checked = true;
            ckEnableS19Generation.Font = new Font(ckEnableS19Generation.Font.Name, ckEnableS19Generation.Font.Size + 2.0F, ckEnableS19Generation.Font.Style, ckEnableS19Generation.Font.Unit);
            ckEnableS19Generation.CheckedChanged += new EventHandler(this.ckEnableS19Generation_CheckedChanged);
            this.Controls.Add(ckEnableS19Generation);

            // S19 Start Address Label
            lbS19StartAddress = new Label();
            lbS19StartAddress.Location = new Point(ckEnableS19Generation.Location.X, ckEnableS19Generation.Location.Y + ckEnableS19Generation.Size.Height + 5);
            lbS19StartAddress.Size = new Size(180, 20);
            lbS19StartAddress.Text = "S19 Start Address (Hex):";
            lbS19StartAddress.Font = new Font(lbS19StartAddress.Font.Name, lbS19StartAddress.Font.Size + 2.0F, FontStyle.Bold, lbS19StartAddress.Font.Unit);
            this.Controls.Add(lbS19StartAddress);

            // S19 Start Address TextBox
            txtS19StartAddress = new TextBox();
            txtS19StartAddress.Location = new Point(lbS19StartAddress.Location.X, lbS19StartAddress.Location.Y + lbS19StartAddress.Size.Height + 5);
            txtS19StartAddress.Size = new Size(180, 25);
            txtS19StartAddress.Text = "00000000";
            txtS19StartAddress.MaxLength = 8;
            txtS19StartAddress.Font = new Font(txtS19StartAddress.Font.Name, txtS19StartAddress.Font.Size + 2.0F, txtS19StartAddress.Font.Style, txtS19StartAddress.Font.Unit);
            txtS19StartAddress.TextChanged += new EventHandler(this.txtS19StartAddress_TextChanged);
            this.Controls.Add(txtS19StartAddress);

            lbProgress = new Label();
            lbProgress.Location = new Point(txtS19StartAddress.Location.X, txtS19StartAddress.Location.Y + txtS19StartAddress.Size.Height + 10);
            lbProgress.Size = new Size(180, 20);
            lbProgress.Font = new Font(lbProgress.Font.Name, lbProgress.Font.Size + 2.0F, lbProgress.Font.Style, lbProgress.Font.Unit);
            lbProgress.Text = "Progress status:";
            lbProgress.Visible = true;
            this.Controls.Add(lbProgress);

            pbProgressBar = new ProgressBar();
            pbProgressBar.Location = new Point(lbProgress.Location.X, lbProgress.Location.Y + lbProgress.Size.Height + 5);
            pbProgressBar.Size = new Size(180, 20);
            pbProgressBar.Style = ProgressBarStyle.Blocks;
            this.Controls.Add(pbProgressBar);

            btnLoadFromDevice = new Button();
            btnLoadFromDevice.Location = new Point(pbProgressBar.Location.X, pbProgressBar.Location.Y + pbProgressBar.Size.Height + 20);
            btnLoadFromDevice.Size = new Size(180, 50);
            btnLoadFromDevice.Font = new Font(btnLoadFromDevice.Font.Name, btnLoadFromDevice.Font.Size + 2.0F, btnLoadFromDevice.Font.Style, btnLoadFromDevice.Font.Unit);
            btnLoadFromDevice.Text = "Read Config From Device";
            btnLoadFromDevice.BackColor = Color.LightSeaGreen;
            btnLoadFromDevice.Visible = true;
            btnLoadFromDevice.Click += new EventHandler(this.btnLoadFromDevice_Click);
            this.Controls.Add(btnLoadFromDevice);

            btnWriteToDevice = new Button();
            btnWriteToDevice.Location = new Point(btnLoadFromDevice.Location.X, btnLoadFromDevice.Location.Y + btnLoadFromDevice.Size.Height + 20);
            btnWriteToDevice.Size = new Size(180, 50);
            btnWriteToDevice.Font = new Font(btnWriteToDevice.Font.Name, btnWriteToDevice.Font.Size + 2.0F, btnWriteToDevice.Font.Style, btnWriteToDevice.Font.Unit);
            btnWriteToDevice.Text = "Send Config To Device";
            btnWriteToDevice.BackColor = Color.LightSeaGreen;
            btnWriteToDevice.Visible = true;
            btnWriteToDevice.Click += new EventHandler(this.btnWriteToDevice_Click);
            this.Controls.Add(btnWriteToDevice);

            btnReadFromDeviceFlash = new Button();
            btnReadFromDeviceFlash.Location = new Point(btnWriteToDevice.Location.X, btnWriteToDevice.Location.Y + btnWriteToDevice.Size.Height + 20);
            btnReadFromDeviceFlash.Size = new Size(180, 50);
            btnReadFromDeviceFlash.Font = new Font(btnReadFromDeviceFlash.Font.Name, btnReadFromDeviceFlash.Font.Size + 2.0F, btnReadFromDeviceFlash.Font.Style, btnReadFromDeviceFlash.Font.Unit);
            btnReadFromDeviceFlash.Text = "Read Config From Device Flash";
            btnReadFromDeviceFlash.BackColor = Color.LightSalmon;
            btnReadFromDeviceFlash.Visible = true;
            btnReadFromDeviceFlash.Click += new EventHandler(this.btnReadFromDeviceFlash_Click);
            this.Controls.Add(btnReadFromDeviceFlash);

            btnWriteToDeviceFlash = new Button();
            btnWriteToDeviceFlash.Location = new Point(btnReadFromDeviceFlash.Location.X, btnReadFromDeviceFlash.Location.Y + btnReadFromDeviceFlash.Size.Height + 20);
            btnWriteToDeviceFlash.Size = new Size(180, 50);
            btnWriteToDeviceFlash.Font = new Font(btnWriteToDeviceFlash.Font.Name, btnWriteToDeviceFlash.Font.Size + 2.0F, btnWriteToDeviceFlash.Font.Style, btnWriteToDeviceFlash.Font.Unit);
            btnWriteToDeviceFlash.Text = "Write Config To Device Flash";
            btnWriteToDeviceFlash.BackColor = Color.LightSalmon;
            btnWriteToDeviceFlash.Visible = true;
            btnWriteToDeviceFlash.Click += new EventHandler(this.btnWriteToDeviceFlash_Click);
            this.Controls.Add(btnWriteToDeviceFlash);

            btnClear = new Button();
            btnClear.Location = new Point(btnWriteToDeviceFlash.Location.X, btnWriteToDeviceFlash.Location.Y + btnWriteToDeviceFlash.Size.Height + 20);
            btnClear.Size = new Size(180, 25);
            btnClear.Font = new Font(btnClear.Font.Name, btnClear.Font.Size + 2.0F, btnClear.Font.Style, btnClear.Font.Unit);
            btnClear.Text = "Clear";
            btnClear.BackColor = Color.Khaki;
            btnClear.Visible = true;
            btnClear.Click += new EventHandler(this.btnClear_Click);
            this.Controls.Add(btnClear);

            btnSoftReset = new Button();
            btnSoftReset.Location = new Point(btnClear.Location.X, btnClear.Location.Y + btnClear.Size.Height + 20);
            btnSoftReset.Size = new Size(180, 25);
            btnSoftReset.Font = new Font(btnSoftReset.Font.Name, btnSoftReset.Font.Size + 2.0F, btnSoftReset.Font.Style, btnSoftReset.Font.Unit);
            btnSoftReset.Text = "Soft Reset";
            btnSoftReset.BackColor = Color.Khaki;
            btnSoftReset.Visible = true;
            btnSoftReset.Click += new EventHandler(this.btnSoftReset_Click);
            this.Controls.Add(btnSoftReset);

            lbHeartBeatCnt = new Label();
            lbHeartBeatCnt.Location = new Point(btnSoftReset.Location.X, btnSoftReset.Location.Y + btnSoftReset.Size.Height + 20);
            lbHeartBeatCnt.Size = new Size(180, 25);
            lbHeartBeatCnt.Font = new Font(lbHeartBeatCnt.Font.Name, lbHeartBeatCnt.Font.Size + 2.0F, lbHeartBeatCnt.Font.Style, lbHeartBeatCnt.Font.Unit);
            lbHeartBeatCnt.Text = "HB Count: 0";
            lbHeartBeatCnt.Visible = true;
            this.Controls.Add(lbHeartBeatCnt);

            btnFactoryDefault = new Button();
            btnFactoryDefault.Location = new Point(lbHeartBeatCnt.Location.X, lbHeartBeatCnt.Location.Y + lbHeartBeatCnt.Size.Height + 10);
            btnFactoryDefault.Size = new Size(180, 50);
            btnFactoryDefault.Font = new Font(btnFactoryDefault.Font.Name, btnFactoryDefault.Font.Size + 2.0F, btnFactoryDefault.Font.Style, btnFactoryDefault.Font.Unit);
            btnFactoryDefault.Text = "Restore Factory Default";
            btnFactoryDefault.BackColor = Color.Khaki;
            btnFactoryDefault.Visible = true;
            btnFactoryDefault.Click += new EventHandler(this.btnFactoryDefault_Click);
            this.Controls.Add(btnFactoryDefault);

            tcConfigTabs = new TabControl();
            tcConfigTabs.Location = new Point(cbSlaveIdSelect.Location.X + cbSlaveIdSelect.Size.Width + 10, cbSlaveIdSelect.Location.Y - 10);
            tcConfigTabs.Size = new Size(this.Width - 220, this.Height - 8);
            tcConfigTabs.Selected += new TabControlEventHandler(this.tcConfigTabs_Selected);
            this.Controls.Add(tcConfigTabs);

            if (control.IsOfflineModeActive() == true)
            {
                btnLoadFromDevice.Enabled = false;
                btnWriteToDevice.Enabled = false;
                btnWriteToDeviceFlash.Enabled = false;
                btnReadFromDeviceFlash.Enabled = false;
                btnSoftReset.Enabled = false;
            }

            this.bViewInitComplete = true;
        }

        private void ckEnableS19Generation_CheckedChanged(object sender, EventArgs e)
        {
            lbS19StartAddress.Enabled = ckEnableS19Generation.Checked;
            txtS19StartAddress.Enabled = ckEnableS19Generation.Checked;
        }

        private void txtS19StartAddress_TextChanged(object sender, EventArgs e)
        {
            string text = txtS19StartAddress.Text.ToUpper();
            string validText = "";

            foreach (char c in text)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))
                {
                    validText += c;
                }
            }

            if (validText != text)
            {
                int selectionStart = txtS19StartAddress.SelectionStart;
                txtS19StartAddress.Text = validText;
                txtS19StartAddress.SelectionStart = Math.Max(0, selectionStart - 1);
            }
        }

        private void cbSlaveIdSelect_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            iSelectedSlaveId = cbSlaveIdSelect.SelectedIndex;
        }

        private void cbTouchChipType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox device_type = (ComboBox)sender;
            cbTouchSeningMethod.Enabled = true;
            control.SetActualDeviceType((byte)iSelectedSlaveId, device_type.Text);
        }

        private void tcConfigTabs_Selected(Object sender, TabControlEventArgs e)
        {
            if (e.TabPage == null) return;

            Console.WriteLine(String.Format("[{0}] = [{1}] [{2}] [{3}]", "TabPage", e.TabPage, e.Action, e.TabPage.Text));
            if (e.TabPage.Text.Contains("RX130 Button Config"))
            {
                RX130TouchCapConfigTabPage cap_tab = (RX130TouchCapConfigTabPage)e.TabPage;
                cap_tab.enableSelectedkeys();
            }
        }

        private void cbTouchSeningMethod_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            bMutualTouchType = cbTouchSeningMethod.Text == "Mutual" ? true : false;
            cbTouchChipType.Enabled = false;

            for (int i = 0; i < iNumberOfSlaves; i++)
            {
                if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
                {
                    if (cbTouchChipType.Text == "Rx130" && bMutualTouchType == false)
                    {
                        MessageBox.Show("ERROR: Self capacitance sensing mode is still not supported for dedicated I2C based Rx130 touch");
                        return;
                    }

                    if (devCfgViewDict.ContainsKey(i) == false)
                    {
                        RX130TouchDevConfigTabPage dev_tab = new RX130TouchDevConfigTabPage(control, i, cbTouchChipType.Text, bMutualTouchType);
                        devCfgViewDict.Add(i, dev_tab);
                        tcConfigTabs.Controls.Add(dev_tab);
                    }

                    if (capCfgViewDict.ContainsKey(i) == false)
                    {
                        RX130TouchCapConfigTabPage cap_tab = new RX130TouchCapConfigTabPage(control, i, cbTouchChipType.Text, bMutualTouchType);
                        capCfgViewDict.Add(i, cap_tab);
                        tcConfigTabs.Controls.Add(cap_tab);
                    }

                    TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");
                    model.createConfigModel((byte)i, cbTouchChipType.Text, cbTouchSeningMethod.Text);
                }
                else if (cbTouchChipType.SelectedText == "PSoC4000")
                {
                }
                else
                {
                }
            }
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private String ByteArrayToString(byte[] data, int bytes_per_line)
        {
            String s = "";
            int length = data.Length;
            int num_rows = length / bytes_per_line;

            for (int i = 0; i < num_rows; i++)
            {
                s += BitConverter.ToString(data, i * bytes_per_line, bytes_per_line).Replace("-", "");
                s += System.Environment.NewLine;
            }

            return s;
        }

        private void btnLoadFromFile_Click(object sender, System.EventArgs e)
        {
            if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "txt files (*.txt)|*.txt|HEX Files (*.hex)|*.hex|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.Multiselect = false;
                DialogResult result = openFileDialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }

                string file = openFileDialog.FileName;
                string config_text = String.Empty;

                using (StreamReader sr = File.OpenText(file))
                {
                    string s = String.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        config_text = config_text + s;
                    }
                }

                byte[] config_data = StringToByteArray(config_text);

                TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");
                if (model != null)
                {
                    int cfg_data_length = config_text.Length / 2;
                    byte[] data = new byte[5 + cfg_data_length];
                    data[0] = (byte)iSelectedSlaveId;
                    data[1] = 0x00;
                    data[2] = 0x00;
                    data[3] = (byte)((cfg_data_length >> 8) & 0xFF);
                    data[4] = (byte)(cfg_data_length & 0xFF);
                    System.Buffer.BlockCopy(config_data, 0, data, 5, cfg_data_length);
                    model.updateModel(data, "full");
                    this.devCfgViewDict[iSelectedSlaveId].updateView(String.Format("{0}", data[0]), (object)model);
                    this.capCfgViewDict[iSelectedSlaveId].updateView(String.Format("{0}", data[0]), (object)model);
                }
            }
            else
            {
                MessageBox.Show("ERROR: Please select Touch Chip Type and Sensing method first.");
            }
        }

        private void btnSaveToFile_Click(object sender, System.EventArgs e)
        {
            if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Hex Format|*.hex";
                saveFileDialog1.RestoreDirectory = true;
                saveFileDialog1.CheckPathExists = true;
                saveFileDialog1.DefaultExt = "hex";
                DialogResult result = saveFileDialog1.ShowDialog();
                saveFileDialog1.Title = "Save Touch Config File";
                if (result != DialogResult.OK)
                {
                    return;
                }

                TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");
                if (model != null)
                {
                    byte[] dev_cfg;
                    byte[] cap_cfg;
                    int dev_cfg_size = model.getDevConfigSize(iSelectedSlaveId);
                    int cap_cfg_size = model.getCapConfigSize(iSelectedSlaveId);

                    if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
                    {
                        RX130TouchDevConfigTabPage dev_cfg_tab = (RX130TouchDevConfigTabPage)this.devCfgViewDict[iSelectedSlaveId];
                        dev_cfg = dev_cfg_tab.getDevConfigData(iSelectedSlaveId);
                        byte[] dev_cfg_data = new byte[dev_cfg.Length + 5];
                        dev_cfg_data[0] = (byte)iSelectedSlaveId;
                        dev_cfg_data[1] = 0x00;
                        dev_cfg_data[2] = 0x00;
                        dev_cfg_data[3] = (byte)((dev_cfg.Length >> 8) & 0xFF);
                        dev_cfg_data[4] = (byte)(dev_cfg.Length & 0xFF);
                        System.Buffer.BlockCopy(dev_cfg, 0, dev_cfg_data, 5, dev_cfg.Length);
                        model.updateModel(dev_cfg_data, "dev_cfg");

                        RX130TouchCapConfigTabPage cap_cfg_tab = (RX130TouchCapConfigTabPage)this.capCfgViewDict[iSelectedSlaveId];
                        cap_cfg = cap_cfg_tab.getCapConfigData(iSelectedSlaveId);
                        byte[] cap_cfg_data = new byte[cap_cfg.Length + 5];
                        cap_cfg_data[0] = (byte)iSelectedSlaveId;
                        cap_cfg_data[1] = 0x00;
                        cap_cfg_data[2] = 0x00;
                        cap_cfg_data[3] = (byte)((cap_cfg.Length >> 8) & 0xFF);
                        cap_cfg_data[4] = (byte)(cap_cfg.Length & 0xFF);
                        System.Buffer.BlockCopy(cap_cfg, 0, cap_cfg_data, 5, cap_cfg.Length);
                        model.updateModel(cap_cfg_data, "cap_cfg");

                        byte[] data = new byte[dev_cfg_size + cap_cfg_size + (256 - dev_cfg_size)];
                        Array.Clear(data, 0, data.Length);

                        System.Buffer.BlockCopy(model.getCapConfigData(iSelectedSlaveId), 0, data, 0, cap_cfg_size);
                        System.Buffer.BlockCopy(model.getDevConfigData(iSelectedSlaveId), 0, data, cap_cfg_size, dev_cfg_size);

                        UInt16 crc = model.getDevCfgCRC(iSelectedSlaveId);
                        data[dev_cfg_size + cap_cfg_size] = (byte)(crc & 0xFF);
                        data[dev_cfg_size + cap_cfg_size + 1] = (byte)((crc >> 8) & 0xFF);

                        crc = model.getCapCfgCRC(iSelectedSlaveId);
                        data[dev_cfg_size + cap_cfg_size + 2] = (byte)(crc & 0xFF);
                        data[dev_cfg_size + cap_cfg_size + 3] = (byte)((crc >> 8) & 0xFF);

                        String cfg_str = ByteArrayToString(data, 128);

                        if (saveFileDialog1.FileName != "")
                        {
                            File.WriteAllText(saveFileDialog1.FileName, cfg_str);
                        }
                        else
                        {
                            MessageBox.Show("The File name was empty !!!");
                            return;
                        }

                        using (Stream file = File.OpenWrite(saveFileDialog1.FileName + ".bin"))
                        {
                            file.Write(data, 0, data.Length);
                        }

                        SaveBinaryConfigToFile(saveFileDialog1.FileName, data);

                        // Generate S19 file if enabled
                        if (ckEnableS19Generation.Checked)
                        {
                            try
                            {
                                UInt32 startAddress = 0;
                                if (!string.IsNullOrEmpty(txtS19StartAddress.Text))
                                {
                                    startAddress = Convert.ToUInt32(txtS19StartAddress.Text, 16);
                                }

                                S19FileGenerator.SaveS19File(saveFileDialog1.FileName, data, startAddress);

                                MessageBox.Show("Configuration files saved successfully!\n\n" +
                                              "Generated files:\n" +
                                              "- .hex (text format)\n" +
                                              "- .bin (binary format)\n" +
                                              "- .s19 (Motorola S-Record format)\n" +
                                              "- .c and .h (C source files)",
                                              "Success");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error generating S19 file: " + ex.Message + "\n\nOther files were saved successfully.", "Warning");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Configuration files saved successfully!\n\n" +
                                          "Generated files:\n" +
                                          "- .hex (text format)\n" +
                                          "- .bin (binary format)\n" +
                                          "- .c and .h (C source files)",
                                          "Success");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("ERROR: Please select Touch Chip Type and Sensing method first.");
            }
        }

        private void SaveBinaryConfigToFile(string FileName, byte[] Data)
        {
            String config_file_h = Path.Combine(Path.GetDirectoryName(FileName), Path.GetFileNameWithoutExtension(FileName) + "_device_config.h");
            String config_file_c = Path.Combine(Path.GetDirectoryName(FileName), Path.GetFileNameWithoutExtension(FileName) + "_device_config.c");

            int bytes_per_line = 16;
            int total_device_config_bytes = 160;
            int total_capsense_config_bytes = 512;

            using (StreamWriter file = new StreamWriter(config_file_h))
            {
                file.WriteLine("#ifndef TOUCH_CONFIG_FILE_H\n#define TOUCH_CONFIG_FILE_H\n");

                file.WriteLine(String.Format("extern const uint8 device_config[{0}];", total_device_config_bytes));
                file.WriteLine(String.Format("extern const uint8 capsense_config[{0}];", total_capsense_config_bytes));

                file.WriteLine("\n#endif /*TOUCH_CONFIG_FILE_H*/");
            }
            using (StreamWriter file = new StreamWriter(config_file_c))
            {
                file.WriteLine(String.Format("#include \"{0}\"\n", Path.GetFileName(config_file_h)));

                file.WriteLine(String.Format("const uint8 capsense_config[{0}] = {1}", total_capsense_config_bytes, "{"));

                int count = total_capsense_config_bytes / bytes_per_line;
                for (int i = 0; i < count; i++)
                {
                    String line = "    ";
                    for (int j = 0; j < bytes_per_line; j++)
                    {
                        line += String.Format("0x{0}, ", Data[(i * bytes_per_line) + j].ToString("X2"));
                    }
                    file.WriteLine(line);
                }
                file.WriteLine("};\n");

                file.WriteLine(String.Format("const uint8 device_config[{0}] = {1}", total_device_config_bytes, "{"));

                count = total_device_config_bytes / bytes_per_line;
                for (int i = 0; i < count; i++)
                {
                    String line = "    ";
                    for (int j = 0; j < bytes_per_line; j++)
                    {
                        line += String.Format("0x{0}, ", Data[total_capsense_config_bytes + (i * bytes_per_line) + j].ToString("X2"));
                    }
                    file.WriteLine(line);
                }

                file.WriteLine("};\n");

                file.WriteLine(String.Format("const uint8 config_crc[{0}] = {1}", 4, "{"));

                String l = "    ";
                for (int j = 0; j < 4; j++)
                {
                    l += String.Format("0x{0}, ", Data[total_capsense_config_bytes + total_device_config_bytes + j].ToString("X2"));
                }
                file.WriteLine(l);

                file.WriteLine("};\n");
            }
        }

        private void btnLoadFromDevice_Click(object sender, System.EventArgs e)
        {
            if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
            {
                TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");
                UInt16 dev_cfg_start_offset = model.getDevCfgStartOffset(iSelectedSlaveId);
                UInt16 dev_cfg_data_size = (UInt16)model.getDevConfigDataSize(iSelectedSlaveId);

                UInt16 cap_cfg_start_offset = model.getCapCfgStartOffset(iSelectedSlaveId);
                UInt16 cap_cfg_data_size = (UInt16)model.getCapConfigDataSize(iSelectedSlaveId);

                UInt16 dev_cfg_crc_offset = model.getDevCfgCRCOffset(iSelectedSlaveId);
                UInt16 cap_cfg_crc_offset = model.getCapCfgCRCOffset(iSelectedSlaveId);

                byte block_size = 32;
                UInt16 bytes_sent = 0;

                sendReadStateCmd(true);

                while (bytes_sent < cap_cfg_data_size)
                {
                    control.SendI2CReadCommand("Touch Config", "i2c_cap_cfg", (byte)iSelectedSlaveId, "Secondary", (UInt16)(cap_cfg_start_offset + bytes_sent), block_size);
                    bytes_sent += block_size;

                    if ((bytes_sent != cap_cfg_data_size) && ((bytes_sent + block_size) > cap_cfg_data_size))
                    {
                        UInt16 bytes_to_send = (UInt16)(cap_cfg_data_size - bytes_sent);
                        control.SendI2CReadCommand("Touch Config", "i2c_cap_cfg", (byte)iSelectedSlaveId, "Secondary", (UInt16)(cap_cfg_start_offset + bytes_sent), (byte)bytes_to_send);
                        break;
                    }
                }

                bytes_sent = 0;
                while (bytes_sent < dev_cfg_data_size)
                {
                    control.SendI2CReadCommand("Touch Config", "i2c_dev_cfg", (byte)iSelectedSlaveId, "Secondary", (UInt16)(dev_cfg_start_offset + bytes_sent), block_size);

                    bytes_sent += block_size;
                    if ((bytes_sent != dev_cfg_data_size) && ((bytes_sent + block_size) > dev_cfg_data_size))
                    {
                        UInt16 bytes_to_send = (UInt16)(dev_cfg_data_size - bytes_sent);
                        control.SendI2CReadCommand("Touch Config", "i2c_dev_cfg", (byte)iSelectedSlaveId, "Secondary", (UInt16)(dev_cfg_start_offset + bytes_sent), (byte)bytes_to_send);
                        break;
                    }
                }

                control.SendI2CReadCommand("Touch Config", "i2c_read_crc", (byte)iSelectedSlaveId, "Secondary", model.getDevCfgCRCOffset(iSelectedSlaveId), 4);

                sendReadStateCmd(false);
            }
            else
            {
                MessageBox.Show("ERROR: Please select Touch Chip Type and Sensing method first.");
            }
        }

        private void updateModelFromView()
        {
            byte[] dev_cfg;
            byte[] cap_cfg;
            TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");

            RX130TouchDevConfigTabPage dev_cfg_tab = (RX130TouchDevConfigTabPage)this.devCfgViewDict[iSelectedSlaveId];
            dev_cfg = dev_cfg_tab.getDevConfigData(iSelectedSlaveId);
            byte[] dev_cfg_data = new byte[dev_cfg.Length + 5];
            dev_cfg_data[0] = (byte)iSelectedSlaveId;
            dev_cfg_data[1] = 0x00;
            dev_cfg_data[2] = 0x00;
            dev_cfg_data[3] = (byte)((dev_cfg.Length >> 8) & 0xFF);
            dev_cfg_data[4] = (byte)(dev_cfg.Length & 0xFF);
            System.Buffer.BlockCopy(dev_cfg, 0, dev_cfg_data, 5, dev_cfg.Length);
            model.updateModel(dev_cfg_data, "dev_cfg");

            RX130TouchCapConfigTabPage cap_cfg_tab = (RX130TouchCapConfigTabPage)this.capCfgViewDict[iSelectedSlaveId];
            cap_cfg = cap_cfg_tab.getCapConfigData(iSelectedSlaveId);
            byte[] cap_cfg_data = new byte[cap_cfg.Length + 5];
            cap_cfg_data[0] = (byte)iSelectedSlaveId;
            cap_cfg_data[1] = 0x00;
            cap_cfg_data[2] = 0x00;
            cap_cfg_data[3] = (byte)((cap_cfg.Length >> 8) & 0xFF);
            cap_cfg_data[4] = (byte)(cap_cfg.Length & 0xFF);
            System.Buffer.BlockCopy(cap_cfg, 0, cap_cfg_data, 5, cap_cfg.Length);
            model.updateModel(cap_cfg_data, "cap_cfg");
        }

        private void btnWriteToDevice_Click(object sender, System.EventArgs e)
        {
            if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
            {
                TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");

                UInt16 dev_cfg_start_offset = model.getDevCfgStartOffset(iSelectedSlaveId);
                UInt16 cap_cfg_start_offset = model.getCapCfgStartOffset(iSelectedSlaveId);
                int dev_cfg_data_size = model.getDevConfigDataSize(iSelectedSlaveId);
                int cap_cfg_data_size = model.getCapConfigDataSize(iSelectedSlaveId);
                UInt16 bytes_sent = 0;
                byte block_size = 32;
                byte[] data_block = new byte[block_size];
                byte[] crc_bytes = new byte[2];

                updateModelFromView();

                sendReadStateCmd(true);
                sendPauseKeyScanningCmd();

                bytes_sent = 0;
                byte[] cap_cfg_data = model.getCapConfigData(iSelectedSlaveId);
                while (bytes_sent < cap_cfg_data_size)
                {
                    System.Buffer.BlockCopy(cap_cfg_data, bytes_sent, data_block, 0, block_size);
                    control.SendI2CWriteCommand("Touch Config", "cap_cfg_write", (byte)iSelectedSlaveId, "Secondary", (UInt16)(cap_cfg_start_offset + bytes_sent), data_block);
                    bytes_sent += block_size;

                    if ((bytes_sent != cap_cfg_data_size) && ((bytes_sent + block_size) > cap_cfg_data_size))
                    {
                        UInt16 bytes_to_send = (UInt16)(cap_cfg_data_size - bytes_sent);
                        byte[] remaining_data_block = new byte[bytes_to_send];
                        System.Buffer.BlockCopy(cap_cfg_data, bytes_sent, remaining_data_block, 0, bytes_to_send);
                        control.SendI2CWriteCommand("Touch Config", "cap_cfg_write", (byte)iSelectedSlaveId, "Secondary", (UInt16)(cap_cfg_start_offset + bytes_sent), remaining_data_block);
                        break;
                    }
                }
                UInt16 crc = model.getCapCfgCRC(iSelectedSlaveId);
                crc_bytes[0] = (byte)(crc & 0xFF);
                crc_bytes[1] = (byte)((crc >> 8) & 0xFF);
                control.SendI2CWriteCommand("Touch Config", "cap_cfg_write", (byte)iSelectedSlaveId, "Secondary", model.getCapCfgCRCOffset(iSelectedSlaveId), crc_bytes);

                bytes_sent = 0;
                byte[] dev_cfg_data = model.getDevConfigData(iSelectedSlaveId);

                byte[] dev_cfg_classb_byte = new byte[1];
                System.Buffer.BlockCopy(dev_cfg_data, 5, dev_cfg_classb_byte, 0, 1);
                dev_cfg_classb_byte[0] = (byte)(dev_cfg_classb_byte[0] & 0xF7);
                control.SendI2CWriteCommand("Touch Config", "dev_cfg_write", (byte)iSelectedSlaveId, "Secondary", (UInt16)(dev_cfg_start_offset + 5), dev_cfg_classb_byte);
                bytes_sent = 6;

                while (bytes_sent < dev_cfg_data_size)
                {
                    System.Buffer.BlockCopy(dev_cfg_data, bytes_sent, data_block, 0, block_size);
                    control.SendI2CWriteCommand("Touch Config", "dev_cfg_write", (byte)iSelectedSlaveId, "Secondary", (UInt16)(dev_cfg_start_offset + bytes_sent), data_block);
                    bytes_sent += block_size;

                    if ((bytes_sent != dev_cfg_data_size) && ((bytes_sent + block_size) > dev_cfg_data_size))
                    {
                        UInt16 bytes_to_send = (UInt16)(dev_cfg_data_size - bytes_sent);
                        byte[] remaining_data_block = new byte[bytes_to_send];
                        System.Buffer.BlockCopy(dev_cfg_data, bytes_sent, remaining_data_block, 0, bytes_to_send);
                        control.SendI2CWriteCommand("Touch Config", "dev_cfg_write", (byte)iSelectedSlaveId, "Secondary", (UInt16)(dev_cfg_start_offset + bytes_sent), remaining_data_block);
                        break;
                    }
                }

                byte[] dev_cfg_inital_bytes = new byte[6];
                System.Buffer.BlockCopy(dev_cfg_data, 0, dev_cfg_inital_bytes, 0, 6);
                control.SendI2CWriteCommand("Touch Config", "dev_cfg_write", (byte)iSelectedSlaveId, "Secondary", (UInt16)(dev_cfg_start_offset), dev_cfg_inital_bytes);

                crc = model.getDevCfgCRC(iSelectedSlaveId);
                crc_bytes[0] = (byte)(crc & 0xFF);
                crc_bytes[1] = (byte)((crc >> 8) & 0xFF);
                control.SendI2CWriteCommand("Touch Config", "dev_cfg_write", (byte)iSelectedSlaveId, "Secondary", model.getDevCfgCRCOffset(iSelectedSlaveId), crc_bytes);
                sendResumeKeyScanningCmd();
                control.SendDelayCommand(2000);
                sendReadStateCmd(false);
            }
            else
            {
                MessageBox.Show("ERROR: Please select Touch Chip Type and Sensing method first.");
            }
        }

        private void btnReadFromDeviceFlash_Click(object sender, System.EventArgs e)
        {
            if (cbTouchChipType.Text == "Rx130")
            {
                sendReadStateCmd(true);
                control.SendI2CCommand("Touch Config", "Read from Flash", (byte)iSelectedSlaveId, 0x05, 0xF5);
                control.SendDelayCommand(2000);
                control.SendI2CCommand("Touch Config", "Read from Flash", (byte)iSelectedSlaveId, 0x06, 0xA6);
                control.SendDelayCommand(2000);
                sendResumeKeyScanningCmd("Read from Flash");
                control.SendDelayCommand(2000);
                sendReadStateCmd(false);
            }
            else if (cbTouchChipType.Text == "Rx130INT")
            {
                MessageBox.Show("ERROR: Reading from Device Flash is not supported for Rx130 Integrated touch");
            }
            else
            {
                MessageBox.Show("ERROR: Please select Touch Chip Type and Sensing method first.");
            }
        }

        private void btnWriteToDeviceFlash_Click(object sender, System.EventArgs e)
        {
            if (cbTouchChipType.Text == "Rx130")
            {
                TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");
                updateModelFromView();
                sendReadStateCmd(true);
                control.SendI2CReadCommand("Touch Config", "validate_crc", (byte)iSelectedSlaveId, "Secondary", model.getDevCfgCRCOffset(iSelectedSlaveId), 4);
                sendReadStateCmd(false);
            }
            else if (cbTouchChipType.Text == "Rx130INT")
            {
                MessageBox.Show("ERROR: Writing to Device Flash is not supported for Rx130 Integrated touch");
            }
            else
            {
                MessageBox.Show("ERROR: Please select Touch Chip Type and Sensing method first.");
            }
        }

        private void btnClear_Click(object sender, System.EventArgs e)
        {
            devCfgViewDict.Clear();
            capCfgViewDict.Clear();
            tuningViewDict.Clear();
            tcConfigTabs.Controls.Clear();

            TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");
            model.clearConfigModel();

            cbTouchChipType.Enabled = true;
            cbTouchChipType.Text = "Select the Touch Micro";

            cbTouchSeningMethod.Enabled = false;
            cbTouchSeningMethod.Text = "Select Sensing Method";
            enableButtons(true);
        }

        private void btnSoftReset_Click(object sender, System.EventArgs e)
        {
            control.SendI2CCommand("Touch Config", (byte)iSelectedSlaveId, 0x0B, 0xEA);
        }

        private void btnFactoryDefault_Click(object sender, System.EventArgs e)
        {
            if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
            {
                sendPauseKeyScanningCmd();
                control.SendI2CCommand("Touch Config", (byte)iSelectedSlaveId, 0x22, 0xe4);
                control.SendDelayCommand(2000);
                sendResumeKeyScanningCmd();
            }
            else
            {
                MessageBox.Show("ERROR: Please select Touch Chip Type and Sensing method first.");
            }
        }

        public void sendReadStateCmd(bool isStart)
        {
            if (isStart == true)
            {
                enableButtons(false);
                updateProgress(0);
                control.SendI2CReadCommand("Touch Config", "State Check Before Start", (byte)iSelectedSlaveId, "Primary", 0x00EC, 2);
            }
            else
            {
                control.SendI2CReadCommand("Touch Config", "State Check After End", (byte)iSelectedSlaveId, "Primary", 0x00EC, 2);
            }
        }

        public void sendPauseKeyScanningCmd()
        {
            control.SendI2CCommand("Touch Config", (byte)iSelectedSlaveId, 0x0E, 0x1F);
            control.SendDelayCommand(1000);
        }

        public void sendResumeKeyScanningCmd()
        {
            sendResumeKeyScanningCmd("");
        }

        public void sendResumeKeyScanningCmd(String subview)
        {
            control.SendI2CCommand("Touch Config", subview, (byte)iSelectedSlaveId, 0x0F, 0x2E);
            control.SendDelayCommand(500);
        }

        public void sendUpdateKeyDataCmd(byte keyid)
        {
            updateModelFromView();

            byte[] crc_bytes = new byte[2];
            TouchConfigModel model = (TouchConfigModel)control.GetModel("Touch Config");

            int cap_cfg_data_size = model.getCapConfigDataSize(iSelectedSlaveId);
            UInt16 cap_cfg_start_offset = model.getCapCfgStartOffset(iSelectedSlaveId);
            byte[] cap_cfg_data = model.getCapConfigData(iSelectedSlaveId);
            UInt16 offset_addr = (UInt16)(cap_cfg_start_offset + 16 + (15 * keyid));

            byte[] key_cfg_data = new byte[15];
            System.Buffer.BlockCopy(cap_cfg_data, 16 + (15 * keyid), key_cfg_data, 0, 15);
            control.SendI2CWriteCommand("Touch Config", (byte)iSelectedSlaveId, "Secondary", offset_addr, key_cfg_data);

            UInt16 crc = model.getCapCfgCRC(iSelectedSlaveId);
            crc_bytes[0] = (byte)(crc & 0xFF);
            crc_bytes[1] = (byte)((crc >> 8) & 0xFF);
            control.SendI2CWriteCommand("Touch Config", (byte)iSelectedSlaveId, "Secondary", model.getCapCfgCRCOffset(iSelectedSlaveId), crc_bytes);
        }

        public void sendRequestCounterDataCmd(byte keyid)
        {
            byte[] data = new byte[2];
            data[0] = 0x00;
            data[1] = keyid;
            control.SendI2CWriteCommand("Touch Config", (byte)iSelectedSlaveId, "Primary", 0x00EA, data);
            control.SendDelayCommand(100);
        }

        public void sendReadCounterDataCmd()
        {
            control.SendI2CReadCommand("Touch Config", "tuning_data", (byte)iSelectedSlaveId, "Primary", 0x00EA, 21);
        }

        public void sendClearQueue()
        {
            control.ClearCommandQueue();
        }

        public override void updateView(Object data)
        {
            if (data == null) return;

            TouchConfigModel model = (TouchConfigModel)data;
            if (bViewInitComplete == false)
            {
                if (control.IsOfflineModeActive() == true)
                {
                    this.iNumberOfSlaves = 1;
                }
                else
                {
                    this.iNumberOfSlaves = control.GetNumberOfSlaves();
                }
                this.initView(data);
            }
            else
            {
                if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
                {
                }
            }
        }

        private void updateProgress(int value)
        {
            pbProgressBar.Value = value;
            pbProgressBar.Text = value != 100 ? "Working..." : "Complete";
        }

        private void enableButtons(bool enable)
        {
            btnLoadFromDevice.Enabled = enable;
            btnReadFromDeviceFlash.Enabled = enable;
            btnWriteToDevice.Enabled = enable;
            btnWriteToDeviceFlash.Enabled = enable;
            tcConfigTabs.Enabled = enable;
        }

        private void updateProgress()
        {
            pbProgressBar.Value = pbProgressBar.Value + 5 < 100 ? pbProgressBar.Value + 5 : pbProgressBar.Value;
        }

        public override void updateView(string section, Object data)
        {
            TouchConfigModel model = (TouchConfigModel)data;

            if (cbTouchChipType == null) return;

            if (cbTouchChipType.Text == "Rx130" || cbTouchChipType.Text == "Rx130INT")
            {
                if (section == "i2c_dev_cfg")
                {
                    updateProgress();
                    this.devCfgViewDict[iSelectedSlaveId].updateView(String.Format("{0}", (byte)iSelectedSlaveId), (object)model);
                }
                else if (section == "i2c_cap_cfg")
                {
                    this.capCfgViewDict[iSelectedSlaveId].updateView(String.Format("{0}", (byte)iSelectedSlaveId), (object)model);
                    updateProgress();
                }
                else if (section == "State Check Before Start")
                {
                    if (model.getDeviceBusyStatus(iSelectedSlaveId) == true)
                    {
                        MessageBox.Show("Error: Touch Device is Busy ... \nPlease try again after few seconds", "ERROR");
                        control.ClearCommandQueue();
                        enableButtons(true);
                    }
                    else
                    {
                        updateProgress();
                    }
                }
                else if (section == "State Check After End")
                {
                    enableButtons(true);
                    if (model.getDeviceBusyStatus(iSelectedSlaveId) == true)
                    {
                        MessageBox.Show("Warning: Operation is complete but Touch Device is Busy ... \nPlease read configuration back from the chip to verify content", "WARNING");
                        control.ClearCommandQueue();
                    }
                    else
                    {
                        updateProgress(100);
                    }
                }
                else if (section == "cap_cfg_write" || section == "dev_cfg_write" || section == "Read from Flash")
                {
                    updateProgress();
                }
            }

            if (section == "hmi_connected")
            {
                btnLoadFromDevice.Enabled = true;
                btnWriteToDevice.Enabled = true;
                btnWriteToDeviceFlash.Enabled = true;
                btnReadFromDeviceFlash.Enabled = true;
                btnSoftReset.Enabled = true;
            }
            else if (section == "hmi_disconnected")
            {
                btnLoadFromDevice.Enabled = false;
                btnWriteToDevice.Enabled = false;
                btnWriteToDeviceFlash.Enabled = false;
                btnReadFromDeviceFlash.Enabled = false;
                btnSoftReset.Enabled = false;
            }
            else if (section == "validate_crc")
            {
                if ((model.getDevCfgCRC(iSelectedSlaveId) != model.getDevCfgDeviceCRC(iSelectedSlaveId)) ||
                    (model.getCapCfgCRC(iSelectedSlaveId) != model.getCapCfgDeviceCRC(iSelectedSlaveId)))
                {
                    MessageBox.Show("ERROR: CRC Mismatch\nConfig CRC read back from device does not match with loaded config data\nSending the Save to Flash command anyway !!!");
                }
                sendPauseKeyScanningCmd();
                control.SendI2CCommand("Touch Config", (byte)iSelectedSlaveId, 0x01, 0x31);
                control.SendDelayCommand(2000);
                control.SendI2CCommand("Touch Config", (byte)iSelectedSlaveId, 0x02, 0x62);
                control.SendDelayCommand(2000);
                sendResumeKeyScanningCmd();
            }
            else if (section == "i2c_read_crc")
            {
                if ((model.getDevCfgCRC(iSelectedSlaveId) != model.getDevCfgDeviceCRC(iSelectedSlaveId)) ||
                    (model.getCapCfgCRC(iSelectedSlaveId) != model.getCapCfgDeviceCRC(iSelectedSlaveId)))
                {
                    MessageBox.Show(String.Format("ERROR: CRC Mismatch\nDev CRC Calculated: 0x{0:X4}\nDev CRC Read from Device: 0x{1:X4}\nCap CRC Calculated: 0x{2:X4}\nCap CRC Read from Device: 0x{3:X4}\n",
                                    model.getDevCfgCRC(iSelectedSlaveId), model.getDevCfgDeviceCRC(iSelectedSlaveId),
                                    model.getCapCfgCRC(iSelectedSlaveId), model.getCapCfgDeviceCRC(iSelectedSlaveId)));
                }
            }
            else if (section == "tuning_data")
            {
                String tuning_section_data = String.Format("tuning_data,{0}", iSelectedSlaveId);
                if (this.capCfgViewDict.ContainsKey(iSelectedSlaveId) == true)
                {
                    this.capCfgViewDict[iSelectedSlaveId].updateView(tuning_section_data, (object)model);
                }
            }
            else if (section == "Heart Beat")
            {
                lbHeartBeatCnt.Text = String.Format("HB Count: {0}", model.getHearBeatCounter((byte)iSelectedSlaveId));
            }
        }

        public TouchCommanderTabBase getDevCfgViewForSlave(int slave_id)
        {
            return (TouchCommanderTabBase)devCfgViewDict[slave_id];
        }

        public TouchCommanderTabBase getCapCfgViewForSlave(int slave_id)
        {
            return (TouchCommanderTabBase)capCfgViewDict[slave_id];
        }
    }
}