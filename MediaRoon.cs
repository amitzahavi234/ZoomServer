using System;
using System.Collections.Generic;

namespace ZoomServer
{
    public class MediaRoom
    {
        public int RoomId { get; }

        private readonly Dictionary<int, int> _usersMediaPorts;

        public MediaRoom(int roomId)
        {
            RoomId = roomId;
            _usersMediaPorts = new Dictionary<int, int>();
        }

        /// <summary>
        /// Attempts to add a user to the media room.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <param name="mediaPort">The media port associated with the user.</param>
        /// <returns>True if the user was added successfully, false if the user already exists.</returns>
      

        /// <summary>
        /// Tries to add a user if they are not already in the room.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <param name="mediaPort">The media port associated with the user.</param>
        public bool AddUser(int userId, int mediaPort)
        {
            if (!_usersMediaPorts.ContainsKey(userId))
            {
                _usersMediaPorts.Add(userId, mediaPort);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Removes a user from the media room.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <returns>True if the user was removed, false otherwise.</returns>
        public bool RemoveUser(int userId)
        {
            return _usersMediaPorts.Remove(userId);
        }

        /// <summary>
        /// Checks if a user is in the room.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <returns>True if the user is in the room, false otherwise.</returns>
        public bool IsUserInRoom(int userId)
        {
            return _usersMediaPorts.ContainsKey(userId);
        }

        /// <summary>
        /// Retrieves the media port of a specific user.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <returns>The media port if found, otherwise null.</returns>
        public int? GetUserMediaPort(int userId)
        {
            return _usersMediaPorts.TryGetValue(userId, out int port) ? port : (int?)null;
        }

        /// <summary>
        /// Gets all users and their associated media ports in the room.
        /// </summary>
        /// <returns>A read-only dictionary of users and media ports.</returns>
        public IReadOnlyDictionary<int, int> GetAllUsers()
        {
            return _usersMediaPorts;
        }
    }
}
