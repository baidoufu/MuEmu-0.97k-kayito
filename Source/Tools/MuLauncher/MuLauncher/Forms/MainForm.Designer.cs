namespace MuLauncher.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlLogin = new System.Windows.Forms.Panel();
            this.btnRegister = new System.Windows.Forms.Button();
            this.btnLogin = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtAccount = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.lblAccount = new System.Windows.Forms.Label();
            this.pnlCharacterManagement = new System.Windows.Forms.Panel();
            this.dgvCharacters = new System.Windows.Forms.DataGridView();
            this.btnAddPoints = new System.Windows.Forms.Button();
            this.btnClearPK = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnGrandReset = new System.Windows.Forms.Button();
            this.btnLaunch = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlLogin.SuspendLayout();
            this.pnlCharacterManagement.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCharacters)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlLogin
            // 
            this.pnlLogin.Controls.Add(this.btnRegister);
            this.pnlLogin.Controls.Add(this.btnLogin);
            this.pnlLogin.Controls.Add(this.txtPassword);
            this.pnlLogin.Controls.Add(this.txtAccount);
            this.pnlLogin.Controls.Add(this.lblPassword);
            this.pnlLogin.Controls.Add(this.lblAccount);
            this.pnlLogin.Location = new System.Drawing.Point(10, 40);
            this.pnlLogin.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pnlLogin.Name = "pnlLogin";
            this.pnlLogin.Size = new System.Drawing.Size(223, 120);
            this.pnlLogin.TabIndex = 0;
            // 
            // btnRegister
            // 
            this.btnRegister.Location = new System.Drawing.Point(60, 94);
            this.btnRegister.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(154, 22);
            this.btnRegister.TabIndex = 5;
            this.btnRegister.Text = "注册";
            this.btnRegister.UseVisualStyleBackColor = true;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(60, 68);
            this.btnLogin.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(154, 22);
            this.btnLogin.TabIndex = 4;
            this.btnLogin.Text = "登录";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(60, 40);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(155, 21);
            this.txtPassword.TabIndex = 3;
            // 
            // txtAccount
            // 
            this.txtAccount.Location = new System.Drawing.Point(60, 12);
            this.txtAccount.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtAccount.Name = "txtAccount";
            this.txtAccount.Size = new System.Drawing.Size(155, 21);
            this.txtAccount.TabIndex = 2;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(4, 42);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(59, 12);
            this.lblPassword.TabIndex = 1;
            this.lblPassword.Text = "密码:";
            // 
            // lblAccount
            // 
            this.lblAccount.AutoSize = true;
            this.lblAccount.Location = new System.Drawing.Point(4, 14);
            this.lblAccount.Name = "lblAccount";
            this.lblAccount.Size = new System.Drawing.Size(53, 12);
            this.lblAccount.TabIndex = 0;
            this.lblAccount.Text = "账号:";
            // 
            // pnlCharacterManagement
            // 
            this.pnlCharacterManagement.Controls.Add(this.dgvCharacters);
            this.pnlCharacterManagement.Controls.Add(this.btnAddPoints);
            this.pnlCharacterManagement.Controls.Add(this.btnClearPK);
            this.pnlCharacterManagement.Controls.Add(this.btnReset);
            this.pnlCharacterManagement.Controls.Add(this.btnGrandReset);
            this.pnlCharacterManagement.Enabled = false;
            this.pnlCharacterManagement.Location = new System.Drawing.Point(10, 172);
            this.pnlCharacterManagement.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pnlCharacterManagement.Name = "pnlCharacterManagement";
            this.pnlCharacterManagement.Size = new System.Drawing.Size(566, 176);
            this.pnlCharacterManagement.TabIndex = 1;
            // 
            // dgvCharacters
            // 
            this.dgvCharacters.AllowUserToAddRows = false;
            this.dgvCharacters.AllowUserToDeleteRows = false;
            this.dgvCharacters.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvCharacters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCharacters.Location = new System.Drawing.Point(4, 4);
            this.dgvCharacters.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dgvCharacters.MultiSelect = false;
            this.dgvCharacters.Name = "dgvCharacters";
            this.dgvCharacters.ReadOnly = true;
            this.dgvCharacters.RowHeadersWidth = 20;
            this.dgvCharacters.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCharacters.Size = new System.Drawing.Size(557, 136);
            this.dgvCharacters.TabIndex = 0;
            this.dgvCharacters.SelectionChanged += new System.EventHandler(this.dgvCharacters_SelectionChanged);
            // 
            // btnAddPoints
            // 
            this.btnAddPoints.Enabled = false;
            this.btnAddPoints.Location = new System.Drawing.Point(4, 144);
            this.btnAddPoints.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnAddPoints.Name = "btnAddPoints";
            this.btnAddPoints.Size = new System.Drawing.Size(129, 24);
            this.btnAddPoints.TabIndex = 1;
            this.btnAddPoints.Text = "加点";
            this.btnAddPoints.UseVisualStyleBackColor = true;
            this.btnAddPoints.Click += new System.EventHandler(this.btnAddPoints_Click);
            // 
            // btnClearPK
            // 
            this.btnClearPK.Enabled = false;
            this.btnClearPK.Location = new System.Drawing.Point(141, 144);
            this.btnClearPK.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnClearPK.Name = "btnClearPK";
            this.btnClearPK.Size = new System.Drawing.Size(129, 24);
            this.btnClearPK.TabIndex = 2;
            this.btnClearPK.Text = "洗红";
            this.btnClearPK.UseVisualStyleBackColor = true;
            this.btnClearPK.Click += new System.EventHandler(this.btnClearPK_Click);
            // 
            // btnReset
            // 
            this.btnReset.Enabled = false;
            this.btnReset.Location = new System.Drawing.Point(279, 144);
            this.btnReset.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(129, 24);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "转生";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnGrandReset
            // 
            this.btnGrandReset.Enabled = false;
            this.btnGrandReset.Location = new System.Drawing.Point(416, 144);
            this.btnGrandReset.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnGrandReset.Name = "btnGrandReset";
            this.btnGrandReset.Size = new System.Drawing.Size(137, 24);
            this.btnGrandReset.TabIndex = 4;
            this.btnGrandReset.Text = "转世";
            this.btnGrandReset.UseVisualStyleBackColor = true;
            this.btnGrandReset.Click += new System.EventHandler(this.btnGrandReset_Click);
            // 
            // btnLaunch
            // 
            this.btnLaunch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnLaunch.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnLaunch.ForeColor = System.Drawing.Color.White;
            this.btnLaunch.Location = new System.Drawing.Point(240, 56);
            this.btnLaunch.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(171, 48);
            this.btnLaunch.TabIndex = 2;
            this.btnLaunch.Text = "开始游戏";
            this.btnLaunch.UseVisualStyleBackColor = false;
            this.btnLaunch.Click += new System.EventHandler(this.btnLaunch_Click);
            // 
            // btnLogout
            // 
            this.btnLogout.Location = new System.Drawing.Point(430, 56);
            this.btnLogout.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(69, 24);
            this.btnLogout.TabIndex = 3;
            this.btnLogout.Text = "注销";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Visible = false;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(240, 116);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(83, 12);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "未登录";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(214, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(150, 30);
            this.lblTitle.TabIndex = 5;
            this.lblTitle.Text = "MU 启动器";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(586, 360);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnLogout);
            this.Controls.Add(this.btnLaunch);
            this.Controls.Add(this.pnlCharacterManagement);
            this.Controls.Add(this.pnlLogin);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MU 启动器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.pnlLogin.ResumeLayout(false);
            this.pnlLogin.PerformLayout();
            this.pnlCharacterManagement.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCharacters)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlLogin;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtAccount;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label lblAccount;
        private System.Windows.Forms.Panel pnlCharacterManagement;
        private System.Windows.Forms.DataGridView dgvCharacters;
        private System.Windows.Forms.Button btnAddPoints;
        private System.Windows.Forms.Button btnClearPK;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnGrandReset;
        private System.Windows.Forms.Button btnLaunch;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblTitle;
    }
}
