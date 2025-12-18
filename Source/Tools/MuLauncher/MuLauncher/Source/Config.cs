using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MuLauncher.Source
{
    public static class Config
    {
        [DllImport("kernel32", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern int GetPrivateProfileInt(string section, string key, int def, string filePath);

        // Connection mode: 0 = Database, 1 = Server
        public static int ConnectionMode;

        // Server settings (when ConnectionMode = 1)
        public static string ServerHost;
        public static int ServerPort;

        // Database settings (when ConnectionMode = 0)
        public static int EnableTrusted;
        public static string DbServer;
        public static string DbPort;
        public static string DbName;
        public static string DbUser;
        public static string DbPass;

        // Launcher settings
        public static string MainExePath;
        public static int ResetLevel;
        public static int ResetPoints;
        public static int GrandResetResets;
        public static int GrandResetPoints;
        public static int ClearPKZenCost;

        private static readonly string ConfigPath = ".\\config.ini";

        private static string ReadString(string section, string key, string defValue)
        {
            StringBuilder sb = new StringBuilder(255);
            if (GetPrivateProfileString(section, key, "", sb, 255, ConfigPath) <= 0)
            {
                return defValue;
            }
            return sb.ToString();
        }

        private static int ReadInt(string section, string key, int defValue)
        {
            return GetPrivateProfileInt(section, key, defValue, ConfigPath);
        }

        public static bool Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    MessageBox.Show("config.ini not found. Please create config.ini file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Connection mode: 0 = Database (legacy), 1 = Server (recommended)
                ConnectionMode = ReadInt("Connection", "Mode", 1);

                // Server settings (for ConnectionMode = 1)
                // NOTE: Port should be the JoinServer port (default: 55990), NOT the GameServer port!
                ServerHost = ReadString("Server", "Host", "127.0.0.1");
                ServerPort = ReadInt("Server", "Port", 55990);

                // Database settings (for ConnectionMode = 0, legacy mode)
                EnableTrusted = ReadInt("Database", "EnableTrusted", 0);
                DbServer = ReadString("Database", "DataBaseHost", "127.0.0.1");
                DbPort = ReadString("Database", "DataBasePort", "1433");
                DbName = ReadString("Database", "DataBaseName", "MuOnline97");
                DbUser = ReadString("Database", "DataBaseUser", "sa");
                DbPass = ReadString("Database", "DataBasePass", "");

                // Launcher settings
                MainExePath = ReadString("Launcher", "MainExePath", ".\\main.exe");
                ResetLevel = ReadInt("Launcher", "ResetLevel", 400);
                ResetPoints = ReadInt("Launcher", "ResetPoints", 400);
                GrandResetResets = ReadInt("Launcher", "GrandResetResets", 10);
                GrandResetPoints = ReadInt("Launcher", "GrandResetPoints", 100);
                ClearPKZenCost = ReadInt("Launcher", "ClearPKZenCost", 1000000);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
