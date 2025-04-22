using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ZoomServer
{
    internal class Program
    {
        const int portNo = 500;


        /// <summary>
        /// Main function to run
        /// </summary>
        /// <param name="args">arguments</param>
        static void Main(string[] args)
        {
            Sqlconnect.GetInstance().CreateDbSchemaIfDoesNotExist();

            TcpListener listener = new TcpListener(IPAddress.Any, portNo);


            Console.WriteLine("Server is ready.");

            // Start listen to incoming connection requests
            listener.Start();
            Debug.WriteLine($"[Program] Server started listening on port {portNo}.");

            // infinit loop.
            while (true)
            {

                // AcceptTcpClient - Blocking call
                // Execute will not continue until a connection is established

                // We create an instance of ChatClient so the server will be able to 
                // server multiple client at the same time.
                try
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    Debug.WriteLine($"[Program] Client connected from {tcpClient.Client.RemoteEndPoint}, on socket {tcpClient.Client.SocketType}");
                    // Create a new ZoomClientConnection instance for each accepted client
                    // This will handle the connection and communication with the client
                    ZoomClientConnection user = new ZoomClientConnection(tcpClient);
                }
                catch (Exception)
                {
                    // ignoring the exception because its probably DOS error
                }
            }




        }
    }
}


    