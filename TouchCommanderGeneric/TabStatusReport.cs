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
    public struct ControlCommandInfo
    {
        public byte CtrlCmd;
        public byte Oprand;
        public byte Counter;
        public byte EchoCmd;
        public byte EchoCounter;
        public byte ErrorStatus;
    }

    public struct ErrorInfo
    {
        public byte ErrStatus;
        public byte ErrCounter;
        public byte ClassB_Error1;
        public byte ClassB_Error2;
        public byte IOFault1;
        public byte IOFault2;
        public byte ClassB_Reserved;
        public byte Crc;
    }

    public struct KeyStatusInfo
    {
        public byte[] keyStatus;
        public byte KeyStatusCrc;
        public byte[] LatchedStatus;
        public byte LatchedStatusCrc;
    }

    public struct OtherInfo
    {
        public UInt16 KeyPressCount;
        public UInt16 HBCount;
        public byte ResetReason;
        public byte FirmwareInfo;
    }

    public struct StatusReportInfo
    {
        public ControlCommandInfo ctrl_info;
        public ErrorInfo error_info;
        public KeyStatusInfo key_status_info;
        public OtherInfo other_info;
        public bool bModelReady;
    }

    public class StatusReportModel : TouchCommanderModelBase
    {
        private Dictionary<byte, StatusReportInfo> srInfo;

        public StatusReportModel(TouchCommanderController c) : base(c)
        {
            srInfo = new Dictionary<byte, StatusReportInfo>();
        }

        public override void updateModel(Object d)
        {
            byte[] data = (byte[])d;
            byte key = data[0];

            if (srInfo.ContainsKey(key) == false)
            {
                StatusReportInfo info = new StatusReportInfo();
                info.key_status_info.keyStatus = new byte[5];
                info.key_status_info.LatchedStatus = new byte[5];
                srInfo.Add(key, info);
            }

            StatusReportInfo report = srInfo[key];
            report.ctrl_info.CtrlCmd  = data[1];
            report.ctrl_info.Oprand   = data[2];
            report.ctrl_info.Counter  = data[3];
            report.ctrl_info.EchoCmd  = data[4];
            report.ctrl_info.EchoCounter = data[5];
            report.ctrl_info.ErrorStatus = data[6];

            report.error_info.ErrStatus = data[7];
            report.error_info.ErrCounter = data[8];
            report.error_info.ClassB_Error1 = data[9];
            report.error_info.ClassB_Error2 = data[10];
            report.error_info.IOFault1 = data[11];
            report.error_info.IOFault2 = data[12];
            report.error_info.ClassB_Reserved = data[13];
            report.error_info.Crc = data[14];

            for (int i = 0; i < 5; i++)
            {
                report.key_status_info.keyStatus[i] = data[15 + i];
            }
            report.key_status_info.KeyStatusCrc = data[20];

            for (int i = 0; i < 5; i++)
            {
                report.key_status_info.LatchedStatus[i] = data[21 + i];
            }
            report.key_status_info.LatchedStatusCrc = data[26];

            report.other_info.KeyPressCount = (UInt16)((data[28] << 8) | data[27]);
            report.other_info.HBCount = (UInt16)((data[40] << 8) | data[39]);
            report.other_info.ResetReason = data[41];
            report.other_info.FirmwareInfo = data[42];

            srInfo[key] = report;
            report.bModelReady = true;
        }

        public override void updateModel(Object d, string section)
        {

        }

        public int getNumSlaves()
        {
            return srInfo.Count;
        }

        public ControlCommandInfo getControlCommandInfo(byte saveid)
        {
            return srInfo.ElementAt(saveid).Value.ctrl_info;
        }

        public ErrorInfo getErrorStatusInfo(byte saveid)
        {
            return srInfo.ElementAt(saveid).Value.error_info;
        }

        public KeyStatusInfo getKeyStatus(byte saveid)
        {
            return srInfo.ElementAt(saveid).Value.key_status_info;
        }

        public OtherInfo getOtherInfo(byte saveid)
        {
            return srInfo.ElementAt(saveid).Value.other_info;
        }
    }


    public struct StatusReportView
    {
        public GroupBox gbSrInfo;

        public GroupBox gbCmdInfo;
        public Label[] lbCmdInfo;

        public GroupBox gbErrorInfo;
        public Label[] lbErrorInfo;

        public GroupBox gbKeyStatus;
        public Label[] lbKeyStatus;

        public GroupBox gbLatchStatus;
        public Label[] lbLatchStatus;

        public GroupBox gbOtherInfo;
        public Label[] lbOtherInfo;

        public bool bViewInitComplete;
    }

    public class StatusReportTabPage : TouchCommanderTabBase
    {
        private int num_slaves;
        private Button btnReadAgain;
        private Dictionary<byte, StatusReportView> srInfo;
        private List<string> strCmdInfo = new List<string>() { "Command ID: {0}", "Oprand: {0}", "Cmd Counter: {0}",
                                            "Echo Cmd ID: {0}", "Echo Counter: {0}", "Error Status: {0}" };

        private List<string> strErrInfo = new List<string>() { "Error Status: {0}", "Error Counter: {0}", "Class B Err 1: {0}", "Class B Err 2: {0}",
                                            "IO Fault 1: {0}", "IO Fault 2: {0}", "Class B Reserved: {0}", "Error Status CRC: {0}"};

        private List<string> strKeyStatus = new List<string>() { "{0} Status {1}: {2}", "{0} Status {1}: {2}", "{0} Status {1}: {2}",
                                            "{0} Status {1}: {2}", "{0} Status {1}: {2}", "{0} Status CRC: {2}"};

        private List<string> strOtherInfo = new List<string>() { "KeyPress Cnt: {0}", "Last Reset: {0}", "HeartBeat Cnt: {0}", "Firmware Info: {0}"};

        public StatusReportTabPage(TouchCommanderController c) : base(c)
        {
            this.Text = "Status Report";
            srInfo = new Dictionary<byte, StatusReportView>();

            btnReadAgain = new Button();
            btnReadAgain.Location = new Point(10, 10);
            btnReadAgain.Size = new Size(150, 30);
            btnReadAgain.Font = new Font(btnReadAgain.Font.Name, btnReadAgain.Font.Size + 2.0F, btnReadAgain.Font.Style, btnReadAgain.Font.Unit);
            btnReadAgain.Text = "Read Again";
            btnReadAgain.Click += new EventHandler(this.btnReadAgain_Click);
            this.Controls.Add(btnReadAgain);

            num_slaves = 0;
        }

        private void btnReadAgain_Click(Object sender, EventArgs e)
        {
            if (num_slaves != 0)
            {
                control.StartStatusReportPublishing((byte)num_slaves, true);
            }
            else
            {
                MessageBox.Show("ERROR!!! - Number of Slaves not available yet", "ERROR !!!");
            }
        }

        private StatusReportView initView(byte slaveid)
        {
            StatusReportView view = new StatusReportView();

            view.gbSrInfo = new GroupBox();
            view.gbSrInfo.Location = new Point(10 + (slaveid * 550), 60);
            view.gbSrInfo.Size = new Size(500, 725);
            view.gbSrInfo.Text = string.Format("Slave {0}: Status Report", slaveid);
            view.gbSrInfo.Font = new Font(view.gbSrInfo.Font.Name, view.gbSrInfo.Font.Size + 2.0F,
                                            view.gbSrInfo.Font.Style, view.gbSrInfo.Font.Unit);
            this.Controls.Add(view.gbSrInfo);

            view.gbCmdInfo = new GroupBox();
            view.gbCmdInfo.Location = new Point(20, 20);
            view.gbCmdInfo.Size = new Size(460, 130);
            view.gbCmdInfo.Text = string.Format("Comand Info");
            view.gbCmdInfo.Font = new Font(view.gbCmdInfo.Font.Name, view.gbCmdInfo.Font.Size + 2.0F,
                                            view.gbCmdInfo.Font.Style, view.gbCmdInfo.Font.Unit);
            view.gbSrInfo.Controls.Add(view.gbCmdInfo);

            view.lbCmdInfo = new Label[6];
            for (int i = 0; i < view.lbCmdInfo.Count(); i++)
            {
                int x, y, c;
                view.lbCmdInfo[i] = new Label();
                x = 20 + ((view.gbCmdInfo.Width / 2) * (i / 3));
                c = i < (view.lbCmdInfo.Count() / 2) ? i : i - (view.lbCmdInfo.Count() / 2);
                y = 30 + (30 * c);
                view.lbCmdInfo[i].Location = new Point(x, y);
                view.lbCmdInfo[i].Size = new Size((view.gbCmdInfo.Width / 2) - 40, 20);
                view.lbCmdInfo[i].Text = string.Format(strCmdInfo[i], 0);
                view.lbCmdInfo[i].Font = new Font(view.lbCmdInfo[i].Font.Name, view.lbCmdInfo[i].Font.Size + 4.0F,
                                                view.lbCmdInfo[i].Font.Style | FontStyle.Bold, view.lbCmdInfo[i].Font.Unit);
                view.gbCmdInfo.Controls.Add(view.lbCmdInfo[i]);
            }

            view.gbErrorInfo = new GroupBox();
            view.gbErrorInfo.Location = new Point(20, view.gbCmdInfo.Location.Y + view.gbCmdInfo.Size.Height + 10);
            view.gbErrorInfo.Size = new Size(460, 160);
            view.gbErrorInfo.Text = string.Format("Error Status");
            view.gbErrorInfo.Font = new Font(view.gbErrorInfo.Font.Name, view.gbErrorInfo.Font.Size + 2.0F,
                                            view.gbErrorInfo.Font.Style, view.gbErrorInfo.Font.Unit);
            view.gbSrInfo.Controls.Add(view.gbErrorInfo);

            view.lbErrorInfo = new Label[8];
            for (int i = 0; i < view.lbErrorInfo.Count(); i++)
            {
                int x, y, c;
                view.lbErrorInfo[i] = new Label();
                x = 20 + ((view.gbErrorInfo.Width / 2) * (i / 4));
                c = i < (view.lbErrorInfo.Count() / 2) ? i : i - (view.lbErrorInfo.Count() / 2);
                y = 30 + (30 * c);
                view.lbErrorInfo[i].Location = new Point(x, y);
                view.lbErrorInfo[i].Size = new Size((view.gbCmdInfo.Width / 2) - 40, 20);
                view.lbErrorInfo[i].Text = string.Format(strErrInfo[i], 0);
                view.lbErrorInfo[i].Font = new Font(view.lbErrorInfo[i].Font.Name, view.lbErrorInfo[i].Font.Size + 4.0F,
                                                view.lbErrorInfo[i].Font.Style | FontStyle.Bold, view.lbErrorInfo[i].Font.Unit);
                view.gbErrorInfo.Controls.Add(view.lbErrorInfo[i]);
            }

            view.gbKeyStatus = new GroupBox();
            view.gbKeyStatus.Location = new Point(20, view.gbErrorInfo.Location.Y + view.gbErrorInfo.Size.Height + 10);
            view.gbKeyStatus.Size = new Size(460, 130);
            view.gbKeyStatus.Text = string.Format("Key Status");
            view.gbKeyStatus.Font = new Font(view.gbKeyStatus.Font.Name, view.gbKeyStatus.Font.Size + 2.0F,
                                            view.gbKeyStatus.Font.Style, view.gbKeyStatus.Font.Unit);
            view.gbSrInfo.Controls.Add(view.gbKeyStatus);

            view.lbKeyStatus = new Label[6];
            for (int i = 0; i < view.lbKeyStatus.Count(); i++)
            {
                int x, y, c;
                view.lbKeyStatus[i] = new Label();
                x = 20 + ((view.gbKeyStatus.Width / 2) * (i / 3));
                c = i < (view.lbKeyStatus.Count() / 2) ? i : i - (view.lbKeyStatus.Count() / 2);
                y = 30 + (30 * c);
                view.lbKeyStatus[i].Location = new Point(x, y);
                view.lbKeyStatus[i].Size = new Size((view.gbKeyStatus.Width / 2) - 40, 20);
                view.lbKeyStatus[i].Text = string.Format(strKeyStatus[i], "Key", i, 0);
                view.lbKeyStatus[i].Font = new Font(view.lbKeyStatus[i].Font.Name, view.lbKeyStatus[i].Font.Size + 4.0F,
                                                view.lbKeyStatus[i].Font.Style | FontStyle.Bold, view.lbKeyStatus[i].Font.Unit);
                view.gbKeyStatus.Controls.Add(view.lbKeyStatus[i]);
            }

            view.gbLatchStatus = new GroupBox();
            view.gbLatchStatus.Location = new Point(20, view.gbKeyStatus.Location.Y + view.gbKeyStatus.Size.Height + 10);
            view.gbLatchStatus.Size = new Size(460, 130);
            view.gbLatchStatus.Text = string.Format("Latched Status");
            view.gbLatchStatus.Font = new Font(view.gbLatchStatus.Font.Name, view.gbLatchStatus.Font.Size + 2.0F,
                                            view.gbLatchStatus.Font.Style, view.gbLatchStatus.Font.Unit);
            view.gbSrInfo.Controls.Add(view.gbLatchStatus);

            view.lbLatchStatus = new Label[6];
            for (int i = 0; i < view.lbLatchStatus.Count(); i++)
            {
                int x, y, c;
                view.lbLatchStatus[i] = new Label();
                x = 20 + ((view.gbLatchStatus.Width / 2) * (i / 3));
                c = i < (view.lbLatchStatus.Count() / 2) ? i : i - (view.lbLatchStatus.Count() / 2);
                y = 30 + (30 * c);
                view.lbLatchStatus[i].Location = new Point(x, y);
                view.lbLatchStatus[i].Size = new Size((view.gbLatchStatus.Width / 2) - 40, 20);
                view.lbLatchStatus[i].Text = string.Format(strKeyStatus[i], "Latch", i, 0);
                view.lbLatchStatus[i].Font = new Font(view.lbLatchStatus[i].Font.Name, view.lbLatchStatus[i].Font.Size + 4.0F,
                                                view.lbLatchStatus[i].Font.Style | FontStyle.Bold, view.lbLatchStatus[i].Font.Unit);
                view.gbLatchStatus.Controls.Add(view.lbLatchStatus[i]);
            }

            view.gbOtherInfo = new GroupBox();
            view.gbOtherInfo.Location = new Point(20, view.gbLatchStatus.Location.Y + view.gbLatchStatus.Size.Height + 10);
            view.gbOtherInfo.Size = new Size(460, 100);
            view.gbOtherInfo.Text = string.Format("Other Info");
            view.gbOtherInfo.Font = new Font(view.gbOtherInfo.Font.Name, view.gbOtherInfo.Font.Size + 2.0F,
                                            view.gbOtherInfo.Font.Style, view.gbOtherInfo.Font.Unit);
            view.gbSrInfo.Controls.Add(view.gbOtherInfo);

            view.lbOtherInfo = new Label[4];
            for (int i = 0; i < view.lbOtherInfo.Count(); i++)
            {
                int x, y, c;
                view.lbOtherInfo[i] = new Label();
                x = 20 + ((view.gbOtherInfo.Width / 2) * (i / 2));
                c = i < (view.lbOtherInfo.Count() / 2) ? i : i - (view.lbOtherInfo.Count() / 2);
                y = 30 + (30 * c);
                view.lbOtherInfo[i].Location = new Point(x, y);
                view.lbOtherInfo[i].Size = new Size((view.gbOtherInfo.Width / 2) - 40, 20);
                view.lbOtherInfo[i].Text = string.Format(strOtherInfo[i], 0);
                view.lbOtherInfo[i].Font = new Font(view.lbOtherInfo[i].Font.Name, view.lbOtherInfo[i].Font.Size + 4.0F,
                                                view.lbOtherInfo[i].Font.Style | FontStyle.Bold, view.lbOtherInfo[i].Font.Unit);
                view.gbOtherInfo.Controls.Add(view.lbOtherInfo[i]);
            }

            view.bViewInitComplete = true;
            return view;
        }

        private void updateCmdInfo(byte slaveid, StatusReportModel model)
        {
            ControlCommandInfo info = model.getControlCommandInfo(slaveid);
            srInfo[slaveid].lbCmdInfo[0].Text = string.Format(strCmdInfo[0], info.CtrlCmd);
            srInfo[slaveid].lbCmdInfo[1].Text = string.Format(strCmdInfo[1], info.Oprand);
            srInfo[slaveid].lbCmdInfo[2].Text = string.Format(strCmdInfo[2], info.Counter);
            srInfo[slaveid].lbCmdInfo[3].Text = string.Format(strCmdInfo[3], info.EchoCmd);
            srInfo[slaveid].lbCmdInfo[4].Text = string.Format(strCmdInfo[4], info.EchoCounter);
            srInfo[slaveid].lbCmdInfo[5].Text = string.Format(strCmdInfo[5], info.ErrorStatus);
        }

        private void updateErrorInfo(byte slaveid, StatusReportModel model)
        {
            ErrorInfo info = model.getErrorStatusInfo(slaveid);
            srInfo[slaveid].lbErrorInfo[0].Text = string.Format(strErrInfo[0], info.ErrStatus);
            srInfo[slaveid].lbErrorInfo[1].Text = string.Format(strErrInfo[1], info.ErrCounter);
            srInfo[slaveid].lbErrorInfo[2].Text = string.Format(strErrInfo[2], info.ClassB_Error1);
            srInfo[slaveid].lbErrorInfo[3].Text = string.Format(strErrInfo[3], info.ClassB_Error2);
            srInfo[slaveid].lbErrorInfo[4].Text = string.Format(strErrInfo[4], info.IOFault1);
            srInfo[slaveid].lbErrorInfo[5].Text = string.Format(strErrInfo[5], info.IOFault2);
            srInfo[slaveid].lbErrorInfo[6].Text = string.Format(strErrInfo[6], info.ClassB_Reserved);
            srInfo[slaveid].lbErrorInfo[7].Text = string.Format(strErrInfo[7], info.Crc);
        }

        private void updateKeyStatus(byte slaveid, StatusReportModel model)
        {
            KeyStatusInfo info = model.getKeyStatus(slaveid);

            for (int i = 0; i < 5; i++)
            {
                srInfo[slaveid].lbKeyStatus[i].Text = string.Format(strKeyStatus[i], "Key", i, info.keyStatus[i]);
                srInfo[slaveid].lbLatchStatus[i].Text = string.Format(strKeyStatus[i], "Latch", i, info.LatchedStatus[i]);
            }

            srInfo[slaveid].lbKeyStatus[5].Text = string.Format(strKeyStatus[5], "Key", 0, info.KeyStatusCrc);
            srInfo[slaveid].lbLatchStatus[5].Text = string.Format(strKeyStatus[5], "Latch", 0, info.LatchedStatusCrc);
        }

        private void updateOtherInfo(byte slaveid, StatusReportModel model)
        {
            OtherInfo info = model.getOtherInfo(slaveid);
            srInfo[slaveid].lbOtherInfo[0].Text = string.Format(strOtherInfo[0], info.KeyPressCount);
            srInfo[slaveid].lbOtherInfo[1].Text = string.Format(strOtherInfo[1], info.ResetReason);
            srInfo[slaveid].lbOtherInfo[2].Text = string.Format(strOtherInfo[2], info.HBCount);
            srInfo[slaveid].lbOtherInfo[3].Text = string.Format(strOtherInfo[3], info.FirmwareInfo);
        }


        public override void updateView(Object data)
        {
            if (data == null) return;

            StatusReportModel model = (StatusReportModel)data;
            this.num_slaves = model.getNumSlaves();

            for (int i = 0; i < num_slaves; i++)
            {
                if (srInfo.ContainsKey((byte)i) == false)
                {
                    StatusReportView view = this.initView((byte)i);
                    srInfo.Add((byte)i, view);
                }

                if (srInfo.ContainsKey((byte)i) == true)
                {
                    updateCmdInfo((byte)i, model);
                    updateErrorInfo((byte)i, model);
                    updateKeyStatus((byte)i, model);
                    updateOtherInfo((byte)i, model);
                }
            }
        }

        public override void updateView(string section, Object data)
        {
            if (data == null) return;

            StatusReportModel model = (StatusReportModel)data;
        }
    }
}