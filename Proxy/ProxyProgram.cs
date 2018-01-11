using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Proxy {
    public class ProxyProgram : IDisposable {
        List<ProxyTryEntry> Tries;
        TimeSpan TTL;
        Process Process;
        CancellationTokenSource DisposedToken;
        public event Action<ProxySettings> Finished;

        void LineRead(object sender, DataReceivedEventArgs e) {
            if (e.Data == null) {
                Dispose();
            } else {
                string[] parts = e.Data.Split(' ');
                switch (parts[0]) {
                    case "TTL":
                        if (parts.Length >= 2) {
                            TTL = TimeSpan.FromSeconds(double.Parse(parts[1]));
                        } else {
                            Console.Error.WriteLine("Invalid proxy command use for command {0}", parts[0]);
                        }
                        break;
                    case "DIRECT":
                        Tries.Add(ProxyHandler.DirectTry);
                        break;
                    case "PROXY":
                        if (parts.Length >= 3) {
                            Tries.Add(new ProxyTryEntry {
                                Type = ProxyType.Proxy,
                                Host = parts[1],
                                Port = ushort.Parse(parts[2])
                            });
                        } else {
                            Console.Error.WriteLine("Invalid proxy command use for command {0}", parts[0]);
                        }
                        break;
                    case "SOCKS":
                        if (parts.Length >= 3) {
                            Tries.Add(new ProxyTryEntry {
                                Type = ProxyType.Socks,
                                Host = parts[1],
                                Port = ushort.Parse(parts[2])
                            });
                        } else {
                            Console.Error.WriteLine("Invalid proxy command use for command {0}", parts[0]);
                        }
                        break;
                    default:
                        Console.Error.WriteLine("Unknown proxy command {0}", parts[0]);
                        break;
                }
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!DisposedToken.IsCancellationRequested) {
                DisposedToken.Cancel();
                if (disposing) {
                    if (!Process.HasExited) {
                        Process.Kill();
                    }
                    if (Tries.Count > 0) {
                        Finished?.Invoke(new ProxySettings {
                            Tries = Tries.ToArray(),
                            TimeToLive = TTL
                        });
                    } else {
                        Console.Error.WriteLine("Proxy program crashed.");
                        Finished?.Invoke(ProxyHandler.DefaultSettings);
                    }
                }
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public ProxyProgram(string program, string host, TimeSpan defaultTTL, TimeSpan timeout) {
            Tries = new List<ProxyTryEntry>();
            TTL = defaultTTL;
            Process = new Process();
            Process.StartInfo.Arguments = host;
            Process.StartInfo.FileName = program;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.EnableRaisingEvents = true;
            Process.OutputDataReceived += LineRead;
            Process.Start();
            Process.BeginOutputReadLine();
            DisposedToken = new CancellationTokenSource();
            Task.Delay(timeout, DisposedToken.Token).ContinueWith(t => Dispose());
        }
    }
}
