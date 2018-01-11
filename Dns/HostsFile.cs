using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class HostsFile : IDisposable {
        CancellationTokenSource DisposedToken;
        string FileName;
        TimeSpan RefreshPeriod;
        Dictionary<string, byte[]> Entries;
        DateTime NextRefresh;

        public byte[] this[string host] {
            get {
                if (Entries.ContainsKey(host)) {
                    return Entries[host];
                } else {
                    return null;
                }
            }
        }

        public TimeSpan TimeToLive {
            get {
                return NextRefresh - DateTime.Now;
            }
        }

        void Refresh() {
            Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>();
            foreach (string _line in File.ReadAllLines(FileName)) {
                string line = _line.Trim();
                int commentStart = line.IndexOf('#');
                if (commentStart >= 0) {
                    line = line.Substring(0, commentStart);
                }
                string[] parts = line.Split(new [] {
                    ' ',
                    '\t'
                }, StringSplitOptions.RemoveEmptyEntries);
                IPAddress address;
                if (parts.Length > 1 && IPAddress.TryParse(parts[0], out address)) {
                    byte[] bytes = address.GetAddressBytes();
                    if (bytes.Length == 4) {
                        foreach (string host in parts.Skip(1)) {
                            entries[host] = bytes;
                        }
                    }
                }
            }
            Entries = entries;
        }

        void DelayedRefresh() {
            NextRefresh = DateTime.Now + RefreshPeriod;
            Task.Delay(RefreshPeriod, DisposedToken.Token).ContinueWith(t => {
                if (!t.IsCanceled) {
                    Refresh();
                    DelayedRefresh();
                }
            });
        }

        protected virtual void Dispose(bool disposing) {
            if (!DisposedToken.IsCancellationRequested) {
                DisposedToken.Dispose();
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public HostsFile(ConfigFile cfg) {
            DisposedToken = new CancellationTokenSource();
            FileName = cfg["Hosts"]["file"].ToString("/etc/hosts");
            RefreshPeriod = TimeSpan.FromSeconds(cfg["Hosts"]["refresh"].ToDouble(3600));
            Entries = new Dictionary<string, byte[]>();
            Refresh();
            DelayedRefresh();
        }
    }
}
