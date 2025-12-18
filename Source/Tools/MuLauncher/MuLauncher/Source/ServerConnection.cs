using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace MuLauncher.Source
{
    public class ServerConnection
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private byte[] _buffer = new byte[4096];

        public bool Connect()
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(Config.ServerHost, Config.ServerPort);
                _stream = _client.GetStream();
                _stream.ReadTimeout = 10000; // 10 second timeout
                _stream.WriteTimeout = 10000;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Server connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            finally
            {
                _stream = null;
                _client = null;
            }
        }

        public bool IsConnected => _client?.Connected == true;

        private bool SendPacket(byte[] data)
        {
            if (!IsConnected) return false;
            try
            {
                _stream.Write(data, 0, data.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] ReceivePacket()
        {
            if (!IsConnected) return null;
            try
            {
                int bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
                if (bytesRead > 0)
                {
                    byte[] result = new byte[bytesRead];
                    Array.Copy(_buffer, result, bytesRead);
                    return result;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Login to the server (protocol 0x10)
        /// Returns: 0=Invalid password, 1=Success, 2=Account not found, 3=Account online
        /// </summary>
        public int ValidateLogin(string account, string password)
        {
            try
            {
                // Build packet: C1 size head account[11] password[11] ipAddress[16]
                byte[] packet = new byte[3 + 11 + 11 + 16];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x10; // Head

                byte[] accountBytes = Encoding.ASCII.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] passwordBytes = Encoding.ASCII.GetBytes(password.PadRight(11, '\0').Substring(0, 11));
                byte[] ipBytes = Encoding.ASCII.GetBytes("127.0.0.1".PadRight(16, '\0'));

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(passwordBytes, 0, packet, 14, 11);
                Array.Copy(ipBytes, 0, packet, 25, 16);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                if (response != null && response.Length >= 4 && response[2] == 0x10)
                {
                    return response[3]; // result
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Check if account is online (protocol 0x16)
        /// Returns: true if online, false if offline
        /// </summary>
        public bool IsAccountOnline(string account)
        {
            try
            {
                byte[] packet = new byte[3 + 11];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x16;

                byte[] accountBytes = Encoding.ASCII.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                Array.Copy(accountBytes, 0, packet, 3, 11);

                if (!SendPacket(packet))
                    return true; // Assume online if we can't check

                byte[] response = ReceivePacket();
                if (response != null && response.Length >= 4 && response[2] == 0x16)
                {
                    return response[3] == 1;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Get character list (protocol 0x11)
        /// </summary>
        public DataTable GetCharacters(string account)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Level", typeof(int));
            dt.Columns.Add("Resets", typeof(int));
            dt.Columns.Add("GrandResets", typeof(int));
            dt.Columns.Add("Points", typeof(int));
            dt.Columns.Add("Strength", typeof(int));
            dt.Columns.Add("Dexterity", typeof(int));
            dt.Columns.Add("Vitality", typeof(int));
            dt.Columns.Add("Energy", typeof(int));
            dt.Columns.Add("PKLevel", typeof(int));
            dt.Columns.Add("Money", typeof(int));

            try
            {
                byte[] packet = new byte[3 + 11];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x11;

                byte[] accountBytes = Encoding.ASCII.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                Array.Copy(accountBytes, 0, packet, 3, 11);

                if (!SendPacket(packet))
                    return dt;

                byte[] response = ReceivePacket();
                if (response == null || response.Length < 6 || response[3] != 0x11)
                    return dt;

                // C2 format: type, size[2], head, result, count, data...
                byte result = response[4];
                byte count = response[5];

                if (result != 1)
                    return dt;

                // Parse character info
                int offset = 6;
                int charInfoSize = 11 + (4 * 10); // name[11] + 10 ints (4 bytes each)

                for (int i = 0; i < count && offset + charInfoSize <= response.Length; i++)
                {
                    DataRow row = dt.NewRow();

                    // Name (11 bytes)
                    row["Name"] = Encoding.ASCII.GetString(response, offset, 11).TrimEnd('\0');
                    offset += 11;

                    // Ints (4 bytes each)
                    row["Level"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["Resets"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["GrandResets"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["Points"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["Strength"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["Dexterity"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["Vitality"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["Energy"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["PKLevel"] = BitConverter.ToInt32(response, offset); offset += 4;
                    row["Money"] = BitConverter.ToInt32(response, offset); offset += 4;

                    dt.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading characters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }

        /// <summary>
        /// Add points to character stat (protocol 0x12)
        /// Returns: 0=Failed, 1=Success, 2=Not enough points, 3=Character not found, 4=Account online
        /// </summary>
        public int AddPoints(string account, string name, string stat, int points)
        {
            try
            {
                // Map stat name to stat code
                byte statCode;
                switch (stat)
                {
                    case "Strength": statCode = 0; break;
                    case "Dexterity": statCode = 1; break;
                    case "Vitality": statCode = 2; break;
                    case "Energy": statCode = 3; break;
                    default: return 0;
                }

                // C1 size head account[11] name[11] stat(1) points(2)
                byte[] packet = new byte[3 + 11 + 11 + 1 + 2];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x12;

                byte[] accountBytes = Encoding.ASCII.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.ASCII.GetBytes(name.PadRight(11, '\0').Substring(0, 11));

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(nameBytes, 0, packet, 14, 11);
                packet[25] = statCode;
                byte[] pointsBytes = BitConverter.GetBytes((ushort)points);
                packet[26] = pointsBytes[0];
                packet[27] = pointsBytes[1];

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                if (response != null && response.Length >= 4 && response[2] == 0x12)
                {
                    return response[3];
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding points: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Clear PK status (protocol 0x13)
        /// Returns: 0=Failed, 1=Success, 2=Not enough zen, 3=Character not found, 4=Account online
        /// </summary>
        public int ClearPK(string account, string name, int zenCost)
        {
            try
            {
                // C1 size head account[11] name[11] zenCost(4)
                byte[] packet = new byte[3 + 11 + 11 + 4];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x13;

                byte[] accountBytes = Encoding.ASCII.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.ASCII.GetBytes(name.PadRight(11, '\0').Substring(0, 11));
                byte[] zenBytes = BitConverter.GetBytes(zenCost);

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(nameBytes, 0, packet, 14, 11);
                Array.Copy(zenBytes, 0, packet, 25, 4);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                if (response != null && response.Length >= 4 && response[2] == 0x13)
                {
                    return response[3];
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing PK: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Perform reset (protocol 0x14)
        /// Returns: 0=Failed, 1=Success, 2=Level too low, 3=Character not found, 4=Account online
        /// </summary>
        public int Reset(string account, string name, int requiredLevel, int rewardPoints)
        {
            try
            {
                // C1 size head account[11] name[11] requiredLevel(2) rewardPoints(4)
                byte[] packet = new byte[3 + 11 + 11 + 2 + 4];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x14;

                byte[] accountBytes = Encoding.ASCII.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.ASCII.GetBytes(name.PadRight(11, '\0').Substring(0, 11));
                byte[] levelBytes = BitConverter.GetBytes((ushort)requiredLevel);
                byte[] pointsBytes = BitConverter.GetBytes(rewardPoints);

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(nameBytes, 0, packet, 14, 11);
                Array.Copy(levelBytes, 0, packet, 25, 2);
                Array.Copy(pointsBytes, 0, packet, 27, 4);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                if (response != null && response.Length >= 4 && response[2] == 0x14)
                {
                    return response[3];
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing reset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Perform grand reset (protocol 0x15)
        /// Returns: 0=Failed, 1=Success, 2=Level too low, 3=Resets too low, 4=Character not found, 5=Account online
        /// </summary>
        public int GrandReset(string account, string name, int requiredLevel, int requiredResets, int rewardPoints)
        {
            try
            {
                // C1 size head account[11] name[11] requiredLevel(2) requiredResets(2) rewardPoints(4)
                byte[] packet = new byte[3 + 11 + 11 + 2 + 2 + 4];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x15;

                byte[] accountBytes = Encoding.ASCII.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.ASCII.GetBytes(name.PadRight(11, '\0').Substring(0, 11));
                byte[] levelBytes = BitConverter.GetBytes((ushort)requiredLevel);
                byte[] resetsBytes = BitConverter.GetBytes((ushort)requiredResets);
                byte[] pointsBytes = BitConverter.GetBytes(rewardPoints);

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(nameBytes, 0, packet, 14, 11);
                Array.Copy(levelBytes, 0, packet, 25, 2);
                Array.Copy(resetsBytes, 0, packet, 27, 2);
                Array.Copy(pointsBytes, 0, packet, 29, 4);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                if (response != null && response.Length >= 4 && response[2] == 0x15)
                {
                    return response[3];
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing grand reset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }
    }
}
