using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Config {
    public class ConfigFile {
        public IReadOnlyDictionary<string, ConfigSection> Sections;

        public ConfigSection this[string section] {
            get {
                if (Sections.ContainsKey(section)) {
                    return Sections[section];
                } else {
                    return new ConfigSection(section, new ConfigValue[0].ToImmutableDictionary(o => ""));
                }
            }
        }

        public ConfigFile(string path) {
            Dictionary<string, ConfigSection> sections = new Dictionary<string, ConfigSection>();
            string section = "DEFAULT";
            Dictionary<string, ConfigValue> values = new Dictionary<string, ConfigValue>();
            using (StreamReader reader = File.OpenText(path)) {
                while (!reader.EndOfStream) {
                    string line = reader.ReadLine().Trim();
                    if (line.StartsWith("[") && line.EndsWith("]")) {
                        sections[section] = new ConfigSection(section, values.ToImmutableDictionary());
                        section = line.Substring(1, line.Length - 2);
                        values = new Dictionary<string, ConfigValue>();
                    } else if (!line.StartsWith("#") && !line.StartsWith("//") && line.Contains("=")) {
                        string[] parts = line.Split('=', 2);
                        values[parts[0]] = new ConfigValue(parts[0], parts[1]);
                    }
                }
            }
            sections[section] = new ConfigSection(section, values.ToImmutableDictionary());
            Sections = sections.ToImmutableDictionary();
        }
    }
}
