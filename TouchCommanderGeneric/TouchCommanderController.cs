using System;
using System.Windows.Forms;

namespace TouchCommanderGenericNamespace
{
    public struct Version
    {
        public byte major;
        public byte minor;
        public byte validation;
    }

    public class TouchCommanderController
    {
        TouchCommanderGenericForm widebox;
        TouchCommanderMainView mainView;
        TouchCommanderMainModel mainModel;

        byte[] i2c_addr_list;
        byte[] i2c_bus_list;
        byte[][] key_mapping;
        byte[] keys_per_slave;
        byte no_of_slaves;
        String[] device_type;
        String[] actual_device_type;
        String[] sensing_type;
        String[] firmware_type;
        Version TouchI2cGDMVer = new Version();
        Version API17GDMVer = new Version();
        bool offline_mode;

        public TouchCommanderController(TouchCommanderGenericForm form)
        {
            widebox = form;
            mainView = new TouchCommanderMainView(this);
            mainModel = new TouchCommanderMainModel(this);
            no_of_slaves = 0;
            offline_mode = true;
        }

        public Control getMainView()
        {
            return mainView;
        }

        public TouchCommanderModelBase GetModel(string name)
        {
            return mainModel.getModel(name);
        }

        public TouchCommanderTabBase GetView(string name)
        {
            return mainView.getView(name);
        }

        public void setHMINodeId(int nodeID)
        {
            widebox.setHMINodeId((byte)nodeID);
        }

        public void RequestHWInfo()
        {
            widebox.SendRuequestTouchHardwreInformation();
        }

        public void RequestActualSensorInfo()
        {
            widebox.SendRuequestActualSensorInfo();
        }

        public void ClearData()
        {
            mainModel.Clear();
            mainView.Clear();
            ClearCommandQueue();
        }

        public void StartHeartBeatForSlave()
        {
            widebox.SendRequestHeartBeat();
        }

        public void StartStatusReportPublishing(byte slave_count, bool periodic)
        {
            widebox.SendRequestStatusReport(slave_count, periodic);
        }

        public void SendI2CCommand(String source_view, byte slaveid, byte command, byte oprand)
        {
            SendI2CCommand(source_view, "", slaveid, command, oprand);
        }
        public void SendI2CCommand(String source_view, String subview, byte slaveid, byte command, byte oprand)
        {
            widebox.SendI2CCommand(source_view, subview, slaveid, command, oprand);
        }

        public void SendI2CWriteCommand(String source_view, byte slaveid, String address_type, UInt16 offset_addr, byte[] data)
        {
            SendI2CWriteCommand(source_view, "", slaveid, address_type, offset_addr, data);
        }
        public void SendI2CWriteCommand(String source_view, String subview, byte slaveid, String address_type, UInt16 offset_addr, byte[] data)
        {
            widebox.SendI2CWriteCommand(source_view, subview, slaveid, address_type, offset_addr, data);
        }

        public void SendI2CReadCommand(String source_view, byte slaveid, String address_type, UInt16 offset_addr, byte bytes_to_read)
        {
            SendI2CReadCommand(source_view, "", slaveid, address_type, offset_addr, bytes_to_read);
        }
        public void SendI2CReadCommand(String source_view, String subview, byte slaveid, String address_type, UInt16 offset_addr, byte bytes_to_read)
        {
            widebox.SendI2CReadCommand(source_view, subview, slaveid, address_type, offset_addr, bytes_to_read);
        }

        public void SendDelayCommand(UInt16 duration)
        {
            widebox.SendDelayCommand(duration);
        }

        public void ClearCommandQueue()
        {
            widebox.ClearCommandQueue();
        }

        public void SendStartStopRawRefDeltaPMonitoring(bool start)
        {
            widebox.SendStartStopRawRefDeltaPMonitoring(start);
        }

        public void SendStartStopCPMonitoring(bool start, byte slaveid)
        {
            widebox.SendStartStopCPMonitoring(start, slaveid);
        }

        public void update(string name, byte[] data)
        {
            mainModel.updateModel(name, data);
            mainView.updateView(name, mainModel.getModel(name));
        }

        public void update(string name, string section, byte[] data)
        {
            mainModel.updateModel(name, section, data);
            mainView.updateView(name, section, mainModel.getModel(name));
        }

        public void SetSlaveI2CAddressList(byte[] i2c_addrs)
        {
            i2c_addr_list = i2c_addrs;
        }

        public void SetTouchI2cGDMVersion(byte major, byte minor, byte validation)
        {
            TouchI2cGDMVer.major = major;
            TouchI2cGDMVer.minor = minor;
            TouchI2cGDMVer.validation = validation;
        }

        public void SetAPI17GDMVersion(byte major, byte minor, byte validation)
        {
            API17GDMVer.major = major;
            API17GDMVer.minor = minor;
            API17GDMVer.validation = validation;
        }

        public void SetSlaveI2CBusList(byte[] i2c_buses)
        {
            i2c_bus_list = i2c_buses;
        }

        public void GetTouchI2cGDMVersion(out byte major, out byte minor, out byte validation)
        {
            major = TouchI2cGDMVer.major;
            minor = TouchI2cGDMVer.minor;
            validation = TouchI2cGDMVer.validation;
        }

        public void GetAPI17GDMVersion(out byte major, out byte minor, out byte validation)
        {
            major = API17GDMVer.major;
            minor = API17GDMVer.minor;
            validation = API17GDMVer.validation;
        }

        public byte GetSlaveI2CAddress(byte slave_id)
        {
            if (i2c_addr_list != null)
            {
                return i2c_addr_list[slave_id];
            }
            else
            {
                return 0xFF;
            }
        }

        public byte GetSlaveIdFromI2CAddress(byte i2c_address)
        {
            if (i2c_addr_list != null)
            {
                for (byte i = 0; i < no_of_slaves; i++)
                {
                    if (i2c_addr_list[i] == i2c_address)
                    {
                        return i;
                    }
                }
            }
            return 0xFF;
        }

        public byte GetSlaveI2CBus(byte slave_id)
        {
            if (i2c_bus_list != null)
            {
                return i2c_bus_list[slave_id];
            }
            else
            {
                return 0xFF;
            }
        }

        public byte GetNumberOfSlaves()
        {
            return no_of_slaves;
        }

        public void SetNumberOfSlaves(byte slave_count)
        {
            no_of_slaves = slave_count;

            device_type = new String[no_of_slaves];
            actual_device_type = new String[no_of_slaves];
            sensing_type = new String[no_of_slaves];
            firmware_type = new String[no_of_slaves];
            keys_per_slave = new Byte[no_of_slaves];
        }


        public byte GetActualKeyNumber(byte key_number)
        {
            byte Acual_Key = 0;
            int key_count = 0;

            for (int i = 0; i < no_of_slaves; i++)
            {
                int keys_per_slave = GetKeysPerSlave((byte)i);
                if (key_number < key_count + keys_per_slave)
                {
                    Acual_Key = key_mapping[i][key_number - key_count];
                    break;
                }
                key_count += keys_per_slave;
            }
            return Acual_Key;
        }

        public byte[][] GetKeyMapping()
        {
            return key_mapping;
        }

        public void SetKeyMapping(byte[][] map)
        {
            key_mapping = map;
        }

        public String GetDeviceType(byte slave_id)
        {
            return device_type[slave_id];
        }

        public String GetActualDeviceType(byte slave_id)
        {
            return actual_device_type[slave_id];
        }


        public void SetDeviceType(byte slave_id, String type)
        {
            if (device_type[slave_id] == null)
            {
                device_type[slave_id] = new String(type.ToCharArray());
                actual_device_type[slave_id] = new string(type.ToCharArray());
            }
            else
            {
                device_type[slave_id] = type;
                actual_device_type[slave_id] = type;
            }
            SetActualDeviceType(slave_id, type);
        }

        public void SetActualDeviceType(byte slave_id, String type)
        {
            if (actual_device_type == null) return; // In offline mode actual device type is not applicable

            if (actual_device_type[slave_id] == null)
            {
                actual_device_type[slave_id] = new string(type.ToCharArray());
            }
            else
            {
                actual_device_type[slave_id] = type;
            }
        }

        public String GetSensingType(byte slave_id)
        {
            return sensing_type[slave_id];
        }

        public void SetSensingType(byte slave_id, String type)
        {
            if (sensing_type[slave_id] == null)
            {
                sensing_type[slave_id] = new String(type.ToCharArray());
            }
            else
            {
                sensing_type[slave_id] = type;
            }
        }

        public void SetFirmwareType(byte slave_id, String type)
        {
            if (firmware_type[slave_id] == null)
            {
                firmware_type[slave_id] = new String(type.ToCharArray());
            }
            else
            {
                firmware_type[slave_id] = type;
            }
        }

        public String GetFirmwareType(byte slave_id)
        {
            return firmware_type[slave_id];
        }

        public void SetKeysPerSlave(byte slave_id, byte num_keys)
        {
            keys_per_slave[slave_id] = num_keys;
        }

        public byte GetKeysPerSlave(byte slave_id)
        {
            return keys_per_slave[slave_id];
        }

        public bool ToggleOfflineMode()
        {
            offline_mode = offline_mode == true ? false : true;

            if (offline_mode == true)
            {
                update("Touch Config", null);
            }

            return offline_mode;
        }

        public bool IsOfflineModeActive()
        {
            return offline_mode;
        }

        public void SetOfflineMode(bool status)
        {
            offline_mode = status;
        }

        public string GetSelectedTabName()
        {
            return mainView.getSelectedTabName();
        }
    }
}