using System;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Collections;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public class DnsCacheEntry : CacheEntry {
        public byte[] Data;

        public DnsResourceRecord ToResourceRecord(DnsQuestion question) {
            return new DnsResourceRecord {
                Name = question.QueryName,
                Type = question.Type,
                Class = question.Class,
                TimeToLive = (uint) TimeToLive.TotalSeconds,
                Data = Data
            };
        }
    }
}
