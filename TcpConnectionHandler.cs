// ZoomServer/TcpConnectionHandler.cs - Modified
using NLog;
using System;
using System.Diagnostics; // For Debug.WriteLine
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography; // For CryptographicException
using System.Text;

namespace ZoomServer
{
    public class TcpConnectionHandler
    {
        private int messageLength = -1;
        private int totalBytesRead = 0;
        // REMOVED: private bool _isFirstMessage = true; // State now managed by ZoomClientConnection
        private byte[] _data;
        private MemoryStream memoryStream = new MemoryStream();
        private TcpClient _client;
        // Store the owner connection
        private ZoomClientConnection _ownerConnection; // Renamed for clarity

        public TcpConnectionHandler(TcpClient tcpClient, ZoomClientConnection ownerConnection) // Renamed parameter
        {
            this._client = tcpClient;
            this._ownerConnection = ownerConnection; // Store the owner
            this._data = new byte[this._client.ReceiveBufferSize];
        }

        public void StartListen()
        {
            try
            {
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Starting async read."); // Use owner's IP for logging
                _client.GetStream().BeginRead(this._data, 0, System.Convert.ToInt32(this._client.ReceiveBufferSize),
                                              ReceiveMessage, _client.GetStream());
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Error starting listen (connection likely closed): {ex.Message}");
                this._ownerConnection.HandleDisconnect(); // Ensure cleanup if start fails
            }
        }

        public void SendMessage(string message)
        {
            // Determine if this is the AES key bundle being sent (first logical message from server)
            // A more robust check might be needed if other JSON messages are sent unencrypted early.
            bool isSendingAesKey = !_ownerConnection.IsHandshakeComplete && message.Contains("\"Key\":");

            try
            {
                NetworkStream ns = _client.GetStream(); // Get stream inside try
                string encryptedMessage;

                if (isSendingAesKey)
                {
                    // Encrypt the AES key bundle using the specific client's RSA public key
                    Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Sending AES Key (Plaintext JSON): {message}");
                    encryptedMessage = RsaFunctions.Encrypt(message, _ownerConnection.ClientRsaPublicKey);
                    Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Sending AES Key (RSA Encrypted): {encryptedMessage}");
                }
                else // Subsequent messages use AES
                {
                    // Ensure handshake is complete and AES key exists before encrypting
                    if (!_ownerConnection.IsHandshakeComplete || _ownerConnection.SessionAesKeyBundle == null)
                    {
                        Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Cannot send message - handshake not complete or AES key missing.");
                        return;
                    }
                    Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Sending message (Plaintext): {message}");
                    // Encrypt using the session-specific AES key
                    encryptedMessage = SymmetricEncryptionManager.EncodeText(message, _ownerConnection.SessionAesKeyBundle);
                    Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Sending message (AES Encrypted): {encryptedMessage}");
                }

                byte[] data = Encoding.UTF8.GetBytes(encryptedMessage);
                byte[] length = BitConverter.GetBytes(data.Length);
                byte[] bytesToSend = new byte[data.Length + 4];

                Buffer.BlockCopy(length, 0, bytesToSend, 0, length.Length);
                Buffer.BlockCopy(data, 0, bytesToSend, length.Length, data.Length);

                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Writing {bytesToSend.Length} bytes to network stream.");
                // Consider using BeginWrite/EndWrite for async sending if this becomes a bottleneck
                ns.Write(bytesToSend, 0, bytesToSend.Length);
                ns.Flush();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is ObjectDisposedException)
            {
                // Catch common network errors during send
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Network error sending message: {ex.Message}");
                this._ownerConnection.HandleDisconnect(); // Trigger cleanup
            }
            catch (CryptographicException cryptoEx)
            {
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Encryption error sending message: {cryptoEx.Message}");
                // Decide how to handle - maybe close connection?
                this._ownerConnection.HandleDisconnect();
            }
            catch (Exception ex)
            {
                // Catch unexpected errors
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Unexpected error sending message: {ex.Message}");
                this._ownerConnection.HandleDisconnect();
            }
        }

        private void ReceiveMessage(IAsyncResult ar)
        {
            NetworkStream stream;
            IPEndPoint clientIP = _ownerConnection.ClientIP;
            string clientIPString = clientIP != null ? clientIP.ToString() : "Unknown";
            try
            {
                stream = (NetworkStream)ar.AsyncState;

                if (!_client.Connected || stream == null)
                {
                    Debug.WriteLine($"[TcpConnectionHandler] [{clientIPString}] ReceiveMessage callback invoked but client not connected/stream null.");
                    _ownerConnection?.HandleDisconnect();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TcpConnectionHandler] [{clientIPString}] Error getting NetworkStream in ReceiveMessage: {ex.Message}");
                _ownerConnection?.HandleDisconnect();
                return;
            }


            try
            {
                int bytesRead = stream.EndRead(ar);
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Read {bytesRead} bytes from network stream.");

                if (bytesRead > 0)
                {
                    HandleReceivedMessage(bytesRead);

                    // Continue reading ONLY if the client is still connected
                    if (_client.Connected)
                    {
                        stream.BeginRead(this._data, 0, this._client.ReceiveBufferSize,
                                         ReceiveMessage, stream);
                    }
                    else
                    {
                        Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Client disconnected during read cycle.");
                        _ownerConnection.HandleDisconnect();
                    }
                }
                else // bytesRead == 0 means graceful disconnect
                {
                    Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Client disconnected gracefully (0 bytes read).");
                    _ownerConnection.HandleDisconnect();
                    this.Close(); // Close handler resources
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                // Common network/stream errors during read
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Network/Stream error receiving message: {ex.Message}");
                _ownerConnection.HandleDisconnect();
                this.Close();
            }
            catch (Exception ex)
            {
                // Unexpected errors during read
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Unexpected error receiving message: {ex.Message}");
                _ownerConnection.HandleDisconnect();
                this.Close();
            }
        }

        private void HandleReceivedMessage(int bytesRead)
        {
            // Pass processing logic directly to ZoomClientConnection instance
            // The framing logic needs to handle partial reads correctly.
            // This implementation assumes framing logic is sound, but needs careful testing.

            try
            {
                // Append received data to the memory stream
                memoryStream.Write(this._data, 0, bytesRead);

                // Process messages as long as enough data is available
                while (true)
                {
                    if (messageLength == -1) // Need to read length prefix
                    {
                        if (memoryStream.Length >= 4)
                        {
                            memoryStream.Position = 0;
                            byte[] lengthBytes = new byte[4];
                            memoryStream.Read(lengthBytes, 0, 4);
                            messageLength = BitConverter.ToInt32(lengthBytes, 0);

                            // Basic sanity check for message length
                            if (messageLength < 0 || messageLength > 10 * 1024 * 1024) // e.g., 10MB limit
                            {
                                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Invalid message length received: {messageLength}. Closing connection.");
                                Close();
                                return;
                            }


                            Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Expecting message length: {messageLength}");

                            // Remove length prefix from stream buffer
                            byte[] remainingData = new byte[memoryStream.Length - 4];
                            memoryStream.Read(remainingData, 0, remainingData.Length);
                            memoryStream.SetLength(0);
                            memoryStream.Write(remainingData, 0, remainingData.Length);
                        }
                        else
                        {
                            // Not enough data for length prefix yet
                            break;
                        }
                    }

                    // Check if we have the complete message body
                    if (messageLength != -1 && memoryStream.Length >= messageLength)
                    {
                        byte[] messageBytes = new byte[messageLength];
                        memoryStream.Position = 0;
                        memoryStream.Read(messageBytes, 0, messageLength);

                        Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Full message received ({messageLength} bytes). Processing...");
                        // Determine if it's the first message based on handshake state
                        bool isFirstLogicalMessage = !_ownerConnection.IsHandshakeComplete && _ownerConnection.SessionAesKeyBundle == null;
                        _ownerConnection.ProcessMessage(messageBytes, messageLength, isFirstLogicalMessage);


                        // Remove processed message from stream buffer
                        byte[] remainingData = new byte[memoryStream.Length - messageLength];
                        memoryStream.Position = messageLength; // Set position after the read message
                        memoryStream.Read(remainingData, 0, remainingData.Length);
                        memoryStream.SetLength(0); // Clear the stream
                        memoryStream.Write(remainingData, 0, remainingData.Length); // Write back remaining data


                        // Reset for next message
                        messageLength = -1;

                        // Check if owner closed the connection during processing
                        if (!_client.Connected) break;
                    }
                    else
                    {
                        // Not enough data for message body yet
                        break;
                    }
                }
                // Set position back to the end for future writes
                memoryStream.Position = memoryStream.Length;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TcpConnectionHandler] [{this._ownerConnection.ClientIP}] Error handling received message buffer: {ex.Message}");
                _ownerConnection.HandleDisconnect();
                Close();
            }
        }


        public void Close()
        {
            string clientIPString = _ownerConnection?.ClientIP != null ? _ownerConnection.ClientIP.ToString() : "Unknown";
            try
            {
                Debug.WriteLine($"[TcpConnectionHandler] [{clientIPString}] Closing TcpConnectionHandler.");
                if (_client?.Connected ?? false) // Check if client exists and is connected
                {
                    _client.Close();
                }
                memoryStream?.Dispose(); // Dispose the memory stream
            }
            catch (Exception ex)
            {
                // Log errors during close, but don't let them prevent cleanup
                Debug.WriteLine($"[TcpConnectionHandler] [{clientIPString}] Error during TcpConnectionHandler close: {ex.Message}");
            }
        }
    }
}