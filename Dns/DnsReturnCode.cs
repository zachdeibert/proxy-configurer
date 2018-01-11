using System;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Dns {
    public enum DnsReturnCode : byte {
        NoError = 0,
        FormatError,
        SevereFailure,
        NameError,
        NotImplemented,
        Refused,
        YXDomain,
        YXRRSet,
        NXRRSet,
        NotAuth,
        NotZone,
        BadVers = 16,
        BadSig = 16,
        BadKey,
        BadTime,
        BadMode,
        BadName,
        BadAlg,
        BadTrunc
    }
}
