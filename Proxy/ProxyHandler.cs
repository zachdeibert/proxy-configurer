using System;
using System.Threading.Tasks;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Collections;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Proxy {
    public class ProxyHandler {
        static readonly ProxyTryEntry DefaultTry = new ProxyTryEntry {
            Type = ProxyType.EndOfList
        };
        public static readonly ProxyTryEntry DirectTry = new ProxyTryEntry {
            Type = ProxyType.Direct
        };
        public static readonly ProxySettings DefaultSettings = new ProxySettings {
            Tries = new [] {
                DirectTry
            }
        };
        CacheDictionary<string, ProxySettings> MainCache;
        CacheDictionary<ushort, ProxyEntry> ConnectionCache;
        TimeSpan ConnectionTimeout;

        void SerializeTry(ProxySettings settings, int tryNum, byte[] buffer, int index) {
            ProxyTryEntry entry = tryNum < settings.Tries.Length ? settings.Tries[tryNum] : DefaultTry;
            BitConverter.GetBytes(entry.Port).CopyTo(buffer, index);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(buffer, index, 2);
            }
            buffer[index] &= 0x3F;
            buffer[index] |= (byte) (((int) entry.Type) << 6);
        }

        public Task<Tuple<byte[], uint>> GetFirstConfigurationPacket(string host) {
            return MainCache[host].ContinueWith(t => {
                ProxyEntry entry = new ProxyEntry {
                    Settings = t.Result,
                    ConfigurationTriesSent = 1,
                    TimeToLive = ConnectionTimeout
                };
                ConnectionCache.Add(entry.Nonce, entry);
                byte[] buffer = new byte[4];
                BitConverter.GetBytes(entry.Nonce).CopyTo(buffer, 0);
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(buffer, 0, 2);
                }
                SerializeTry(t.Result, 0, buffer, 2);
                return new Tuple<byte[], uint>(buffer, (uint) t.Result.TimeToLive.TotalSeconds);
            });
        }

        public Task<Tuple<byte[], uint>> GetConfigurationPacket(ushort nonce) {
            return ConnectionCache[nonce].ContinueWith(t => {
                byte[] buffer = new byte[4];
                SerializeTry(t.Result.Settings, t.Result.ConfigurationTriesSent++, buffer, 0);
                SerializeTry(t.Result.Settings, t.Result.ConfigurationTriesSent++, buffer, 2);
                return new Tuple<byte[], uint>(buffer, (uint) t.Result.TimeToLive.TotalSeconds);
            });
        }

        public Task<Tuple<string, uint>> ResolveHost(ushort nonce, int tryNum) {
            return ConnectionCache[nonce].ContinueWith(t => {
                if (tryNum < t.Result.Settings.Tries.Length) {
                    return new Tuple<string, uint>(t.Result.Settings.Tries[tryNum].Host, (uint) t.Result.TimeToLive.TotalSeconds);
                } else {
                    return new Tuple<string, uint>("", 0);
                }
            });
        }

        public ProxyHandler(ConfigFile cfg) {
            MainCache = new CacheDictionary<string, ProxySettings>();
            string proxyProgram = cfg["Proxy"]["program"].Value;
            TimeSpan defaultTTL = TimeSpan.FromSeconds(cfg["Proxy"]["defaultTTL"].ToDouble(300));
            TimeSpan timeout = TimeSpan.FromSeconds(cfg["Proxy"]["programTimeout"].ToDouble(15));
            MainCache.CacheMiss += host => new ProxyProgram(proxyProgram, host, defaultTTL, timeout).Finished += settings => MainCache.Add(host, settings);
            ConnectionCache = new CacheDictionary<ushort, ProxyEntry>();
            ConnectionCache.CacheMiss += nonce => {
                Console.Error.WriteLine("Connection has expired.");
                ConnectionCache.Add(nonce, new ProxyEntry(nonce) {
                    Settings = DefaultSettings,
                    TimeToLive = TimeSpan.Zero
                });
            };
            ConnectionTimeout = TimeSpan.FromSeconds(cfg["Proxy"]["connectionTimeout"].ToDouble(60));
        }
    }
}
