using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Com.GitHub.ZachDeibert.ProxyConfigurer.Config;

namespace Com.GitHub.ZachDeibert.ProxyConfigurer.Pac {
    public class PacServer : IDisposable {
        bool Disposed;
        TcpListener Listener;
        string PacFile;

        void AcceptCallback(IAsyncResult iar) {
            if (!Disposed) {
                TcpClient client = Listener.EndAcceptTcpClient(iar);
                Listener.BeginAcceptTcpClient(AcceptCallback, null);
                using (Stream stream = client.GetStream()) {
                    using (StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true)) {
                        while (reader.ReadLine() != "");
                    }
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII)) {
                        writer.NewLine = "\r\n";
                        writer.WriteLine("HTTP/1.1 200 OK");
                        writer.WriteLine("Content-Type: application/x-ns-proxy-autoconfig");
                        writer.WriteLine("Content-Length: {0}", PacFile.Length);
                        writer.WriteLine();
                        writer.Write(PacFile);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!Disposed) {
                if (disposing) {
                    Listener.Stop();
                }
                Disposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public PacServer(ConfigFile cfg) {
            Listener = new TcpListener(cfg["PAC"]["address"].ToIPAddress(IPAddress.Any), cfg["PAC"]["port"].ToInt(80));
            Listener.Start();
            Listener.BeginAcceptTcpClient(AcceptCallback, null);
            using (StreamReader reader = new StreamReader(typeof(PacServer).Assembly.GetManifestResourceStream("proxy-configurer.Pac.script.pac"))) {
                PacFile = reader.ReadToEnd();
            }
        }
    }
}
