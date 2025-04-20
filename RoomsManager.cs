using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoomServer
{
    public class RoomsManager
    {
            public static List<MediaRoom> MediaRooms = new List<MediaRoom>
        {
            new MediaRoom(1),
            new MediaRoom(2),
            new MediaRoom(3)
        };

            public static void SendMessageToOtherUsers(int userId, string username, string message, int chatRoomId)
            {
                var protocol = new ClientServerProtocol
                {
                    TypeOfCommand = TypeOfCommandenum.Message_From_Other_User_Command,
                    Username = username,
                    UserId = userId,
                    MessageThatTheUserSent = message,
                    TimeThatTheMessageWasSent = DateTime.UtcNow,
                    ChatRoomId = chatRoomId
                };

                var userMessage = new UserMessage
                {
                    userId = userId,
                    Username = username,
                    Message = message,
                    Time = DateTime.UtcNow,
                    ChatRoomId = chatRoomId
                };

                MongoDBClient.GetInstance().InsertMessage(userMessage);
            }

            public static async void GetChatRoomHistory(int userId, int chatRoomId)
            {
                var messages = await MongoDBClient.GetInstance().GetAllMessageOfChatRoom(chatRoomId);

                var protocol = new ClientServerProtocol
                {
                    TypeOfCommand = TypeOfCommandenum.Return_Messages_History_Of_Chat_Room_Command,
                    MessagesOfAChatRoomJson = JsonConvert.SerializeObject(messages)
                };

            }

            public static MediaRoom GetMediaRoomById(int mediaRoomId)
            {
                return MediaRooms.FirstOrDefault(media => media.RoomId == mediaRoomId);
            }

          

         
            private static Dictionary<string, int> GetConnectedUsersDetails(int userId, MediaRoom mediaRoom)
            {
                return mediaRoom.GetAllUsers()
                    .Where(user => user.Key != userId)
                    .ToDictionary(
                        user => ZoomClientConnection.GetUserIpById(user.Key),
                        user => user.Value
                    );
            }

            public static void RemoveUserFromAllMediaRooms(int userId)
            {
                foreach (var mediaRoom in MediaRooms)
                {
                    mediaRoom.RemoveUser(userId);
                }
            }

           
        }
    }


