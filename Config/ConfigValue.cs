using System;
using System.Net;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Config {
    public class ConfigValue {
        public readonly string Key;
        public readonly string Value;

        public string ToString(string def = null) {
            if (Value == null) {
                return def;
            } else {
                return Value;
            }
        }

        public IPAddress ToIPAddress(IPAddress def = null) {
            IPAddress val;
            if (Value != null && IPAddress.TryParse(Value, out val)) {
                return val;
            } else {
                return def;
            }
        }

        public int ToInt(int def = -1) {
            int val;
            if (Value != null && int.TryParse(Value, out val)) {
                return val;
            } else {
                return def;
            }
        }

        public double ToDouble(double def = -1) {
            double val;
            if (Value != null && double.TryParse(Value, out val)) {
                return val;
            } else {
                return def;
            }
        }

        public ConfigValue(string key, string value) {
            Key = key;
            Value = value;
        }
    }
}
