namespace Startup
{
    using Server;
    using Server.Wrappers;

    class Startup
    {
        static void Main()
        {
            Buffers.Init();
            ServerManager.Instance.StartServer(3000);
            ServerManager.Instance.Listener.Dispose();
        }
    }
}
