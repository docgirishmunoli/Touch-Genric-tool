using System;
using System.Collections.Generic;

namespace TouchCommanderGenericNamespace
{
    public abstract class TouchCommanderModelBase
    {
        protected TouchCommanderController control;

        public TouchCommanderModelBase(TouchCommanderController c)
        {
            control = c;
        }

        public abstract void updateModel(Object data);

        public abstract void updateModel(Object d, string section);
    }

    public class TouchCommanderMainModel
    {
        Dictionary<string, TouchCommanderModelBase> model_dict;
        TouchCommanderController control;

        public TouchCommanderMainModel(TouchCommanderController c)
        {
            model_dict = new Dictionary<string, TouchCommanderModelBase>();
            control = c;
        }

        public void Clear()
        {
            model_dict.Clear();
        }

        public void updateModel(string name, Object data)
        {
            if (model_dict.ContainsKey(name) == false)
            {
                createtModel(name);
            }

            if (model_dict.ContainsKey(name) == true)
            {
                model_dict[name].updateModel(data);
            }
        }

        public void updateModel(string name, string section, Object data)
        {
            if (model_dict.ContainsKey(name) == false)
            {
               createtModel(name);
            }

            if (model_dict.ContainsKey(name) == true)
            {
                model_dict[name].updateModel(data, section);
            }
        }

        public TouchCommanderModelBase getModel(string name)
        {
            if (model_dict.ContainsKey(name) == true)
            {
                return model_dict[name];
            }
            else
            {
                return null;
            }
        }

        private void createtModel(string name)
        {
            TouchCommanderModelBase model = null;

            if (name == "HW Info")
            {
                model = new HardwareInfoModel(this.control);
            }
            else if (name == "Status Report")
            {
                model = new StatusReportModel(this.control);
            }

            if (model != null)
            {
                model_dict.Add(name, model);
            }

            if (name == "Status Report")
            {
                model = new DataMonitoringModel(this.control);
                if (model != null)
                {
                    model_dict.Add("Data Monitoring", model);
                }

                model = new FastDataMonitoringModel(this.control);
                if (model != null)
                {
                    model_dict.Add("Fast Data Monitoring", model);
                }

                model = new CpMonitoringModel(this.control);
                if (model != null)
                {
                    model_dict.Add("CP Monitoring", model);
                }
            }

            if (name == "Touch Config")
            {
                model = new TouchConfigModel(this.control);
                if (model != null)
                {
                    model_dict.Add("Touch Config", model);
                }
            }
        }
    }
}