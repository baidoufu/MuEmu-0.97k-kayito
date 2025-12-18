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

            if (dgvCharacters.SelectedRows.Count == 0) return;

            if (CheckAccountOnline())
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (AddPointsDialog dlg = new AddPointsDialog(dgvCharacters.SelectedRows[0]))
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

    // Dialog for adding points with character stats display
    public class AddPointsDialog : Form
    {
        private ComboBox cmbStat;
        private NumericUpDown nudPoints;
        private Button btnOK;
        private Button btnCancel;

        // Read-only stat display labels
        private Label lblNameValue;
        private Label lblLevelValue;
        private Label lblResetsValue;
        private Label lblGrandResetsValue;
        private Label lblAvailablePointsValue;
        private Label lblStrengthValue;
        private Label lblDexterityValue;
        private Label lblVitalityValue;
        private Label lblEnergyValue;

        public string SelectedStat => cmbStat.SelectedItem?.ToString();
        public int PointsToAdd => (int)nudPoints.Value;

        public AddPointsDialog(DataGridViewRow characterRow)
        {
            this.Text = "Add Points - Character Stats";
            this.Size = new Size(400, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Character Info Section
            GroupBox grpCharInfo = new GroupBox { Text = "Character Info (Read-Only)", Location = new Point(10, 10), Size = new Size(365, 180) };

            int row = 20;
            int labelWidth = 100;
            int valueWidth = 100;
            int col1 = 15;
            int col2 = 125;
            int col3 = 195;
            int col4 = 280;
            int rowHeight = 25;

            // Row 1: Name
            grpCharInfo.Controls.Add(new Label { Text = "Name:", Location = new Point(col1, row), Size = new Size(labelWidth, 20), Font = new Font(this.Font, FontStyle.Bold) });
            lblNameValue = new Label { Text = GetCellValue(characterRow, "Name"), Location = new Point(col2, row), Size = new Size(240, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblNameValue);

            // Row 2: Level, Resets
            row += rowHeight;
            grpCharInfo.Controls.Add(new Label { Text = "Level:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblLevelValue = new Label { Text = GetCellValue(characterRow, "Level"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblLevelValue);

            grpCharInfo.Controls.Add(new Label { Text = "Resets:", Location = new Point(col3, row), Size = new Size(80, 20) });
            lblResetsValue = new Label { Text = GetCellValue(characterRow, "Resets"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblResetsValue);

            // Row 3: GrandResets, Available Points
            row += rowHeight;
            grpCharInfo.Controls.Add(new Label { Text = "Grand Resets:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblGrandResetsValue = new Label { Text = GetCellValue(characterRow, "GrandResets"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblGrandResetsValue);

            grpCharInfo.Controls.Add(new Label { Text = "Points:", Location = new Point(col3, row), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) });
            lblAvailablePointsValue = new Label { Text = GetCellValue(characterRow, "Points"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D, BackColor = Color.LightYellow };
            grpCharInfo.Controls.Add(lblAvailablePointsValue);

            // Row 4: Strength, Dexterity
            row += rowHeight + 10;
            grpCharInfo.Controls.Add(new Label { Text = "Strength:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblStrengthValue = new Label { Text = GetCellValue(characterRow, "Strength"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblStrengthValue);

            grpCharInfo.Controls.Add(new Label { Text = "Dexterity:", Location = new Point(col3, row), Size = new Size(80, 20) });
            lblDexterityValue = new Label { Text = GetCellValue(characterRow, "Dexterity"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblDexterityValue);

            // Row 5: Vitality, Energy
            row += rowHeight;
            grpCharInfo.Controls.Add(new Label { Text = "Vitality:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblVitalityValue = new Label { Text = GetCellValue(characterRow, "Vitality"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblVitalityValue);

            grpCharInfo.Controls.Add(new Label { Text = "Energy:", Location = new Point(col3, row), Size = new Size(80, 20) });
            lblEnergyValue = new Label { Text = GetCellValue(characterRow, "Energy"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblEnergyValue);

            this.Controls.Add(grpCharInfo);

            // Add Points Section
            GroupBox grpAddPoints = new GroupBox { Text = "Add Points", Location = new Point(10, 200), Size = new Size(365, 90) };

            Label lblStat = new Label { Text = "Stat:", Location = new Point(15, 25), Size = new Size(80, 20) };
            cmbStat = new ComboBox { Location = new Point(100, 22), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStat.Items.AddRange(new object[] { "Strength", "Dexterity", "Vitality", "Energy" });
            cmbStat.SelectedIndex = 0;

            Label lblPoints = new Label { Text = "Points:", Location = new Point(15, 55), Size = new Size(80, 20) };
            nudPoints = new NumericUpDown { Location = new Point(100, 52), Size = new Size(120, 25), Minimum = 1, Maximum = 65535, Value = 1 };

            // Set max points based on available points
            int availablePoints = 0;
            if (int.TryParse(GetCellValue(characterRow, "Points"), out availablePoints))
            {
                nudPoints.Maximum = Math.Max(1, availablePoints);
                nudPoints.Value = Math.Min(1, availablePoints);
            }

            grpAddPoints.Controls.AddRange(new Control[] { lblStat, cmbStat, lblPoints, nudPoints });
            this.Controls.Add(grpAddPoints);

            // Buttons
            btnOK = new Button { Text = "Add Points", Location = new Point(180, 300), Size = new Size(90, 30), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Location = new Point(280, 300), Size = new Size(90, 30), DialogResult = DialogResult.Cancel };

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private string GetCellValue(DataGridViewRow row, string columnName)
        {
            try
            {
                if (row?.Cells[columnName]?.Value != null)
                    return row.Cells[columnName].Value.ToString();
            }
            catch { }
            return "N/A";
        }
    }
}
