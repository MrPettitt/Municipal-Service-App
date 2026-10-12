using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    public class MainMenu : Form
    {
        private readonly Label summaryLabel = new Label();

        private ReportIssuesForm reportForm;
        private LocalEventsForm eventsForm;
        private ServiceRequestStatusForm statusForm;

        public MainMenu()
        {
            Theme.StyleForm(this, "Municipal Services", 900, 540, 760, 480);
            BuildUi();

            AppData.Requests.Changed += OnRequestsChanged;
            FormClosed += (s, e) => AppData.Requests.Changed -= OnRequestsChanged;
            UpdateSummary();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            root.Controls.Add(Theme.CreateHeader("Municipal Services", "Report issues, follow your requests and stay informed about your community."), 0, 0);

            var cards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(24, 24, 24, 12)
            };
            for (int i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            cards.Controls.Add(CreateCard("Report an Issue",
                "Tell us about potholes, broken streetlights, uncollected waste and more. Attach a photo or document to help crews find and fix it.",
                "Report an issue", Color.FromArgb(198, 40, 40), (s, e) => OpenReport()), 0, 0);
            cards.Controls.Add(CreateCard("Local Events",
                "Browse upcoming events and municipal announcements. Search by name or category and add events to your calendar.",
                "View events", Theme.Accent, (s, e) => OpenEvents()), 1, 0);
            cards.Controls.Add(CreateCard("Service Request Status",
                "Track every request by its unique ID, filter by status, sort by priority and explore how requests depend on each other.",
                "Track requests", Color.FromArgb(25, 118, 210), (s, e) => OpenStatus()), 2, 0);
            root.Controls.Add(cards, 0, 1);

            summaryLabel.Dock = DockStyle.Fill;
            summaryLabel.TextAlign = ContentAlignment.MiddleCenter;
            summaryLabel.ForeColor = Theme.Muted;
            summaryLabel.Font = Theme.SmallFont;
            root.Controls.Add(summaryLabel, 0, 2);

            Controls.Add(root);
        }

        private static Control CreateCard(string title, string description, string buttonText, Color accent, EventHandler onClick)
        {
            Panel card = Theme.CreateCard();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(8);
            card.Padding = new Padding(20, 24, 20, 20);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                Text = title,
                Font = Theme.SectionFont,
                ForeColor = Theme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            var descriptionLabel = new Label
            {
                Text = description,
                ForeColor = Theme.Muted,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            var button = Theme.CreateButton(buttonText, true, 140);
            button.BackColor = accent;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(accent, 0.1F);
            button.Dock = DockStyle.Fill;
            button.Margin = Padding.Empty;
            button.Click += onClick;

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(descriptionLabel, 0, 1);
            layout.Controls.Add(button, 0, 2);
            card.Controls.Add(layout);

            // Accent strip along the top of the card.
            card.Paint += (s, e) =>
            {
                using (var brush = new SolidBrush(accent))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, card.Width, 5);
                }
            };
            return card;
        }

        private void OnRequestsChanged(object sender, EventArgs e)
        {
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var requests = AppData.Requests.All;
            int open = requests.Count(r => r.IsOpen);
            int upcoming = EventCatalog.CreateEvents().Count(ev => ev.Date >= DateTime.Today && ev.Date <= DateTime.Today.AddDays(14));
            summaryLabel.Text = $"{open} open service request{(open == 1 ? "" : "s")}  ·  {upcoming} event{(upcoming == 1 ? "" : "s")} in the next two weeks";
        }

        private void OpenReport()
        {
            if (reportForm == null || reportForm.IsDisposed) reportForm = new ReportIssuesForm();
            ShowForm(reportForm);
        }

        private void OpenEvents()
        {
            if (eventsForm == null || eventsForm.IsDisposed) eventsForm = new LocalEventsForm();
            ShowForm(eventsForm);
        }

        private void OpenStatus()
        {
            if (statusForm == null || statusForm.IsDisposed) statusForm = new ServiceRequestStatusForm();
            ShowForm(statusForm);
        }

        private static void ShowForm(Form form)
        {
            if (!form.Visible) form.Show();
            if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
            form.Activate();
        }
    }
}
