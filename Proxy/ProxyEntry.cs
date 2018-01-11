using System;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Collections;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Proxy {
    public class ProxyEntry : CacheEntry {
        static ushort NextNonce = 0;
        public ushort Nonce;
        public ProxySettings Settings;
        public int ConfigurationTriesSent;

        public ProxyEntry(ushort nonce) {
            Nonce = nonce;
        }

        public ProxyEntry() : this(NextNonce++) {
        }
    }
}
