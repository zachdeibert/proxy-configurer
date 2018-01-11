using System;
using System.Collections.Generic;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Config {
    public class ConfigSection {
        public readonly string Name;
        public readonly IReadOnlyDictionary<string, ConfigValue> Values;

        public ConfigValue this[string key, string def = null] {
            get {
                if (Values.ContainsKey(key)) {
                    return Values[key];
                } else {
                    return new ConfigValue(key, def);
                }
            }
        }

        public ConfigSection(string name, IReadOnlyDictionary<string, ConfigValue> values) {
            Name = name;
            Values = values;
        }
    }
}
