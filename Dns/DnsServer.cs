using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsServer : IDisposable {
        bool Disposed;
        UdpClient Listener;
        IPAddress Address;
        int Port;
        DnsCache Cache;

        void ReadCallback(IAsyncResult iar) {
            IPEndPoint endPoint = new IPEndPoint(Address, Port);
            byte[] data = Listener.EndReceive(iar, ref endPoint);
            Listener.BeginReceive(ReadCallback, null);
            DnsPacket packet = new DnsPacket(data);
            List<Task<DnsResourceRecord>> answers = new List<Task<DnsResourceRecord>>();
            foreach (DnsQuestion question in packet.Questions) {
                if (question.QueryName.EndsWith(".proxyconfigurer.localhost")) {
                    answers.Add(Task.FromResult(new DnsResourceRecord {
                        Name = question.QueryName,
                        Type = question.Type,
                        Class = question.Class,
                        TimeToLive = 10,
                        Data = question.QueryName == "www.ipchicken.com.proxyconfigurer.localhost" ? new [] {
                            (byte) 0,
                            (byte) 0,
                            (byte) 0b10011111,
                            (byte) 0x40
                        } : new [] {
                            (byte) 0b10011111,
                            (byte) 0x41,
                            (byte) 0b01000000,
                            (byte) 0
                        }
                    }));
                } else {
                    answers.Add(Cache[question]);
                }
                Console.WriteLine("Query for {0}", question.QueryName);
            }
            Task.WhenAll(answers).ContinueWith(t => {
                byte[] pkt = new DnsPacket {
                    Identification = packet.Identification,
                    QueryResponse = true,
                    Questions = packet.Questions,
                    Answers = t.Result
                }.ToByteArray();
                Listener.Send(pkt, pkt.Length, endPoint);
            });
        }

        protected virtual void Dispose(bool disposing) {
            if (!Disposed) {
                if (disposing) {
                    Listener.Dispose();
                    Cache.Dispose();
                }
                Disposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public DnsServer(ConfigFile cfg) {
            Address = cfg["DNS"]["address"].ToIPAddress(IPAddress.Any);
            Port = cfg["DNS"]["port"].ToInt(53);
            Listener = new UdpClient(Port);
            Listener.BeginReceive(ReadCallback, null);
            Cache = new DnsCache(cfg);
        }
    }
}
