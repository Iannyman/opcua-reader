<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainForm
    Inherits MaterialSkin.Controls.MaterialForm

    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.tabMain = New MaterialSkin.Controls.MaterialTabControl()
        Me.tabPageOverview = New System.Windows.Forms.TabPage()
        Me.dgvOverview = New System.Windows.Forms.DataGridView()
        Me.pnlToolbar = New System.Windows.Forms.Panel()
        Me.btnDisconnect = New MaterialSkin.Controls.MaterialButton()
        Me.btnConnect = New MaterialSkin.Controls.MaterialButton()
        Me.tabSelector = New MaterialSkin.Controls.MaterialTabSelector()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.lblSqlStatus = New System.Windows.Forms.Label()
        Me.pnlStatus = New System.Windows.Forms.Panel()
        Me.tabMain.SuspendLayout()
        Me.tabPageOverview.SuspendLayout()
        CType(Me.dgvOverview, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.pnlToolbar.SuspendLayout()
        Me.pnlStatus.SuspendLayout()
        Me.SuspendLayout()
        '
        'tabMain
        '
        Me.tabMain.Controls.Add(Me.tabPageOverview)
        Me.tabMain.Depth = 0
        Me.tabMain.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabMain.Location = New System.Drawing.Point(3, 100)
        Me.tabMain.MouseState = MaterialSkin.MouseState.HOVER
        Me.tabMain.Multiline = True
        Me.tabMain.Name = "tabMain"
        Me.tabMain.SelectedIndex = 0
        Me.tabMain.Size = New System.Drawing.Size(1058, 426)
        Me.tabMain.TabIndex = 0
        '
        'tabPageOverview
        '
        Me.tabPageOverview.BackColor = System.Drawing.Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.tabPageOverview.Controls.Add(Me.dgvOverview)
        Me.tabPageOverview.Controls.Add(Me.pnlToolbar)
        Me.tabPageOverview.Location = New System.Drawing.Point(4, 22)
        Me.tabPageOverview.Name = "tabPageOverview"
        Me.tabPageOverview.Padding = New System.Windows.Forms.Padding(3)
        Me.tabPageOverview.Size = New System.Drawing.Size(1050, 400)
        Me.tabPageOverview.TabIndex = 0
        Me.tabPageOverview.Text = "Overview"
        '
        'dgvOverview
        '
        Me.dgvOverview.AllowUserToAddRows = False
        Me.dgvOverview.AllowUserToDeleteRows = False
        Me.dgvOverview.AllowUserToResizeRows = False
        Me.dgvOverview.BackgroundColor = System.Drawing.Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.dgvOverview.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.dgvOverview.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal
        Me.dgvOverview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        Me.dgvOverview.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvOverview.GridColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.dgvOverview.Location = New System.Drawing.Point(3, 59)
        Me.dgvOverview.MultiSelect = False
        Me.dgvOverview.Name = "dgvOverview"
        Me.dgvOverview.ReadOnly = True
        Me.dgvOverview.RowHeadersVisible = False
        Me.dgvOverview.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvOverview.Size = New System.Drawing.Size(1044, 338)
        Me.dgvOverview.TabIndex = 1
        '
        'pnlToolbar
        '
        Me.pnlToolbar.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.pnlToolbar.Controls.Add(Me.btnDisconnect)
        Me.pnlToolbar.Controls.Add(Me.btnConnect)
        Me.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlToolbar.Location = New System.Drawing.Point(3, 3)
        Me.pnlToolbar.Name = "pnlToolbar"
        Me.pnlToolbar.Size = New System.Drawing.Size(1044, 56)
        Me.pnlToolbar.TabIndex = 0
        '
        'btnDisconnect
        '
        Me.btnDisconnect.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.btnDisconnect.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.[Default]
        Me.btnDisconnect.Depth = 0
        Me.btnDisconnect.HighEmphasis = True
        Me.btnDisconnect.Icon = Nothing
        Me.btnDisconnect.Location = New System.Drawing.Point(151, 10)
        Me.btnDisconnect.Margin = New System.Windows.Forms.Padding(4, 6, 4, 6)
        Me.btnDisconnect.MouseState = MaterialSkin.MouseState.HOVER
        Me.btnDisconnect.Name = "btnDisconnect"
        Me.btnDisconnect.NoAccentTextColor = System.Drawing.Color.Empty
        Me.btnDisconnect.Size = New System.Drawing.Size(141, 36)
        Me.btnDisconnect.TabIndex = 1
        Me.btnDisconnect.Text = "Disconnect All"
        Me.btnDisconnect.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained
        Me.btnDisconnect.UseAccentColor = False
        '
        'btnConnect
        '
        Me.btnConnect.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.btnConnect.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.[Default]
        Me.btnConnect.Depth = 0
        Me.btnConnect.HighEmphasis = True
        Me.btnConnect.Icon = Nothing
        Me.btnConnect.Location = New System.Drawing.Point(12, 10)
        Me.btnConnect.Margin = New System.Windows.Forms.Padding(4, 6, 4, 6)
        Me.btnConnect.MouseState = MaterialSkin.MouseState.HOVER
        Me.btnConnect.Name = "btnConnect"
        Me.btnConnect.NoAccentTextColor = System.Drawing.Color.Empty
        Me.btnConnect.Size = New System.Drawing.Size(119, 36)
        Me.btnConnect.TabIndex = 0
        Me.btnConnect.Text = "Connect All"
        Me.btnConnect.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained
        Me.btnConnect.UseAccentColor = False
        '
        'tabSelector
        '
        Me.tabSelector.BaseTabControl = Me.tabMain
        Me.tabSelector.CharacterCasing = MaterialSkin.Controls.MaterialTabSelector.CustomCharacterCasing.Normal
        Me.tabSelector.Depth = 0
        Me.tabSelector.Dock = System.Windows.Forms.DockStyle.Top
        Me.tabSelector.Font = New System.Drawing.Font("Roboto", 14.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel)
        Me.tabSelector.Location = New System.Drawing.Point(3, 64)
        Me.tabSelector.MouseState = MaterialSkin.MouseState.HOVER
        Me.tabSelector.Name = "tabSelector"
        Me.tabSelector.Size = New System.Drawing.Size(1058, 36)
        Me.tabSelector.TabIndex = 3
        Me.tabSelector.Text = "tabSelector"
        '
        'lblStatus
        '
        Me.lblStatus.AutoSize = True
        Me.lblStatus.BackColor = System.Drawing.Color.Transparent
        Me.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lblStatus.Font = New System.Drawing.Font("Segoe UI", 8.5!)
        Me.lblStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(130, Byte), Integer), CType(CType(140, Byte), Integer), CType(CType(155, Byte), Integer))
        Me.lblStatus.Location = New System.Drawing.Point(14, 0)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Padding = New System.Windows.Forms.Padding(0, 5, 0, 0)
        Me.lblStatus.Size = New System.Drawing.Size(39, 20)
        Me.lblStatus.TabIndex = 0
        Me.lblStatus.Text = "Ready"
        '
        'lblSqlStatus
        '
        Me.lblSqlStatus.AutoSize = True
        Me.lblSqlStatus.BackColor = System.Drawing.Color.Transparent
        Me.lblSqlStatus.Dock = System.Windows.Forms.DockStyle.Right
        Me.lblSqlStatus.Font = New System.Drawing.Font("Segoe UI Semibold", 8.5!, System.Drawing.FontStyle.Bold)
        Me.lblSqlStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(82, Byte), Integer), CType(CType(82, Byte), Integer))
        Me.lblSqlStatus.Location = New System.Drawing.Point(962, 0)
        Me.lblSqlStatus.Name = "lblSqlStatus"
        Me.lblSqlStatus.Padding = New System.Windows.Forms.Padding(0, 5, 14, 0)
        Me.lblSqlStatus.Size = New System.Drawing.Size(96, 20)
        Me.lblSqlStatus.TabIndex = 1
        Me.lblSqlStatus.Text = "SQL: Testing..."
        '
        'pnlStatus
        '
        Me.pnlStatus.BackColor = System.Drawing.Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.pnlStatus.Controls.Add(Me.lblSqlStatus)
        Me.pnlStatus.Controls.Add(Me.lblStatus)
        Me.pnlStatus.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.pnlStatus.Location = New System.Drawing.Point(3, 526)
        Me.pnlStatus.Name = "pnlStatus"
        Me.pnlStatus.Padding = New System.Windows.Forms.Padding(14, 0, 0, 0)
        Me.pnlStatus.Size = New System.Drawing.Size(1058, 26)
        Me.pnlStatus.TabIndex = 4
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1064, 555)
        Me.Controls.Add(Me.tabMain)
        Me.Controls.Add(Me.tabSelector)
        Me.Controls.Add(Me.pnlStatus)
        Me.MinimumSize = New System.Drawing.Size(640, 400)
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "OPC UA <---> SQL"
        Me.tabMain.ResumeLayout(False)
        Me.tabPageOverview.ResumeLayout(False)
        CType(Me.dgvOverview, System.ComponentModel.ISupportInitialize).EndInit()
        Me.pnlToolbar.ResumeLayout(False)
        Me.pnlToolbar.PerformLayout()
        Me.pnlStatus.ResumeLayout(False)
        Me.pnlStatus.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents tabMain As MaterialSkin.Controls.MaterialTabControl
    Friend WithEvents tabSelector As MaterialSkin.Controls.MaterialTabSelector
    Friend WithEvents tabPageOverview As System.Windows.Forms.TabPage
    Friend WithEvents pnlToolbar As System.Windows.Forms.Panel
    Friend WithEvents dgvOverview As System.Windows.Forms.DataGridView
    Friend WithEvents btnConnect As MaterialSkin.Controls.MaterialButton
    Friend WithEvents btnDisconnect As MaterialSkin.Controls.MaterialButton
    Friend WithEvents pnlStatus As System.Windows.Forms.Panel
    Friend WithEvents lblStatus As System.Windows.Forms.Label
    Friend WithEvents lblSqlStatus As System.Windows.Forms.Label

End Class
