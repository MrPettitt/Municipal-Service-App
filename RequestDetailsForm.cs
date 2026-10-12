using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    /// <summary>Shows a single request, lets the user change its status, and lists related requests.</summary>
    public class RequestDetailsForm : Form
    {
        private readonly int requestId;
        private readonly RequestRepository repository;
        private readonly ComboBox cmbStatus = new ComboBox();
        private readonly ListBox lstRelated = new ListBox();

        public RequestDetailsForm(int requestId, RequestRepository repository)
        {
            this.requestId = requestId;
            this.repository = repository;

            ServiceRequest request = repository.Get(requestId);
            if (request == null) throw new ArgumentException("No request with ID " + requestId, nameof(requestId));

            Theme.StyleForm(this, $"Request #{requestId}", 560, 640, 520, 600);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BuildUi(request);
        }

        private void BuildUi(ServiceRequest request)
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(Theme.CreateHeader($"Request #{request.RequestId}", $"{request.Category} - {request.Location}"), 0, 0);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(24, 12, 24, 16)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;
            AddRow(body, ref row, "Priority", request.PriorityLabel);
            AddRow(body, ref row, "Created", request.DateCreated.ToString("d MMM yyyy, HH:mm"));
            AddRow(body, ref row, "Last updated", request.DateUpdated.ToString("d MMM yyyy, HH:mm"));

            var description = new Label { Text = request.Description, AutoSize = true, MaximumSize = new Size(380, 0), Margin = new Padding(3, 8, 3, 3) };
            body.Controls.Add(FieldCaption("Description"), 0, row);
            body.Controls.Add(description, 1, row++);

            if (!string.IsNullOrEmpty(request.AttachmentPath))
            {
                var link = new LinkLabel { Text = Path.GetFileName(request.AttachmentPath), AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
                link.LinkClicked += (s, e) => OpenAttachment(request.AttachmentPath);
                body.Controls.Add(FieldCaption("Attachment"), 0, row);
                body.Controls.Add(link, 1, row++);
            }

            string dependsOn = request.Dependencies.Count == 0 ? "None" : string.Join(", ", request.Dependencies.Select(d => "#" + d));
            AddRow(body, ref row, "Depends on", dependsOn);

            // Status editor
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.Items.AddRange(RequestStatus.All);
            cmbStatus.SelectedItem = request.Status;
            cmbStatus.Dock = DockStyle.Left;
            cmbStatus.Width = 220;
            cmbStatus.Margin = new Padding(3, 6, 3, 3);
            body.Controls.Add(FieldCaption("Status"), 0, row);
            body.Controls.Add(cmbStatus, 1, row++);

            // Related requests found by breadth-first search over the dependency graph
            var graph = Graph.FromRequests(repository.All);
            var related = graph.Related(request.RequestId);
            var relatedCaption = FieldCaption("Related requests");
            relatedCaption.Margin = new Padding(3, 14, 3, 3);
            body.Controls.Add(relatedCaption, 0, row);
            body.SetColumnSpan(relatedCaption, 2);
            row++;

            lstRelated.Dock = DockStyle.Fill;
            lstRelated.Height = 130;
            lstRelated.IntegralHeight = false;
            lstRelated.SelectionMode = SelectionMode.None;
            lstRelated.BorderStyle = BorderStyle.FixedSingle;
            if (related.Count == 0)
            {
                lstRelated.Items.Add("No linked requests.");
            }
            else
            {
                foreach (int id in related)
                {
                    ServiceRequest other = repository.Get(id);
                    if (other != null) lstRelated.Items.Add($"#{other.RequestId}  {other.Status}  -  {other.Description}");
                }
            }
            body.Controls.Add(lstRelated, 0, row);
            body.SetColumnSpan(lstRelated, 2);
            row++;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 16, 0, 0) };
            var btnSave = Theme.CreateButton("Save status", true, 130);
            btnSave.Click += btnSave_Click;
            var btnClose = Theme.CreateButton("Close", false, 100);
            btnClose.Click += (s, e) => Close();
            buttons.Controls.AddRange(new Control[] { btnSave, btnClose });
            body.Controls.Add(buttons, 0, row);
            body.SetColumnSpan(buttons, 2);

            root.Controls.Add(body, 0, 1);
            Controls.Add(root);
            AcceptButton = btnSave;
            CancelButton = btnClose;
        }

        private static Label FieldCaption(string text)
        {
            return new Label { Text = text, AutoSize = true, Font = Theme.BoldFont, Margin = new Padding(3, 8, 3, 3) };
        }

        private static void AddRow(TableLayoutPanel table, ref int row, string caption, string value)
        {
            table.Controls.Add(FieldCaption(caption), 0, row);
            table.Controls.Add(new Label { Text = value, AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, 1, row);
            row++;
        }

        private void OpenAttachment(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(this, "The attached file could not be found. It may have been moved or deleted.", "File missing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string newStatus = cmbStatus.SelectedItem as string;
            ServiceRequest request = repository.Get(requestId);
            if (newStatus == null || request == null || newStatus == request.Status)
            {
                Close();
                return;
            }

            // A request shouldn't be marked complete while the work it depends on is still open.
            if (newStatus == RequestStatus.Completed)
            {
                var blockers = request.Dependencies
                    .Select(repository.Get)
                    .Where(d => d != null && d.IsOpen)
                    .ToList();

                if (blockers.Count > 0)
                {
                    string ids = string.Join(", ", blockers.Select(b => "#" + b.RequestId));
                    var answer = MessageBox.Show(this,
                        $"This request depends on open request(s) {ids}. Mark it completed anyway?",
                        "Open dependencies", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes) return;
                }
            }

            repository.UpdateStatus(requestId, newStatus);
            Close();
        }
    }
}
