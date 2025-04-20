using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ZoomServer
{
  
       

        public class MongoDBClient
        {

        private static MongoDBClient _client = null;

            private MongoClient mongoClient;

            private IMongoDatabase database;

            private const string DATA__BASE_NAME = "admin";

            private const string DATA_BASE_IP = "13.60.63.74";

            private const string COLLECTION_NAME = "UserMessages";



            [MethodImpl(MethodImplOptions.Synchronized)]
            public static MongoDBClient GetInstance()
            {
                if (_client == null)
                {
                    _client = new MongoDBClient();
                }
                return _client;

            }

            private MongoDBClient()
            {
                string connectionString = $"mongodb://admin:Qwer!234@{DATA_BASE_IP}:27017/{DATA__BASE_NAME}?authSource=admin&ssl=false";
                this.mongoClient = new MongoClient(connectionString);
                this.database = mongoClient.GetDatabase(DATA__BASE_NAME);
            }

            public async void InsertMessage(UserMessage userMessage)
            {
                var collection = database.GetCollection<UserMessage>(COLLECTION_NAME);
                await collection.InsertOneAsync(userMessage);
            }

            public async Task<List<UserMessage>> GetAllMessageOfChatRoom(int chatRoomId)
            {
                var collection = database.GetCollection<UserMessage>(COLLECTION_NAME);
                var filter = Builders<UserMessage>.Filter.Eq(e => e.ChatRoomId, chatRoomId);
                var sort = Builders<UserMessage>.Sort.Ascending(e => e.Time);
                return await collection.Find(filter).Sort(sort).ToListAsync();
            }

        }
    }

