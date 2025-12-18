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
                    MessageBox.Show("无法连接到服务器。启动器将以离线模式运行。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // Database mode (legacy)
                _database = new Database();
                if (!_database.Connect())
                {
                    MessageBox.Show("无法连接到数据库。启动器将以离线模式运行。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            lblStatus.Text = _isLoggedIn ? $"已登录: {_currentAccount}" : "未登录";
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string account = txtAccount.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入账号和密码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        MessageBox.Show("登录成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case 0: // Invalid password
                        MessageBox.Show("密码错误。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case 2: // Account not found
                        MessageBox.Show("账号未找到。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    case 3: // Account online
                        MessageBox.Show("该账号当前在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    default:
                        MessageBox.Show("登录失败。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
            else
            {
                // Database mode login (legacy)
                if (_database.IsAccountOnline(account))
                {
                    MessageBox.Show("该账号当前在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_database.ValidateLogin(account, password))
                {
                    _currentAccount = account;
                    _isLoggedIn = true;
                    LoadCharacters();
                    UpdateUI();
                    MessageBox.Show("登录成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("账号或密码错误。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void btnRegister_Click(object sender, EventArgs e)
        {
            if (!_useServerMode)
            {
                MessageBox.Show("仅在服务器模式下可注册。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_serverConnection.IsConnected)
            {
                MessageBox.Show("未连接到服务器。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (RegisterDialog dlg = new RegisterDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    int result = _serverConnection.RegisterAccount(dlg.Account, dlg.Password, dlg.PersonalCode);
                    switch (result)
                    {
                        case 1:
                            MessageBox.Show("账号注册成功！现在可以登录。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtAccount.Text = dlg.Account;
                            txtPassword.Focus();
                            break;
                        case 0:
                            MessageBox.Show("账号已存在。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            break;
                        case 2:
                            MessageBox.Show("输入无效。请检查账号名和密码。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            break;
                        case 3:
                            MessageBox.Show("服务器错误。请稍后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        default:
                            MessageBox.Show("注册失败。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                    }
                }
            }
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

            // Ensure columns auto-size to fill and then adjust header text and widths
            dgvCharacters.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCharacters.AutoResizeColumns();

            // Set header text from DataTable column captions (Chinese) while keeping internal column names
            try
            {
                foreach (DataGridViewColumn col in dgvCharacters.Columns)
                {
                    if (_characters != null && _characters.Columns.Contains(col.Name))
                    {
                        // Use Caption if provided, otherwise fall back to column name
                        string caption = _characters.Columns[col.Name].Caption;
                        col.HeaderText = string.IsNullOrEmpty(caption) ? col.Name : caption;

                        // Adjust column fill weight to increase Name column width
                        switch (col.Name)
                        {
                            case "Name":
                                col.FillWeight = 200; // make name column wider
                                col.MinimumWidth = 120;
                                break;
                            case "Level":
                            case "Resets":
                            case "GrandResets":
                            case "Points":
                            case "PKLevel":
                            case "Money":
                                col.FillWeight = 80;
                                col.MinimumWidth = 60;
                                break;
                            default:
                                col.FillWeight = 70;
                                col.MinimumWidth = 50;
                                break;
                        }
                    }
                }
            }
            catch
            {
                // ignore if something goes wrong setting headers
            }

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
                MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                            MessageBox.Show("可用点数不足。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        else if (result == 3)
                            MessageBox.Show("未找到角色。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else if (result == 4)
                            MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        success = _database.AddPoints(name, dlg.SelectedStat, dlg.PointsToAdd);
                    }

                    if (success)
                    {
                        MessageBox.Show($"已为 {dlg.SelectedStat} 添加 {dlg.PointsToAdd} 点。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"是否清除 {name} 的 PK 状态？\n费用：{Config.ClearPKZenCost:N0} 金币", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                bool success;
                if (_useServerMode)
                {
                    int clearResult = _serverConnection.ClearPK(_currentAccount, name, Config.ClearPKZenCost);
                    success = clearResult == 1;
                    if (clearResult == 2)
                        MessageBox.Show($"金钱不足。所需：{Config.ClearPKZenCost:N0}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (clearResult == 3)
                        MessageBox.Show("未找到角色。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (clearResult == 4)
                        MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    success = _database.ClearPK(name);
                }

                if (success)
                {
                    MessageBox.Show("PK 状态已成功清除。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"是否重置角色 {name}？\n所需等级：{Config.ResetLevel}\n奖励点数：{Config.ResetPoints}", "确认重置", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                bool success;
                if (_useServerMode)
                {
                    int resetResult = _serverConnection.Reset(_currentAccount, name, Config.ResetLevel, Config.ResetPoints);
                    success = resetResult == 1;
                    if (resetResult == 2)
                        MessageBox.Show($"所需等级：{Config.ResetLevel}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (resetResult == 3)
                        MessageBox.Show("未找到角色。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (resetResult == 4)
                        MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    success = _database.Reset(name, _currentAccount);
                }

                if (success)
                {
                    MessageBox.Show("重置成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"是否转世角色 {name}？\n所需转生次数：{Config.GrandResetResets}\n所需等级：{Config.ResetLevel}\n奖励点数：{Config.GrandResetPoints}", "确认转世", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                bool success;
                if (_useServerMode)
                {
                    int grandResetResult = _serverConnection.GrandReset(_currentAccount, name, Config.ResetLevel, Config.GrandResetResets, Config.GrandResetPoints);
                    success = grandResetResult == 1;
                    if (grandResetResult == 2)
                        MessageBox.Show($"所需等级：{Config.ResetLevel}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (grandResetResult == 3)
                        MessageBox.Show($"所需转生次数：{Config.GrandResetResets}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else if (grandResetResult == 4)
                        MessageBox.Show("未找到角色。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (grandResetResult == 5)
                        MessageBox.Show("账号在线。请先断开连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    success = _database.GrandReset(name, _currentAccount);
                }

                if (success)
                {
                    MessageBox.Show("转世成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    MessageBox.Show($"未找到 main.exe：{mainExePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show($"启动 main.exe 失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            this.Text = "加点 - 角色属性";
            this.Size = new Size(400, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Character Info Section
            GroupBox grpCharInfo = new GroupBox { Text = "角色信息（只读）", Location = new Point(10, 10), Size = new Size(365, 180) };

            int row = 20;
            int labelWidth = 100;
            int valueWidth = 100;
            int col1 = 15;
            int col2 = 125;
            int col3 = 195;
            int col4 = 280;
            int rowHeight = 25;

            // Row 1: Name
            grpCharInfo.Controls.Add(new Label { Text = "姓名:", Location = new Point(col1, row), Size = new Size(labelWidth, 20), Font = new Font(this.Font, FontStyle.Bold) });
            lblNameValue = new Label { Text = GetCellValue(characterRow, "Name"), Location = new Point(col2, row), Size = new Size(240, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblNameValue);

            // Row 2: Level, Resets
            row += rowHeight;
            grpCharInfo.Controls.Add(new Label { Text = "等级:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblLevelValue = new Label { Text = GetCellValue(characterRow, "Level"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblLevelValue);

            grpCharInfo.Controls.Add(new Label { Text = "转生:", Location = new Point(col3, row), Size = new Size(80, 20) });
            lblResetsValue = new Label { Text = GetCellValue(characterRow, "Resets"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblResetsValue);

            // Row 3: GrandResets, Available Points
            row += rowHeight;
            grpCharInfo.Controls.Add(new Label { Text = "转世:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblGrandResetsValue = new Label { Text = GetCellValue(characterRow, "GrandResets"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblGrandResetsValue);

            grpCharInfo.Controls.Add(new Label { Text = "点数:", Location = new Point(col3, row), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) });
            lblAvailablePointsValue = new Label { Text = GetCellValue(characterRow, "Points"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D, BackColor = Color.LightYellow };
            grpCharInfo.Controls.Add(lblAvailablePointsValue);

            // Row 4: Strength, Dexterity
            row += rowHeight + 10;
            grpCharInfo.Controls.Add(new Label { Text = "力量:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblStrengthValue = new Label { Text = GetCellValue(characterRow, "Strength"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblStrengthValue);

            grpCharInfo.Controls.Add(new Label { Text = "敏捷:", Location = new Point(col3, row), Size = new Size(80, 20) });
            lblDexterityValue = new Label { Text = GetCellValue(characterRow, "Dexterity"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblDexterityValue);

            // Row 5: Vitality, Energy
            row += rowHeight;
            grpCharInfo.Controls.Add(new Label { Text = "体力:", Location = new Point(col1, row), Size = new Size(labelWidth, 20) });
            lblVitalityValue = new Label { Text = GetCellValue(characterRow, "Vitality"), Location = new Point(col2, row), Size = new Size(60, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblVitalityValue);

            grpCharInfo.Controls.Add(new Label { Text = "智力:", Location = new Point(col3, row), Size = new Size(80, 20) });
            lblEnergyValue = new Label { Text = GetCellValue(characterRow, "Energy"), Location = new Point(col4, row), Size = new Size(70, 20), BorderStyle = BorderStyle.Fixed3D };
            grpCharInfo.Controls.Add(lblEnergyValue);

            this.Controls.Add(grpCharInfo);

            // Add Points Section
            GroupBox grpAddPoints = new GroupBox { Text = "加点", Location = new Point(10, 200), Size = new Size(365, 90) };

            Label lblStat = new Label { Text = "属性:", Location = new Point(15, 25), Size = new Size(80, 20) };
            cmbStat = new ComboBox { Location = new Point(100, 22), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStat.Items.AddRange(new object[] { "力量", "敏捷", "体力", "智力" });
            cmbStat.SelectedIndex = 0;

            Label lblPoints = new Label { Text = "点数:", Location = new Point(15, 55), Size = new Size(80, 20) };
            nudPoints = new NumericUpDown { Location = new Point(100, 52), Size = new Size(120, 25), Minimum = 1, Maximum = 65535, Value = 1 };

            // Set max points based on available points
            int availablePoints = 0;
            if (int.TryParse(GetCellValue(characterRow, "Points"), out availablePoints) && availablePoints > 0)
            {
                nudPoints.Maximum = availablePoints;
            }

            grpAddPoints.Controls.AddRange(new Control[] { lblStat, cmbStat, lblPoints, nudPoints });
            this.Controls.Add(grpAddPoints);

            // Buttons
            btnOK = new Button { Text = "加点", Location = new Point(180, 300), Size = new Size(90, 30), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "取消", Location = new Point(280, 300), Size = new Size(90, 30), DialogResult = DialogResult.Cancel };

            // Disable OK button if no points available
            btnOK.Enabled = availablePoints > 0;

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
            catch (ArgumentException)
            {
                // Column not found, return default
            }
            return "N/A";
        }
    }

    // Dialog for registering a new account
    public class RegisterDialog : Form
    {
        private TextBox txtAccount;
        private TextBox txtPassword;
        private TextBox txtConfirmPassword;
        private TextBox txtPersonalCode;
        private Button btnOK;
        private Button btnCancel;
        private Label lblValidation;

        public string Account => txtAccount.Text.Trim();
        public string Password => txtPassword.Text;
        public string PersonalCode => txtPersonalCode.Text.Trim();

        public RegisterDialog()
        {
            this.Text = "注册账号";
            this.Size = new Size(350, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int row = 15;
            int labelWidth = 120;
            int inputX = 130;
            int inputWidth = 190;
            int rowHeight = 35;

            // Account
            this.Controls.Add(new Label { Text = "账号:", Location = new Point(10, row + 3), Size = new Size(labelWidth, 20) });
            txtAccount = new TextBox { Location = new Point(inputX, row), Size = new Size(inputWidth, 23), MaxLength = 10 };
            this.Controls.Add(txtAccount);

            // Password
            row += rowHeight;
            this.Controls.Add(new Label { Text = "密码:", Location = new Point(10, row + 3), Size = new Size(labelWidth, 20) });
            txtPassword = new TextBox { Location = new Point(inputX, row), Size = new Size(inputWidth, 23), MaxLength = 10, PasswordChar = '*' };
            this.Controls.Add(txtPassword);

            // Confirm Password
            row += rowHeight;
            this.Controls.Add(new Label { Text = "确认密码:", Location = new Point(10, row + 3), Size = new Size(labelWidth, 20) });
            txtConfirmPassword = new TextBox { Location = new Point(inputX, row), Size = new Size(inputWidth, 23), MaxLength = 10, PasswordChar = '*' };
            this.Controls.Add(txtConfirmPassword);

            // Personal Code (Name)
            row += rowHeight;
            this.Controls.Add(new Label { Text = "姓名:", Location = new Point(10, row + 3), Size = new Size(labelWidth, 20) });
            txtPersonalCode = new TextBox { Location = new Point(inputX, row), Size = new Size(inputWidth, 23), MaxLength = 10 };
            this.Controls.Add(txtPersonalCode);

            // Validation message
            row += rowHeight;
            lblValidation = new Label { Text = "4-10 个字符，仅允许字母/数字/下划线", Location = new Point(10, row), Size = new Size(320, 20), ForeColor = Color.Gray, Font = new Font(this.Font.FontFamily, 8) };
            this.Controls.Add(lblValidation);

            // Buttons
            row += 25;
            btnOK = new Button { Text = "注册", Location = new Point(130, row), Size = new Size(100, 30) };
            btnOK.Click += BtnOK_Click;
            btnCancel = new Button { Text = "取消", Location = new Point(235, row), Size = new Size(90, 30), DialogResult = DialogResult.Cancel };

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });
            this.CancelButton = btnCancel;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate account
            if (!ValidateInput(txtAccount.Text, 4, 10))
            {
                ShowError("账号必须为 4-10 个字符（仅允许字母、数字、下划线）");
                txtAccount.Focus();
                return;
            }

            // Validate password
            if (txtPassword.Text.Length < 4 || txtPassword.Text.Length > 10)
            {
                ShowError("密码必须为 4-10 个字符");
                txtPassword.Focus();
                return;
            }

            // Validate password confirmation
            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                ShowError("两次输入的密码不一致");
                txtConfirmPassword.Focus();
                return;
            }

            // Validate personal code
            if (!ValidateInput(txtPersonalCode.Text, 4, 10))
            {
                ShowError("姓名必须为 4-10 个字符（仅允许字母、数字、下划线）");
                txtPersonalCode.Focus();
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool ValidateInput(string input, int minLength, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            if (input.Length < minLength || input.Length > maxLength)
                return false;

            // Only allow letters, numbers, and underscore
            foreach (char c in input)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            lblValidation.Text = message;
            lblValidation.ForeColor = Color.Red;
        }
    }
}
