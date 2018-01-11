using System;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public enum DnsOpcode : byte {
        Query = 0,
        Iquery,
        Status,
        Notify = 4,
        Update 
    }
}
