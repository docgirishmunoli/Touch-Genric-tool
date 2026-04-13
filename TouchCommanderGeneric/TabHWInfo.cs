using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WideBoxLib;

namespace TouchCommanderGenericNamespace
{
    public struct HBInfo
    {
        public int current;
        public int previous;
    }

    public struct SlaveInfo
    {
        public byte SlaveIndex;
        public byte SlaveAddr;
        public byte SlaveBusIndex;
        public byte DeviceType;
        public byte CapSenseType;
        public byte FirmwareType;
        public byte KeysPerDevice;
        public byte[] KeyMapping;
        public HBInfo HBInfo;
        public int KeyPressCount;
    }

    public struct HardwareInfo
    {
        public byte deviceSignature;
        public byte TouchI2c_GDM_Major_Version;
        public byte TouchI2c_GDM_Minor_Version;
        public byte TouchI2c_GDM_Test_Version;
        public byte API017Touch_GDM_Major_Version;
        public byte API017Touch_GDM_Minor_Version;
        public byte API017Touch_GDM_Test_Version;
        public byte numberOfSlaves;
        public byte numberOfKeys;
        public SlaveInfo[] Slave_Hw_Info;
        public HBInfo HostHBInfo;
    }

    public struct DeviceInfo
    {
        public String TouchConfigWPNum;
        public String TouchConfigVersion;
        public String TouchFirmwareWPNum;
        public String TouchFirmwareVersion;
        public String TouchFirmwareDate;
    }

    public class HardwareInfoModel : TouchCommanderModelBase
    {
        private DeviceInfo[] devInfo;
        private HardwareInfo hwInfo;
        private List<string> deviceType = new List<string>() { "PSoC4000", "PSoC4000S", "PSoC4100S", "Rx130I2C", "Rx130INT" };
        private List<string> capsenseType = new List<string>() { "Self", "Mutual" };
        private List<string> firmwareType = new List<string>() { "Class A", "Class B" };

        private bool bModelReady;
        private byte current_slave;

        public HardwareInfoModel(TouchCommanderController c) : base(c)
        {
            hwInfo = new HardwareInfo();
            bModelReady = false;
            current_slave = 0;
        }

        public override void updateModel(Object d)
        {
            byte[] data = (byte[])d;

            if (data[0] == 0xDE)
            {
                hwInfo.deviceSignature = data[0];
                hwInfo.TouchI2c_GDM_Major_Version = data[1];
                hwInfo.TouchI2c_GDM_Minor_Version = data[2];
                hwInfo.TouchI2c_GDM_Test_Version = data[3];
                control.SetTouchI2cGDMVersion(hwInfo.TouchI2c_GDM_Major_Version, hwInfo.TouchI2c_GDM_Minor_Version,
                                                hwInfo.TouchI2c_GDM_Test_Version);
                hwInfo.API017Touch_GDM_Major_Version = data[4];
                hwInfo.API017Touch_GDM_Minor_Version = data[5];
                hwInfo.API017Touch_GDM_Test_Version = data[6];
                control.SetAPI17GDMVersion(hwInfo.API017Touch_GDM_Major_Version, hwInfo.API017Touch_GDM_Minor_Version,
                                                hwInfo.API017Touch_GDM_Test_Version);
                hwInfo.numberOfSlaves = data[7];
                hwInfo.numberOfKeys = data[8];
                hwInfo.Slave_Hw_Info = new SlaveInfo[hwInfo.numberOfSlaves];

                byte[] i2c_addrs = new byte[hwInfo.numberOfSlaves];
                byte[] i2c_busses = new byte[hwInfo.numberOfSlaves];
                int key_count_offset = 0;
                for (int i = 0; i < hwInfo.numberOfSlaves; i++)
                {
                    int start_indx = 9 + hwInfo.numberOfKeys + (i * 7);
                    hwInfo.Slave_Hw_Info[i].SlaveIndex = data[start_indx];
                    hwInfo.Slave_Hw_Info[i].SlaveAddr = data[start_indx + 1];
                    hwInfo.Slave_Hw_Info[i].SlaveBusIndex = data[start_indx + 2];
                    hwInfo.Slave_Hw_Info[i].DeviceType = data[start_indx + 3];
                    hwInfo.Slave_Hw_Info[i].CapSenseType = data[start_indx + 4];
                    hwInfo.Slave_Hw_Info[i].FirmwareType = data[start_indx + 5];
                    hwInfo.Slave_Hw_Info[i].KeysPerDevice = data[start_indx + 6];
                    hwInfo.Slave_Hw_Info[i].KeyMapping = new byte[hwInfo.Slave_Hw_Info[i].KeysPerDevice];

                    for (int j = 0; j < hwInfo.Slave_Hw_Info[i].KeysPerDevice; j++)
                    {
                        hwInfo.Slave_Hw_Info[i].KeyMapping[j] = data[9 + key_count_offset + j];
                    }

                    i2c_addrs[i] = hwInfo.Slave_Hw_Info[i].SlaveAddr;
                    i2c_busses[i] = hwInfo.Slave_Hw_Info[i].SlaveBusIndex;
                    key_count_offset = hwInfo.Slave_Hw_Info[i].KeysPerDevice;
                }

                control.SetSlaveI2CAddressList(i2c_addrs);
                control.SetSlaveI2CBusList(i2c_busses);
                control.StartStatusReportPublishing(hwInfo.numberOfSlaves, true);
                control.SetNumberOfSlaves(hwInfo.numberOfSlaves);
                {
                    byte[][] key_mapping = new byte[hwInfo.numberOfSlaves][];
                    for (int i = 0; i < hwInfo.numberOfSlaves; i++)
                    {
                        key_mapping[i] = hwInfo.Slave_Hw_Info[i].KeyMapping.ToArray();
                        control.SetDeviceType((byte)i, deviceType[hwInfo.Slave_Hw_Info[i].DeviceType]);
                        control.SetSensingType((byte)i, capsenseType[hwInfo.Slave_Hw_Info[i].CapSenseType]);
                        control.SetFirmwareType((byte)i, firmwareType[hwInfo.Slave_Hw_Info[i].FirmwareType]);
                        control.SetKeysPerSlave((byte)i, hwInfo.Slave_Hw_Info[i].KeysPerDevice);
                    }
                    control.SetKeyMapping(key_mapping);
                }
                devInfo = new DeviceInfo[hwInfo.numberOfSlaves];

                bModelReady = true;

                for (int i = 0; i < hwInfo.numberOfSlaves; i++)
                {
                    control.SendI2CCommand("HW Info", (byte)i, 0x0B, 0xEA);
                    control.SendDelayCommand(1000);
                    control.SendI2CCommand("HW Info", (byte)i, 0x0B, 0xEA);
                    control.SendDelayCommand(1000);
                    control.SendI2CReadCommand("HW Info", "Device Info", (byte)i, "Primary", (UInt16)(0x0030), 27);
                    control.SendDelayCommand(1000);
                    control.SendI2CReadCommand("HW Info", "Key Map", (byte)i, "Secondary", (UInt16)(0x000C), 4);
                }
            }
        }

        public override void updateModel(Object d, string section)
        {
            if (section == "Heart Beat")
            {
                byte[] data = (byte[])d;

                hwInfo.HostHBInfo.previous = hwInfo.HostHBInfo.current;
                hwInfo.HostHBInfo.current = (data[1] << 8) | data[0];

                for (int i = 0; i < hwInfo.numberOfSlaves; i++)
                {
                    hwInfo.Slave_Hw_Info[i].HBInfo.previous = hwInfo.Slave_Hw_Info[i].HBInfo.current;
                    hwInfo.Slave_Hw_Info[i].HBInfo.current = (data[2 + (4 * i) + 1] << 8) | data[2 + (4 * i)];
                    hwInfo.Slave_Hw_Info[i].KeyPressCount = (data[4 + (4 * i) + 1] << 8) | data[4 + (4 * i)];
                }
            }
            else if (section == "Device Info")
            {
                byte[] data = (byte[])d;

                byte slave_id = control.GetSlaveIdFromI2CAddress((byte)(data[1] >> 1));

                if (devInfo == null)
                {
                    return;
                }

                current_slave = slave_id;

                devInfo[slave_id].TouchConfigWPNum = "";
                for (int i = 0; i < 9; i++)
                {
                    devInfo[slave_id].TouchConfigWPNum += String.Format("{0}", Convert.ToChar(data[6 + i]));
                }

                devInfo[slave_id].TouchConfigVersion = String.Format("{0}.{1}.{2}", data[15], data[16], data[17]);

                devInfo[slave_id].TouchFirmwareWPNum = "";
                for (int i = 0; i < 9; i++)
                {
                    devInfo[slave_id].TouchFirmwareWPNum += String.Format("{0}", Convert.ToChar(data[18 + i]));
                }

                devInfo[slave_id].TouchFirmwareDate = String.Format("{0}-{1}-{2} (YY-MM-DD)", data[27], data[28], data[29]);
                devInfo[slave_id].TouchFirmwareVersion = String.Format("{0}.{1}.{2}", data[30], data[31], data[32]);

                if (slave_id == (hwInfo.numberOfSlaves - 1))
                {
                    control.StartHeartBeatForSlave();
                }
            }
            else if (section == "Key Map")
            {
                byte[] data = (byte[])d;
                int i2c_address = (data[1] >> 1) ^ 0x02;
                byte slave_id = control.GetSlaveIdFromI2CAddress((byte)(i2c_address));

                Console.WriteLine("Slave Id: [{0}], Widget Enable: [{1:x}].[{2:x}].[{3:x}].[{4:x}]", slave_id, data[6], data[7], data[8], data[9]);
            }
        }

        public HardwareInfo getHwInfo()
        {
            return hwInfo;
        }

        public void GetTouchI2cGDMVersion(out byte major, out byte minor, out byte test)
        {
            major = hwInfo.TouchI2c_GDM_Major_Version;
            minor = hwInfo.TouchI2c_GDM_Minor_Version;
            test = hwInfo.TouchI2c_GDM_Test_Version;
        }

        public void GetAPI17GDMVersion(out byte major, out byte minor, out byte test)
        {
            major = hwInfo.API017Touch_GDM_Major_Version;
            minor = hwInfo.API017Touch_GDM_Minor_Version;
            test = hwInfo.API017Touch_GDM_Test_Version;
        }

        public byte GetNumSlaves()
        {
            return hwInfo.numberOfSlaves;
        }

        public byte GetNumKeys()
        {
            return hwInfo.numberOfKeys;
        }

        public byte GetSlaveAddr(byte indx)
        {
            return hwInfo.Slave_Hw_Info[indx].SlaveAddr;
        }

        public byte GetSlaveBusIndex(byte indx)
        {
            return hwInfo.Slave_Hw_Info[indx].SlaveBusIndex;
        }

        public string GetDeviceType(byte indx)
        {
            return deviceType[hwInfo.Slave_Hw_Info[indx].DeviceType];
        }

        public string GetCapsenseType(byte indx)
        {
           return capsenseType[hwInfo.Slave_Hw_Info[indx].CapSenseType];
        }

        public string GetFirmwareType(byte indx)
        {
            return firmwareType[hwInfo.Slave_Hw_Info[indx].FirmwareType];
        }

        public byte GetNumKeys(byte indx)
        {
            return hwInfo.Slave_Hw_Info[indx].KeysPerDevice;
        }

        public byte[] GetKeyList(byte indx)
        {
            return hwInfo.Slave_Hw_Info[indx].KeyMapping;
        }

        public int GetSlaveHBCounter(byte indx)
        {
            return hwInfo.Slave_Hw_Info[indx].HBInfo.current;
        }

        public int GetSlaveKeyPressCounter(byte indx)
        {
            return hwInfo.Slave_Hw_Info[indx].KeyPressCount;
        }

        public int GetHostHBCounter()
        {
            return hwInfo.HostHBInfo.current / 1000; // Return value in seconds
        }

        public bool IsModelReady()
        {
            return bModelReady;
        }

        public byte GetCurrenSlaveId()
        {
            return current_slave;
        }

        public DeviceInfo GetDeviceInfo(byte indx)
        {
            return devInfo[indx];
        }

    }

    public class HWInfoTabPage : TouchCommanderTabBase
    {
        private GroupBox gbHWInfo;
        private Label lbTouchI2cGDMVersion;
        private Label lbApi17GDMVersion;
        private Label lbNumOfSlaves;
        private Label lbNumOfKeys;
        private Label lbHostHeartBeatCount;

        public struct slaveinfoview
        {
            public GroupBox gbSlaveInfo;
            public Label lbSlaveAddress;
            public Label lbSlaveBusIndex;
            public Label lbDeviceType;
            public Label lbCapsenseType;
            public Label lbFirmwareType;
            public Label lbNumKeys;
            public GroupBox gbKeyList;
            public Button[] btnKeyList;
            public Label lbHeartBeatCount;
            public Label lbKeyPressCount;
            public Label lbTouchConfigWPNum;
            public Label lbTouchConfigVersion;
            public Label lbTouchFirmwareWPNum;
            public Label lbTouchFirmwareVersion;
            public Label lbTouchFirmwareDate;
        }

        private slaveinfoview[] slaves;
        private bool bViewInitComplete;

        public HWInfoTabPage(TouchCommanderController c) : base (c)
        {
            this.Text = "Hardware Info";
            this.bViewInitComplete = false;
        }

        private void initView(object data)
        {
            HardwareInfoModel model = (HardwareInfoModel)data;

            gbHWInfo = new GroupBox();
            gbHWInfo.Location = new Point(20, 10);
            gbHWInfo.Size = new Size(350, 250);
            gbHWInfo.Text = "Generic Hardware Info";
            gbHWInfo.Font = new Font(gbHWInfo.Font.Name, gbHWInfo.Font.Size + 2.0F, gbHWInfo.Font.Style, gbHWInfo.Font.Unit);
            this.Controls.Add(gbHWInfo);

            lbTouchI2cGDMVersion = new Label();
            lbTouchI2cGDMVersion.Location = new Point(20, 40);
            lbTouchI2cGDMVersion.Size = new Size(gbHWInfo.Width - 80, 30);
            lbTouchI2cGDMVersion.Text = "Touch I2C GDM Ver: 0.0.0";
            lbTouchI2cGDMVersion.Font = new Font(lbTouchI2cGDMVersion.Font.Name, lbTouchI2cGDMVersion.Font.Size + 4.0F,
                                            lbTouchI2cGDMVersion.Font.Style | FontStyle.Bold, lbTouchI2cGDMVersion.Font.Unit);
            gbHWInfo.Controls.Add(lbTouchI2cGDMVersion);

            lbApi17GDMVersion = new Label();
            lbApi17GDMVersion.Location = new Point(20, 80);
            lbApi17GDMVersion.Size = new Size(gbHWInfo.Width - 80, 30);
            lbApi17GDMVersion.Text = "API17 GDM Ver: 0.0.0";
            lbApi17GDMVersion.Font = new Font(lbApi17GDMVersion.Font.Name, lbApi17GDMVersion.Font.Size + 4.0F,
                                            lbApi17GDMVersion.Font.Style | FontStyle.Bold, lbApi17GDMVersion.Font.Unit);
            gbHWInfo.Controls.Add(lbApi17GDMVersion);

            lbNumOfSlaves = new Label();
            lbNumOfSlaves.Location = new Point(20, 120);
            lbNumOfSlaves.Size = new Size(gbHWInfo.Width - 80, 30);
            lbNumOfSlaves.Text = "Number of Slaves: 0";
            lbNumOfSlaves.Font = new Font(lbNumOfSlaves.Font.Name, lbNumOfSlaves.Font.Size + 4.0F,
                                            lbNumOfSlaves.Font.Style | FontStyle.Bold, lbNumOfSlaves.Font.Unit);
            gbHWInfo.Controls.Add(lbNumOfSlaves);

            lbNumOfKeys = new Label();
            lbNumOfKeys.Location = new Point(20, 160);
            lbNumOfKeys.Size = new Size(gbHWInfo.Width - 80, 30);
            lbNumOfKeys.Text = "Number of Keys: 0";
            lbNumOfKeys.Font = new Font(lbNumOfKeys.Font.Name, lbNumOfKeys.Font.Size + 4.0F,
                                            lbNumOfKeys.Font.Style | FontStyle.Bold, lbNumOfKeys.Font.Unit);
            gbHWInfo.Controls.Add(lbNumOfKeys);

            lbHostHeartBeatCount = new Label();
            lbHostHeartBeatCount.Location = new Point(20, 200);
            lbHostHeartBeatCount.Size = new Size(gbHWInfo.Width - 80, 30);
            lbHostHeartBeatCount.Text = "Host Heart Beat: 0";
            lbHostHeartBeatCount.Font = new Font(lbHostHeartBeatCount.Font.Name, lbHostHeartBeatCount.Font.Size + 4.0F,
                                            lbHostHeartBeatCount.Font.Style | FontStyle.Bold, lbHostHeartBeatCount.Font.Unit);
            gbHWInfo.Controls.Add(lbHostHeartBeatCount);

            slaves = new slaveinfoview[model.GetNumSlaves()];
            for (int i = 0; i < model.GetNumSlaves(); i++)
            {
                slaves[i].gbSlaveInfo = new GroupBox();
                slaves[i].gbSlaveInfo.Location = new Point(400 + (i * 570), 10);
                slaves[i].gbSlaveInfo.Size = new Size(550, 780);
                slaves[i].gbSlaveInfo.Text = string.Format("Slave {0}: Info", i);
                slaves[i].gbSlaveInfo.Font = new Font(slaves[i].gbSlaveInfo.Font.Name, slaves[i].gbSlaveInfo.Font.Size + 2.0F,
                                                slaves[i].gbSlaveInfo.Font.Style, slaves[i].gbSlaveInfo.Font.Unit);
                this.Controls.Add(slaves[i].gbSlaveInfo);

                slaves[i].lbSlaveAddress = new Label();
                slaves[i].lbSlaveAddress.Location = new Point(20, 40);
                slaves[i].lbSlaveAddress.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbSlaveAddress.Text = "I2C Slave Address: 0";
                slaves[i].lbSlaveAddress.Font = new Font(slaves[i].lbSlaveAddress.Font.Name, slaves[i].lbSlaveAddress.Font.Size + 4.0F,
                                                slaves[i].lbSlaveAddress.Font.Style | FontStyle.Bold, slaves[i].lbSlaveAddress.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbSlaveAddress);

                slaves[i].lbSlaveBusIndex = new Label();
                slaves[i].lbSlaveBusIndex.Location = new Point(20, 80);
                slaves[i].lbSlaveBusIndex.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbSlaveBusIndex.Text = "I2C BUS Index: 0";
                slaves[i].lbSlaveBusIndex.Font = new Font(slaves[i].lbSlaveBusIndex.Font.Name, slaves[i].lbSlaveBusIndex.Font.Size + 4.0F,
                                                slaves[i].lbSlaveBusIndex.Font.Style | FontStyle.Bold, slaves[i].lbSlaveBusIndex.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbSlaveBusIndex);

                slaves[i].lbDeviceType = new Label();
                slaves[i].lbDeviceType.Location = new Point(20, 120);
                slaves[i].lbDeviceType.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbDeviceType.Text = "Device Type: PSoC 4000S";
                slaves[i].lbDeviceType.Font = new Font(slaves[i].lbDeviceType.Font.Name, slaves[i].lbDeviceType.Font.Size + 4.0F,
                                                slaves[i].lbDeviceType.Font.Style | FontStyle.Bold, slaves[i].lbDeviceType.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbDeviceType);

                slaves[i].lbCapsenseType = new Label();
                slaves[i].lbCapsenseType.Location = new Point(20, 160);
                slaves[i].lbCapsenseType.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbCapsenseType.Text = "Capsense Type: Mutual";
                slaves[i].lbCapsenseType.Font = new Font(slaves[i].lbCapsenseType.Font.Name, slaves[i].lbCapsenseType.Font.Size + 4.0F,
                                                slaves[i].lbCapsenseType.Font.Style | FontStyle.Bold, slaves[i].lbCapsenseType.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbCapsenseType);

                slaves[i].lbFirmwareType = new Label();
                slaves[i].lbFirmwareType.Location = new Point(20, 200);
                slaves[i].lbFirmwareType.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbFirmwareType.Text = "Firmware Type: Class B";
                slaves[i].lbFirmwareType.Font = new Font(slaves[i].lbFirmwareType.Font.Name, slaves[i].lbFirmwareType.Font.Size + 4.0F,
                                                slaves[i].lbFirmwareType.Font.Style | FontStyle.Bold, slaves[i].lbFirmwareType.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbFirmwareType);

                slaves[i].lbNumKeys = new Label();
                slaves[i].lbNumKeys.Location = new Point(20, 240);
                slaves[i].lbNumKeys.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbNumKeys.Text = "Keys per slave: 0";
                slaves[i].lbNumKeys.Font = new Font(slaves[i].lbNumKeys.Font.Name, slaves[i].lbNumKeys.Font.Size + 4.0F,
                                                slaves[i].lbNumKeys.Font.Style | FontStyle.Bold, slaves[i].lbNumKeys.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbNumKeys);

                slaves[i].gbKeyList = new GroupBox();
                slaves[i].gbKeyList.Location = new Point(20, 280);
                slaves[i].gbKeyList.Size = new Size(slaves[i].gbSlaveInfo.Width - 40, 200);
                slaves[i].gbKeyList.Text = "List of Keys";
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].gbKeyList);

                slaves[i].btnKeyList = new Button[24];

                for (int j = 0; j < 24; j++)
                {
                    slaves[i].btnKeyList[j] = new Button();
                    slaves[i].btnKeyList[j].Location = new Point(20 + ((j % 8) * 60), 40 + ((j / 8) * 54));
                    slaves[i].btnKeyList[j].Size = new Size(50, 40);
                    slaves[i].btnKeyList[j].Text = string.Format("S{0}", j);
                    slaves[i].gbKeyList.Controls.Add(slaves[i].btnKeyList[j]);
                }

                slaves[i].lbHeartBeatCount = new Label();
                slaves[i].lbHeartBeatCount.Location = new Point(20, 500);
                slaves[i].lbHeartBeatCount.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbHeartBeatCount.Text = "Heart Beat Counter: 0";
                slaves[i].lbHeartBeatCount.Font = new Font(slaves[i].lbHeartBeatCount.Font.Name, slaves[i].lbHeartBeatCount.Font.Size + 4.0F,
                                                slaves[i].lbHeartBeatCount.Font.Style | FontStyle.Bold, slaves[i].lbHeartBeatCount.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbHeartBeatCount);

                slaves[i].lbKeyPressCount = new Label();
                slaves[i].lbKeyPressCount.Location = new Point(20, 540);
                slaves[i].lbKeyPressCount.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbKeyPressCount.Text = "Key Press Counter: 0";
                slaves[i].lbKeyPressCount.Font = new Font(slaves[i].lbKeyPressCount.Font.Name, slaves[i].lbKeyPressCount.Font.Size + 4.0F,
                                                slaves[i].lbKeyPressCount.Font.Style | FontStyle.Bold, slaves[i].lbKeyPressCount.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbKeyPressCount);

                slaves[i].lbTouchConfigWPNum = new Label();
                slaves[i].lbTouchConfigWPNum.Location = new Point(20, 580);
                slaves[i].lbTouchConfigWPNum.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbTouchConfigWPNum.Text = "Touch Config WPart: W00000000";
                slaves[i].lbTouchConfigWPNum.Font = new Font(slaves[i].lbTouchConfigWPNum.Font.Name, slaves[i].lbTouchConfigWPNum.Font.Size + 4.0F,
                                                slaves[i].lbTouchConfigWPNum.Font.Style | FontStyle.Bold, slaves[i].lbTouchConfigWPNum.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbTouchConfigWPNum);

                slaves[i].lbTouchConfigVersion = new Label();
                slaves[i].lbTouchConfigVersion.Location = new Point(20, 620);
                slaves[i].lbTouchConfigVersion.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbTouchConfigVersion.Text = "Touch Config Version: 0.0.0";
                slaves[i].lbTouchConfigVersion.Font = new Font(slaves[i].lbTouchConfigVersion.Font.Name, slaves[i].lbTouchConfigVersion.Font.Size + 4.0F,
                                                slaves[i].lbTouchConfigVersion.Font.Style | FontStyle.Bold, slaves[i].lbTouchConfigVersion.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbTouchConfigVersion);

                slaves[i].lbTouchFirmwareWPNum = new Label();
                slaves[i].lbTouchFirmwareWPNum.Location = new Point(20, 660);
                slaves[i].lbTouchFirmwareWPNum.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbTouchFirmwareWPNum.Text = "Touch Firmware WPart: W00000000";
                slaves[i].lbTouchFirmwareWPNum.Font = new Font(slaves[i].lbTouchFirmwareWPNum.Font.Name, slaves[i].lbTouchFirmwareWPNum.Font.Size + 4.0F,
                                                slaves[i].lbTouchFirmwareWPNum.Font.Style | FontStyle.Bold, slaves[i].lbTouchFirmwareWPNum.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbTouchFirmwareWPNum);

                slaves[i].lbTouchFirmwareVersion = new Label();
                slaves[i].lbTouchFirmwareVersion.Location = new Point(20, 700);
                slaves[i].lbTouchFirmwareVersion.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbTouchFirmwareVersion.Text = "Touch Firmware Version: 0.0.0";
                slaves[i].lbTouchFirmwareVersion.Font = new Font(slaves[i].lbTouchFirmwareVersion.Font.Name, slaves[i].lbTouchFirmwareVersion.Font.Size + 4.0F,
                                                slaves[i].lbTouchFirmwareVersion.Font.Style | FontStyle.Bold, slaves[i].lbTouchFirmwareVersion.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbTouchFirmwareVersion);

                slaves[i].lbTouchFirmwareDate = new Label();
                slaves[i].lbTouchFirmwareDate.Location = new Point(20, 740);
                slaves[i].lbTouchFirmwareDate.Size = new Size(slaves[i].gbSlaveInfo.Width - 80, 30);
                slaves[i].lbTouchFirmwareDate.Text = "Touch Firmware Date: 00-00-00";
                slaves[i].lbTouchFirmwareDate.Font = new Font(slaves[i].lbTouchFirmwareDate.Font.Name, slaves[i].lbTouchFirmwareDate.Font.Size + 4.0F,
                                                slaves[i].lbTouchFirmwareDate.Font.Style | FontStyle.Bold, slaves[i].lbTouchFirmwareDate.Font.Unit);
                slaves[i].gbSlaveInfo.Controls.Add(slaves[i].lbTouchFirmwareDate);
            }
            this.bViewInitComplete = true;
        }

        public override void updateView(Object data)
        {
            if (data == null) return;

            HardwareInfoModel model = (HardwareInfoModel)data;

            if (model.IsModelReady() == false) return;

            if (bViewInitComplete == false)
            {
                this.initView(data);
            }
            this.updateTouchI2cGDMVersion(model);
            this.updateAPI17GDMVersion(model);
            this.updateNumSlaves(model);
            this.updateNumKeys(model);

            for (byte i = 0; i < model.GetNumSlaves(); i++)
            {
                slaves[i].gbSlaveInfo.Text = string.Format("Slave {0}: Info", i);
                updateSlaveAddress(i, model);
                updateSlaveBusIndex(i, model);
                updateDeviceType(i, model);
                updateCapsenseType(i, model);
                updateFirmwareType(i, model);
                updateNumKeysPerSlave(i, model);
                updateKeyMapping(i, model);
            }
        }

        public override void updateView(string section, Object data)
        {
            if (data == null) return;

            HardwareInfoModel model = (HardwareInfoModel)data;

            if (model.IsModelReady() == false) return;

            if (section == "Heart Beat")
            {
                for (byte i = 0; i < model.GetNumSlaves(); i++)
                {
                    slaves[i].lbHeartBeatCount.Text = string.Format("Heart Beat Counter: {0}", model.GetSlaveHBCounter(i));
                    slaves[i].lbKeyPressCount.Text = string.Format("Key Press Counter: {0}", model.GetSlaveKeyPressCounter(i));
                }
                lbHostHeartBeatCount.Text = string.Format("Host Heart Beat: {0}", model.GetHostHBCounter());
            }
            else if (section == "Device Info")
            {
                byte current_slave = model.GetCurrenSlaveId();
                DeviceInfo devinfo = model.GetDeviceInfo(current_slave);
                slaves[current_slave].lbTouchConfigWPNum.Text = String.Format("Touch Config WPart: {0}", devinfo.TouchConfigWPNum);
                slaves[current_slave].lbTouchConfigVersion.Text = String.Format("Touch Config Version: {0}", devinfo.TouchConfigVersion);
                slaves[current_slave].lbTouchFirmwareWPNum.Text = String.Format("Touch Firmware WPart: {0}", devinfo.TouchFirmwareWPNum);
                slaves[current_slave].lbTouchFirmwareVersion.Text = String.Format("Touch Firmware Version: {0}", devinfo.TouchFirmwareVersion);
                slaves[current_slave].lbTouchFirmwareDate.Text = String.Format("Touch Firmware Date: {0}", devinfo.TouchFirmwareDate);
            }
        }

        private void updateTouchI2cGDMVersion(HardwareInfoModel model)
        {
            byte major;
            byte minor;
            byte test;
            model.GetTouchI2cGDMVersion(out major, out minor, out test);
            lbTouchI2cGDMVersion.Text = string.Format("Touch I2C GDM Ver: {0}.{1}.{2}", major, minor, test);
        }

        private void updateAPI17GDMVersion(HardwareInfoModel model)
        {
            byte major;
            byte minor;
            byte test;
            model.GetAPI17GDMVersion(out major, out minor, out test);
            lbApi17GDMVersion.Text = string.Format("API17 GDM Ver: {0}.{1}.{2}", major, minor, test);
        }

        private void updateNumSlaves(HardwareInfoModel model)
        {
            lbNumOfSlaves.Text = string.Format("Number of Slaves: {0}", model.GetNumSlaves());
        }

        private void updateNumKeys(HardwareInfoModel model)
        {
            lbNumOfKeys.Text = string.Format("Number of Keys: {0}", model.GetNumKeys());
        }

        private void updateSlaveAddress(byte indx, HardwareInfoModel model)
        {
            slaves[indx].lbSlaveAddress.Text = string.Format("I2C Slave Address: {0}", model.GetSlaveAddr(indx));
        }

        private void updateSlaveBusIndex(byte indx, HardwareInfoModel model)
        {
            slaves[indx].lbSlaveBusIndex.Text = string.Format("I2C BUS Index: {0}", model.GetSlaveBusIndex(indx));
        }

        private void updateDeviceType(byte indx, HardwareInfoModel model)
        {
            slaves[indx].lbDeviceType.Text = string.Format("Device Type: : {0}", model.GetDeviceType(indx));
        }

        private void updateCapsenseType(byte indx, HardwareInfoModel model)
        {
            slaves[indx].lbCapsenseType.Text = string.Format("Capsense Type: : {0}", model.GetCapsenseType(indx));
        }

        private void updateFirmwareType(byte indx, HardwareInfoModel model)
        {
            slaves[indx].lbFirmwareType.Text = string.Format("Firmware Type: : {0}", model.GetFirmwareType(indx));
        }

        private void updateNumKeysPerSlave(byte indx, HardwareInfoModel model)
        {
            slaves[indx].lbNumKeys.Text = string.Format("Keys per slave: {0}", model.GetNumKeys(indx));
        }

        private void updateKeyMapping(byte indx, HardwareInfoModel model)
        {
            byte[] key_mapping = model.GetKeyList(indx);
            for (int j = 0; j < model.GetNumKeys(indx); j++)
            {
                slaves[indx].btnKeyList[key_mapping[j]].BackColor = Color.LightGreen;
            }
        }
    }
}