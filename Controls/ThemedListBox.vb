Imports System.Runtime.InteropServices

Public Class ThemedListBox
    Inherits UserControl

    <DllImport("user32.dll")>
    Private Shared Function ShowScrollBar(hWnd As IntPtr, wBar As Integer, bShow As Boolean) As Boolean
    End Function

    Private Const SB_VERT As Integer = 1
    Private Const WM_VSCROLL As Integer = &H115
    Private Const WM_MOUSEWHEEL As Integer = &H20A
    Private Const WM_KEYDOWN As Integer = &H100
    Private Const VK_C As Integer = &H43

    Private Class InternalListBox
        Inherits ListBox
        Public Event ScrollChanged()

        Protected Overrides Sub WndProc(ByRef m As Message)
            If m.Msg = WM_MOUSEWHEEL Then
                Dim direction = If((m.WParam.ToInt64() And &H80000000L) <> 0, -1, 1)
                Dim lines = Math.Max(1, SystemInformation.MouseWheelScrollLines)
                Dim newIndex = TopIndex - direction * lines
                TopIndex = Math.Max(0, Math.Min(Items.Count - 1, newIndex))
                RaiseEvent ScrollChanged()
                Return
            End If

            If m.Msg = WM_KEYDOWN Then
                Dim key = CInt(m.WParam.ToInt64())
                Dim ctrl = (ModifierKeys And Keys.Control) = Keys.Control
                If ctrl AndAlso key = VK_C AndAlso SelectedItems.Count > 0 Then
                    Dim sb As New System.Text.StringBuilder()
                    For Each item In SelectedItems
                        sb.AppendLine(item?.ToString())
                    Next
                    Clipboard.SetText(sb.ToString())
                    Return
                End If
            End If

            MyBase.WndProc(m)
            ShowScrollBar(Handle, SB_VERT, False)
            If m.Msg = WM_VSCROLL Then
                RaiseEvent ScrollChanged()
            End If
        End Sub
    End Class

    Private _listBox As InternalListBox
    Private _scrollPanel As Panel
    Private _isDragging As Boolean = False
    Private _dragStartY As Integer
    Private _thumbTopAtDragStart As Integer
    Private _isHoveringThumb As Boolean = False
    Private _autoScroll As Boolean = True
    Private _contextMenu As ContextMenuStrip
    Private _backColor As Color = Color.FromArgb(18, 18, 18)
    Private _foreColor As Color = Color.FromArgb(200, 210, 220)
    Private _fontValue As Font

    Public ReadOnly Property Items As ListBox.ObjectCollection
        Get
            Return _listBox.Items
        End Get
    End Property

    Public Property TopIndex() As Integer
        Get
            Return _listBox.TopIndex
        End Get
        Set(value As Integer)
            _listBox.TopIndex = value
        End Set
    End Property

    Private Function IsAtBottom() As Boolean
        If _listBox.Items.Count = 0 Then Return True
        Dim itemHeight = Math.Max(1, _listBox.ItemHeight)
        Dim visibleItems = Math.Max(1, _listBox.ClientSize.Height \ itemHeight)
        Dim maxScroll = Math.Max(0, _listBox.Items.Count - visibleItems)
        Return _listBox.TopIndex >= maxScroll
    End Function

    Private Sub OnInternalScrollChanged()
        _autoScroll = IsAtBottom()
        _scrollPanel?.Invalidate()
    End Sub

    Public Function AddItem(text As String) As Integer
        Dim index = _listBox.Items.Add(text)
        If _autoScroll Then
            _listBox.TopIndex = Math.Max(0, _listBox.Items.Count - 1)
        End If
        Return index
    End Function

    Public Overrides Property BackColor As Color
        Get
            Return _backColor
        End Get
        Set(value As Color)
            _backColor = value
            _listBox.BackColor = value
        End Set
    End Property

    Public Overrides Property ForeColor As Color
        Get
            Return _foreColor
        End Get
        Set(value As Color)
            _foreColor = value
            _listBox.ForeColor = value
        End Set
    End Property

    Public Overrides Property Font As Font
        Get
            Return If(_fontValue, MyBase.Font)
        End Get
        Set(value As Font)
            _fontValue = value
            _listBox.Font = value
        End Set
    End Property

    Public Sub New()
        _listBox = New InternalListBox()
        _listBox.BorderStyle = BorderStyle.None
        _listBox.IntegralHeight = False
        _listBox.MultiColumn = False
        _listBox.SelectionMode = SelectionMode.MultiExtended
        _listBox.Dock = DockStyle.Fill
        AddHandler _listBox.ScrollChanged, AddressOf OnInternalScrollChanged
        AddHandler _listBox.MouseEnter, Sub() _listBox.Focus()

        _contextMenu = New ContextMenuStrip()
        _contextMenu.Renderer = New DarkContextMenuRenderer()
        Dim copyItem = New ToolStripMenuItem("Copy")
        copyItem.ShortcutKeys = Keys.Control Or Keys.C
        AddHandler copyItem.Click, Sub()
                                       If _listBox.SelectedItems.Count > 0 Then
                                           Dim sb As New System.Text.StringBuilder()
                                           For Each item In _listBox.SelectedItems
                                               sb.AppendLine(item?.ToString())
                                           Next
                                           Clipboard.SetText(sb.ToString())
                                       End If
                                   End Sub
        _contextMenu.Items.Add(copyItem)
        _listBox.ContextMenuStrip = _contextMenu

        _scrollPanel = New Panel()
        _scrollPanel.Width = 10
        _scrollPanel.Dock = DockStyle.Right
        AddHandler _scrollPanel.Paint, AddressOf DrawScrollbar
        AddHandler _scrollPanel.MouseMove, AddressOf OnScrollMouseMove
        AddHandler _scrollPanel.MouseDown, AddressOf OnScrollMouseDown
        AddHandler _scrollPanel.MouseUp, AddressOf OnScrollMouseUp
        AddHandler _scrollPanel.MouseLeave, Sub()
                                                _isHoveringThumb = False
                                                _scrollPanel.Invalidate()
                                            End Sub

        Controls.Add(_listBox)
        Controls.Add(_scrollPanel)
    End Sub

    Private Function GetThumbRect() As Rectangle
        If _listBox.Items.Count = 0 Then Return Rectangle.Empty

        Dim itemHeight = Math.Max(1, _listBox.ItemHeight)
        Dim visibleItems = Math.Max(1, _listBox.ClientSize.Height \ itemHeight)
        Dim totalItems = _listBox.Items.Count

        If totalItems <= visibleItems Then Return Rectangle.Empty

        Dim trackHeight = _scrollPanel.ClientSize.Height
        If trackHeight <= 0 Then Return Rectangle.Empty

        Dim thumbHeight = Math.Max(18, CInt(trackHeight * (visibleItems / totalItems)))
        Dim maxThumbTop = trackHeight - thumbHeight
        Dim maxScroll = Math.Max(1, totalItems - visibleItems)
        Dim ratio = Math.Min(1.0R, _listBox.TopIndex / maxScroll)
        Return New Rectangle(1, CInt(ratio * maxThumbTop), _scrollPanel.ClientSize.Width - 2, thumbHeight)
    End Function

    Private Sub DrawScrollbar(sender As Object, e As PaintEventArgs)
        e.Graphics.Clear(Color.FromArgb(30, 30, 30))

        Dim thumb = GetThumbRect()
        If thumb = Rectangle.Empty Then Return

        Dim c As Color
        If _isDragging Then
            c = Color.FromArgb(0, 172, 193)
        ElseIf _isHoveringThumb Then
            c = Color.FromArgb(100, 110, 120)
        Else
            c = Color.FromArgb(70, 78, 86)
        End If

        Using br As New SolidBrush(c)
            e.Graphics.FillRectangle(br, thumb)
        End Using
    End Sub

    Private Sub OnScrollMouseMove(sender As Object, e As MouseEventArgs)
        If _isDragging Then
            Dim delta = e.Y - _dragStartY
            Dim thumb = GetThumbRect()
            If thumb = Rectangle.Empty Then Return

            Dim itemHeight = Math.Max(1, _listBox.ItemHeight)
            Dim visibleItems = Math.Max(1, _listBox.ClientSize.Height \ itemHeight)
            Dim totalItems = Math.Max(1, _listBox.Items.Count)
            Dim maxScroll = Math.Max(1, totalItems - visibleItems)
            Dim maxThumbTop = _scrollPanel.ClientSize.Height - thumb.Height

            Dim newThumbTop = Math.Max(0, Math.Min(maxThumbTop, _thumbTopAtDragStart + delta))
            Dim ratio = newThumbTop / Math.Max(1, maxThumbTop)
            _listBox.TopIndex = Math.Min(maxScroll, CInt(ratio * maxScroll))
        Else
            _isHoveringThumb = GetThumbRect().Contains(e.Location)
        End If
        _scrollPanel.Invalidate()
    End Sub

    Private Sub OnScrollMouseDown(sender As Object, e As MouseEventArgs)
        Dim thumb = GetThumbRect()
        If thumb = Rectangle.Empty Then Return

        If thumb.Contains(e.Location) Then
            _isDragging = True
            _dragStartY = e.Y
            _thumbTopAtDragStart = thumb.Y
            _scrollPanel.Capture = True
        Else
            Dim itemHeight = Math.Max(1, _listBox.ItemHeight)
            Dim visibleItems = Math.Max(1, _listBox.ClientSize.Height \ itemHeight)
            Dim totalItems = Math.Max(1, _listBox.Items.Count)
            Dim maxScroll = Math.Max(1, totalItems - visibleItems)
            Dim ratio = Math.Max(0.0R, Math.Min(1.0R, e.Y / CDbl(_scrollPanel.ClientSize.Height)))
            _listBox.TopIndex = Math.Min(maxScroll, CInt(ratio * maxScroll))
        End If
        _scrollPanel.Invalidate()
    End Sub

    Private Sub OnScrollMouseUp(sender As Object, e As MouseEventArgs)
        _isDragging = False
        _scrollPanel.Capture = False
        _scrollPanel.Invalidate()
    End Sub

    Private Class DarkContextMenuRenderer
        Inherits ToolStripProfessionalRenderer

        Public Sub New()
            MyBase.New(New DarkColorTable())
        End Sub
    End Class

    Private Class DarkColorTable
        Inherits ProfessionalColorTable

        Public Overrides ReadOnly Property MenuStripGradientBegin() As Color
            Get
                Return Color.FromArgb(30, 30, 30)
            End Get
        End Property
        Public Overrides ReadOnly Property MenuStripGradientEnd() As Color
            Get
                Return Color.FromArgb(30, 30, 30)
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelected() As Color
            Get
                Return Color.FromArgb(0, 140, 155)
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelectedGradientBegin() As Color
            Get
                Return Color.FromArgb(0, 140, 155)
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelectedGradientEnd() As Color
            Get
                Return Color.FromArgb(0, 140, 155)
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemBorder() As Color
            Get
                Return Color.FromArgb(0, 172, 193)
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientBegin() As Color
            Get
                Return Color.FromArgb(24, 24, 24)
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientMiddle() As Color
            Get
                Return Color.FromArgb(24, 24, 24)
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientEnd() As Color
            Get
                Return Color.FromArgb(24, 24, 24)
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorDark() As Color
            Get
                Return Color.FromArgb(50, 55, 60)
            End Get
        End Property
    End Class

End Class
