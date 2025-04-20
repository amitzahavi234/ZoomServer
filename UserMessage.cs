using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZoomServer
{
        public class UserMessage
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [BsonElement("userId")]
            public int userId { get; set; }

            [BsonElement("username")]
            public string Username { get; set; }

            [BsonElement("message")]
            public string Message { get; set; }

            [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
            public DateTime Time { get; set; }

            [BsonElement("chatRoomId")]
            public int ChatRoomId { get; set; }






        }
    }


