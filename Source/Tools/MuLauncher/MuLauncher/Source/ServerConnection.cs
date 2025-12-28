using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MuLauncher.Source
{
    public class ServerConnection
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private byte[] _buffer = new byte[4096];

        // Reconnect settings
        public bool AutoReconnect { get; set; } = true;
        public int AutoReconnectAttempts { get; set; } = 3;
        public int AutoReconnectDelayMs { get; set; } = 1000;

        // Constants for protocol
        private const int CHARACTER_NAME_SIZE = 11;
        private const int CHARACTER_INT_FIELDS_COUNT = 10; // level, resets, grandResets, points, str, dex, vit, ene, pkLevel, money
        private const int CHARACTER_INFO_SIZE = CHARACTER_NAME_SIZE + (4 * CHARACTER_INT_FIELDS_COUNT);

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

        /// <summary>
        /// Attempt to reconnect using the configured attempt count and delay.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        public bool Reconnect()
        {
            return Reconnect(AutoReconnectAttempts, AutoReconnectDelayMs);
        }

        /// <summary>
        /// Attempt to reconnect a number of times with a delay.
        /// </summary>
        public bool Reconnect(int attempts, int delayMs)
        {
            try
            {
                Disconnect();

                for (int i = 0; i < Math.Max(1, attempts); i++)
                {
                    if (Connect())
                        return true;

                    Thread.Sleep(Math.Max(0, delayMs));
                }
            }
            catch { }

            return false;
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
            if (!IsConnected)
            {
                if (AutoReconnect)
                {
                    if (!Reconnect())
                        return false;
                }
                else
                {
                    return false;
                }
            }

            try
            {
                _stream.Write(data, 0, data.Length);
                return true;
            }
            catch
            {
                // Try one reconnect-and-retry if enabled
                if (AutoReconnect)
                {
                    if (Reconnect())
                    {
                        try
                        {
                            if (IsConnected)
                            {
                                _stream.Write(data, 0, data.Length);
                                return true;
                            }
                        }
                        catch { }
                    }
                }

                return false;
            }
        }

        private byte[] ReceivePacket()
        {
            if (!IsConnected)
            {
                if (AutoReconnect)
                {
                    if (!Reconnect())
                        return null;
                }
                else
                {
                    return null;
                }
            }

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
            catch
            {
                // On read error try to reconnect so subsequent operations may succeed
                if (AutoReconnect)
                {
                    Reconnect();
                }
            }
            return null;
        }

        // Determine head index for C1/C2 packets
        private int GetHeadIndex(byte[] response)
        {
            if (response == null || response.Length < 3)
                return -1;
            if (response[0] == 0xC1)
                return 2; // C1: [0]=0xC1, [1]=size, [2]=head
            if (response[0] == 0xC2)
                return 3; // C2: [0]=0xC2, [1]=size low, [2]=size high, [3]=head
            return -1;
        }

        private int ReadInt32LittleEndian(byte[] buffer, int offset)
        {
            if (buffer == null || buffer.Length < offset + 4)
                return 0;
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToInt32(buffer, offset);
            byte[] tmp = new byte[4];
            Array.Copy(buffer, offset, tmp, 0, 4);
            Array.Reverse(tmp);
            return BitConverter.ToInt32(tmp, 0);
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

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] passwordBytes = Encoding.Default.GetBytes(password.PadRight(11, '\0').Substring(0, 11));
                byte[] ipBytes = Encoding.ASCII.GetBytes("127.0.0.1".PadRight(16, '\0'));

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(passwordBytes, 0, packet, 14, 11);
                Array.Copy(ipBytes, 0, packet, 25, 16);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                int head = GetHeadIndex(response);
                if (response != null && head >= 0 && response.Length >= head + 2 && response[head] == 0x10)
                {
                    return response[head + 1]; // result
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

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                Array.Copy(accountBytes, 0, packet, 3, 11);

                if (!SendPacket(packet))
                    return true; // Assume online if we can't check

                byte[] response = ReceivePacket();
                int head = GetHeadIndex(response);
                if (response != null && head >= 0 && response.Length >= head + 2 && response[head] == 0x16)
                {
                    return response[head + 1] == 1;
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

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                Array.Copy(accountBytes, 0, packet, 3, 11);

                if (!SendPacket(packet))
                    return dt;

                byte[] response = ReceivePacket();
                int head = GetHeadIndex(response);
                if (response == null || head < 0 || response.Length < head + 3 || response[head] != 0x11)
                    return dt;

                // C2/C1 format: head at head, result at head+1, count at head+2, data starts at head+3
                byte result = response[head + 1];
                byte count = response[head + 2];

                if (result != 1)
                    return dt;

                // Parse character info
                int offset = head + 3;

                for (int i = 0; i < count && offset + CHARACTER_INFO_SIZE <= response.Length; i++)
                {
                    DataRow row = dt.NewRow();

                    // Name (11 bytes) - Use Default encoding for Chinese/Korean character support
                    row["Name"] = Encoding.Default.GetString(response, offset, 11).TrimEnd('\0');
                    offset += 12;

                    // Ints: Level, Resets, GrandResets, Points are 4 bytes
                    row["Level"] = ReadInt32LittleEndian(response, offset); offset += 4;
                    row["Resets"] = ReadInt32LittleEndian(response, offset); offset += 4;
                    row["GrandResets"] = ReadInt32LittleEndian(response, offset); offset += 4;
                    row["Points"] = ReadInt32LittleEndian(response, offset); offset += 4;

                    row["Strength"] = ReadInt32LittleEndian(response, offset); offset += 4;
                    row["Dexterity"] = ReadInt32LittleEndian(response, offset); offset += 4;
                    row["Vitality"] = ReadInt32LittleEndian(response, offset); offset += 4;
                    row["Energy"] = ReadInt32LittleEndian(response, offset); offset += 4;

                    // PKLevel and Money are 4 bytes
                    row["PKLevel"] = ReadInt32LittleEndian(response, offset); offset += 4;
                    row["Money"] = ReadInt32LittleEndian(response, offset); offset += 4;

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
                    case "力量": statCode = 0; break;
                    case "敏捷": statCode = 1; break;
                    case "体力": statCode = 2; break;
                    case "智力": statCode = 3; break;
                    default: return 0;
                }

                // C1 size head account[11] name[11] stat(1) points(2)
                byte[] packet = new byte[3 + 11 + 11 + 1 + 2];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x12;

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.Default.GetBytes(name.PadRight(11, '\0').Substring(0, 11));

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(nameBytes, 0, packet, 14, 11);
                packet[25] = statCode;
                byte[] pointsBytes = BitConverter.GetBytes((ushort)points);
                packet[26] = pointsBytes[0];
                packet[27] = pointsBytes[1];

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                int head = GetHeadIndex(response);
                if (response != null && head >= 0 && response.Length >= head + 2 && response[head] == 0x12)
                {
                    return response[head + 1];
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

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.Default.GetBytes(name.PadRight(11, '\0').Substring(0, 11));
                byte[] zenBytes = BitConverter.GetBytes(zenCost);

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(nameBytes, 0, packet, 14, 11);
                Array.Copy(zenBytes, 0, packet, 25, 4);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                int head = GetHeadIndex(response);
                if (response != null && head >= 0 && response.Length >= head + 2 && response[head] == 0x13)
                {
                    return response[head + 1];
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

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.Default.GetBytes(name.PadRight(11, '\0').Substring(0, 11));
                byte[] levelBytes = BitConverter.GetBytes((ushort)requiredLevel);
                byte[] pointsBytes = BitConverter.GetBytes(rewardPoints);

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(nameBytes, 0, packet, 14, 11);
                Array.Copy(levelBytes, 0, packet, 25, 2);
                Array.Copy(pointsBytes, 0, packet, 27, 4);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                int head = GetHeadIndex(response);
                if (response != null && head >= 0 && response.Length >= head + 2 && response[head] == 0x14)
                {
                    return response[head + 1];
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

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] nameBytes = Encoding.Default.GetBytes(name.PadRight(11, '\0').Substring(0, 11));
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
                int head = GetHeadIndex(response);
                if (response != null && head >= 0 && response.Length >= head + 2 && response[head] == 0x15)
                {
                    return response[head + 1];
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing grand reset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        /// <summary>
        /// Register a new account (protocol 0x17)
        /// Returns: 0=Account exists, 1=Success, 2=Invalid input, 3=Server error
        /// </summary>
        public int RegisterAccount(string account, string password, string personalCode)
        {
            try
            {
                // C1 size head account[11] password[11] personalCode[14] ipAddress[16]
                byte[] packet = new byte[3 + 11 + 11 + 14 + 16];
                packet[0] = 0xC1;
                packet[1] = (byte)packet.Length;
                packet[2] = 0x17;

                byte[] accountBytes = Encoding.Default.GetBytes(account.PadRight(11, '\0').Substring(0, 11));
                byte[] passwordBytes = Encoding.Default.GetBytes(password.PadRight(11, '\0').Substring(0, 11));
                byte[] personalCodeBytes = Encoding.Default.GetBytes(personalCode.PadRight(14, '\0').Substring(0, 14));
                byte[] ipBytes = Encoding.ASCII.GetBytes("127.0.0.1".PadRight(16, '\0'));

                Array.Copy(accountBytes, 0, packet, 3, 11);
                Array.Copy(passwordBytes, 0, packet, 14, 11);
                Array.Copy(personalCodeBytes, 0, packet, 25, 14);
                Array.Copy(ipBytes, 0, packet, 39, 16);

                if (!SendPacket(packet))
                    return 0;

                byte[] response = ReceivePacket();
                int head = GetHeadIndex(response);
                if (response != null && head >= 0 && response.Length >= head + 2 && response[head] == 0x17)
                {
                    return response[head + 1];
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error registering account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }
    }
}
