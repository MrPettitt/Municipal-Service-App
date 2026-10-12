using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    public class ReportIssuesForm : Form
    {
        private const int MaxDescriptionLength = 500;
        private const long MaxAttachmentBytes = 10 * 1024 * 1024;

        private readonly TextBox txtLocation = new TextBox();
        private readonly ComboBox cmbCategory = new ComboBox();
        private readonly ComboBox cmbPriority = new ComboBox();
        private readonly TextBox txtDescription = new TextBox();
        private readonly Label lblCounter = new Label();
        private readonly Label lblFileName = new Label();
        private readonly PictureBox picPreview = new PictureBox();
        private readonly Button btnAttach = Theme.CreateButton("Attach file...", false, 120);
        private readonly Button btnRemoveAttachment = Theme.CreateButton("Remove", false, 90);
        private readonly Button btnSubmit = Theme.CreateButton("Submit report");
        private readonly Label lblStatus = new Label();
        private readonly ErrorProvider errors = new ErrorProvider();

        private string attachmentPath;

        public ReportIssuesForm()
        {
            Theme.StyleForm(this, "Report an Issue", 620, 700, 520, 560);
            BuildUi();
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            ResetForm();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(Theme.CreateHeader("Report an Issue", "Tell us what needs attention and where."), 0, 0);

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24, 16, 24, 16) };
            root.Controls.Add(scroll, 0, 1);

            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Location
            form.Controls.Add(Theme.CreateFieldLabel("Location *"), 0, 0);
            form.SetColumnSpan(form.GetControlFromPosition(0, 0), 2);
            txtLocation.Dock = DockStyle.Fill;
            txtLocation.MaxLength = 120;
            txtLocation.TextChanged += (s, e) => errors.SetError(txtLocation, string.Empty);
            form.Controls.Add(txtLocation, 0, 1);
            form.SetColumnSpan(txtLocation, 2);

            // Category and priority side by side
            form.Controls.Add(Theme.CreateFieldLabel("Category *"), 0, 2);
            form.Controls.Add(Theme.CreateFieldLabel("Priority"), 1, 2);

            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.Dock = DockStyle.Fill;
            cmbCategory.Margin = new Padding(3, 3, 8, 3);
            cmbCategory.Items.AddRange(RequestCategories.All);
            cmbCategory.SelectedIndexChanged += (s, e) => errors.SetError(cmbCategory, string.Empty);
            form.Controls.Add(cmbCategory, 0, 3);

            cmbPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPriority.Dock = DockStyle.Fill;
            cmbPriority.Items.AddRange(Priorities.Labels);
            form.Controls.Add(cmbPriority, 1, 3);

            // Description
            form.Controls.Add(Theme.CreateFieldLabel("Description *"), 0, 4);
            form.SetColumnSpan(form.GetControlFromPosition(0, 4), 2);
            txtDescription.Multiline = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            txtDescription.Height = 120;
            txtDescription.Dock = DockStyle.Fill;
            txtDescription.MaxLength = MaxDescriptionLength;
            txtDescription.TextChanged += (s, e) =>
            {
                errors.SetError(txtDescription, string.Empty);
                lblCounter.Text = $"{txtDescription.TextLength}/{MaxDescriptionLength}";
            };
            form.Controls.Add(txtDescription, 0, 5);
            form.SetColumnSpan(txtDescription, 2);

            lblCounter.AutoSize = true;
            lblCounter.Anchor = AnchorStyles.Right;
            lblCounter.ForeColor = Theme.Muted;
            lblCounter.Font = Theme.SmallFont;
            form.Controls.Add(lblCounter, 1, 6);

            // Attachment
            form.Controls.Add(Theme.CreateFieldLabel("Attachment (optional)"), 0, 7);
            form.SetColumnSpan(form.GetControlFromPosition(0, 7), 2);

            var attachRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            btnAttach.Click += btnAttach_Click;
            btnRemoveAttachment.Click += (s, e) => SetAttachment(null);
            lblFileName.AutoSize = true;
            lblFileName.ForeColor = Theme.Muted;
            lblFileName.Margin = new Padding(8, 9, 0, 0);
            attachRow.Controls.AddRange(new Control[] { btnAttach, btnRemoveAttachment, lblFileName });
            form.Controls.Add(attachRow, 0, 8);
            form.SetColumnSpan(attachRow, 2);

            picPreview.Size = new Size(160, 110);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.BorderStyle = BorderStyle.FixedSingle;
            picPreview.BackColor = Theme.Surface;
            picPreview.Visible = false;
            picPreview.Margin = new Padding(3, 8, 3, 3);
            form.Controls.Add(picPreview, 0, 9);
            form.SetColumnSpan(picPreview, 2);

            // Buttons and status
            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 20, 0, 0) };
            btnSubmit.Click += btnSubmit_Click;
            var btnClear = Theme.CreateButton("Clear", false, 90);
            btnClear.Click += (s, e) => ResetForm();
            var btnClose = Theme.CreateButton("Close", false, 90);
            btnClose.Click += (s, e) => Close();
            buttons.Controls.AddRange(new Control[] { btnSubmit, btnClear, btnClose });
            form.Controls.Add(buttons, 0, 10);
            form.SetColumnSpan(buttons, 2);

            lblStatus.AutoSize = true;
            lblStatus.MaximumSize = new Size(520, 0);
            lblStatus.Margin = new Padding(3, 12, 3, 0);
            form.Controls.Add(lblStatus, 0, 11);
            form.SetColumnSpan(lblStatus, 2);

            scroll.Controls.Add(form);
            Controls.Add(root);
            AcceptButton = btnSubmit;
        }

        private void ResetForm()
        {
            errors.Clear();
            txtLocation.Clear();
            cmbCategory.SelectedIndex = -1;
            cmbPriority.SelectedIndex = Priorities.Medium - 1;
            txtDescription.Clear();
            lblCounter.Text = $"0/{MaxDescriptionLength}";
            SetAttachment(null);
            SetStatus("Fill in the form and submit your report.", Theme.Muted);
        }

        private void SetStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }

        private void btnAttach_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select an image or document";
                dialog.Filter = "Images and documents|*.jpg;*.jpeg;*.png;*.pdf;*.doc;*.docx|Image files|*.jpg;*.jpeg;*.png|Document files|*.pdf;*.doc;*.docx";

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                if (new FileInfo(dialog.FileName).Length > MaxAttachmentBytes)
                {
                    MessageBox.Show(this, "Please choose a file smaller than 10 MB.", "File too large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SetAttachment(dialog.FileName);
            }
        }

        private void SetAttachment(string path)
        {
            attachmentPath = path;
            btnRemoveAttachment.Visible = path != null;
            lblFileName.Text = path == null ? "No file selected" : Path.GetFileName(path);

            Image old = picPreview.Image;
            picPreview.Image = null;
            old?.Dispose();
            picPreview.Visible = false;

            string extension = path == null ? null : Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                try
                {
                    // Load via a stream copy so the file isn't locked while the preview is shown.
                    using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                    using (var image = Image.FromStream(stream))
                    {
                        picPreview.Image = new Bitmap(image);
                    }
                    picPreview.Visible = true;
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is OutOfMemoryException)
                {
                    // Not a readable image; the file name is still shown.
                }
            }
        }

        private bool ValidateForm()
        {
            errors.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                errors.SetError(txtLocation, "Please enter the location of the issue.");
                valid = false;
            }
            if (cmbCategory.SelectedIndex < 0)
            {
                errors.SetError(cmbCategory, "Please choose a category.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                errors.SetError(txtDescription, "Please describe the issue.");
                valid = false;
            }
            return valid;
        }

        private async void btnSubmit_Click(object sender, EventArgs e)
        {
            if (!ValidateForm())
            {
                SetStatus("Please complete the highlighted fields.", Theme.Error);
                return;
            }

            var request = new ServiceRequest
            {
                Location = txtLocation.Text.Trim(),
                Category = cmbCategory.SelectedItem.ToString(),
                Priority = cmbPriority.SelectedIndex + 1,
                Description = txtDescription.Text.Trim(),
                AttachmentPath = attachmentPath
            };

            btnSubmit.Enabled = false;
            SetStatus("Submitting your report...", Theme.Muted);

            await Task.Delay(700); // brief pause so the user sees the submission feedback

            // The form may have been closed while "submitting".
            if (IsDisposed) return;

            ServiceRequest saved = AppData.Requests.Add(request);
            btnSubmit.Enabled = true;
            ResetForm();
            SetStatus($"Thank you! Your report was received. Your request ID is #{saved.RequestId}; track it under Service Request Status.", Theme.Success);
        }
    }
}
