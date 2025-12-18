using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MuLauncher.Source;

namespace MuLauncher.Forms
{
    public partial class MainForm : Form
    {
        private Database _database;
        private ServerConnection _serverConnection;
        private string _currentAccount;
        private DataTable _characters;
        private bool _isLoggedIn;
        private bool _useServerMode;

        public MainForm()
        {
            InitializeComponent();
            _isLoggedIn = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!Config.Load())
            {
                Application.Exit();
                return;
            }

            // Determine connection mode
            _useServerMode = Config.ConnectionMode == 1;

            if (_useServerMode)
            {
                // Server mode - connect to JoinServer
                _serverConnection = new ServerConnection();
                if (!_serverConnection.Connect())
                {
                    MessageBox.Show("Failed to connect to server. Launcher will work in offline mode.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // Database mode (legacy)
                _database = new Database();
                if (!_database.Connect())
                {
                    MessageBox.Show("Failed to connect to database. Launcher will work in offline mode.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            UpdateUI();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_useServerMode)
            {
                _serverConnection?.Disconnect();
            }
            else
            {
                _database?.Disconnect();
            }
        }

        private void UpdateUI()
        {
            bool canManageCharacters = _isLoggedIn && dgvCharacters.SelectedRows.Count > 0;

            pnlCharacterManagement.Enabled = _isLoggedIn;
            btnAddPoints.Enabled = canManageCharacters;
            btnClearPK.Enabled = canManageCharacters;
            btnReset.Enabled = canManageCharacters;
            btnGrandReset.Enabled = canManageCharacters;
            btnLogout.Visible = _isLoggedIn;
            pnlLogin.Visible = !_isLoggedIn;

            lblStatus.Text = _isLoggedIn ? $"Logged in as: {_currentAccount}" : "Not logged in";
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string account = txtAccount.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter account and password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_useServerMode)
            {
                // Server mode login
                int result = _serverConnection.ValidateLogin(account, password);
                switch (result)
                {
                    case 1: // Success
                        _currentAccount = account;
                        _isLoggedIn = true;
                        LoadCharacters();
                        UpdateUI();
                        MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case 0: // Invalid password
                        MessageBox.Show("Invalid password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case 2: // Account not found
                        MessageBox.Show("Account not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case 3: // Account online
                        MessageBox.Show("This account is currently online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    default:
                        MessageBox.Show("Login failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
            else
            {
                // Database mode login (legacy)
                if (_database.IsAccountOnline(account))
                {
                    MessageBox.Show("This account is currently online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_database.ValidateLogin(account, password))
                {
                    _currentAccount = account;
                    _isLoggedIn = true;
                    LoadCharacters();
                    UpdateUI();
                    MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Invalid account or password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            _currentAccount = null;
            _isLoggedIn = false;
            _characters?.Clear();
            dgvCharacters.DataSource = null;
            txtAccount.Clear();
            txtPassword.Clear();
            UpdateUI();
        }

        private void LoadCharacters()
        {
            if (!_isLoggedIn) return;

            if (_useServerMode)
            {
                _characters = _serverConnection.GetCharacters(_currentAccount);
            }
            else
            {
                _characters = _database.GetCharacters(_currentAccount);
            }

            dgvCharacters.DataSource = _characters;
            dgvCharacters.AutoResizeColumns();

            if (dgvCharacters.Columns.Contains("Name"))
                dgvCharacters.Columns["Name"].ReadOnly = true;
        }

        private void dgvCharacters_SelectionChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private string GetSelectedCharacterName()
        {
            if (dgvCharacters.SelectedRows.Count > 0)
            {
                return dgvCharacters.SelectedRows[0].Cells["Name"].Value?.ToString();
            }
            return null;
        }

        private bool CheckAccountOnline()
        {
            if (_useServerMode)
            {
                return _serverConnection.IsAccountOnline(_currentAccount);
            }
            else
            {
                return _database.IsAccountOnline(_currentAccount);
            }
        }

        private void btnAddPoints_Click(object sender, EventArgs e)
        {
            string name = GetSelectedCharacterName();
            if (string.IsNullOrEmpty(name)) return;

            if (CheckAccountOnline())
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (AddPointsDialog dlg = new AddPointsDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    bool success;
                    if (_useServerMode)
                    {
                        int result = _serverConnection.AddPoints(_currentAccount, name, dlg.SelectedStat, dlg.PointsToAdd);
                        success = result == 1;
                        if (result == 2)
                            MessageBox.Show("Not enough points available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        else if (result == 3)
                            MessageBox.Show("Character not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else if (result == 4)
                            MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        success = _database.AddPoints(name, dlg.SelectedStat, dlg.PointsToAdd);
                    }

                    if (success)
                    {
                        MessageBox.Show($"Added {dlg.PointsToAdd} points to {dlg.SelectedStat}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadCharacters();
                    }
                }
            }
        }

        private void btnClearPK_Click(object sender, EventArgs e)
        {
            string name = GetSelectedCharacterName();
            if (string.IsNullOrEmpty(name)) return;

            if (CheckAccountOnline())
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Clear PK status for {name}?\nCost: {Config.ClearPKZenCost:N0} zen", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                bool success;
                if (_useServerMode)
                {
                    int clearResult = _serverConnection.ClearPK(_currentAccount, name, Config.ClearPKZenCost);
                    success = clearResult == 1;
                    if (clearResult == 2)
                        MessageBox.Show($"Not enough zen. Required: {Config.ClearPKZenCost:N0}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (clearResult == 3)
                        MessageBox.Show("Character not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (clearResult == 4)
                        MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    success = _database.ClearPK(name);
                }

                if (success)
                {
                    MessageBox.Show("PK status cleared successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCharacters();
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            string name = GetSelectedCharacterName();
            if (string.IsNullOrEmpty(name)) return;

            if (CheckAccountOnline())
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Reset character {name}?\nRequired Level: {Config.ResetLevel}\nPoints awarded: {Config.ResetPoints}", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                bool success;
                if (_useServerMode)
                {
                    int resetResult = _serverConnection.Reset(_currentAccount, name, Config.ResetLevel, Config.ResetPoints);
                    success = resetResult == 1;
                    if (resetResult == 2)
                        MessageBox.Show($"Required level: {Config.ResetLevel}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (resetResult == 3)
                        MessageBox.Show("Character not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (resetResult == 4)
                        MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    success = _database.Reset(name, _currentAccount);
                }

                if (success)
                {
                    MessageBox.Show("Reset successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCharacters();
                }
            }
        }

        private void btnGrandReset_Click(object sender, EventArgs e)
        {
            string name = GetSelectedCharacterName();
            if (string.IsNullOrEmpty(name)) return;

            if (CheckAccountOnline())
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Grand Reset character {name}?\nRequired Resets: {Config.GrandResetResets}\nRequired Level: {Config.ResetLevel}\nPoints awarded: {Config.GrandResetPoints}", "Confirm Grand Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                bool success;
                if (_useServerMode)
                {
                    int grandResetResult = _serverConnection.GrandReset(_currentAccount, name, Config.ResetLevel, Config.GrandResetResets, Config.GrandResetPoints);
                    success = grandResetResult == 1;
                    if (grandResetResult == 2)
                        MessageBox.Show($"Required level: {Config.ResetLevel}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (grandResetResult == 3)
                        MessageBox.Show($"Required resets: {Config.GrandResetResets}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (grandResetResult == 4)
                        MessageBox.Show("Character not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (grandResetResult == 5)
                        MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    success = _database.GrandReset(name, _currentAccount);
                }

                if (success)
                {
                    MessageBox.Show("Grand Reset successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCharacters();
                }
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            try
            {
                string mainExePath = Config.MainExePath;
                if (!File.Exists(mainExePath))
                {
                    MessageBox.Show($"main.exe not found at: {mainExePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = mainExePath,
                    WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(mainExePath))
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start main.exe: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Simple dialog for adding points
    public class AddPointsDialog : Form
    {
        private ComboBox cmbStat;
        private NumericUpDown nudPoints;
        private Button btnOK;
        private Button btnCancel;

        public string SelectedStat => cmbStat.SelectedItem?.ToString();
        public int PointsToAdd => (int)nudPoints.Value;

        public AddPointsDialog()
        {
            this.Text = "Add Points";
            this.Size = new Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblStat = new Label { Text = "Stat:", Location = new Point(10, 15), Size = new Size(50, 20) };
            cmbStat = new ComboBox { Location = new Point(70, 12), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStat.Items.AddRange(new object[] { "Strength", "Dexterity", "Vitality", "Energy" });
            cmbStat.SelectedIndex = 0;

            Label lblPoints = new Label { Text = "Points:", Location = new Point(10, 45), Size = new Size(50, 20) };
            nudPoints = new NumericUpDown { Location = new Point(70, 42), Size = new Size(200, 25), Minimum = 1, Maximum = 65535, Value = 1 };

            btnOK = new Button { Text = "OK", Location = new Point(110, 80), Size = new Size(75, 25), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Location = new Point(195, 80), Size = new Size(75, 25), DialogResult = DialogResult.Cancel };

            this.Controls.AddRange(new Control[] { lblStat, cmbStat, lblPoints, nudPoints, btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}
