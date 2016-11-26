namespace Startup
{
    using Server;

    class Startup
    {
        static void Main()
        {
            ServerManager.Instance.StartServer(3000);
        }
    }
}
