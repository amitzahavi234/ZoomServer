using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZoomServer
{

    public class TcpConnectionHandler
    {
        /// <summary>
        /// Length of the current message being read from the client.
        /// </summary>
        private int messageLength = -1;

        /// <summary>
        /// Total number of bytes read from the current message.
        /// </summary>
        private int totalBytesRead = 0;


        /// <summary>
        /// Indicates if the first message has been received from the client.
        /// </summary>
        private bool _isFirstMessage = true;


        /// <summary>
        /// Buffer for reading data from the client.
        /// </summary>
        private byte[] _data;

        /// <summary>
        /// Memory stream used for assembling incoming message data.
        /// </summary>
        private MemoryStream memoryStream = new MemoryStream();


        /// <summary>
        /// The TCP client representing the connection.
        /// </summary>
        private TcpClient _client;

        private ZoomClientConnection ZoomClientConnection;


        public TcpConnectionHandler(TcpClient tcpClient, ZoomClientConnection ZoomClientConnection)
        {
            this._client = tcpClient;
            this.ZoomClientConnection = ZoomClientConnection;
            // Read data from the client asynchronously
            this._data = new byte[this._client.ReceiveBufferSize];

        }

        public void StartListen()
        {
            // Begin async read from the NetworkStream
            _client.GetStream().BeginRead(this._data,
                                          0,
                                          System.Convert.ToInt32(this._client.ReceiveBufferSize),
                                          ReceiveMessage,
                                          _client.GetStream());

        }



        /// <summary>
        /// The function convert the string that we want to send to the server into byte array and send it to the server over the tcp connection
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            Console.WriteLine(message);
            try
            {
                // send message to the server

                NetworkStream ns;

                // Use lock to prevent multiple threads from accessing the network stream simultaneously
                lock (this._client.GetStream())
                {
                    ns = this._client.GetStream();
                }

                if (this._isFirstMessage)
                {
                    message = RsaFunctions.Encrypt(message);
                }
                else
                {
                    message = SymmetricEncryptionManager.EncodeText(message);
                }

                byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
                byte[] length = BitConverter.GetBytes(data.Length);
                byte[] bytes = new byte[data.Length + 4];
                // combine byte array, I took this code from the website StackOverFlow in this link:
                // https://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
                System.Buffer.BlockCopy(length, 0, bytes, 0, length.Length);
                System.Buffer.BlockCopy(data, 0, bytes, length.Length, data.Length);

                // send the text
                ns.Write(bytes, 0, bytes.Length);
                ns.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        /// <summary>
        /// Receives a message from the client asynchronously and processes it when complete.
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveMessage(IAsyncResult ar)
        {
            NetworkStream stream = (NetworkStream)ar.AsyncState;

            try
            {
                int bytesRead = stream.EndRead(ar);

                if (bytesRead > 0)
                {
                    HandleReceivedMessage(bytesRead);

                    lock (this._client.GetStream())
                    {
                        this._client.GetStream().BeginRead(this._data, 0, this._client.ReceiveBufferSize,
                            ReceiveMessage, _client.GetStream());
                    }
                }
                else
                {
                    // אם bytesRead == 0 → החיבור נסגר בצורה תקינה
                    Console.WriteLine("Client disconnected gracefully.");
                    ZoomClientConnection.HandleDisconnect(); // אם יש
                    this.Close();
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("Connection closed by remote host: " + ioEx.Message);
                ZoomClientConnection.HandleDisconnect(); // טיפוסי
                this.Close();
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Stream already disposed. Ignoring...");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error while receiving message: " + ex.Message);
                ZoomClientConnection.HandleDisconnect(); // אפשרי
                this.Close();
            }
        }
      

        private void HandleReceivedMessage(int bytesRead)
        {
            // If message length is not set, read the first 4 bytes for message length
            if (this.messageLength == -1 && this.totalBytesRead < 4)
            {
                int remainingLengthBytes = 4 - this.totalBytesRead;
                int bytesToCopy = Math.Min(bytesRead, remainingLengthBytes);
                memoryStream.Write(this._data, 0, bytesToCopy);
                this.totalBytesRead += bytesToCopy;
                // Check if we have read the full length header
                if (this.totalBytesRead >= 4)
                {
                    // Move the memory stream's read pointer to the beginning 
                    this.memoryStream.Seek(0, SeekOrigin.Begin);
                    byte[] lengthBytes = new byte[4];
                    // Read 4 bytes from the memory stream into lengthBytes
                    this.memoryStream.Read(lengthBytes, 0, 4);
                    this.messageLength = BitConverter.ToInt32(lengthBytes, 0);
                    // Reset memory stream to accumulate the rest of the message
                    this.memoryStream.SetLength(0);
                }

                // If there’s more data in this chunk, process it as part of the message body
                if (bytesRead > bytesToCopy)
                {
                    this.memoryStream.Write(this._data, bytesToCopy, bytesRead - bytesToCopy);
                    this.totalBytesRead += bytesRead - bytesToCopy;
                }
            }
            else
            {
                // Accumulate message data into memory stream
                this.memoryStream.Write(this._data, 0, bytesRead);
                this.totalBytesRead += bytesRead;
            }

            // If we've accumulated the full message, process it
            if (this.messageLength > 0 && this.totalBytesRead >= this.messageLength + 4)
            {
                this.ZoomClientConnection.ProcessMessage(this.memoryStream.ToArray(), this.messageLength,
                    this._isFirstMessage);
                this._isFirstMessage = false;

                //At this point the memory stream can contain more messages
                int remainingDataBytes = this.totalBytesRead - (this.messageLength + 4);


                // Reset properties for the next message
                this.messageLength = -1;
                this.totalBytesRead = 0;
                this.memoryStream.SetLength(0);

                if (remainingDataBytes > 0) //Handle the next message, if we have one
                {
                    byte[] newData = new byte[this._client.ReceiveBufferSize];
                    Array.Copy(this._data, bytesRead - remainingDataBytes, newData, 0, remainingDataBytes);
                    this._data = newData;
                    HandleReceivedMessage(remainingDataBytes);
                }
            }
        }

        public void Close()
        {
            this._client.Close();
        }
    }
}
