using System;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsCacheEntry {
        public byte[] Data;
        public DateTime EndOfLife;

        public DnsResourceRecord ToResourceRecord(DnsQuestion question) {
            return new DnsResourceRecord {
                Name = question.QueryName,
                Type = question.Type,
                Class = question.Class,
                TimeToLive = (uint) (EndOfLife - DateTime.Now).TotalSeconds,
                Data = Data
            };
        }
    }
}
