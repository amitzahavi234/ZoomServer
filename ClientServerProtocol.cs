using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoomServer
{


    public class ClientServerProtocol
    {
        public TypeOfCommandenum TypeOfCommand { get; set; }

        /// <summary>
        /// The username of this user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password of this user
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The first name of this user
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The last name of this user
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// The email of this user
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The city of this user
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The gender of this user
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// The message that will be showed to the user
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The code that sent to the user mail
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The profile picture of this user converts to byte's array
        /// </summary>
        public byte[] ProfilePicture { get; set; }

        /// <summary>
        /// The number of minutes that the user need to wait
        /// </summary>
        public int TimeToCooldown { get; set; }

        public string MessageThatTheUserSent { get; set; }

        public DateTime TimeThatTheMessageWasSent { get; set; }

        public int ChatRoomId { get; set; }

        public int MediaRoomId { get; set; }

        public int UserId { get; set; }

        public string MessagesOfAChatRoomJson { get; set; }

        public string NewParticipantIp { get; set; }

        public string AllTheConnectedUsersInSomeMediaRoomIpsJson { get; set; }

        public int MediaPort { get; set; }


        public string UserIp { get; set; }

        public string AllUsersDetails { get; set; }


        /// <summary>
        /// Empty constructor
        /// </summary>
        public ClientServerProtocol()
        {

        }



        /// <summary>
        /// Constructor with parameter. This constructor generates the ClientServerProtocol instance from the string we're sending over TCP
        /// </summary>
        /// <param name="message"></param>
        public ClientServerProtocol(string message)
        {
            string[] answer = message.Split('\n');
            Enum.TryParse(answer[0], out TypeOfCommandenum cmd);
            this.TypeOfCommand = cmd;

            switch (cmd)
            {
                case TypeOfCommandenum.Error_Command:
                    this.ErrorMessage = answer[1];
                    break;

                case TypeOfCommandenum.Login_Cooldown_Command:
                    this.ErrorMessage = answer[1];
                    this.TimeToCooldown = Convert.ToInt32(answer[2]);
                    break;

                case TypeOfCommandenum.Login_Command:
                    this.Username = answer[1];
                    this.Password = answer[2];
                    break;

                case TypeOfCommandenum.Registration_Command:
                    this.Username = answer[1];
                    this.Password = answer[2];
                    this.FirstName = answer[3];
                    this.LastName = answer[4];
                    this.Email = answer[5];
                    this.City = answer[6];
                    this.Gender = answer[7];
                    this.ProfilePicture = Convert.FromBase64String(answer[8]);
                    break;

                case TypeOfCommandenum.Check_If_Username_Already_Exist_Command:
                    this.Username = answer[1];
                    break;

                case TypeOfCommandenum.Forgot_Password_Command:
                    this.Username = answer[1];
                    this.Code = answer[2];
                    break;

                case TypeOfCommandenum.Update_Password_Command:
                    this.Username = answer[1];
                    this.Password = answer[2];
                    break;

                case TypeOfCommandenum.Code_Sent_To_Email_Command:
                    this.Code = answer[1];
                    break;

                case TypeOfCommandenum.Get_Username_And_Profile_Picture_Command:
                case TypeOfCommandenum.Success_Username_Not_In_The_System_Command:
                case TypeOfCommandenum.Success_Forgot_Password_Command:
                    break;

                case TypeOfCommandenum.Success_Connected_To_The_Application_Command:
                    this.ProfilePicture = Convert.FromBase64String(answer[1]);
                    this.Username = answer[2];
                    this.UserId = Convert.ToInt32(answer[3]);
                    break;

                case TypeOfCommandenum.Send_Message_Command:
                    this.MessageThatTheUserSent = answer[1];
                    this.ChatRoomId = Convert.ToInt32(answer[2]);
                    break;

                case TypeOfCommandenum.Message_From_Other_User_Command:
                    this.Username = answer[1];
                    this.UserId = Convert.ToInt32(answer[2]);
                    this.MessageThatTheUserSent = answer[3];
                    this.TimeThatTheMessageWasSent = DateTime.Parse(answer[4]);
                    this.ChatRoomId = Convert.ToInt32(answer[5]);
                    break;

                case TypeOfCommandenum.Fetch_Image_Of_User_Command:
                    this.UserId = Convert.ToInt32(answer[1]);
                    break;

                case TypeOfCommandenum.Return_Image_Of_User_Command:
                    this.UserId = Convert.ToInt32(answer[1]);
                    this.ProfilePicture = Convert.FromBase64String(answer[2]);
                    break;

                case TypeOfCommandenum.Get_Messages_History_Of_Chat_Room_Command:
                    this.ChatRoomId = Convert.ToInt32(answer[1]);
                    break;

                case TypeOfCommandenum.Return_Messages_History_Of_Chat_Room_Command:
                    this.MessagesOfAChatRoomJson = answer[1];
                    break;

                case TypeOfCommandenum.Connect_To_Media_Room_Command:
                    this.MediaRoomId = Convert.ToInt32(answer[1]);
                    this.MediaPort = Convert.ToInt32(answer[2]);
                    break;

                case TypeOfCommandenum.New_Participant_Join_The_Media_Room_Command:
                    this.NewParticipantIp = answer[1];
                    this.MediaPort = Convert.ToInt32(answer[2]);
                    this.UserId = Convert.ToInt32(answer[3]);
                    this.Username = answer[4];
                    break;

                case TypeOfCommandenum.Get_All_Ips_Of_Connected_Users_In_Some_Media_Room_Command:
                    this.AllTheConnectedUsersInSomeMediaRoomIpsJson = answer[1];
                    break;

                case TypeOfCommandenum.Disconnect_From_Media_Room_Command:
                    this.MediaRoomId = Convert.ToInt32(answer[1]);
                    break;

                case TypeOfCommandenum.Some_User_Left_The_Media_Room_Command:
                    this.UserIp = answer[1];
                    break;

             


            }
        }

        /// <summary>
        /// The function generates the string of the protocol that we're sending over TCP
        /// from the fields of the class, based on the command
        /// </summary>
        /// <returns></returns>
        public string Generate()
        {
            string toSend = this.TypeOfCommand.ToString() + "\n";

            switch (this.TypeOfCommand)
            {
                case TypeOfCommandenum.Error_Command:
                    toSend += this.ErrorMessage + "\n";
                    break;

                case TypeOfCommandenum.Login_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Password + "\n";
                    break;

                case TypeOfCommandenum.Login_Cooldown_Command:
                    toSend += this.ErrorMessage + "\n";
                    toSend += this.TimeToCooldown.ToString() + "\n";
                    break;

                case TypeOfCommandenum.Registration_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Password + "\n";
                    toSend += this.FirstName + "\n";
                    toSend += this.LastName + "\n";
                    toSend += this.Email + "\n";
                    toSend += this.City + "\n";
                    toSend += this.Gender + "\n";
                    toSend += Convert.ToBase64String(this.ProfilePicture) + "\n";
                    break;

                case TypeOfCommandenum.Check_If_Username_Already_Exist_Command:
                    toSend += this.Username + "\n";
                    break;

                case TypeOfCommandenum.Forgot_Password_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Code + "\n";
                    break;

                case TypeOfCommandenum.Update_Password_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Password + "\n";
                    break;

                case TypeOfCommandenum.Code_Sent_To_Email_Command:
                    toSend += this.Code + "\n";
                    break;


                case TypeOfCommandenum.Success_Username_Not_In_The_System_Command:
                case TypeOfCommandenum.Success_Forgot_Password_Command:
                    break;

                case TypeOfCommandenum.Success_Connected_To_The_Application_Command:
                    toSend += Convert.ToBase64String(this.ProfilePicture) + "\n";
                    toSend += this.Username + "\n";
                    toSend += this.UserId.ToString() + "\n";
                    break;

                case TypeOfCommandenum.Send_Message_Command:
                    toSend += this.MessageThatTheUserSent + "\n";
                    toSend += this.ChatRoomId.ToString() + "\n";
                    break;

                case TypeOfCommandenum.Message_From_Other_User_Command:
                    toSend += this.Username + "\n";
                    toSend += this.UserId.ToString() + "\n";
                    toSend += this.MessageThatTheUserSent + "\n";
                    toSend += this.TimeThatTheMessageWasSent.ToString() + "\n";
                    toSend += this.ChatRoomId.ToString() + "\n";
                    break;

                case TypeOfCommandenum.Fetch_Image_Of_User_Command:
                    toSend += this.UserId.ToString() + "\n";
                    break;

                case TypeOfCommandenum.Return_Image_Of_User_Command:
                    toSend += this.UserId.ToString() + "\n";
                    toSend += Convert.ToBase64String(this.ProfilePicture) + "\n";
                    break;

                case TypeOfCommandenum.Get_Messages_History_Of_Chat_Room_Command:
                    toSend += this.ChatRoomId.ToString() + "\n";
                    break;

                case TypeOfCommandenum.Return_Messages_History_Of_Chat_Room_Command:
                    toSend += this.MessagesOfAChatRoomJson + "\n";
                    break;

                case TypeOfCommandenum.Connect_To_Media_Room_Command:
                    toSend += this.MediaRoomId.ToString() + "\n";
                    toSend += this.MediaPort.ToString() + "\n";
                    break;

                case TypeOfCommandenum.New_Participant_Join_The_Media_Room_Command:
                    toSend += this.NewParticipantIp + "\n";
                    toSend += this.MediaPort.ToString() + "\n";
                    toSend += this.UserId.ToString() + "\n";
                    toSend += this.Username + "\n";
                    break;

                case TypeOfCommandenum.Get_All_Ips_Of_Connected_Users_In_Some_Media_Room_Command:
                    toSend += this.AllTheConnectedUsersInSomeMediaRoomIpsJson + "\n";
                    break;

                case TypeOfCommandenum.Disconnect_From_Media_Room_Command:
                    toSend += this.MediaRoomId.ToString() + "\n";
                    break;

                case TypeOfCommandenum.Some_User_Left_The_Media_Room_Command:
                    toSend += this.UserIp + "\n";
                    break;

           

            }
            toSend += "\r\n";
            return toSend;
        }
    }
}
