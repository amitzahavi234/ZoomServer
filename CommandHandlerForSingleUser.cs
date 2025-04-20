using Microsoft.SqlServer.Server;
using NLog;
using SendGrid.Helpers.Mail;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ZoomServer
{
    public class CommandHandlerForSingleUser
    {
        /// <summary>
        /// The ID of the user.
        /// </summary>
        public int _userId;

        /// <summary>
        /// The count of consecutive failed login attempts by the user.
        /// </summary>
        private int _countLoginFailures;


        private string _username;

        private byte[] _profilePicture;

        /// <summary>
        /// The maximum number of allowed failed login attempts before applying a cooldown.
        /// </summary>
        private const int MAX_NUMBER_OF_LOGIN_FAILED_ATTEMPTS = 10;

        /// <summary>
        /// Logger instance for user-specific logging.
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Instance to handle database operations for user-related commands.
        /// </summary>
        private Sqlconnect _sqlConnect;

        /// <summary>
        /// The connection associated with the Discord client.
        /// </summary>
        private ZoomClientConnection _connection;

        /// <summary>
        /// Constructor with parameter
        /// </summary>
        /// <param name="connection"></param>
        public CommandHandlerForSingleUser(ZoomClientConnection connection)
        {
            this._countLoginFailures = 0;
            this._sqlConnect = new Sqlconnect();
            this._connection = connection;
        }

        /// <summary>
        /// Handles the incoming command from the client and routes it to the appropriate handler.
        /// </summary>
        /// <param name="messageFromClient"></param>
        /// <param name="messageFromClient"></param>
        public void HandleCommand(string messageFromClient)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol(messageFromClient);
            switch (clientServerProtocol.TypeOfCommand)
            {
                case TypeOfCommandenum.Login_Command:
                    this.HandleLogin(clientServerProtocol.Username, clientServerProtocol.Password);
                    break;
                case TypeOfCommandenum.Registration_Command:
                    this.HandleRegistration(clientServerProtocol.Username, clientServerProtocol.Password, clientServerProtocol.FirstName,
                        clientServerProtocol.LastName, clientServerProtocol.Email, clientServerProtocol.City, clientServerProtocol.Gender,
                        clientServerProtocol.ProfilePicture);
                    break;
                case TypeOfCommandenum.Check_If_Username_Already_Exist_Command:
                    this.HandleCheckIfUsernameAlreadyExistCommand(clientServerProtocol.Username);
                    break;
                case TypeOfCommandenum.Forgot_Password_Command:
                    this.HandleForgotPassword(clientServerProtocol.Username, clientServerProtocol.Code);
                    break;
                case TypeOfCommandenum.Update_Password_Command:
                    this.HandleUpdatePassword(clientServerProtocol.Username, clientServerProtocol.Password);
                    break;

                case TypeOfCommandenum.Get_Username_And_Profile_Picture_Command:
                    this.HandleGetUsernameAndProfilePicture();
                    break;

                case TypeOfCommandenum.Send_Message_Command:
                    this.HandleSendMessage(clientServerProtocol.MessageThatTheUserSent, clientServerProtocol.ChatRoomId);
                    break;

               

                case TypeOfCommandenum.Get_Messages_History_Of_Chat_Room_Command:
                    this.HandleGetMessagesHistoryOfChatRoom(clientServerProtocol.ChatRoomId);
                    break;
                







            }
        }












        /// <summary>
        /// Handles the login process, including validation and cooldowns for failed attempts.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
      private void HandleLogin(string username, string password)
    {
    string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
    this._userId = this._sqlConnect.GetUserId(username, hashPassword);

    if (this._userId <= 0)
    {
        this._countLoginFailures++;
        ClientServerProtocol protocol = new ClientServerProtocol();

        if (this._countLoginFailures >= MAX_NUMBER_OF_LOGIN_FAILED_ATTEMPTS)
        {
            protocol.TypeOfCommand = TypeOfCommandenum.Login_Cooldown_Command;
            protocol.TimeToCooldown = this.CalculateTimeToCooldown(this._countLoginFailures);
            protocol.ErrorMessage = $"Too many failed attempts to login, please wait {protocol.TimeToCooldown} minutes";
        }
        else
        {
            protocol.TypeOfCommand = TypeOfCommandenum.Error_Command;
            protocol.ErrorMessage = "Wrong username or password";
        }

        this._connection.SendMessage(protocol.Generate());
        return;
    }

    this._countLoginFailures = 0;
    this._username = username;

    // שליחת הקוד למייל
    string email = this._sqlConnect.GetEmail(username);
    string codeToEmail = this.GetRandomCode();
    Execute(email, codeToEmail).Wait();

    ClientServerProtocol codeProtocol = new ClientServerProtocol
    {
        TypeOfCommand = TypeOfCommandenum.Code_Sent_To_Email_Command,
        Code = codeToEmail
    };
    this._connection.SendMessage(codeProtocol.Generate());

    // המתנה קלה כדי לוודא סדר בתקשורת TCP
    // שליחת פרטי המשתמש (הודעת Success)
    this._profilePicture = this._sqlConnect.GetProfilePictureByUsername(username);

    ClientServerProtocol successProtocol = new ClientServerProtocol
    {
        TypeOfCommand = TypeOfCommandenum.Success_Connected_To_The_Application_Command,
        ProfilePicture = this._profilePicture,
        Username = username,
        UserId = this._userId
    };

    Console.WriteLine("==== Sending Success_Connected_To_The_Application_Command ====");
    Console.WriteLine("Username: " + successProtocol.Username);
    Console.WriteLine("UserId: " + successProtocol.UserId);
    Console.WriteLine("ProfilePicture is null? " + (successProtocol.ProfilePicture == null));
    Console.WriteLine("ProfilePicture length: " + (successProtocol.ProfilePicture?.Length ?? 0));
            // המרת התמונה ל־Base64 בלי שבירת שורות
            string base64Picture = Convert.ToBase64String(successProtocol.ProfilePicture, Base64FormattingOptions.None);

            // בניית ההודעה בצורה תקינה – כל שדה בשורה נפרדת, בסוף סיום עם שורת רווח
            string messageToSend = new StringBuilder()
                .AppendLine(successProtocol.TypeOfCommand.ToString())   // שורה 1: סוג הפקודה
                .AppendLine(base64Picture)                              // שורה 2: תמונה (רציפה)
                .AppendLine(successProtocol.Username)                   // שורה 3: שם משתמש
                .AppendLine(successProtocol.UserId.ToString())          // שורה 4: מזהה משתמש
                .AppendLine()                                            // שורה 5: \r\n בסוף
                .ToString();

            this._connection.SendMessage(messageToSend);
            

            // יצירת לוג
            this._logger = UserLogger.GetLoggerForUser(username);
    this._logger.Info("Successfully logged in");
}


        /// <summary>
        /// Calculates the cooldown time based on the number of failed login attempts.
        /// </summary>
        /// <param name="countLoginFailures"></param>
        private int CalculateTimeToCooldown(int countLoginFailures)
        {
            if (countLoginFailures == 10)
            {
                return 1;
            }
            return ((countLoginFailures - MAX_NUMBER_OF_LOGIN_FAILED_ATTEMPTS) * 5);
        }

        /// <summary>
        /// Handles checking if a username already exists in the system.
        /// </summary>
        /// <param name="username"></param>
        private void HandleCheckIfUsernameAlreadyExistCommand(string username)
        {
            ClientServerProtocol protocol = new ClientServerProtocol();
            if (this._sqlConnect.IsExist(username))
            {
                protocol.TypeOfCommand = TypeOfCommandenum.Error_Command;
                protocol.ErrorMessage = "Username already exists";
                this._connection.SendMessage(protocol.Generate());
                return;
            }
            protocol.TypeOfCommand = TypeOfCommandenum.Success_Username_Not_In_The_System_Command;
            this._connection.SendMessage(protocol.Generate());
        }

        /// <summary>
        /// Handles the user registration process.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="email"></param>
        /// <param name="city"></param>
        /// <param name="gender"></param>
        /// <param name="imageToByteArray"></param>
        private void HandleRegistration(string username, string password, string firstName, string lastName, string email,
             string city, string gender, byte[] imageToByteArray)
        {
            if (this._sqlConnect.IsExist(username))
            {
                ClientServerProtocol protocol = new ClientServerProtocol();
                protocol.TypeOfCommand = TypeOfCommandenum.Error_Command;
                protocol.ErrorMessage = "Username already exists";
                this._connection.SendMessage(protocol.Generate());
                return;
            }
            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._userId = this._sqlConnect.InsertNewUser(username, hashPassword, firstName, lastName, email, city, gender, imageToByteArray);
            this._username = username;
            this._profilePicture = imageToByteArray;
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommandenum.Success_Connected_To_The_Application_Command;
            clientServerProtocol.ProfilePicture = imageToByteArray;
            clientServerProtocol.Username = username;
            clientServerProtocol.UserId = this._userId;
            this._connection.SendMessage(clientServerProtocol.Generate());
            this._logger = UserLogger.GetLoggerForUser(username);
            this._logger.Info("Successfully registered");
        }

        /// <summary>
        /// Hashes a string using SHA256.
        /// </summary>
        /// <param name="value"></param>
        private static string CreateSha256(string value)
        {
            StringBuilder Sb = new StringBuilder();

            using (var hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

        /// <summary>
        /// Generates a random code consisting of letters, digits, and special characters.
        /// </summary>
        private string GetRandomCode()
        {
            var charsALL = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz#?!@$%^&*-";
            var randomIns = new Random();
            var rndChars = Enumerable.Range(0, 6)
                            .Select(_ => charsALL[randomIns.Next(charsALL.Length)])
                            .ToArray();
            return new string(rndChars);
        }

        static async Task Execute(string email, string code)
        {

            try
            {
                email = email?.Trim();

                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    Console.WriteLine($"[SendEmail] Invalid email: '{email}'");
                    return;
                }
                var apiKey = "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";
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("zoomnotifications41@gmail.com", "Zoomnotifications");
                var subject = "Sending with SendGrid is Fun";
                var to = new EmailAddress(email);
                var plainTextContent = $"Your code is: {code}";
                var htmlContent = $"Your code is: {code}";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);
            }


            catch (FormatException ex)
            {
                Console.WriteLine($"[SendEmail] Format error: {ex.Message}");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"[SendEmail] SMTP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendEmail] General error: {ex.Message}");
            }
        }

        
        


        /*private void SendEmail(string email, string code)
        {
            try
            {
                email = email?.Trim();

                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    Console.WriteLine($"[SendEmail] Invalid email: '{email}'");
                    return;
                }

                SmtpClient smtpClient = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential("zoomnotifications41@gmail.com", "jpgv tgvd cick clqq")
                };

                MailMessage msg = new MailMessage("zoomnotifications41@gmail.com", email,
                    "Code For Zoom", $"Your code is: {code}");

                smtpClient.SendMailAsync(msg).Wait(); // אפשר גם await אם הפונקציה async
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"[SendEmail] Format error: {ex.Message}");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"[SendEmail] SMTP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendEmail] General error: {ex.Message}");
            }
        }*/

        /// <summary>
        /// Handles the forgot password process, including sending a code to the user's email.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="code"></param>
        private void HandleForgotPassword(string username, string code)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            if (!this._sqlConnect.IsExist(username))
            {
                clientServerProtocol.TypeOfCommand = TypeOfCommandenum.Error_Command;
                clientServerProtocol.ErrorMessage = "The username isn't exist in the system, please check the username that you entered";
            }
            else
            {
                this._username = username;
                string email = this._sqlConnect.GetEmail(username);
                Execute(email, code).Wait();
                this._logger = UserLogger.GetLoggerForUser(username);
                this._logger.Info("Forgot password email sent");
                clientServerProtocol.TypeOfCommand = TypeOfCommandenum.Success_Forgot_Password_Command;
            }
            this._connection.SendMessage(clientServerProtocol.Generate());


        }

        /// <summary>
        /// Sends an email with the provided code to the recipient.
        /// </summary>
        /// <param name="recipientEmail"></param>
        /// <param name="code"></param>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the password for the user.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        private void HandleUpdatePassword(string username, string password)
        {
            if (!this._sqlConnect.IsExist(username))
            {
                return;
            }
            string hashNewPassword = CommandHandlerForSingleUser.CreateSha256(password);
            string currentPassword = this._sqlConnect.GetPassword(username);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            if (currentPassword == hashNewPassword)
            {
                clientServerProtocol.TypeOfCommand = TypeOfCommandenum.Error_Command;
                clientServerProtocol.ErrorMessage = "the password that you entered is your past password, " +
                    "please enter different password or just login with this password";
            }
            else
            {
                this._sqlConnect.UpdatePassword(username, hashNewPassword);
                this._logger = UserLogger.GetLoggerForUser(username);
                this._logger.Info("Password updated successfully");
                clientServerProtocol.TypeOfCommand = TypeOfCommandenum.Success_Connected_To_The_Application_Command;
                this._profilePicture = this._sqlConnect.GetProfilePictureByUsername(username);
                clientServerProtocol.ProfilePicture = this._profilePicture;
                clientServerProtocol.Username = username;
            }
            this._connection.SendMessage(clientServerProtocol.Generate());

        }

        private void HandleGetUsernameAndProfilePicture()
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommandenum.Success_Connected_To_The_Application_Command;
            this._profilePicture = this._sqlConnect.GetProfilePictureByUsername(this._username);
            clientServerProtocol.ProfilePicture = this._profilePicture;
            clientServerProtocol.Username = this._username;
            this._connection.SendMessage(clientServerProtocol.Generate());
        }

        private void HandleSendMessage(string messageThatTheUserSent, int chatRoomId)
        {
            RoomsManager.SendMessageToOtherUsers(this._userId, this._username, messageThatTheUserSent,
                chatRoomId);
        }

        private void HandleFetchImageOfUser(int userId, string username, string messageThatTheUserSent, DateTime time, int chatRoomId)
        {
            byte[] someUserProfilePicture = this._sqlConnect.GetProfilePictureByUserId(userId);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommandenum.Return_Image_Of_User_Command;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.ProfilePicture = someUserProfilePicture;
            clientServerProtocol.Username = username;
            clientServerProtocol.MessageThatTheUserSent = messageThatTheUserSent;
            clientServerProtocol.TimeThatTheMessageWasSent = time;
            clientServerProtocol.ChatRoomId = chatRoomId;
            this._connection.SendMessage(clientServerProtocol.Generate());
        }

        private void HandleGetMessagesHistoryOfChatRoom(int chatRoomId)
        {
            RoomsManager.GetChatRoomHistory(this._userId, chatRoomId);
        }

       




        
    }
}
