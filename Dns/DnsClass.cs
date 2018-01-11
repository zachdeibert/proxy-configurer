using System;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public enum DnsClass : ushort {
        IN = 1,
        CH = 3,
        HS,
        None = 254,
        Any
    }
}
