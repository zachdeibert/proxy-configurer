using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsCache : IDisposable {
        bool Disposed;
        Dictionary<DnsQuestion, DnsCacheEntry> Cache;
        Dictionary<DnsQuestion, CancellationTokenSource> Callbacks;
        UdpClient[] Upstreams;

        public Task<DnsResourceRecord> this[DnsQuestion question] {
            get {
                if (Cache.ContainsKey(question)) {
                    return Task.FromResult(Cache[question].ToResourceRecord(question));
                } else {
                    CancellationTokenSource source;
                    if (Callbacks.ContainsKey(question)) {
                        source = Callbacks[question];
                    } else {
                        source = Callbacks[question] = new CancellationTokenSource();
                        byte[] pkt = new DnsPacket {
                            RecursionDesired = true,
                            Questions = new [] {
                                question
                            }
                        }.ToByteArray();
                        Console.WriteLine("Question for {0}", question.QueryName);
                        foreach (UdpClient upstream in Upstreams) {
                            upstream.Send(pkt, pkt.Length);
                        }
                    }
                    return Task.Delay(int.MaxValue, source.Token).ContinueWith(t => Cache[question].ToResourceRecord(question));
                }
            }
        }

        void ReadCallback(IAsyncResult iar) {
            UdpClient upstream = (UdpClient) iar.AsyncState;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 53);
            byte[] pkt = upstream.EndReceive(iar, ref endPoint);
            upstream.BeginReceive(ReadCallback, upstream);
            try {
                DnsPacket packet = new DnsPacket(pkt);
                if (packet.ReturnCode == DnsReturnCode.NoError) {
                    foreach (DnsResourceRecord answer in packet.Answers) {
                        Console.WriteLine("Answer for {0}", answer.Name);
                        DnsQuestion question = new DnsQuestion {
                            QueryName = answer.Name,
                            Type = answer.Type,
                            Class = answer.Class
                        };
                        Cache[question] = new DnsCacheEntry {
                            Data = answer.Data,
                            EndOfLife = DateTime.Now + TimeSpan.FromSeconds(answer.TimeToLive)
                        };
                        if (Callbacks.ContainsKey(question)) {
                            Callbacks[question].Cancel();
                            Callbacks.Remove(question);
                        }
                    }
                } else {
                    Console.Error.WriteLine(packet.QueryResponse);
                    pkt = new DnsPacket {
                        Questions = packet.Questions
                    }.ToByteArray();
                    upstream.Send(pkt, pkt.Length);
                }
            } catch (Exception ex) {
                Console.Error.WriteLine(ex);
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!Disposed) {
                if (disposing) {
                    foreach (UdpClient upstream in Upstreams) {
                        upstream.Dispose();
                    }
                }
                Disposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public DnsCache(ConfigFile cfg) {
            Cache = new Dictionary<DnsQuestion, DnsCacheEntry>();
            Callbacks = new Dictionary<DnsQuestion, CancellationTokenSource>();
            string[] upstreamAddresses = cfg["DNS"]["upstream"].ToString("8.8.8.8").Split(' ');
            if (upstreamAddresses.Length == 0 || (upstreamAddresses.Length == 1 && upstreamAddresses[0] == "")) {
                upstreamAddresses = new [] {
                    "8.8.8.8"
                };
            }
            Upstreams = upstreamAddresses.Select(s => {
                UdpClient upstream = new UdpClient();
                upstream.Connect(IPAddress.Parse(s), 53);
                upstream.BeginReceive(ReadCallback, upstream);
                return upstream;
            }).ToArray();
        }
    }
}
