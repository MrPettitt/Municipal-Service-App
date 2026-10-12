using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    public class LocalEventsForm : Form
    {
        private const string AllCategories = "All categories";
        private static readonly string[] RangeLabels = { "All upcoming", "Next 7 days", "Next 30 days" };
        private static readonly int?[] RangeDays = { null, 7, 30 };

        private readonly List<Event> events = EventCatalog.CreateEvents();
        private readonly List<Announcement> announcements = EventCatalog.CreateAnnouncements();

        private readonly TextBox txtSearch = new TextBox();
        private readonly ComboBox cmbCategory = new ComboBox();
        private readonly ComboBox cmbRange = new ComboBox();
        private readonly Label lblCount = new Label();
        private readonly DataGridView grid = new DataGridView();

        private readonly Label lblEventName = new Label();
        private readonly Label lblEventDate = new Label();
        private readonly Label lblEventLocation = new Label();
        private readonly Label lblEventCategory = new Label();
        private readonly Label lblEventDescription = new Label();
        private readonly Button btnCalendar = Theme.CreateButton("Add to calendar", true, 150);

        private readonly ListView announcementList = new ListView();
        private readonly Label lblAnnouncementTitle = new Label();
        private readonly Label lblAnnouncementBody = new Label();

        public LocalEventsForm()
        {
            Theme.StyleForm(this, "Local Events and Announcements", 960, 600, 800, 480);
            BuildUi();

            cmbCategory.Items.Add(AllCategories);
            cmbCategory.Items.AddRange(events.Select(ev => ev.Category).Distinct().OrderBy(c => c).ToArray());
            cmbCategory.SelectedIndex = 0;
            cmbRange.Items.AddRange(RangeLabels);
            cmbRange.SelectedIndex = 0;

            // Wire filters after the initial values are set so we refresh once, not three times.
            txtSearch.TextChanged += (s, e) => ApplyFilters();
            cmbCategory.SelectedIndexChanged += (s, e) => ApplyFilters();
            cmbRange.SelectedIndexChanged += (s, e) => ApplyFilters();

            LoadAnnouncements();
            ApplyFilters();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.Controls.Add(Theme.CreateHeader("Local Events and Announcements", "See what is happening around town."), 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6), Margin = new Padding(16, 12, 16, 0) };
            var eventsTab = new TabPage("Events") { BackColor = Theme.Background, Padding = new Padding(0, 12, 0, 0) };
            var announcementsTab = new TabPage("Announcements") { BackColor = Theme.Background, Padding = new Padding(0, 12, 0, 0) };
            tabs.TabPages.Add(eventsTab);
            tabs.TabPages.Add(announcementsTab);
            eventsTab.Controls.Add(BuildEventsTab());
            announcementsTab.Controls.Add(BuildAnnouncementsTab());
            root.Controls.Add(tabs, 0, 1);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 0), FlowDirection = FlowDirection.RightToLeft };
            var btnClose = Theme.CreateButton("Close", false, 100);
            btnClose.Click += (s, e) => Close();
            footer.Controls.Add(btnClose);
            root.Controls.Add(footer, 0, 2);

            Controls.Add(root);
        }

        private Control BuildEventsTab()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Filter bar
            var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            txtSearch.Width = 220;
            txtSearch.Margin = new Padding(0, 4, 8, 0);
            var searchLabel = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 7, 6, 0) };
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.Width = 160;
            cmbCategory.Margin = new Padding(0, 4, 8, 0);
            cmbRange.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRange.Width = 130;
            cmbRange.Margin = new Padding(0, 4, 8, 0);
            var btnClear = Theme.CreateButton("Clear", false, 70);
            btnClear.Height = 28;
            btnClear.Click += (s, e) =>
            {
                txtSearch.Clear();
                cmbCategory.SelectedIndex = 0;
                cmbRange.SelectedIndex = 0;
            };
            filters.Controls.AddRange(new Control[] { searchLabel, txtSearch, cmbCategory, cmbRange, btnClear });
            layout.Controls.Add(filters, 0, 0);

            lblCount.Dock = DockStyle.Fill;
            lblCount.TextAlign = ContentAlignment.MiddleRight;
            lblCount.ForeColor = Theme.Muted;
            layout.Controls.Add(lblCount, 1, 0);

            // Event grid
            Theme.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;
            grid.Margin = new Padding(0, 0, 12, 0);
            grid.Columns.Add(Theme.Column("Event", "Name", 40));
            grid.Columns.Add(Theme.Column("Date", "Date", 30, "ddd, d MMM yyyy"));
            grid.Columns.Add(Theme.Column("Category", "Category", 30));
            grid.SelectionChanged += (s, e) => ShowSelectedEvent();
            grid.CellFormatting += grid_CellFormatting;
            layout.Controls.Add(grid, 0, 1);

            // Details card
            Panel card = Theme.CreateCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(20);

            var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            lblEventName.Font = Theme.SectionFont;
            lblEventName.AutoSize = true;
            lblEventName.MaximumSize = new Size(280, 0);
            lblEventName.Margin = new Padding(0, 0, 0, 10);
            lblEventDate.AutoSize = true;
            lblEventDate.Font = Theme.BoldFont;
            lblEventLocation.AutoSize = true;
            lblEventLocation.ForeColor = Theme.Muted;
            lblEventCategory.AutoSize = true;
            lblEventCategory.ForeColor = Theme.Accent;
            lblEventDescription.AutoSize = true;
            lblEventDescription.MaximumSize = new Size(280, 0);
            lblEventDescription.Margin = new Padding(0, 14, 0, 0);
            btnCalendar.Margin = new Padding(0, 14, 0, 0);
            btnCalendar.Click += btnCalendar_Click;

            details.Controls.Add(lblEventName, 0, 0);
            details.Controls.Add(lblEventDate, 0, 1);
            details.Controls.Add(lblEventLocation, 0, 2);
            details.Controls.Add(lblEventCategory, 0, 3);
            details.Controls.Add(lblEventDescription, 0, 4);
            details.Controls.Add(btnCalendar, 0, 5);
            card.Controls.Add(details);
            layout.Controls.Add(card, 1, 1);

            return layout;
        }

        private Control BuildAnnouncementsTab()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

            announcementList.Dock = DockStyle.Fill;
            announcementList.View = View.Details;
            announcementList.FullRowSelect = true;
            announcementList.HideSelection = false;
            announcementList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            announcementList.Columns.Add("Date", 110);
            announcementList.Columns.Add("Title", 260);
            announcementList.BorderStyle = BorderStyle.FixedSingle;
            announcementList.Margin = new Padding(0, 0, 12, 0);
            announcementList.SelectedIndexChanged += (s, e) => ShowSelectedAnnouncement();
            layout.Controls.Add(announcementList, 0, 0);

            Panel card = Theme.CreateCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(20);
            var body = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            lblAnnouncementTitle.Font = Theme.SectionFont;
            lblAnnouncementTitle.AutoSize = true;
            lblAnnouncementTitle.MaximumSize = new Size(480, 0);
            lblAnnouncementBody.AutoSize = true;
            lblAnnouncementBody.MaximumSize = new Size(480, 0);
            lblAnnouncementBody.Margin = new Padding(3, 12, 3, 3);
            body.Controls.Add(lblAnnouncementTitle);
            body.Controls.Add(lblAnnouncementBody);
            card.Controls.Add(body);
            layout.Controls.Add(card, 1, 0);

            return layout;
        }

        private void LoadAnnouncements()
        {
            announcementList.Items.Clear();
            foreach (Announcement a in announcements.OrderByDescending(a => a.Date))
            {
                var item = new ListViewItem(a.Date.ToString("d MMM yyyy")) { Tag = a };
                item.SubItems.Add(a.Title);
                announcementList.Items.Add(item);
            }
            if (announcementList.Items.Count > 0) announcementList.Items[0].Selected = true;
        }

        private void ShowSelectedAnnouncement()
        {
            if (announcementList.SelectedItems.Count == 0) return;
            var a = (Announcement)announcementList.SelectedItems[0].Tag;
            lblAnnouncementTitle.Text = a.Title;
            lblAnnouncementBody.Text = a.Message;
        }

        private void ApplyFilters()
        {
            string text = txtSearch.Text.Trim();
            string category = cmbCategory.SelectedItem as string;
            int? days = cmbRange.SelectedIndex >= 0 ? RangeDays[cmbRange.SelectedIndex] : null;
            DateTime today = DateTime.Today;

            Event previous = SelectedEvent;

            var filtered = events
                .Where(ev => ev.Date >= today)
                .Where(ev => string.IsNullOrEmpty(text)
                    || ev.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                    || ev.Description.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                    || ev.Location.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(ev => string.IsNullOrEmpty(category) || category == AllCategories || ev.Category == category)
                .Where(ev => !days.HasValue || ev.Date <= today.AddDays(days.Value))
                .OrderBy(ev => ev.Date)
                .ToList();

            grid.DataSource = filtered;
            lblCount.Text = $"{filtered.Count} event{(filtered.Count == 1 ? "" : "s")}";

            // Keep the same event selected if it is still listed.
            if (previous != null)
            {
                int index = filtered.IndexOf(previous);
                if (index >= 0) grid.Rows[index].Selected = true;
            }
            ShowSelectedEvent();
        }

        private Event SelectedEvent => grid.CurrentRow?.DataBoundItem as Event;

        private void ShowSelectedEvent()
        {
            Event ev = grid.SelectedRows.Count > 0 ? grid.SelectedRows[0].DataBoundItem as Event : null;
            btnCalendar.Visible = ev != null;

            if (ev == null)
            {
                lblEventName.Text = grid.Rows.Count == 0 ? "No events match your filters." : "Select an event";
                lblEventDate.Text = lblEventLocation.Text = lblEventCategory.Text = lblEventDescription.Text = string.Empty;
                return;
            }

            lblEventName.Text = ev.Name;
            lblEventDate.Text = ev.Date.ToString("dddd, d MMMM yyyy");
            lblEventLocation.Text = ev.Location;
            lblEventCategory.Text = ev.Category;
            lblEventDescription.Text = ev.Description;
        }

        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Highlight events happening within the next week.
            var ev = grid.Rows[e.RowIndex].DataBoundItem as Event;
            if (ev != null && ev.Date <= DateTime.Today.AddDays(7))
            {
                e.CellStyle.Font = Theme.BoldFont;
            }
        }

        private void btnCalendar_Click(object sender, EventArgs e)
        {
            Event ev = grid.SelectedRows.Count > 0 ? grid.SelectedRows[0].DataBoundItem as Event : null;
            if (ev == null) return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Add to calendar";
                dialog.Filter = "iCalendar file (*.ics)|*.ics";
                dialog.FileName = string.Concat(ev.Name.Where(c => char.IsLetterOrDigit(c) || c == ' ')).Trim().Replace(' ', '-') + ".ics";

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    File.WriteAllText(dialog.FileName, BuildCalendarEntry(ev), new UTF8Encoding(false));
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(this, "Could not save the file: " + ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>Builds an all-day iCalendar entry that opens in Outlook, Google Calendar, etc.</summary>
        internal static string BuildCalendarEntry(Event ev)
        {
            Func<string, string> escape = text => (text ?? string.Empty)
                .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\\n").Replace("\n", "\\n");

            return string.Join("\r\n", new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//Municipal Services App//EN",
                "BEGIN:VEVENT",
                "UID:" + Guid.NewGuid() + "@municipalservicesapp",
                "DTSTAMP:" + DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'"),
                "DTSTART;VALUE=DATE:" + ev.Date.ToString("yyyyMMdd"),
                "DTEND;VALUE=DATE:" + ev.Date.AddDays(1).ToString("yyyyMMdd"),
                "SUMMARY:" + escape(ev.Name),
                "LOCATION:" + escape(ev.Location),
                "DESCRIPTION:" + escape(ev.Description),
                "END:VEVENT",
                "END:VCALENDAR",
                string.Empty
            });
        }
    }
}
