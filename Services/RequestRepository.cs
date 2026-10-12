using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Stores service requests in a JSON file under %AppData%\MunicipalServicesApp.
    /// Seeds sample data on first run and recovers from a corrupt file.
    /// </summary>
    public class RequestRepository
    {
        private static readonly DataContractJsonSerializerSettings SerializerSettings = new DataContractJsonSerializerSettings
        {
            DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("o")
        };

        private readonly string filePath;
        private List<ServiceRequest> requests;

        /// <summary>Raised after a request is added or updated.</summary>
        public event EventHandler Changed;

        public RequestRepository(string filePath = null)
        {
            this.filePath = filePath ?? DefaultFilePath;
            Load();
        }

        public static string DefaultFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MunicipalServicesApp",
            "requests.json");

        public string FilePath => filePath;

        public IReadOnlyList<ServiceRequest> All => requests.ToList();

        public ServiceRequest Get(int requestId)
        {
            return requests.FirstOrDefault(r => r.RequestId == requestId);
        }

        /// <summary>Assigns the next free ID, stores the request and saves. Returns the stored request.</summary>
        public ServiceRequest Add(ServiceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            request.RequestId = requests.Count == 0 ? 1 : requests.Max(r => r.RequestId) + 1;
            request.DateCreated = DateTime.Now;
            request.DateUpdated = request.DateCreated;
            requests.Add(request);
            Save();
            OnChanged();
            return request;
        }

        /// <summary>Changes a request's status and saves.</summary>
        public void UpdateStatus(int requestId, string status)
        {
            ServiceRequest request = Get(requestId);
            if (request == null) throw new ArgumentException("No request with ID " + requestId, nameof(requestId));
            if (!RequestStatus.All.Contains(status)) throw new ArgumentException("Unknown status: " + status, nameof(status));

            request.Status = status;
            request.DateUpdated = DateTime.Now;
            Save();
            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void Load()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(filePath))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(List<ServiceRequest>), SerializerSettings);
                        requests = (List<ServiceRequest>)serializer.ReadObject(stream) ?? new List<ServiceRequest>();
                    }
                    return;
                }
                catch (Exception ex) when (ex is System.Runtime.Serialization.SerializationException || ex is IOException || ex is UnauthorizedAccessException)
                {
                    // Keep the unreadable file for inspection and start again with sample data.
                    try { File.Copy(filePath, filePath + ".bak", true); } catch (IOException) { }
                }
            }

            requests = CreateSampleRequests();
            Save();
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Write to a temp file first so a crash mid-save can't corrupt the real one.
                string temp = filePath + ".tmp";
                using (FileStream stream = File.Create(temp))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<ServiceRequest>), SerializerSettings);
                    serializer.WriteObject(stream, requests);
                }

                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(temp, filePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Data stays in memory for this session; surface the problem to the caller's log if they have one.
                System.Diagnostics.Debug.WriteLine("Could not save requests: " + ex.Message);
            }
        }

        private static List<ServiceRequest> CreateSampleRequests()
        {
            DateTime now = DateTime.Now;

            ServiceRequest Make(int id, string category, string location, string status, int priority, int daysAgo, string description, params int[] dependencies)
            {
                return new ServiceRequest
                {
                    RequestId = id,
                    Category = category,
                    Location = location,
                    Status = status,
                    Priority = priority,
                    Description = description,
                    DateCreated = now.AddDays(-daysAgo),
                    DateUpdated = now.AddDays(-daysAgo),
                    Dependencies = dependencies.ToList()
                };
            }

            return new List<ServiceRequest>
            {
                Make(1, "Roads", "Main Street & 4th Ave", RequestStatus.Pending, Priorities.High, 1, "Large pothole causing damage to vehicles.", 2, 3),
                Make(2, "Utilities", "Main Street & 4th Ave", RequestStatus.InProgress, Priorities.Urgent, 2, "Burst water main under the road surface.", 3),
                Make(3, "Utilities", "Main Street & 4th Ave", RequestStatus.Completed, Priorities.Medium, 3, "Water shut-off and site survey completed."),
                Make(4, "Sanitation", "Oak Park entrance", RequestStatus.OnHold, Priorities.High, 4, "Overflowing bins; waiting on additional collection truck."),
                Make(5, "Streetlights", "Elm Road", RequestStatus.WaitingForApproval, Priorities.Urgent, 5, "Three streetlights out along the school route.", 1),
                Make(6, "Parks", "Riverside Park", RequestStatus.Rejected, Priorities.Low, 6, "Request for new fountain declined under current budget."),
                Make(7, "Traffic Signals", "Station Road junction", RequestStatus.Pending, Priorities.Low, 7, "Pedestrian crossing signal takes too long to change."),
                Make(8, "Roads", "Main Street", RequestStatus.InProgress, Priorities.Medium, 8, "Resurfacing of the damaged section.", 2),
                Make(9, "Sanitation", "Market Square", RequestStatus.Completed, Priorities.High, 9, "Illegal dumping cleared and signage installed."),
                Make(10, "Parks", "Central Park", RequestStatus.OnHold, Priorities.Medium, 10, "Playground equipment repair awaiting parts."),
                Make(11, "Streetlights", "Elm Road", RequestStatus.WaitingForApproval, Priorities.Urgent, 11, "Replace the damaged cabling feeding the streetlights.", 5),
                Make(12, "Utilities", "Hill Crest", RequestStatus.Rejected, Priorities.Urgent, 12, "Duplicate of an existing outage report."),
                Make(13, "Traffic Signals", "Station Road junction", RequestStatus.Pending, Priorities.High, 13, "Signal timing review for peak hours.", 7),
                Make(14, "Roads", "Harbour Road", RequestStatus.InProgress, Priorities.Urgent, 14, "Road markings faded; hazardous at night."),
                Make(15, "Sanitation", "Lake View", RequestStatus.Completed, Priorities.High, 15, "Storm drain unblocked."),
                Make(16, "Parks", "Oak Park", RequestStatus.OnHold, Priorities.Medium, 16, "Bench replacement on hold pending inspection.", 4)
            };
        }
    }
}
