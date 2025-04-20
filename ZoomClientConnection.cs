using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace ZoomServer
{
    public class ZoomClientConnection
    {
        public static Hashtable AllClients = new Hashtable();


        /// <summary>
        /// The IP and port address of the connected client.
        /// </summary>
        private IPEndPoint _clientIP;

        /// <summary>
        /// Timestamp of the client's last connection.
        /// </summary>
        private DateTime _lastConnect;



        /// <summary>
        /// Handler for processing commands for a single user.
        /// </summary>
        public CommandHandlerForSingleUser CommandHandlerForSingleUser;

        private TcpConnectionHandler _tcpConnectionHandler;



        /// <summary>
        /// Constructor with parameter
        /// </summary>
        /// <param name="client">The TCP client representing the connection.</param>
        public ZoomClientConnection(TcpClient client)
        {
            int count = 0;

            // Get the IP address of the client to register in our client list
            this._clientIP = client.Client.RemoteEndPoint as IPEndPoint;

            // DOS protection
            foreach (DictionaryEntry user in AllClients)
            {
                bool sameIpAddress = IPAddress.Equals((IPEndPoint)client.Client.RemoteEndPoint, user.Key);
                if (sameIpAddress)
                {
                    DateTime time = ((ZoomClientConnection)user.Value)._lastConnect;
                    if ((DateTime.Now - time).TotalSeconds < 10)
                    {
                        count++;
                    }
                }
            }
            if (count >= 10)
            {
                throw new Exception("Someone is trying to connect too fast (DOS)");
            }

            this._lastConnect = DateTime.Now;


            // Add the new client to our clients collection
            AllClients.Add(this._clientIP, this);

            this._tcpConnectionHandler = new TcpConnectionHandler(client, this);


            this.CommandHandlerForSingleUser = new CommandHandlerForSingleUser(this);

            this._tcpConnectionHandler.StartListen();
        }

        /// <summary>
        /// Sends a message to the client over the TCP connection.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            this._tcpConnectionHandler.SendMessage(message);
        }



        /// <summary>
        /// Processes a received message from the client.
        /// </summary>
        /// <param name="messageData">The raw message data.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        public void ProcessMessage(byte[] messageData, int bytesRead, bool isFirstMessage)
        {
            string commandRecive = System.Text.Encoding.UTF8.GetString(messageData, 0, bytesRead);
            Console.WriteLine("commandRecive: " + commandRecive);

            if (isFirstMessage)
            {
                RsaFunctions.PublicKey = JsonConvert.DeserializeObject<RSAParameters>(commandRecive);
                Console.WriteLine("Rsa public key recived");
                var jsonString = JsonConvert.SerializeObject(SymmetricEncryptionManager.EncryptionData);
                this.SendMessage(jsonString);
                Console.WriteLine("Aes key and iv sent");

            }
            else
            {
                commandRecive = SymmetricEncryptionManager.DecodeText(commandRecive);
                string[] stringSeparators = new string[] { "\r\n\r\n" };
                string[] lines = commandRecive.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine(lines[i]);
                    this.CommandHandlerForSingleUser.HandleCommand(lines[i]);
                }
            }
        }

        /// <summary>
        /// Closes the client connection and removes it from the list of active clients.
        /// </summary>
        public void Close()
        {
            AllClients.Remove(this._clientIP);
            this._tcpConnectionHandler.Close();
        }
    


        public static void SendMessageToAllUserExceptOne(int userIdToExclude, ClientServerProtocol protocol)
        {
            foreach (ZoomClientConnection user in AllClients.Values)
            {
                if (user.CommandHandlerForSingleUser._userId > 0 && user.CommandHandlerForSingleUser._userId != userIdToExclude)
                {
                    user.SendMessage(protocol.Generate());
                }
            }
        }

        public static void SendMessageToSpecificUser(int userId, ClientServerProtocol protocol)
        {
            foreach (ZoomClientConnection user in AllClients.Values)
            {
                if (user.CommandHandlerForSingleUser._userId > 0 && user.CommandHandlerForSingleUser._userId == userId)
                {
                    user.SendMessage(protocol.Generate());
                    return;
                }
            }
        }

        public static string GetUserIpById(int userId)
        {
            foreach (ZoomClientConnection user in AllClients.Values)
            {
                if (user.CommandHandlerForSingleUser._userId == userId)
                {
                    return user._clientIP.Address.ToString();
                }
            }
            return null;
        }

        public void HandleDisconnect()
        {
            Console.WriteLine($"User {_clientIP} disconnected.");

            // 1. הסרה מהטבלה
            ZoomClientConnection.AllClients.Remove(this._clientIP);

            // 2. סגירת חיבור TCP
            this._tcpConnectionHandler?.Close();

            // 3. כתיבת לוג
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info($"Client from IP {_clientIP} disconnected at {DateTime.Now}");

            // 4. אם יש עוד דברים כמו הסרה מחדרים / צ'אטים / שידורים - תעשה כאן
        }


        public static List<int> GetIdsOfAllConnectedUsers()
        {
            List<int> ids = new List<int>();
            foreach (ZoomClientConnection user in AllClients.Values)
            {
                ids.Add(user.CommandHandlerForSingleUser._userId);
            }
            return ids;
        }




    }
}
