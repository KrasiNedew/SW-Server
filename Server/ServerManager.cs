﻿namespace Server
{
    public class ServerManager
    {
        private static ServerManager instance;


        private ServerManager(AsynchronousSocketListener listener)
        {
            this.Listener = listener;
        }

        private AsynchronousSocketListener Listener { get; }

        public static ServerManager Instance => instance ?? (instance = new ServerManager(new AsynchronousSocketListener()));


        public void StartServer(int port)
        {
            this.Listener.StartListening(port);
        }
    }
}