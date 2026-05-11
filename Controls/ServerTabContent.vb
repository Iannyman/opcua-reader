<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ServerTabContent
    Inherits System.Windows.Forms.UserControl

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
        Me.pnlToolbar = New System.Windows.Forms.Panel()
        Me.btnClear = New MaterialSkin.Controls.MaterialButton()
        Me.btnReconnect = New MaterialSkin.Controls.MaterialButton()
        Me.btnDisconnect = New MaterialSkin.Controls.MaterialButton()
        Me.btnConnect = New MaterialSkin.Controls.MaterialButton()
        Me.logList = New ThemedListBox()
        Me.pnlToolbar.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlToolbar
        '
        Me.pnlToolbar.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.pnlToolbar.Controls.Add(Me.btnClear)
        Me.pnlToolbar.Controls.Add(Me.btnReconnect)
        Me.pnlToolbar.Controls.Add(Me.btnDisconnect)
        Me.pnlToolbar.Controls.Add(Me.btnConnect)
        Me.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlToolbar.Location = New System.Drawing.Point(0, 0)
        Me.pnlToolbar.Name = "pnlToolbar"
        Me.pnlToolbar.Size = New System.Drawing.Size(600, 56)
        Me.pnlToolbar.TabIndex = 0
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
        Me.btnConnect.Text = "Connect"
        Me.btnConnect.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained
        Me.btnConnect.UseAccentColor = False
        '
        'btnDisconnect
        '
        Me.btnDisconnect.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.btnDisconnect.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.[Default]
        Me.btnDisconnect.Depth = 0
        Me.btnDisconnect.HighEmphasis = True
        Me.btnDisconnect.Icon = Nothing
        Me.btnDisconnect.Location = New System.Drawing.Point(130, 10)
        Me.btnDisconnect.Margin = New System.Windows.Forms.Padding(4, 6, 4, 6)
        Me.btnDisconnect.MouseState = MaterialSkin.MouseState.HOVER
        Me.btnDisconnect.Name = "btnDisconnect"
        Me.btnDisconnect.NoAccentTextColor = System.Drawing.Color.Empty
        Me.btnDisconnect.Size = New System.Drawing.Size(141, 36)
        Me.btnDisconnect.TabIndex = 1
        Me.btnDisconnect.Text = "Disconnect"
        Me.btnDisconnect.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained
        Me.btnDisconnect.UseAccentColor = False
        '
        'btnReconnect
        '
        Me.btnReconnect.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.btnReconnect.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.[Default]
        Me.btnReconnect.Depth = 0
        Me.btnReconnect.HighEmphasis = True
        Me.btnReconnect.Icon = Nothing
        Me.btnReconnect.Location = New System.Drawing.Point(258, 10)
        Me.btnReconnect.Margin = New System.Windows.Forms.Padding(4, 6, 4, 6)
        Me.btnReconnect.MouseState = MaterialSkin.MouseState.HOVER
        Me.btnReconnect.Name = "btnReconnect"
        Me.btnReconnect.NoAccentTextColor = System.Drawing.Color.Empty
        Me.btnReconnect.Size = New System.Drawing.Size(141, 36)
        Me.btnReconnect.TabIndex = 2
        Me.btnReconnect.Text = "Reconnect"
        Me.btnReconnect.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained
        Me.btnReconnect.UseAccentColor = False
        '
        'btnClear
        '
        Me.btnClear.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.btnClear.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.[Default]
        Me.btnClear.Depth = 0
        Me.btnClear.HighEmphasis = True
        Me.btnClear.Icon = Nothing
        Me.btnClear.Location = New System.Drawing.Point(386, 10)
        Me.btnClear.Margin = New System.Windows.Forms.Padding(4, 6, 4, 6)
        Me.btnClear.MouseState = MaterialSkin.MouseState.HOVER
        Me.btnClear.Name = "btnClear"
        Me.btnClear.NoAccentTextColor = System.Drawing.Color.Empty
        Me.btnClear.Size = New System.Drawing.Size(136, 36)
        Me.btnClear.TabIndex = 3
        Me.btnClear.Text = "Clear Log"
        Me.btnClear.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained
        Me.btnClear.UseAccentColor = False
        '
        'logList
        '
        Me.logList.BackColor = System.Drawing.Color.FromArgb(CType(CType(18, Byte), Integer), CType(CType(18, Byte), Integer), CType(CType(18, Byte), Integer))
        Me.logList.Dock = System.Windows.Forms.DockStyle.Fill
        Me.logList.Font = New System.Drawing.Font("Cascadia Code", 9.0!)
        Me.logList.ForeColor = System.Drawing.Color.FromArgb(CType(CType(200, Byte), Integer), CType(CType(210, Byte), Integer), CType(CType(220, Byte), Integer))
        Me.logList.Location = New System.Drawing.Point(0, 56)
        Me.logList.Name = "logList"
        Me.logList.Size = New System.Drawing.Size(600, 344)
        Me.logList.TabIndex = 1
        '
        'ServerTabContent
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.Controls.Add(Me.logList)
        Me.Controls.Add(Me.pnlToolbar)
        Me.Name = "ServerTabContent"
        Me.Size = New System.Drawing.Size(600, 400)
        Me.pnlToolbar.ResumeLayout(False)
        Me.pnlToolbar.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents pnlToolbar As System.Windows.Forms.Panel
    Friend WithEvents btnConnect As MaterialSkin.Controls.MaterialButton
    Friend WithEvents btnDisconnect As MaterialSkin.Controls.MaterialButton
    Friend WithEvents btnReconnect As MaterialSkin.Controls.MaterialButton
    Friend WithEvents btnClear As MaterialSkin.Controls.MaterialButton
    Friend WithEvents logList As ThemedListBox

End Class
