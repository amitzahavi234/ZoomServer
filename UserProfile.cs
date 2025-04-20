using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoomServer
{
    public class UserProfile
    {
        public int UserId;

        public string Username;

        public byte[] Picture;

        public int MediaChannelId = -1;

        public bool Status;

        public UserProfile()
        {

        }

        public UserProfile(int userId, string username, byte[] picture)
        {
            UserId = userId;
            Username = username;
            Picture = picture;
        }
    }
}

