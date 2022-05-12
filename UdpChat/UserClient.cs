using System;
using System.Net.Sockets;

namespace UdpChat
{
    class UserClient
    {
        public string UserName { get; set; } 
        public UdpClient Client { get; }
        public UserClient(string uName, int localPort)
        {
            try
            {
                UserName = uName;
                Client = new UdpClient(localPort);
            }
            catch
            {
                throw new Exception("Ошибка инициализации клиента!");
            }
        }
    }
}
