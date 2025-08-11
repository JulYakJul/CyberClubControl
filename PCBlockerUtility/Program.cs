using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace PCBlockerUtility
{
    internal static class Program
    {
        private static TcpListener _listener;
        private static Thread _listenerThread;
        private const int ListenerPort = 54321; // Порт для прослушивания

        [STAThread]
        static void Main()
        {
            //StartTcpServer();

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            //AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            //{
            //    Exception ex = (Exception)e.ExceptionObject;
            //    MessageBox.Show($"Critical error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    EventLog.WriteEntry("PCBlockerUtility", $"Fatal error: {ex}", EventLogEntryType.Error);
            //};

            Application.Run(new Form1());
        }

        //private static void StartTcpServer()
        //{
        //    _listenerThread = new Thread(() =>
        //    {
        //        try
        //        {
        //            _listener = new TcpListener(IPAddress.Any, ListenerPort);
        //            _listener.Start();
        //            Debug.WriteLine($"Server started on port {ListenerPort}");

        //            while (true)
        //            {
        //                var client = _listener.AcceptTcpClient();
        //                ThreadPool.QueueUserWorkItem(HandleClientConnection, client);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Server error: {ex.Message}");
        //            EventLog.WriteEntry("PCBlockerUtility", $"Server crashed: {ex}", EventLogEntryType.Error);
        //        }
        //    })
        //    {
        //        IsBackground = true,
        //        Priority = ThreadPriority.BelowNormal
        //    };
        //    _listenerThread.Start();
        //}

        //private static void HandleClientConnection(object state)
        //{
        //    using (var client = (TcpClient)state)
        //    using (var stream = client.GetStream())
        //    using (var reader = new StreamReader(stream))
        //    using (var writer = new StreamWriter(stream))
        //    {
        //        try
        //        {
        //            client.ReceiveTimeout = 5000; // 5 секунд таймаут
        //            var request = reader.ReadLine();

        //            Debug.WriteLine($"Received: {request}");

        //            // Обработка команд
        //            switch (request?.ToUpper())
        //            {
        //                case "PING":
        //                    writer.WriteLine("PONG");
        //                    break;

        //                case "CHECK_ALIVE":
        //                    writer.WriteLine("ALIVE_OK");
        //                    break;

        //                case string s when s.StartsWith("UNLOCK"):
        //                    var parts = s.Split('|');
        //                    if (parts.Length == 2 && DateTime.TryParse(parts[1], out var endTime))
        //                    {
        //                        Form1.Instance?.OnUnlockReceived(endTime);
        //                        writer.WriteLine("UNLOCK_OK");
        //                    }
        //                    else
        //                    {
        //                        writer.WriteLine("INVALID_FORMAT");
        //                    }
        //                    break;

        //                case "LOCK":
        //                    Form1.Instance?.OnLockReceived();
        //                    writer.WriteLine("LOCK_OK");
        //                    break;

        //                default:
        //                    writer.WriteLine("UNKNOWN_COMMAND");
        //                    break;
        //            }
        //            writer.Flush();
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Client handling error: {ex.Message}");
        //        }
        //    }
        //}
    }
}