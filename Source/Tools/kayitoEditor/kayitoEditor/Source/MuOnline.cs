using kayito_Editor.Source;
#if SQLITE
using System.Data.SQLite;
#elif MYSQL
using MySql.Data.MySqlClient;
#else
using System.Data.OleDb;
#endif
using System;
using System.Windows.Forms;
using System.Data;

namespace kayito_Editor
{
	class MuOnline
	{
		public MuOnline()
		{
			string connection_string;

			try
			{
			#if SQLITE
				connection_string = $"Data Source={Import.MU_DB_PATH};Version=3;";

				Import.Mu_Connection = new SQLiteConnection(connection_string);
			#elif MYSQL
				connection_string = $"SERVER={Import.MU_SERVER};PORT={Import.MU_PORT};DATABASE={Import.MU_DB};UID={Import.MU_DB_USER};PASSWORD={Import.MU_DB_PASS};";

				Import.Mu_Connection = new MySqlConnection(connection_string);
			#else
				connection_string = $"Provider=SQLOLEDB.1;Data Source={Import.MU_SERVER},{Import.MU_PORT};Initial Catalog={Import.MU_DB};Uid={Import.MU_DB_USER};Pwd={Import.MU_DB_PASS}";

				if (Import.MU_TRUSTED != 0)
				{
					connection_string = $"Provider=SQLOLEDB.1;Data Source={Import.MU_SERVER};Initial Catalog={Import.MU_DB};Integrated Security=SSPI;";
				}

				Import.Mu_Connection = new OleDbConnection(connection_string);
			#endif

				Import.Mu_Connection.Open();

			#if MYSQL
				// Set ANSI_QUOTES mode
				using (MySqlCommand cmd = new MySqlCommand("SET SESSION sql_mode = 'ANSI_QUOTES'", Import.Mu_Connection))
				{
					cmd.ExecuteNonQuery();
				}
			#endif

				if (Import.USE_ME == 1)
				{
				#if SQLITE
					connection_string = $"Data Source={Import.ME_DB_PATH};Version=3;";

					Import.Me_Connection = new SQLiteConnection(connection_string);
				#elif MYSQL
					connection_string = $"SERVER={Import.ME_SERVER};PORT={Import.ME_PORT};DATABASE={Import.ME_DB};UID={Import.ME_DB_USER};PASSWORD={Import.ME_DB_PASS};";

					Import.Me_Connection = new MySqlConnection(connection_string);
				#else
					connection_string = $"Provider=SQLOLEDB.1;Data Source={Import.ME_SERVER},{Import.ME_PORT};Initial Catalog={Import.ME_DB};Uid={Import.ME_DB_USER};Pwd={Import.ME_DB_PASS}";

					if (Import.ME_TRUSTED != 0)
					{
						connection_string = $"Provider=SQLOLEDB.1;Data Source={Import.MU_SERVER};Initial Catalog={Import.MU_DB};Integrated Security=SSPI;";
					}

					Import.Me_Connection = new OleDbConnection(connection_string);
				#endif
					Import.Me_Connection.Open();

				#if MYSQL
					// Set ANSI_QUOTES mode
					using (MySqlCommand cmd = new MySqlCommand("SET SESSION sql_mode = 'ANSI_QUOTES'", Import.Me_Connection))
					{
						cmd.ExecuteNonQuery();
					}
				#endif
				}
				else
				{
					Import.Me_Connection = Import.Mu_Connection;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);

				Environment.Exit(0);
			}
		}

		public static bool Mu_ExecuteSQL(string query)
		{
			bool flag = false;

			try
			{
			#if SQLITE
				SQLiteCommand cmd = new SQLiteCommand(query, Import.Mu_Connection);
			#elif MYSQL
				MySqlCommand cmd = new MySqlCommand(query, Import.Mu_Connection);
			#else
				OleDbCommand cmd = new OleDbCommand(query, Import.Mu_Connection);
			#endif

				cmd.ExecuteNonQuery();

				cmd.Dispose();

				flag = true;
			}
			catch (Exception exception)
			{
				MessageBox.Show($"[SQL] {query}\n[Error] {exception.Message}\n[Source] {exception.Source}\n[Trace] {exception.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

				flag = false;
			}

			return flag;
		}

		public static bool Me_ExecuteSQL(string query)
		{
			bool flag = false;

			try
			{
			#if SQLITE
				SQLiteCommand cmd = new SQLiteCommand(query, Import.Me_Connection);
			#elif MYSQL
				MySqlCommand cmd = new MySqlCommand(query, Import.Me_Connection);
			#else
				OleDbCommand cmd = new OleDbCommand(query, Import.Me_Connection);
			#endif

				cmd.ExecuteNonQuery();

				cmd.Dispose();

				flag = true;
			}
			catch (Exception exception)
			{
				MessageBox.Show($"[SQL] {query}\n[Error] {exception.Message}\n[Source] {exception.Source}\n[Trace] {exception.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

				flag = false;
			}

			return flag;
		}

		public static void CloseConnection()
		{
#if SQLITE
			// For SQLite: if this process holds the last open connection(s) we control, try to checkpoint/truncate the WAL
			try
			{
				// Determine whether Me_Connection refers to the same connection or is open separately
				bool muOpen = Import.Mu_Connection != null && Import.Mu_Connection.State == ConnectionState.Open;
				bool meOpen = Import.Me_Connection != null && Import.Me_Connection.State == ConnectionState.Open;
				bool meSameAsMu = Import.Me_Connection == Import.Mu_Connection;

				// If Mu connection is open and either Me is not open or Me is the same handle,
				// then performing checkpoint on Mu_Connection makes sense before closing
				if (muOpen && (!meOpen || meSameAsMu))
				{
					try
					{
						using (var cmd = new SQLiteCommand("PRAGMA wal_checkpoint(TRUNCATE);", Import.Mu_Connection))
						{
							// ExecuteReader is used because some PRAGMA statements return rows
							using (var rdr = cmd.ExecuteReader())
							{
								// consume any result, ignore values
								while (rdr.Read()) { }
							}
						}
					}
					catch { /* ignore - best effort checkpoint */ }
				}

				// If Me connection is open and different from Mu, checkpoint it as well
				if (meOpen && !meSameAsMu)
				{
					try
					{
						using (var cmd = new SQLiteCommand("PRAGMA wal_checkpoint(TRUNCATE);", Import.Me_Connection))
						{
							using (var rdr = cmd.ExecuteReader())
							{
								while (rdr.Read()) { }
							}
						}
					}
					catch { }
				}
			}
			catch { }

			// Finally close the connections if open
			try { if (Import.Mu_Connection != null && Import.Mu_Connection.State == ConnectionState.Open) Import.Mu_Connection.Close(); } catch { }

			if (Import.USE_ME == 1)
			{
				try { if (Import.Me_Connection != null && Import.Me_Connection.State == ConnectionState.Open && Import.Me_Connection != Import.Mu_Connection) Import.Me_Connection.Close(); } catch { }
			}
#else
			Import.Mu_Connection.Close();

			if (Import.USE_ME == 1)
			{
				Import.Me_Connection.Close();
			}
#endif
		}
	}
}