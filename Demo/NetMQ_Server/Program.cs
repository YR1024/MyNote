
using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NetMQContext context = NetMQContext.Create())
            {
                Server(context);
            }
        }

        static void Server(NetMQContext context)
        {
            using (NetMQSocket serverSocket = context.CreateResponseSocket())
            {
                serverSocket.Bind("tcp://*:5555");

                while (true)
                {
                    string message = serverSocket.ReceiveString();

                    Console.WriteLine("Receive message {0}", message);

                    serverSocket.Send("World");

                    if (message == "exit")
                    {
                        break;
                    }
                }
            }
        }
    }
}
