using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoomServer
{
    public enum TypeOfCommandenum
    {
        Registration_Command = 1, // משתמש נרשם
        Login_Command = 2, // משתמש מנסה להתחבר
        Check_If_Username_Already_Exist_Command = 3, // בדיקת שם משתמש קיים
        Forgot_Password_Command = 4, // משתמש מבקש איפוס סיסמה
        Update_Password_Command = 5, // עדכון סיסמה קיימת
        Login_Cooldown_Command = 6, // מנגנון השהייה לאחר יותר מדי ניסיונות כושלים

        // 🔹 קודי אימות
        Code_Sent_To_Email_Command = 10, // שליחת קוד אימות למייל
        Success_Username_Not_In_The_System_Command = 11, // שם המשתמש לא קיים במערכת
        Success_Connected_To_The_Application_Command = 12, // חיבור מוצלח
        Success_Forgot_Password_Command = 13, // איפוס סיסמה בוצע בהצלחה

        // 🔹 הודעות שגיאה
        Error_Command = 20, // הודעת שגיאה כללית

        // 🔹 ניהול מדיה ופרופיל
        Profile_Picture_Selected_Command = 30, // תמונת פרופיל נבחרה
        Get_Username_And_Profile_Picture_Command = 31, // שליפת שם משתמש ותמונה

        // 🔹 הודעות בצ'אט
        Send_Message_Command = 40, // שליחת הודעה למשתמש אחר
        Message_From_Other_User_Command = 41, // קבלת הודעה ממשתמש אחר
        Fetch_Image_Of_User_Command = 42, // בקשת תמונת פרופיל של משתמש אחר
        Return_Image_Of_User_Command = 43, // החזרת תמונה למשתמש שביקש

        // 🔹 היסטוריית הודעות
        Get_Messages_History_Of_Chat_Room_Command = 50, // בקשת היסטוריית הודעות בצ'אט
        Return_Messages_History_Of_Chat_Room_Command = 51, // החזרת היסטוריית הודעות

        // 🔹 ניהול חדרי מדיה
        Connect_To_Media_Room_Command = 60, // חיבור לחדר מדיה
        New_Participant_Join_The_Media_Room_Command = 61, // משתמש חדש נכנס לחדר מדיה
        Get_All_Ips_Of_Connected_Users_In_Some_Media_Room_Command = 62, // קבלת כתובות IP של משתמשים מחוברים בחדר מדיה
        Disconnect_From_Media_Room_Command = 63, // התנתקות מחדר מדיה
        Some_User_Left_The_Media_Room_Command = 64
        // משתמש עזב חדר מדיה
    }
}



    