using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Collections;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsCache : IDisposable {
        bool Disposed;
        CacheDictionary<DnsQuestion, DnsCacheEntry> Cache;
        UdpClient[] Upstreams;
        HostsFile HostsFile;

        public Task<DnsResourceRecord> this[DnsQuestion question]
            => Cache[question].ContinueWith(t => t.Result.ToResourceRecord(question));

        void ResolveHost(DnsQuestion question) {
            IPAddress address;
            if (IPAddress.TryParse(question.QueryName, out address)) {
                Console.WriteLine("Ignored question for IP {0}.", address);
                Cache.Add(question, new DnsCacheEntry {
                    Data = address.GetAddressBytes(),
                    TimeToLive = TimeSpan.FromMinutes(1)
                });
            } else {
                byte[] addr = HostsFile[question.QueryName];
                if (addr == null) {
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
                } else {
                    Console.WriteLine("Found {0} in hosts file.", question.QueryName);
                    Cache.Add(question, new DnsCacheEntry {
                        Data = addr,
                        TimeToLive = HostsFile.TimeToLive
                    });
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
                        Cache.Add(question, new DnsCacheEntry {
                            Data = answer.Data,
                            TimeToLive = TimeSpan.FromSeconds(answer.TimeToLive)
                        });
                    }
                } else {
                    Console.Error.WriteLine(packet.ReturnCode);
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
                    HostsFile.Dispose();
                }
                Disposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public DnsCache(ConfigFile cfg) {
            Cache = new CacheDictionary<DnsQuestion, DnsCacheEntry>();
            Cache.CacheMiss += ResolveHost;
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
            HostsFile = new HostsFile(cfg);
        }
    }
}
