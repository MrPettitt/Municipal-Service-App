using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    public class ServiceRequestStatusForm : Form
    {
        private const string AllStatuses = "All statuses";
        private static readonly string[] SortOptions = { "Request ID", "Newest first", "Oldest first", "Priority (heap)" };

        private readonly RequestRepository repository = AppData.Requests;
        private AvlTree tree = new AvlTree();

        private readonly TextBox txtSearch = new TextBox();
        private readonly ComboBox cmbStatus = new ComboBox();
        private readonly ComboBox cmbSort = new ComboBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly BufferedPanel graphPanel = new BufferedPanel();
        private readonly Label lblTotal = new Label();
        private readonly Label lblOpen = new Label();
        private readonly Label lblCompleted = new Label();
        private readonly Label lblUrgent = new Label();
        private readonly Label lblShowing = new Label();

        // Graph state, refreshed whenever the visible list changes.
        private List<ServiceRequest> visible = new List<ServiceRequest>();
        private Graph visibleGraph = new Graph();
        private readonly Dictionary<int, PointF> nodePositions = new Dictionary<int, PointF>();
        private float nodeRadius = 18;

        public ServiceRequestStatusForm()
        {
            Theme.StyleForm(this, "Service Request Status", 1020, 760, 860, 600);
            BuildUi();

            cmbStatus.Items.Add(AllStatuses);
            cmbStatus.Items.AddRange(RequestStatus.All);
            cmbStatus.SelectedIndex = 0;
            cmbSort.Items.AddRange(SortOptions);
            cmbSort.SelectedIndex = 0;

            txtSearch.TextChanged += (s, e) => Refresh_();
            cmbStatus.SelectedIndexChanged += (s, e) => Refresh_();
            cmbSort.SelectedIndexChanged += (s, e) => Refresh_();

            repository.Changed += OnRepositoryChanged;
            FormClosed += (s, e) => repository.Changed -= OnRepositoryChanged;

            Reload();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.Controls.Add(Theme.CreateHeader("Service Request Status", "Search, filter and track every request by its unique ID."), 0, 0);

            // Summary tiles
            var stats = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 0), WrapContents = false };
            stats.Controls.Add(CreateStatTile("Total requests", lblTotal, Theme.TextDark));
            stats.Controls.Add(CreateStatTile("Open", lblOpen, Color.FromArgb(25, 118, 210)));
            stats.Controls.Add(CreateStatTile("Completed", lblCompleted, Theme.Success));
            stats.Controls.Add(CreateStatTile("Urgent & open", lblUrgent, Theme.Error));
            root.Controls.Add(stats, 0, 1);

            // Toolbar
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 16, 0), WrapContents = false };
            txtSearch.Width = 200;
            txtSearch.Margin = new Padding(0, 4, 12, 0);
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.Width = 180;
            cmbStatus.Margin = new Padding(0, 4, 12, 0);
            cmbSort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSort.Width = 140;
            cmbSort.Margin = new Padding(0, 4, 12, 0);
            var btnOpen = Theme.CreateButton("View / update", true, 130);
            btnOpen.Height = 30;
            btnOpen.Click += (s, e) => OpenSelected();
            var btnExport = Theme.CreateButton("Export CSV", false, 110);
            btnExport.Height = 30;
            btnExport.Click += btnExport_Click;

            toolbar.Controls.Add(Caption("Search (ID, place, keyword):"));
            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(Caption("Status:"));
            toolbar.Controls.Add(cmbStatus);
            toolbar.Controls.Add(Caption("Sort:"));
            toolbar.Controls.Add(cmbSort);
            toolbar.Controls.Add(btnOpen);
            toolbar.Controls.Add(btnExport);
            root.Controls.Add(toolbar, 0, 2);

            // Grid on top, graph below
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Margin = new Padding(16, 8, 16, 0),
                FixedPanel = FixedPanel.None,
                SplitterWidth = 8
            };
            split.Panel1MinSize = 120;
            split.Panel2MinSize = 120;

            Theme.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;
            grid.Columns.Add(Theme.Column("ID", "RequestId", 8));
            grid.Columns.Add(Theme.Column("Category", "Category", 16));
            grid.Columns.Add(Theme.Column("Location", "Location", 20));
            grid.Columns.Add(Theme.Column("Status", "Status", 18));
            grid.Columns.Add(Theme.Column("Priority", "PriorityLabel", 10));
            grid.Columns.Add(Theme.Column("Created", "DateCreated", 14, "d MMM yyyy"));
            grid.Columns.Add(Theme.Column("Description", "Description", 40));
            grid.CellFormatting += grid_CellFormatting;
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenSelected(); };
            grid.SelectionChanged += (s, e) => graphPanel.Invalidate();
            split.Panel1.Controls.Add(grid);

            var graphCard = Theme.CreateCard();
            graphCard.Dock = DockStyle.Fill;
            graphCard.Padding = new Padding(1);
            var graphCaption = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(10, 6, 0, 0),
                Text = "Dependency graph - an arrow points from a request to the request it depends on. Click a node to select it.",
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont
            };
            graphPanel.Dock = DockStyle.Fill;
            graphPanel.BackColor = Theme.Surface;
            graphPanel.Paint += graphPanel_Paint;
            graphPanel.MouseClick += graphPanel_MouseClick;
            graphCard.Controls.Add(graphPanel);
            graphCard.Controls.Add(graphCaption);
            split.Panel2.Controls.Add(graphCard);
            root.Controls.Add(split, 0, 3);

            // Footer
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16, 8, 16, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblShowing.Dock = DockStyle.Fill;
            lblShowing.TextAlign = ContentAlignment.MiddleLeft;
            lblShowing.ForeColor = Theme.Muted;
            footer.Controls.Add(lblShowing, 0, 0);
            var btnClose = Theme.CreateButton("Close", false, 100);
            btnClose.Margin = Padding.Empty;
            btnClose.Click += (s, e) => Close();
            footer.Controls.Add(btnClose, 1, 0);
            root.Controls.Add(footer, 0, 4);

            Controls.Add(root);

            // Position the splitter once the form has its final size.
            Shown += (s, e) => split.SplitterDistance = Math.Max(split.Panel1MinSize, (int)(split.Height * 0.52));
        }

        private static Label Caption(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = new Padding(0, 8, 6, 0), ForeColor = Theme.Muted };
        }

        private static Control CreateStatTile(string caption, Label valueLabel, Color color)
        {
            Panel tile = Theme.CreateCard();
            tile.Size = new Size(190, 62);
            tile.Margin = new Padding(0, 0, 12, 0);
            tile.Padding = new Padding(14, 6, 8, 4);

            valueLabel.Font = Theme.StatFont;
            valueLabel.ForeColor = color;
            valueLabel.AutoSize = true;
            valueLabel.Dock = DockStyle.Left;
            valueLabel.Text = "0";

            var captionLabel = new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                ForeColor = Theme.Muted,
                Font = Theme.SmallFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 6, 0, 0)
            };
            tile.Controls.Add(captionLabel);
            tile.Controls.Add(valueLabel);
            return tile;
        }

        // ---- Data -----------------------------------------------------------------------------------------

        private void OnRepositoryChanged(object sender, EventArgs e)
        {
            Reload();
        }

        /// <summary>Rebuilds the search tree from the repository and refreshes everything shown.</summary>
        private void Reload()
        {
            tree = new AvlTree();
            foreach (ServiceRequest request in repository.All) tree.Insert(request);

            var all = tree.InOrder();
            lblTotal.Text = all.Count.ToString();
            lblOpen.Text = all.Count(r => r.IsOpen).ToString();
            lblCompleted.Text = all.Count(r => r.Status == RequestStatus.Completed).ToString();
            lblUrgent.Text = all.Count(r => r.IsOpen && r.Priority <= Priorities.Urgent).ToString();

            Refresh_();
        }

        /// <summary>Applies the search, status filter and sort order to the grid and graph.</summary>
        private void Refresh_()
        {
            int? selectedId = SelectedRequest?.RequestId;

            string status = cmbStatus.SelectedItem as string;
            string text = txtSearch.Text.Trim();

            IEnumerable<ServiceRequest> list = tree.InOrder();

            if (!string.IsNullOrEmpty(status) && status != AllStatuses)
                list = list.Where(r => r.Status == status);

            int id;
            if (int.TryParse(text, out id))
            {
                // Exact ID lookup uses the balanced tree instead of scanning.
                ServiceRequest match = tree.Find(id);
                list = list.Where(r => match != null && r.RequestId == match.RequestId);
            }
            else if (text.Length > 0)
            {
                list = list.Where(r => Contains(r.Location, text) || Contains(r.Description, text) || Contains(r.Category, text));
            }

            switch (cmbSort.SelectedIndex)
            {
                case 1: list = list.OrderByDescending(r => r.DateCreated); break;
                case 2: list = list.OrderBy(r => r.DateCreated); break;
                case 3: list = MinHeap.SortByPriority(list); break;
            }

            visible = list.ToList();
            grid.DataSource = visible;
            lblShowing.Text = $"Showing {visible.Count} of {tree.Count} requests";

            if (selectedId.HasValue) SelectRow(selectedId.Value);

            visibleGraph = Graph.FromRequests(visible);
            graphPanel.Invalidate();
        }

        private static bool Contains(string source, string value)
        {
            return source != null && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private ServiceRequest SelectedRequest => grid.CurrentRow?.DataBoundItem as ServiceRequest;

        private void SelectRow(int requestId)
        {
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                if (((ServiceRequest)grid.Rows[i].DataBoundItem).RequestId == requestId)
                {
                    grid.ClearSelection();
                    grid.Rows[i].Selected = true;
                    grid.CurrentCell = grid.Rows[i].Cells[0];
                    return;
                }
            }
        }

        private void OpenSelected()
        {
            ServiceRequest request = SelectedRequest;
            if (request == null)
            {
                MessageBox.Show(this, "Select a request first.", "No request selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new RequestDetailsForm(request.RequestId, repository))
            {
                dialog.ShowDialog(this);
            }
        }

        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string column = grid.Columns[e.ColumnIndex].DataPropertyName;
            var request = grid.Rows[e.RowIndex].DataBoundItem as ServiceRequest;
            if (request == null) return;

            if (column == "Status")
            {
                e.CellStyle.ForeColor = Theme.StatusColor(request.Status);
                e.CellStyle.Font = Theme.BoldFont;
            }
            else if (column == "PriorityLabel" && request.IsOpen && request.Priority <= Priorities.Urgent)
            {
                e.CellStyle.ForeColor = Theme.Error;
                e.CellStyle.Font = Theme.BoldFont;
            }
        }

        // ---- Export ---------------------------------------------------------------------------------------

        private void btnExport_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Export requests";
                dialog.Filter = "CSV file (*.csv)|*.csv";
                dialog.FileName = "service-requests.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    // UTF-8 with BOM so Excel reads accented characters correctly.
                    File.WriteAllText(dialog.FileName, ToCsv(visible), new UTF8Encoding(true));
                    MessageBox.Show(this, $"Exported {visible.Count} requests.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "Could not save the file: " + ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        internal static string ToCsv(IEnumerable<ServiceRequest> requests)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Category,Location,Status,Priority,Created,Updated,Description");
            foreach (ServiceRequest r in requests)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    r.RequestId.ToString(),
                    CsvField(r.Category),
                    CsvField(r.Location),
                    CsvField(r.Status),
                    CsvField(r.PriorityLabel),
                    r.DateCreated.ToString("yyyy-MM-dd HH:mm"),
                    r.DateUpdated.ToString("yyyy-MM-dd HH:mm"),
                    CsvField(r.Description)
                }));
            }
            return sb.ToString();
        }

        private static string CsvField(string value)
        {
            value = value ?? string.Empty;
            // Guard against spreadsheet formula injection from user-entered text.
            if (value.Length > 0 && "=+-@".IndexOf(value[0]) >= 0) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // ---- Graph ----------------------------------------------------------------------------------------

        private void LayoutNodes(Size area)
        {
            nodePositions.Clear();
            var ids = visible.Select(r => r.RequestId).ToList();
            int n = ids.Count;
            if (n == 0) return;

            nodeRadius = n > 24 ? 12 : n > 14 ? 15 : 18;
            float cx = area.Width / 2f;
            float cy = area.Height / 2f;
            float radius = Math.Min(area.Width, area.Height) / 2f - nodeRadius - 14;

            if (n == 1 || radius < 10)
            {
                nodePositions[ids[0]] = new PointF(cx, cy);
                return;
            }

            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                nodePositions[ids[i]] = new PointF(cx + (float)(radius * Math.Cos(angle)), cy + (float)(radius * Math.Sin(angle)));
            }
        }

        private void graphPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            LayoutNodes(graphPanel.ClientSize);

            if (nodePositions.Count == 0)
            {
                TextRenderer.DrawText(g, "No requests to display.", Theme.BaseFont, graphPanel.ClientRectangle, Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            int? selectedId = SelectedRequest?.RequestId;
            var related = selectedId.HasValue ? new HashSet<int>(visibleGraph.Related(selectedId.Value)) : new HashSet<int>();

            // Edges first so nodes draw on top.
            foreach (var edge in visibleGraph.Edges)
            {
                PointF from, to;
                if (!nodePositions.TryGetValue(edge.Key, out from) || !nodePositions.TryGetValue(edge.Value, out to)) continue;

                bool highlighted = selectedId.HasValue && (edge.Key == selectedId.Value || edge.Value == selectedId.Value);
                DrawArrow(g, from, to, highlighted);
            }

            foreach (ServiceRequest request in visible)
            {
                PointF p = nodePositions[request.RequestId];
                bool isSelected = selectedId == request.RequestId;
                bool isRelated = related.Contains(request.RequestId);

                var bounds = new RectangleF(p.X - nodeRadius, p.Y - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                using (var fill = new SolidBrush(Theme.StatusColor(request.Status)))
                {
                    g.FillEllipse(fill, bounds);
                }
                if (isSelected || isRelated)
                {
                    using (var ring = new Pen(isSelected ? Theme.TextDark : Theme.Accent, isSelected ? 3f : 2f))
                    {
                        g.DrawEllipse(ring, bounds);
                    }
                }

                using (var font = new Font("Segoe UI Semibold", nodeRadius > 14 ? 9f : 8f))
                {
                    var label = request.RequestId.ToString();
                    SizeF size = g.MeasureString(label, font);
                    g.DrawString(label, font, Brushes.White, p.X - size.Width / 2, p.Y - size.Height / 2);
                }
            }
        }

        private void DrawArrow(Graphics g, PointF from, PointF to, bool highlighted)
        {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < nodeRadius * 2) return;

            // Start and end at the node borders so arrowheads aren't hidden under the circles.
            float ux = dx / length, uy = dy / length;
            var start = new PointF(from.X + ux * nodeRadius, from.Y + uy * nodeRadius);
            var end = new PointF(to.X - ux * (nodeRadius + 2), to.Y - uy * (nodeRadius + 2));

            using (var pen = new Pen(highlighted ? Theme.TextDark : Color.FromArgb(170, 176, 184), highlighted ? 2f : 1.2f))
            using (var cap = new AdjustableArrowCap(4, 5))
            {
                pen.CustomEndCap = cap;
                g.DrawLine(pen, start, end);
            }
        }

        private void graphPanel_MouseClick(object sender, MouseEventArgs e)
        {
            foreach (var node in nodePositions)
            {
                float dx = e.X - node.Value.X, dy = e.Y - node.Value.Y;
                if (dx * dx + dy * dy <= nodeRadius * nodeRadius)
                {
                    SelectRow(node.Key);
                    graphPanel.Invalidate();
                    return;
                }
            }
        }
    }
}
