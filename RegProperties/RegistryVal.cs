using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenSaver.RegProperties
{
	
    class RegistryVal
    {
        public enum propertyType{Free, Pro}

        public RegistryVal(string valueName, propertyType usageMode, string propertyValue, List<string> propertyOptions, string DefaultVal, string registryValName)
        {
            this._valueName = valueName;
            this._usageMode = usageMode;
            if (propertyValue == null)
            {
                this._propertyValue = DefaultVal;
            }
            else
            {
                this._propertyValue = propertyValue;
            }
            
            this._propertyOptions = propertyOptions;
            this._defaultVal = DefaultVal;
            this._registryValName = registryValName;
        }

        private string _valueName;

        public string ValueName
        {
            get { return this._valueName; }
            set { this._valueName = value; }
        }

        private string _defaultVal;
        public string DefaultVal
        {
            get { return this._defaultVal; }
            set { this._defaultVal = value; }
        }

        private propertyType _usageMode;

        public propertyType UsageMode
        {
            get { return this._usageMode; }
            set { this._usageMode = value; }
        }

        private string _propertyValue;

        public string PropertyValue
        {
            get { return this._propertyValue; }
            set { 
                this._propertyValue = validate(value);
            }
        }

        //validate list value
        private string validate(string value)
        {
            if (value == null)
            {
                return PropertyValue;
            }
            if (PropertyOptions.Count == 0)
            {
                return value;
            }
            //enable multiple values selection
            string[] values = value.Split(';');
            foreach (string valOpt in values)
            {
                if (!PropertyOptions.Contains(valOpt))
                {
                    //invalid selection - prevent update
                    return _propertyValue;
                }
            }
            //all value components are valid - enable update
            return value;
        }

        private List<string> _propertyOptions;

        public List<string> PropertyOptions
        {
            get { return this._propertyOptions; }
            set { this._propertyOptions = value; }
        }

        private string _registryValName;

        public string RegistryValName
        {
            get { return this._registryValName; }
            set { this._registryValName = value; }
        }
        
    }
}
