using System;
using System.Collections.Generic;

namespace MunicipalServicesApp
{
    public class Event
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }

        public Event(string name, string category, DateTime date, string location, string description)
        {
            Name = name;
            Category = category;
            Date = date;
            Location = location;
            Description = description;
        }
    }

    public class Announcement
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }

        public Announcement(string title, DateTime date, string message)
        {
            Title = title;
            Date = date;
            Message = message;
        }
    }

    /// <summary>Sample events and announcements. Dates are relative to today so the demo never goes stale.</summary>
    public static class EventCatalog
    {
        public static List<Event> CreateEvents()
        {
            DateTime today = DateTime.Today;
            return new List<Event>
            {
                new Event("Community Cleanup", "Community", today.AddDays(5), "Central Park", "Help clean the local park. Gloves and bags are provided."),
                new Event("Town Hall Meeting", "Government", today.AddDays(7), "Municipal Hall", "Discuss city development plans and put questions to council."),
                new Event("Farmers' Market", "Market", today.AddDays(10), "Market Square", "Local farmers selling fresh produce."),
                new Event("Charity Run", "Community", today.AddDays(13), "Riverside Promenade", "Join the 5k run to support local charities."),
                new Event("Book Fair", "Culture", today.AddDays(17), "Public Library", "Explore a variety of books from local authors."),
                new Event("Tech Expo", "Technology", today.AddDays(21), "Convention Centre", "Discover the latest innovations in technology."),
                new Event("Music Festival", "Entertainment", today.AddDays(26), "Amphitheatre", "Enjoy live performances by local bands and artists."),
                new Event("Art Workshop", "Arts", today.AddDays(31), "Community Arts Centre", "Hands-on art sessions with professional artists.")
            };
        }

        public static List<Announcement> CreateAnnouncements()
        {
            DateTime today = DateTime.Today;
            return new List<Announcement>
            {
                new Announcement("Planned water maintenance", today.AddDays(-1),
                    "Water supply will be interrupted in the northern suburbs between 09:00 and 13:00 on Saturday while mains are replaced."),
                new Announcement("Refuse collection schedule", today.AddDays(-3),
                    "Collection on public holidays moves to the following working day. Please place bins out by 06:00."),
                new Announcement("Road resurfacing on Main Street", today.AddDays(-6),
                    "Main Street will be reduced to one lane for two weeks. Expect delays during peak hours and consider alternative routes."),
                new Announcement("Library extended hours", today.AddDays(-9),
                    "The public library now opens until 20:00 on weekdays during exam season.")
            };
        }
    }
}
