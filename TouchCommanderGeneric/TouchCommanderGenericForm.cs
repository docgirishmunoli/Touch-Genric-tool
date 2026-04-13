using System;
using System.Threading;
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
    public partial class TouchCommanderGenericForm : WideInterface
    {

        public struct i2c_rw_cmd
        {
            public String view_name;
            public String subview_name;
            public UInt16 offset;
            public byte size;
            public RevealPacket pkt;
        };

        private const byte API_NUMBER = 17;//TODO: change to API value
        private enum API017_TOUCH_OPCODES
        {
            API017TOUCH_TOUCH_REQUEST_HW_CONFIG_INFO = 0,
            API017TOUCH_I2C_READ_WRITE_CMD = 1,
            API017TOUCH_START_HEARTBEAT_PUBLISH = 5,
            API017TOUCH_START_STATUS_REPORT_PUBLISH = 6,
            API017TOUCH_START_CP_VALUE_PUBLISH = 10,
            API017TOUCH_START_RAW_REF_DELTA_PUBLISH = 11,
            API017TOUCH_PUBLISH_ACTUAL_SENSOR_INFO=13,
        };
        private byte cmd_cntr = 0;
        private byte dest_node = 0;
        private TouchCommanderController control;
        Queue<i2c_rw_cmd> i2c_rw_cmd_q;
        bool i2c_rw_cmd_pending;
        BackgroundWorker i2c_rw_cmd_sender;
        // FIX: Add retry logic variables
        private int retry_count = 0;
        private const int MAX_RETRIES = 2;

        //to access wideLocal use base.WideLocal or simple WideLocal
        public TouchCommanderGenericForm(WideBox wideLocal)
            : base(wideLocal)
        {
            InitializeComponent();

            i2c_rw_cmd_pending = false;
            i2c_rw_cmd_q = new Queue<i2c_rw_cmd>();
            i2c_rw_cmd_sender = new BackgroundWorker();
            i2c_rw_cmd_sender.DoWork += i2c_rw_cmd_sender_DoWork;
            if (i2c_rw_cmd_sender.IsBusy != true)
            {
                i2c_rw_cmd_sender.RunWorkerAsync();
            }

            //Add your constructor operations here

            //AutoSave: Load and Save your CheckBoxes, TextBoxes and ComboBoxes last conditions
            //Default is true, this line can be removed
            AutoSave = true;
            //To facilitate the debug of your form you can trace your exception or print messages using LogException function
            //  LogException("My Error Message");
            //  or
            //  try { //your code } catch (Exception ex) { LogException(ex,false); }
            //Avoid to do a to much operations during the construction time to have a faster open time and avoid opening the form errors.
            //Errors during the construction time are hard to debug because your object are not instantiated yet
            //view = new TouchCommanderMainView();
            control = new TouchCommanderController(this);
            this.Controls.Add(control.getMainView());
            this.Text = "Generic Touch Commander ( " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " )";
        }

        private bool is_i2c_rw_cmd_pending()
        {
            return i2c_rw_cmd_pending;
        }

        private void set_i2c_rw_cmd_pending(bool status)
        {
            i2c_rw_cmd_pending = status;
        }

        private void queue_i2c_rw_cmd(String name, String subview_name, UInt16 offset, byte size, RevealPacket pkt)
        {
            i2c_rw_cmd i2c_cmd = new i2c_rw_cmd();
            i2c_cmd.view_name = name;
            i2c_cmd.subview_name = subview_name;
            i2c_cmd.offset = offset;
            i2c_cmd.size = size;
            i2c_cmd.pkt = pkt;
            i2c_rw_cmd_q.Enqueue(i2c_cmd);
            //Console.WriteLine(String.Format("Enqueue Count: {0}, Name: {1}", i2c_rw_cmd_q.Count, i2c_cmd.view_name));
        }

        private void queue_i2c_rw_clear()
        {
            set_i2c_rw_cmd_pending(false);
            i2c_rw_cmd_q.Clear();
        }

        private DateTime cmd_sent_time = DateTime.Now;
        private const int CMD_TIMEOUT_MS = 3000;  // 3 second timeout

        private void i2c_rw_cmd_sender_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (is_i2c_rw_cmd_pending())
                {
                    // FIX: Improved timeout handling with retry logic
                    TimeSpan elapsed = DateTime.Now - cmd_sent_time;
                    if (elapsed.TotalMilliseconds > CMD_TIMEOUT_MS)
                    {
                        if (retry_count < MAX_RETRIES && i2c_rw_cmd_q.Count != 0)
                        {
                            // Retry the command
                            retry_count++;
                            LogException($"Timeout - Retry {retry_count}/{MAX_RETRIES}");
                            i2c_rw_cmd i2c_cmd = i2c_rw_cmd_q.Peek();
                            cmd_sent_time = DateTime.Now;
                            if (i2c_cmd.pkt != null)
                            {
                                SendRevealMessage(i2c_cmd.pkt);
                            }
                        }
                        else
                        {
                            // Max retries exceeded, clear and move on
                            LogException($"Timeout after {retry_count} retries. Clearing command: {(i2c_rw_cmd_q.Count > 0 ? i2c_rw_cmd_q.Peek().view_name : "unknown")}");
                            if (i2c_rw_cmd_q.Count != 0)
                            {
                                i2c_rw_cmd_q.Dequeue();
                            }
                            set_i2c_rw_cmd_pending(false);
                            retry_count = 0;
                        }
                    }
                }
                else
                {
                    retry_count = 0;  // Reset retry count when processing new command
                    if (i2c_rw_cmd_q.Count != 0)
                    {
                        set_i2c_rw_cmd_pending(true);
                        cmd_sent_time = DateTime.Now;  // START TIMER
                        i2c_rw_cmd i2c_cmd = i2c_rw_cmd_q.Peek();

                        if (i2c_cmd.view_name == "Delay")
                        {
                            Thread.Sleep(i2c_cmd.offset);
                            if (i2c_rw_cmd_q.Count != 0)
                            {
                                i2c_rw_cmd_q.Dequeue();
                            }
                            set_i2c_rw_cmd_pending(false);
                        }
                        else
                        {
                            SendRevealMessage(i2c_cmd.pkt);
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Parse message from WideBoxInterface
        /// </summary>
        /// <param name="data">Simple Whirlpool packet data with extended info (TimeStamp and IsValid)</param>
        public override void parseSimpleWhirlpoolMessage(ExtendedSimpleWhirlpoolPacket data)
        {
            RevealPacket reveal_pkt = new RevealPacket();
            if (reveal_pkt.ParseSimpleWhirlpoolMessage(data))
            {
                if (reveal_pkt.IsFeedback == true)
                {
                    switch (reveal_pkt.API)
                    {
                        case API_NUMBER:   //parse opcodes to this API (already without the feedback bit, i.e. 0x25: reveal_pkt.OpCode = 5 , reveal_pkt.IsFeedback = true )
                            switch ((API017_TOUCH_OPCODES)reveal_pkt.OpCode)
                            {
                                case API017_TOUCH_OPCODES.API017TOUCH_TOUCH_REQUEST_HW_CONFIG_INFO:
                                    control.update("HW Info", reveal_pkt.PayLoad);
                                    break;
                                case API017_TOUCH_OPCODES.API017TOUCH_START_HEARTBEAT_PUBLISH:
                                    control.update("HW Info", "Heart Beat", reveal_pkt.PayLoad);
                                    control.update("Data Monitoring", "Heart Beat", reveal_pkt.PayLoad);
                                    control.update("Fast Data Monitoring", "Heart Beat", reveal_pkt.PayLoad);
                                    control.update("Touch Config", "Heart Beat", reveal_pkt.PayLoad);
                                    break;
                                case API017_TOUCH_OPCODES.API017TOUCH_START_STATUS_REPORT_PUBLISH:
                                    control.update("Status Report", reveal_pkt.PayLoad);
                                    break;
                                case API017_TOUCH_OPCODES.API017TOUCH_I2C_READ_WRITE_CMD:
                                    if (i2c_rw_cmd_q.Count != 0)
                                    {
                                        i2c_rw_cmd i2c_cmd = i2c_rw_cmd_q.Peek();

                                        UInt16 offset = (UInt16)((reveal_pkt.PayLoad[2] << 8) | reveal_pkt.PayLoad[3]);
                                        byte size = reveal_pkt.PayLoad[5];
                                        if ((offset == i2c_cmd.offset) && (size == i2c_cmd.size))
                                        {
                                            //Console.WriteLine(String.Format("Dequeue Count: {0}, Name: {1}", i2c_rw_cmd_q.Count, i2c_cmd.view_name));
                                            if (i2c_cmd.subview_name != "")
                                            {
                                                control.update(i2c_cmd.view_name, i2c_cmd.subview_name, reveal_pkt.PayLoad);
                                            }
                                            else
                                            {
                                                control.update(i2c_cmd.view_name, reveal_pkt.PayLoad);
                                            }
                                            if (i2c_rw_cmd_q.Count != 0)
                                            {
                                                i2c_rw_cmd_q.Dequeue();
                                            }
                                            set_i2c_rw_cmd_pending(false);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Unhandled READ_WRITE CMD Response");
                                        }
                                    }

                                    break;


                                case API017_TOUCH_OPCODES.API017TOUCH_START_CP_VALUE_PUBLISH:
                                    control.update("CP Monitoring", reveal_pkt.PayLoad);
                                    break;
                                case API017_TOUCH_OPCODES.API017TOUCH_START_RAW_REF_DELTA_PUBLISH:
                                    control.update("Fast Data Monitoring", reveal_pkt.PayLoad);
                                    break;
                                case API017_TOUCH_OPCODES.API017TOUCH_PUBLISH_ACTUAL_SENSOR_INFO:
                                    control.update("Touch Config","Physical Key", reveal_pkt.PayLoad);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Parse messages from the device between the bus and the PC
        /// </summary>
        /// <param name="message">string containing a message from the device</param>
        /// <param name="data">data from the device message</param>
        public override void parseDeviceMessage(DeviceMessage message)
        {
            //Using DeviceMesageString to parse a Device message
            //Checking if message income is Data Flow State message
            if (DeviceMessageStrings.CheckDeviceString(DeviceMessageStrings.DataFlowState, message.Message))
            {
                //Get the Data from the message into a string list, See DeviceMessageStrings.(message) for info about the data
                List<string> values = DeviceMessageStrings.GetDeviceStringParameters(wbox.DeviceMessageStrings.DataFlowState, message.Message);
                //textBoxDataConnected.Text = values[0].ToUpperInvariant() == "ON" ? "Data OFF" : "Data ON";
            }
        }

        /// <summary>
        /// Send a wide Message over the bus
        /// </summary>
        /// <param name="destination">Address to send the message</param>
        /// <param name="sap">sap to transport layer defined in Wide docs (1/2-TDD/UDD 3-Gmcl 4-Reveal)</param>
        /// <param name="data">wide payload</param>
        public void SendWideMessage(byte destination, byte sap, byte[] data)
        {
            WideLocal.SendWideMsg(destination, sap, data);
        }

        /// <summary>
        /// Send a Reveal Message over the bus.
        /// Packet Source address will be ignored as the WideBox address that will be used
        /// To set WideBox address send the WideBoxConstants.CMD_GET_WIDE_BOX_ADDR using the WideLocal.SendCommand
        /// </summary>
        /// <param name="pkt">Reveal packet containing the command or feedback to be sent</param>
        public void SendRevealMessage(RevealPacket pkt)
        {
            WideLocal.SendMessage(pkt.Destination, WideBoxConstants.REVEAL_SAP, pkt.getMessagePayload());
        }

        /// <summary>
        /// Send a Reveal Message over the bus.
        /// </summary>
        /// <param name="destination">Address to send the message</param>
        /// <param name="api">Reveal Api</param>
        /// <param name="opcode">Reveal Opcode</param>
        /// <param name="isFeedback">Set true if the message is a feedback</param>
        /// <param name="payload">payload bytes</param>
        public void SendRevealMessage(byte destination, byte api, byte opcode, bool isFeedback, byte[] payload)
        {
            RevealPacket pkt = new RevealPacket(api, opcode, 0, destination, isFeedback, payload);
            WideLocal.SendMessage(pkt.Destination, WideBoxConstants.REVEAL_SAP, pkt.getMessagePayload());
        }

        public void SendRuequestTouchHardwreInformation()
        {
            byte[] packet = new byte[1];
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_TOUCH_REQUEST_HW_CONFIG_INFO;
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            SendRevealMessage(pkt);
        }

        public void SendRuequestActualSensorInfo()
        {
            byte[] packet = new byte[1];
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_PUBLISH_ACTUAL_SENSOR_INFO;
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            SendRevealMessage(pkt);
        }

        public void SendRequestHeartBeat()
        {
            byte[] packet = new byte[1];
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_START_HEARTBEAT_PUBLISH;
            packet[0] = 1; //Request every 1 second
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            SendRevealMessage(pkt);
        }

        public void SendRequestStatusReport(byte slave_count, bool periodic)
        {
            byte[] packet = new byte[2];
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_START_STATUS_REPORT_PUBLISH;
            byte slaves = 0;

            for (int i = 0; i < slave_count; i++)
            {
                slaves |= (byte)(1 << i);
            }

            packet[0] = slaves;
            packet[1] = periodic == true ? (byte)1 : (byte)0;
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            SendRevealMessage(pkt);
        }

        public void SendI2CCommand(String source_view, byte slaveid, byte command, byte oprand)
        {
            SendI2CCommand(source_view, "", slaveid, command, oprand);
        }
        public void SendI2CCommand(String source_view, String subview, byte slaveid, byte command, byte oprand)
        {
            byte[] packet = new byte[10];
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_I2C_READ_WRITE_CMD;

            packet[0] = control.GetSlaveI2CBus(slaveid);
            packet[1] = (byte)(control.GetSlaveI2CAddress(slaveid) << 1);
            packet[2] = 0; // 16 Bit Offset address MSB
            packet[3] = 0; // Unused byte, sent as dummy
            packet[4] = 1; // Write Command
            packet[5] = 4; // I2C cmd data Payload size + 1 byte to accomodate offset LSB
            packet[6] = 0; // 16 Bit Offset address LSB
            packet[7] = command;
            packet[8] = oprand;
            packet[9] = this.cmd_cntr;
            this.cmd_cntr++;
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            //SendRevealMessage(pkt);
            queue_i2c_rw_cmd(source_view, subview, 0, packet[5], pkt);
        }

        public void SendI2CWriteCommand(String source_view, byte slaveid, String address_type, UInt16 offset_addr, byte[] data)
        {
            SendI2CWriteCommand(source_view, "", slaveid, address_type, offset_addr, data);
        }
        public void SendI2CWriteCommand(String source_view, String subview, byte slaveid, String address_type, UInt16 offset_addr, byte[] data)
        {
            byte[] packet;
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_I2C_READ_WRITE_CMD;

            if (data != null)
            {
                packet = new byte[7 + data.Length];
            }
            else
            {
                packet = new byte[7];
            }

            packet[0] = control.GetSlaveI2CBus(slaveid);
            if (address_type == "Primary")
            {
                packet[1] = (byte)(control.GetSlaveI2CAddress(slaveid) << 1);
            }
            else
            {
                packet[1] = (byte)((control.GetSlaveI2CAddress(slaveid) ^ 0x02) << 1);
            }
            packet[2] = (byte)(offset_addr >> 8); // 16 Bit Offset address MSB
            packet[3] = (byte)(offset_addr); // Unused byte, sent as dummy
            packet[4] = 1; // Write Command
            packet[5] = (byte)(data != null ? data.Length + 1 : 1); // I2C cmd data Payload size + 1 byte to accomodate offset LSB
            packet[6] = (byte)(offset_addr); // 16 Bit Offset address LSB
            System.Buffer.BlockCopy(data, 0, packet, 7, data.Length);
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            //SendRevealMessage(pkt);
            queue_i2c_rw_cmd(source_view, subview, offset_addr, packet[5], pkt);
        }

        public void SendI2CReadCommand(String source_view, byte slaveid, String address_type, UInt16 offset_addr, byte bytes_to_read)
        {
            SendI2CReadCommand(source_view, "", slaveid, address_type, offset_addr, bytes_to_read);
        }
        public void SendI2CReadCommand(String source_view, String subview, byte slaveid, String address_type, UInt16 offset_addr, byte bytes_to_read)
        {
            byte[] packet = new byte[6];
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_I2C_READ_WRITE_CMD;

            packet[0] = control.GetSlaveI2CBus(slaveid);
            if (address_type == "Primary")
            {
                packet[1] = (byte)(control.GetSlaveI2CAddress(slaveid) << 1);
            }
            else
            {
                packet[1] = (byte)((control.GetSlaveI2CAddress(slaveid) ^ 0x02) << 1);
            }
            packet[2] = (byte)(offset_addr >> 8); ; // 16 Bit Offset address MSB
            packet[3] = (byte)(offset_addr); // 16 Bit Offset address LSB
            packet[4] = 0; // Read Command
            packet[5] = bytes_to_read; // I2C cmd data Payload size + 1 byte to accomodate offset LSB
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            //SendRevealMessage(pkt);
            queue_i2c_rw_cmd(source_view, subview, offset_addr, packet[5], pkt);
        }

        public void SendDelayCommand(UInt16 duration)
        {
            queue_i2c_rw_cmd("Delay", "", duration, 0, null);
        }

        public void ClearCommandQueue()
        {
            queue_i2c_rw_clear();
        }

        public void SendStartStopRawRefDeltaPMonitoring(bool start)
        {
            byte[] packet;
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_START_RAW_REF_DELTA_PUBLISH;
            packet = new byte[1];
            if (start == true)
            {
                packet[0] = 1; // 1 - Request, 2 - Response, 255 - OFF
            }
            else
            {
                packet[0] = 255;
            }
            RevealPacket pkt = new RevealPacket(17, opcode, 0, dest_node, false, packet);
            SendRevealMessage(pkt);
        }

        public void SendStartStopCPMonitoring(bool start, byte slaveid)
        {
            byte[] packet;
            byte opcode = (byte)API017_TOUCH_OPCODES.API017TOUCH_START_CP_VALUE_PUBLISH;
            Int16 interval = 100;
            packet = new byte[4];
            if (start == true)
            {
                packet[0] = 1; // 1 - Request, 2 - Response, 255 - OFF
            }
            else
            {
                packet[0] = 255;
            }
            packet[1] = slaveid;
            packet[2] = (byte)((interval & 0xFF00) >> 8);
            packet[3] = (byte)(interval & 0x00FF);
            RevealPacket pkt = new RevealPacket(API_NUMBER, opcode, 0, dest_node, false, packet);
            SendRevealMessage(pkt);
        }

        public void setHMINodeId(byte nodeID)
        {
            dest_node = nodeID;
        }

        private void TouchCommanderGenericForm_Load(object sender, EventArgs e)
        {

        }
    }
}
