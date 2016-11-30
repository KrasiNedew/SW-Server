namespace Startup
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Server;
    using Server.Services;
    using Server.Wrappers;

    class Startup
    {
        #region Before exit
        private static readonly ManualResetEvent exitSystem = new ManualResetEvent(false);

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            try
            {      
                server.Dispose();
            }
            finally
            {
                // allow main to run off
                exitSystem.Set();               
            }
        
            // shutdown right away so there are no lingering threads
            Environment.Exit(-1);
                        
            return true;
        }
        #endregion

        static AsynchronousSocketListener server = new AsynchronousSocketListener();
        static void Main()
        {
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            Buffers.Init();
            try
            {
                server.StartListening(3000);
            }
            finally
            {
                server.Dispose();
                exitSystem.Set();
            }

            exitSystem.WaitOne();
        }
    }
}
