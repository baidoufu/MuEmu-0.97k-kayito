using System;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MuLauncher.Source
{
    public class Database
    {
        private OleDbConnection _connection;

        public bool Connect()
        {
            try
            {
                string connectionString;
                if (Config.EnableTrusted != 0)
                {
                    connectionString = $"Provider=SQLOLEDB.1;Data Source={Config.DbServer};Initial Catalog={Config.DbName};Integrated Security=SSPI;";
                }
                else
                {
                    connectionString = $"Provider=SQLOLEDB.1;Data Source={Config.DbServer},{Config.DbPort};Initial Catalog={Config.DbName};Uid={Config.DbUser};Pwd={Config.DbPass}";
                }

                _connection = new OleDbConnection(connectionString);
                _connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public void Disconnect()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        private static string MD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public bool ValidateLogin(string account, string password)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                return false;

            try
            {
                string query = $"SELECT memb__pwd FROM MEMB_INFO WHERE memb___id = ?";
                using (OleDbCommand cmd = new OleDbCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@account", account);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPwd = reader.GetString(0);
                            // Compare with MD5 hash only for security
                            string md5Pwd = MD5Hash(password);
                            return storedPwd == md5Pwd;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login validation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool IsAccountOnline(string account)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                return true;

            try
            {
                string query = $"SELECT ConnectStat FROM MEMB_STAT WHERE memb___id = ?";
                using (OleDbCommand cmd = new OleDbCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@account", account);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetByte(0) == 1;
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                // Log or handle the exception if needed - returning false indicates account status is unknown
                return false;
            }
        }

        public DataTable GetCharacters(string account)
        {
            DataTable dt = new DataTable();
            // Use English column names internally (code relies on these), but set Chinese captions for UI
            DataColumn col;

            col = dt.Columns.Add("Name", typeof(string)); col.Caption = "名称";
            col = dt.Columns.Add("Level", typeof(int)); col.Caption = "等级";
            col = dt.Columns.Add("Resets", typeof(int)); col.Caption = "转生";
            col = dt.Columns.Add("GrandResets", typeof(int)); col.Caption = "转世";
            col = dt.Columns.Add("Points", typeof(int)); col.Caption = "点数";
            col = dt.Columns.Add("Strength", typeof(int)); col.Caption = "力量";
            col = dt.Columns.Add("Dexterity", typeof(int)); col.Caption = "敏捷";
            col = dt.Columns.Add("Vitality", typeof(int)); col.Caption = "体力";
            col = dt.Columns.Add("Energy", typeof(int)); col.Caption = "智力"; // "智力" displayed for Energy/Int
            col = dt.Columns.Add("PKLevel", typeof(int)); col.Caption = "PK等级";
            col = dt.Columns.Add("Money", typeof(int)); col.Caption = "金币";

            if (_connection == null || _connection.State != ConnectionState.Open)
                return dt;

            try
            {
                // First get character names from AccountCharacter
                string query = "SELECT GameID1, GameID2, GameID3, GameID4, GameID5 FROM AccountCharacter WHERE Id = ?";
                using (OleDbCommand cmd = new OleDbCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@account", account);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                string charName = reader.GetValue(i)?.ToString();
                                if (!string.IsNullOrEmpty(charName))
                                {
                                    LoadCharacterInfo(dt, charName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading characters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }

        private void LoadCharacterInfo(DataTable dt, string name)
        {
            try
            {
                string query = "SELECT Name, cLevel, ResetCount, GrandResetCount, LevelUpPoint, Strength, Dexterity, Vitality, Energy, PkLevel, Money FROM \"Character\" WHERE Name = ?";
                using (OleDbCommand cmd = new OleDbCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            DataRow row = dt.NewRow();
                            row["Name"] = reader.GetString(0);
                            row["Level"] = reader.GetInt32(1);
                            row["Resets"] = reader.GetInt32(2);
                            row["GrandResets"] = reader.GetInt32(3);
                            row["Points"] = reader.GetInt32(4);
                            row["Strength"] = reader.GetInt32(5);
                            row["Dexterity"] = reader.GetInt32(6);
                            row["Vitality"] = reader.GetInt32(7);
                            row["Energy"] = reader.GetInt32(8);
                            row["PKLevel"] = reader.GetInt32(9);
                            row["Money"] = reader.GetInt32(10);
                            dt.Rows.Add(row);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Skip character if there's an error loading its info
            }
        }

        public bool AddPoints(string name, string stat, int points)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                return false;

            // Validate stat name using a dictionary to prevent SQL injection
            // The stat parameter MUST match exactly one of the predefined column names
            var validStatColumns = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Strength", "Strength" },
                { "Dexterity", "Dexterity" },
                { "Vitality", "Vitality" },
                { "Energy", "Energy" }
            };

            if (!validStatColumns.TryGetValue(stat, out string columnName))
            {
                return false;
            }

            try
            {
                // Check available points
                string checkQuery = "SELECT LevelUpPoint FROM \"Character\" WHERE Name = ?";
                int availablePoints = 0;
                using (OleDbCommand cmd = new OleDbCommand(checkQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        availablePoints = Convert.ToInt32(result);
                    }
                }

                if (availablePoints < points)
                {
                    MessageBox.Show("Not enough points available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Update character - columnName is from a hardcoded whitelist, not user input
                string updateQuery = $"UPDATE \"Character\" SET [{columnName}] = [{columnName}] + ?, LevelUpPoint = LevelUpPoint - ? WHERE Name = ?";
                using (OleDbCommand cmd = new OleDbCommand(updateQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@points", points);
                    cmd.Parameters.AddWithValue("@points2", points);
                    cmd.Parameters.AddWithValue("@name", name);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding points: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool ClearPK(string name)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                return false;

            try
            {
                // Check if character has money for zen cost
                string checkQuery = "SELECT Money FROM \"Character\" WHERE Name = ?";
                int money = 0;
                using (OleDbCommand cmd = new OleDbCommand(checkQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        money = Convert.ToInt32(result);
                    }
                }

                if (money < Config.ClearPKZenCost)
                {
                    MessageBox.Show($"Not enough zen. Required: {Config.ClearPKZenCost:N0}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Update PK level to 3 (normal) and deduct zen
                string updateQuery = "UPDATE \"Character\" SET PkLevel = 3, PkCount = 0, PkTime = 0, Money = Money - ? WHERE Name = ?";
                using (OleDbCommand cmd = new OleDbCommand(updateQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@cost", Config.ClearPKZenCost);
                    cmd.Parameters.AddWithValue("@name", name);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing PK: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool Reset(string name, string account)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                return false;

            try
            {
                // Check level requirement
                string checkQuery = "SELECT cLevel FROM \"Character\" WHERE Name = ?";
                int level = 0;
                using (OleDbCommand cmd = new OleDbCommand(checkQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        level = Convert.ToInt32(result);
                    }
                }

                if (level < Config.ResetLevel)
                {
                    MessageBox.Show($"Required level: {Config.ResetLevel}. Current level: {level}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Perform reset - reset level to 1, add reset count, give points
                string updateQuery = "UPDATE \"Character\" SET cLevel = 1, Experience = 0, ResetCount = ResetCount + 1, LevelUpPoint = LevelUpPoint + ?, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name = ?";
                using (OleDbCommand cmd = new OleDbCommand(updateQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@points", Config.ResetPoints);
                    cmd.Parameters.AddWithValue("@name", name);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing reset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool GrandReset(string name, string account)
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                return false;

            try
            {
                // Check reset requirement
                string checkQuery = "SELECT ResetCount, cLevel FROM \"Character\" WHERE Name = ?";
                int resets = 0;
                int level = 0;
                using (OleDbCommand cmd = new OleDbCommand(checkQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resets = reader.GetInt32(0);
                            level = reader.GetInt32(1);
                        }
                    }
                }

                if (resets < Config.GrandResetResets)
                {
                    MessageBox.Show($"Required resets: {Config.GrandResetResets}. Current resets: {resets}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (level < Config.ResetLevel)
                {
                    MessageBox.Show($"Required level: {Config.ResetLevel}. Current level: {level}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Perform grand reset - reset level to 1, reset resets to 0, add grand reset count, give points
                string updateQuery = "UPDATE \"Character\" SET cLevel = 1, Experience = 0, ResetCount = 0, GrandResetCount = GrandResetCount + 1, LevelUpPoint = LevelUpPoint + ?, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name = ?";
                using (OleDbCommand cmd = new OleDbCommand(updateQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@points", Config.GrandResetPoints);
                    cmd.Parameters.AddWithValue("@name", name);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing grand reset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
