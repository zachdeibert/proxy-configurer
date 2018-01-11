using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Collections {
    public class CacheDictionary<TKey, TValue> : IDisposable where TValue : CacheEntry {
        static readonly TimeSpan MinimumTTL = TimeSpan.FromSeconds(1);
        Dictionary<TKey, TValue> Values;
        Dictionary<TKey, CancellationTokenSource> Callbacks;
        CancellationTokenSource DisposedToken;
        object Lock;
        public event Action<TKey> CacheMiss;

        public Task<TValue> this[TKey key] {
            get {
                lock (Lock) {
                    if (Values.ContainsKey(key)) {
                        return Task.FromResult(Values[key]);
                    } else {
                        CancellationTokenSource callback;
                        if (Callbacks.ContainsKey(key)) {
                            callback = Callbacks[key];
                        } else {
                            callback = Callbacks[key] = new CancellationTokenSource();
                            CacheMiss?.Invoke(key);
                        }
                        return Task.Delay(int.MaxValue, callback.Token).ContinueWith(t => Values[key]);
                    }
                }
            }
        }

        public void Add(TKey key, TValue value) {
            lock (Lock) {
                if (Values.ContainsKey(key)) {
                    Values[key].ExpirationToken.Cancel();
                }
                Values[key] = value;
                TimeSpan ttl = value.TimeToLive;
                if (ttl < MinimumTTL) {
                    ttl = MinimumTTL;
                }
                Task.Delay(ttl, CancellationTokenSource.CreateLinkedTokenSource(DisposedToken.Token, value.ExpirationToken.Token).Token).ContinueWith(t => {
                    if (!t.IsCanceled) {
                        lock (Lock) {
                            Values.Remove(key);
                        }
                    }
                });
                if (Callbacks.ContainsKey(key)) {
                    Callbacks[key].Cancel();
                    Callbacks.Remove(key);
                }
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!DisposedToken.IsCancellationRequested) {
                DisposedToken.Cancel();
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public CacheDictionary() {
            Values = new Dictionary<TKey, TValue>();
            Callbacks = new Dictionary<TKey, CancellationTokenSource>();
            DisposedToken = new CancellationTokenSource();
            Lock = new object();
        }
    }
}
