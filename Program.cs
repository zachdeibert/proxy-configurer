using System;
using System.Threading;
using System.Threading.Tasks;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Dns;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Pac;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer {
    class Program {
        static CancellationTokenSource CTS;

        static void Interrupted(object sender, ConsoleCancelEventArgs args) {
            CTS.Cancel();
        }

        static void Main(string[] args) {
            ConfigFile cfg = new ConfigFile(
#if DEBUG
                "proxy-configurer.cfg"
#else
                "/etc/proxy-configurer.cfg"
#endif
            );
            CTS = new CancellationTokenSource();
            Console.CancelKeyPress += Interrupted;
            using (DnsServer dns = new DnsServer(cfg)) {
                using (PacServer pac = new PacServer(cfg)) {
                    Console.WriteLine("Server is running!");
                    Task.Delay(int.MaxValue, CTS.Token).Wait();
                }
            }
        }
    }
}
