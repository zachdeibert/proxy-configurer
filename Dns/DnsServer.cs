using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Proxy;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsServer : IDisposable {
        bool Disposed;
        UdpClient Listener;
        IPAddress Address;
        int Port;
        DnsCache Cache;
        ProxyHandler Handler;

        void ReadCallback(IAsyncResult iar) {
            IPEndPoint endPoint = new IPEndPoint(Address, Port);
            byte[] data = Listener.EndReceive(iar, ref endPoint);
            Listener.BeginReceive(ReadCallback, null);
            DnsPacket packet = new DnsPacket(data);
            List<Task<DnsResourceRecord>> answers = new List<Task<DnsResourceRecord>>();
            foreach (DnsQuestion question in packet.Questions) {
                if (question.QueryName.EndsWith(".proxyconfigurer.localhost")) {
                    string query = question.QueryName.Substring(0, question.QueryName.Length - ".proxyconfigurer.localhost".Length);
                    string[] parts = query.Split('.');
                    string last = parts[parts.Length - 1];
                    ushort nonce;
                    if (last.StartsWith('n') && ushort.TryParse(last.Substring(1), out nonce)) {
                        if (parts[0] == "p") {
                            answers.Add(Handler.GetConfigurationPacket(nonce).ContinueWith(t => new DnsResourceRecord {
                                Name = question.QueryName,
                                Type = question.Type,
                                Class = question.Class,
                                TimeToLive = t.Result.Item2,
                                Data = t.Result.Item1
                            }));
                        } else {
                            answers.Add(Handler.ResolveHost(nonce, int.Parse(parts[0].Substring(1))).ContinueWith(t => {
                                Task<DnsResourceRecord> task = Cache[new DnsQuestion {
                                    QueryName = t.Result.Item1,
                                    Type = question.Type,
                                    Class = question.Class
                                }];
                                task.Wait();
                                task.Result.Name = question.QueryName;
                                task.Result.TimeToLive = Math.Min(task.Result.TimeToLive, t.Result.Item2);
                                return task.Result;
                            }));
                        }
                    } else {
                        answers.Add(Handler.GetFirstConfigurationPacket(query).ContinueWith(t => new DnsResourceRecord {
                            Name = question.QueryName,
                            Type = question.Type,
                            Class = question.Class,
                            TimeToLive = t.Result.Item2,
                            Data = t.Result.Item1
                        }));
                    }
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
            Cache = new DnsCache(cfg);
            Handler = new ProxyHandler(cfg);
            Listener.BeginReceive(ReadCallback, null);
        }
    }
}
