
// ZoomServer/ZoomClientConnection.cs - Modified
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections; // Consider replacing Hashtable with ConcurrentDictionary
using System.Collections.Concurrent; // For ConcurrentDictionary
using System.Collections.Generic;
using System.Diagnostics; // For Debug.WriteLine
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace ZoomServer
{
    public class ZoomClientConnection
    {
        // Use ConcurrentDictionary for thread safety
        public static ConcurrentDictionary<IPEndPoint, ZoomClientConnection> AllClients = new ConcurrentDictionary<IPEndPoint, ZoomClientConnection>();

        private IPEndPoint _clientIP;
        public IPEndPoint ClientIP => _clientIP; // Read-only property for client IP
        private DateTime _lastConnect;
        public CommandHandlerForSingleUser CommandHandlerForSingleUser { get; private set; } // Make setter private
        private TcpConnectionHandler _tcpConnectionHandler;

        // --- Instance variables for per-connection keys ---
        private RSAParameters _clientRsaPublicKey;
        private SymmetricKeyBundle _sessionAesKeyBundle;
        private bool _handshakeComplete = false; // Flag to track handshake status
                                                 // ----------------------------------------------------

        public ZoomClientConnection(TcpClient client)
        {
            this._clientIP = client.Client.RemoteEndPoint as IPEndPoint;
            Debug.WriteLine($"[ZoomClientConnection] Creating connection handler for {this._clientIP}");

            // DOS protection (simplified - consider more robust approach if needed)
            int count = 0;
            foreach (var entry in AllClients)
            {
                if (entry.Key.Address.Equals(this._clientIP.Address) &&
                    (DateTime.Now - entry.Value._lastConnect).TotalSeconds < 10)
                {
                    count++;
                }
            }
            if (count >= 10)
            {
                Debug.WriteLine($"[ZoomClientConnection] DOS Protection triggered for {this._clientIP}. Closing connection.");
                client.Close(); // Close the connection immediately
                                // Optionally throw or just return to prevent adding to collection
                throw new Exception("DOS Protection: Too many rapid connection attempts from this IP.");
            }

            this._lastConnect = DateTime.Now;

            // Add to collection *after* checks if possible, or handle removal on exception
            if (!AllClients.TryAdd(this._clientIP, this))
            {
                Debug.WriteLine($"[ZoomClientConnection] Failed to add client {this._clientIP} to collection (already exists?). Closing.");
                client.Close();
                return; // Exit constructor
            }


            this.CommandHandlerForSingleUser = new CommandHandlerForSingleUser(this); // Pass 'this'
                                                                                      // Pass 'this' (ZoomClientConnection) to TcpConnectionHandler
            this._tcpConnectionHandler = new TcpConnectionHandler(client, this);

            Debug.WriteLine($"[ZoomClientConnection] Starting listener for {this._clientIP}");
            this._tcpConnectionHandler.StartListen();
        }

        // --- Add Properties to access keys if needed by TcpConnectionHandler ---
        internal RSAParameters ClientRsaPublicKey => _clientRsaPublicKey;
        internal SymmetricKeyBundle SessionAesKeyBundle => _sessionAesKeyBundle;
        internal bool IsHandshakeComplete => _handshakeComplete;
        // --------------------------------------------------------------------

        public void SendMessage(string message)
        {
            // Only send if handshake is complete or if it's the AES key itself being sent
            if (_handshakeComplete || message.Contains("\"Key\":")) // Basic check for AES key json
            {
                this._tcpConnectionHandler.SendMessage(message);
            }
            else
            {
                Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Attempted to send message before handshake complete. Message: {message}");
            }
        }

        public void ProcessMessage(byte[] messageData, int bytesRead, bool isFirstMessage)
        {
            string commandRecive; // Declare outside blocks

            if (isFirstMessage)
            {
                commandRecive = Encoding.UTF8.GetString(messageData, 0, bytesRead);
                Debug.WriteLine($"[{this._clientIP}] Received first message (RSA Public Key JSON): {commandRecive}");
                try
                {
                    // Store the specific client's public key in the instance variable
                    this._clientRsaPublicKey = JsonConvert.DeserializeObject<RSAParameters>(commandRecive);
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Stored client RSA Public Key.");

                    // Generate a UNIQUE AES key bundle for THIS session
                    using (Aes aes = Aes.Create())
                    {
                        aes.GenerateKey();
                        aes.GenerateIV();
                        this._sessionAesKeyBundle = new SymmetricKeyBundle
                        {
                            Key = aes.Key,
                            Iv = aes.IV
                        };
                    }
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Generated session AES Key Bundle: Key={Convert.ToBase64String(_sessionAesKeyBundle.Key)}, IV={Convert.ToBase64String(_sessionAesKeyBundle.Iv)}");


                    // Serialize the UNIQUE session key bundle
                    var jsonString = JsonConvert.SerializeObject(this._sessionAesKeyBundle);
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Sending session AES Key Bundle (JSON): {jsonString}");

                    // Send the session key bundle back (will be encrypted by TcpConnectionHandler using _clientRsaPublicKey)
                    this.SendMessage(jsonString);
                    // Note: Handshake isn't fully complete until client confirms/uses key.
                    // We set _handshakeComplete after receiving the *next* message.


                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] *** RSA Public Key JSON Deserialization FAILED: {jsonEx.Message} ***");
                    Close(); // Close connection on bad handshake data
                }
                catch (CryptographicException cryptoEx)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] *** Cryptographic error during key generation/handling: {cryptoEx.Message} ***");
                    Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] *** Unexpected error processing first message: {ex.Message} ***");
                    Close();
                }
            }
            else // Not the first message, should be AES encrypted
            {
                // If AES key isn't set, handshake failed or is out of order.
                if (this._sessionAesKeyBundle == null)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] *** Received subsequent message but AES key bundle is null. Closing connection. ***");
                    Close();
                    return;
                }

                // Now we consider the handshake complete enough to process AES messages
                if (!_handshakeComplete)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Handshake considered complete after receiving first AES message.");
                    _handshakeComplete = true;
                }


                string encryptedCommand = Encoding.UTF8.GetString(messageData, 0, bytesRead); // Assume UTF8 for Base64 encoded string
                Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Received encrypted message: {encryptedCommand}");
                try
                {
                    // Decrypt using the INSTANCE's session key bundle
                    commandRecive = SymmetricEncryptionManager.DecodeText(encryptedCommand, this._sessionAesKeyBundle);
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Decrypted message: {commandRecive}");

                    // Protocol splitting logic (using \r\n as identified in client generate)
                    string[] stringSeparators = new string[] { "\r\n\r\n" };
                    string[] lines = commandRecive.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0)
                    {
                        Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Decrypted message was empty or contained only whitespace after splitting.");
                        return; // Or handle as error?
                    }

                    for (int i = 0; i < lines.Length; i++)
                    {
                        Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Handling command line: {lines[i]}");
                        // Run command handling on a background thread if it might block (e.g., DB access)
                        // Task.Run(() => this.CommandHandlerForSingleUser.HandleCommand(lines[i]));
                        // Or handle directly if handlers are quick/async internally
                        this.CommandHandlerForSingleUser.HandleCommand(lines[i]);
                    }
                }
                catch (FormatException formatEx) // Catch Base64 decoding errors
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] *** Invalid Base64 data received: {formatEx.Message} ***");
                    // Consider closing connection on invalid format
                    // Close();
                }
                catch (CryptographicException cryptoEx)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] *** AES DECRYPTION FAILED: {cryptoEx.Message} ***");
                    // This likely means wrong key or corrupted data. Close connection.
                    Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] *** Unexpected error processing subsequent message: {ex.Message} ***");
                    Close();
                }
            }
        }

        public void Close()
        {
            Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Closing connection.");
            if (AllClients.TryRemove(this._clientIP, out _))
            {
                Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Removed from client collection.");
            }
            else
            {
                Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] Could not remove from client collection (already removed?).");
            }
            // Ensure handler is closed
            this._tcpConnectionHandler?.Close(); // Use null-conditional operator
                                                 // Release resources if needed
            this._sessionAesKeyBundle = null; // Allow GC
        }

        public void HandleDisconnect()
        {
            // This might be called by TcpConnectionHandler on socket error
            Debug.WriteLine($"[ZoomClientConnection] [{this._clientIP}] HandleDisconnect called.");
            Close(); // Perform cleanup
        }

        // --- Static Methods ---
        // These might need rethinking if they rely on specific connection state not accessible statically.
        // For now, assuming they work by iterating the static collection.

        public static void SendMessageToAllUserExceptOne(int userIdToExclude, ClientServerProtocol protocol)
        {
            string messageToSend = protocol.Generate();
            foreach (var userConnection in AllClients.Values)
            {
                // Check if the user is authenticated and not the excluded one
                // Also check if their handshake is complete before sending AES encrypted messages
                if (userConnection.CommandHandlerForSingleUser?._userId > 0 &&
                    userConnection.CommandHandlerForSingleUser._userId != userIdToExclude &&
                    userConnection.IsHandshakeComplete) // Check handshake
                {
                    Debug.WriteLine($"[ZoomClientConnection] [Broadcast] Sending message type {protocol.TypeOfCommand} to User {userConnection.CommandHandlerForSingleUser._userId} ({userConnection._clientIP})");
                    userConnection.SendMessage(messageToSend);
                }
            }
        }

        public static void SendMessageToSpecificUser(int userId, ClientServerProtocol protocol)
        {
            string messageToSend = protocol.Generate();
            foreach (var userConnection in AllClients.Values)
            {
                if (userConnection.CommandHandlerForSingleUser?._userId == userId && userConnection.IsHandshakeComplete)
                {
                    Debug.WriteLine($"[ZoomClientConnection] [Unicast] Sending message type {protocol.TypeOfCommand} to User {userId} ({userConnection._clientIP})");
                    userConnection.SendMessage(messageToSend);
                    return; // Found the user
                }
            }
            Debug.WriteLine($"[ZoomClientConnection] [Unicast] User {userId} not found or handshake not complete for message type {protocol.TypeOfCommand}.");
        }
        // Other static methods remain similar, iterating over AllClients...
        public static string GetUserIpById(int userId)
        {
            foreach (ZoomClientConnection user in AllClients.Values)
            {
                // Ensure CommandHandler isn't null before accessing _userId
                if (user.CommandHandlerForSingleUser != null && user.CommandHandlerForSingleUser._userId == userId)
                {
                    return user._clientIP.Address.ToString();
                }
            }
            return null;
        }
        public static List<int> GetIdsOfAllConnectedUsers()
        {
            List<int> ids = new List<int>();
            foreach (ZoomClientConnection user in AllClients.Values)
            {
                // Only add users who have an ID (implies they are logged in/registered)
                if (user.CommandHandlerForSingleUser != null && user.CommandHandlerForSingleUser._userId > 0)
                {
                    ids.Add(user.CommandHandlerForSingleUser._userId);
                }
            }
            return ids;
        }


    }
}
