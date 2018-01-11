using System;
using System.Threading;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Collections {
    public class CacheEntry {
        public DateTime EndOfLife;
        public TimeSpan TimeToLive {
            get {
                return EndOfLife - DateTime.Now;
            }
            set {
                EndOfLife = DateTime.Now + value;
            }
        }
        public readonly CancellationTokenSource ExpirationToken;

        public CacheEntry() {
            ExpirationToken = new CancellationTokenSource();
        }
    }
}
