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
        private string _currentAccount;
        private DataTable _characters;
        private bool _isLoggedIn;

        public MainForm()
        {
            InitializeComponent();
            _database = new Database();
            _isLoggedIn = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!Config.Load())
            {
                Application.Exit();
                return;
            }

            if (!_database.Connect())
            {
                MessageBox.Show("Failed to connect to database. Launcher will work in offline mode.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            UpdateUI();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _database?.Disconnect();
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

            _characters = _database.GetCharacters(_currentAccount);
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

        private void btnAddPoints_Click(object sender, EventArgs e)
        {
            string name = GetSelectedCharacterName();
            if (string.IsNullOrEmpty(name)) return;

            if (_database.IsAccountOnline(_currentAccount))
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (AddPointsDialog dlg = new AddPointsDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (_database.AddPoints(name, dlg.SelectedStat, dlg.PointsToAdd))
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

            if (_database.IsAccountOnline(_currentAccount))
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Clear PK status for {name}?\nCost: {Config.ClearPKZenCost:N0} zen", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                if (_database.ClearPK(name))
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

            if (_database.IsAccountOnline(_currentAccount))
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Reset character {name}?\nRequired Level: {Config.ResetLevel}\nPoints awarded: {Config.ResetPoints}", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                if (_database.Reset(name, _currentAccount))
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

            if (_database.IsAccountOnline(_currentAccount))
            {
                MessageBox.Show("Account is online. Please disconnect first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Grand Reset character {name}?\nRequired Resets: {Config.GrandResetResets}\nRequired Level: {Config.ResetLevel}\nPoints awarded: {Config.GrandResetPoints}", "Confirm Grand Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                if (_database.GrandReset(name, _currentAccount))
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
