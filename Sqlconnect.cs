using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoomServer
{
    internal class Sqlconnect
    {


        private static Sqlconnect _instance = null;
        private static readonly object _lock = new object();

        private static string SchemaGenerationScript = $"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{USERS_TABLE}') BEGIN " +                                                         
                                                            $"CREATE TABLE [dbo].[{USERS_TABLE}]" +
                                                            @"(
	                                                            [Id] INT NOT NULL PRIMARY KEY, 
                                                                [Username] NVARCHAR(50) NOT NULL, 
                                                                [Password] NVARCHAR(255) NOT NULL, 
                                                                [Firstname] NVARCHAR(50) NULL, 
                                                                [Lastname] NVARCHAR(50) NULL, 
                                                                [Email] NVARCHAR(50) NULL, 
                                                                [City] NVARCHAR(50) NULL, 
                                                                [Gender] VARCHAR(10) NULL, 
                                                                [ProfilePicture] VARBINARY(MAX) NULL
                                                            ) 
                                                            SELECT 0;
                                                            END
                                                            
                                                            ELSE SELECT 1";
                                                            
        /// <summary>
        /// The SQL Connection object - actual connection with the SQL database
        /// </summary>
        private SqlConnection connection;
        private const string USERS_TABLE = "Users";

        /// <summary>
        /// The constructor - creating the SQL connection with the database
        /// </summary>
        public Sqlconnect()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["zoom_serverConnectionString"].ConnectionString;
            connection = new SqlConnection(connectionString);
        }


        /// <summary>
        /// The function insert the user into the data base
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="email"></param>
        /// <param name="city"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        public int InsertNewUser(string username, string password, string firstName, string lastName, string email,
                string city, string gender, byte[] imageToByteArray)
        {

            string query = $"INSERT INTO {USERS_TABLE} (Username, Password, FirstName, LastName, Email, City, Gender, ProfilePicture) VALUES (@username, @password, @firstname, @lastname, @email, @city, @gender, @imageToByteArray)";
            SqlCommand command = new SqlCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@firstname", firstName);
            command.Parameters.AddWithValue("@lastname", lastName);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@city", city);
            command.Parameters.AddWithValue("@gender", gender);
            command.Parameters.AddWithValue("@imageToByteArray", imageToByteArray);
            connection.Open();
            command.Connection = connection;
            int id = command.ExecuteNonQuery();
            Console.WriteLine("New Id is: " + id);
            connection.Close();
            return id;
        }

        /// <summary>
        /// The function check if this username exist in the data base
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool IsExist(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            int b = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
            return b > 0;
        }


        /// <summary>
        /// The function return the email of this user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetEmail(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Email FROM Users WHERE Username  = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            string b = (string)command.ExecuteScalar();
            connection.Close();
            return b;
        }


        /// <summary>
        /// The function return the current password of this user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public string GetPassword(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Password FROM Users WHERE Username  = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            string b = (string)command.ExecuteScalar();
            connection.Close();
            return b;
        }


        /// <summary>
        /// The function return the profile picture of the user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public byte[] GetProfilePictureByUsername(string username)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT ProfilePicture FROM Users WHERE Username  = @username";
            command.Parameters.AddWithValue("@username", username);
            connection.Open();
            command.Connection = connection;
            byte[] b = (byte[])command.ExecuteScalar();
            connection.Close();
            return b;
        }



        /// <summary>
        /// The function return the profile picture of the user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public byte[] GetProfilePictureByUserId(int userId)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT ProfilePicture FROM Users WHERE Id  = @userId";
            command.Parameters.AddWithValue("@userId", userId);
            connection.Open();
            command.Connection = connection;
            byte[] b = (byte[])command.ExecuteScalar();
            connection.Close();
            return b;
        }

        /// <summary>
        /// The function return the user Id from his username and his password and on the way check if there is a registered
        /// user with this username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int GetUserId(string username, string password)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "SELECT Id FROM Users WHERE Username = @username AND Password = @password ";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);
            long ticks = DateTime.Now.Ticks;
            connection.Open();
            command.Connection = connection;
            int b = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
            return b;
        }


        /// <summary>
        /// The function update the Password in the database when someone forgot his password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="newPassword"></param>
        public void UpdatePassword(string username, string newPassword)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "UPDATE Users SET Password = @newPassword WHERE Username = @username ";
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@newPassword", newPassword);
            connection.Open();
            command.Connection = connection;
            command.ExecuteNonQuery();
            connection.Close();
        }
        public static Sqlconnect GetInstance()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new Sqlconnect();
                }
                return _instance;
            }
        }

        public bool CreateDbSchemaIfDoesNotExist()
        {
            try
            {

                SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.CommandText = SchemaGenerationScript;
                connection.Open();
                bool result = (int)command.ExecuteScalar() == 1;
                Console.WriteLine(result ? "Schema already exists." : "Schema created successfully.");
                return result;
            }
            finally
            {
                connection.Close();
            }

        }


        /// <summary>
        /// The function update the profile picture in the database when someone wants to update his profile picture
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="userId"></param>
        public void UpdateProfilePictureByUserId(byte[] bytes, int userId)
        {
            SqlCommand command = new SqlCommand();
            command.CommandText = "UPDATE Users SET ProfilePicture = @newProfilePicture WHERE Id = @id ";
            command.Parameters.AddWithValue("@newProfilePicture", bytes);
            command.Parameters.AddWithValue("@id", userId);
            connection.Open();
            command.Connection = connection;
            command.ExecuteNonQuery();
            connection.Close();
        }

        public List<string> GetAllEmails()
        {
            List<string> emails = new List<string>();

            try
            {
                string query = "SELECT DISTINCT Email FROM Users"; // שליפה ללא כפילויות
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string email = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        emails.Add(email);
                    }
                }

                reader.Close();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching emails: " + ex.Message);
            }

            return emails;
        }

    }
}


