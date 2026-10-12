using System;
using System.Drawing;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    /// <summary>Shared application services (single repository instance used by every form).</summary>
    internal static class AppData
    {
        public static RequestRepository Requests { get; } = new RequestRepository();
    }

    /// <summary>A Panel with double buffering, used for custom-drawn content such as the dependency graph.</summary>
    internal class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    /// <summary>Colours, fonts and control factories so every form looks consistent.</summary>
    internal static class Theme
    {
        public static readonly Color Primary = Color.FromArgb(31, 58, 95);
        public static readonly Color PrimaryHover = Color.FromArgb(44, 80, 128);
        public static readonly Color Accent = Color.FromArgb(0, 150, 136);
        public static readonly Color Background = Color.FromArgb(244, 246, 249);
        public static readonly Color Surface = Color.White;
        public static readonly Color TextDark = Color.FromArgb(33, 37, 41);
        public static readonly Color Muted = Color.FromArgb(108, 117, 125);
        public static readonly Color Border = Color.FromArgb(222, 226, 230);
        public static readonly Color Error = Color.FromArgb(198, 40, 40);
        public static readonly Color Success = Color.FromArgb(46, 125, 50);

        public static readonly Font BaseFont = new Font("Segoe UI", 10F);
        public static readonly Font BoldFont = new Font("Segoe UI Semibold", 10F);
        public static readonly Font SmallFont = new Font("Segoe UI", 9F);
        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 20F);
        public static readonly Font SectionFont = new Font("Segoe UI Semibold", 13F);
        public static readonly Font StatFont = new Font("Segoe UI Semibold", 18F);

        public static Color StatusColor(string status)
        {
            switch (status)
            {
                case RequestStatus.Pending: return Color.FromArgb(176, 106, 0);
                case RequestStatus.InProgress: return Color.FromArgb(25, 118, 210);
                case RequestStatus.Completed: return Color.FromArgb(46, 125, 50);
                case RequestStatus.OnHold: return Color.FromArgb(96, 125, 139);
                case RequestStatus.WaitingForApproval: return Color.FromArgb(123, 31, 162);
                case RequestStatus.Rejected: return Color.FromArgb(198, 40, 40);
                default: return Muted;
            }
        }

        /// <summary>Applies the standard look to a top-level form.</summary>
        public static void StyleForm(Form form, string title, int clientWidth, int clientHeight, int minWidth, int minHeight)
        {
            form.Text = title;
            form.Font = BaseFont;
            form.BackColor = Background;
            form.ForeColor = TextDark;
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ClientSize = new Size(clientWidth, clientHeight);
            form.MinimumSize = new Size(minWidth, minHeight);
        }

        /// <summary>Coloured banner with a title and subtitle, docked to the top of its container.</summary>
        public static Panel CreateHeader(string title, string subtitle)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Primary,
                Height = 84,
                Margin = Padding.Empty
            };
            panel.Controls.Add(new Label
            {
                Text = title,
                Font = TitleFont,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 8)
            });
            panel.Controls.Add(new Label
            {
                Text = subtitle,
                Font = BaseFont,
                ForeColor = Color.FromArgb(200, 214, 232),
                AutoSize = true,
                Location = new Point(27, 52)
            });
            return panel;
        }

        public static Button CreateButton(string text, bool primary = true, int width = 130)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = BoldFont,
                Margin = new Padding(0, 0, 8, 0),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Border;

            if (primary)
            {
                button.BackColor = Primary;
                button.ForeColor = Color.White;
                button.FlatAppearance.MouseOverBackColor = PrimaryHover;
            }
            else
            {
                button.BackColor = Surface;
                button.ForeColor = TextDark;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 239, 244);
            }
            return button;
        }

        public static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = BoldFont,
                ForeColor = TextDark,
                Margin = new Padding(0, 8, 0, 3)
            };
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 239, 244);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextDark;
            grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 239, 244);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.Font = BaseFont;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 235, 250);
            grid.DefaultCellStyle.SelectionForeColor = TextDark;
            grid.RowTemplate.Height = 32;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoGenerateColumns = false;
        }

        public static DataGridViewTextBoxColumn Column(string header, string property, int fillWeight, string format = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                DataPropertyName = property,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = fillWeight,
                MinimumWidth = 60
            };
            if (format != null) column.DefaultCellStyle.Format = format;
            return column;
        }

        /// <summary>White card with a thin border, used to group content.</summary>
        public static Panel CreateCard()
        {
            var card = new Panel
            {
                BackColor = Surface,
                Margin = new Padding(0),
                Padding = new Padding(16)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            return card;
        }
    }
}
