using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MunicipalServicesApp
{
    /// <summary>A citizen-reported service request. Persisted as JSON by <see cref="RequestRepository"/>.</summary>
    [DataContract]
    public class ServiceRequest
    {
        [DataMember] public int RequestId { get; set; }
        [DataMember] public string Category { get; set; }
        [DataMember] public string Location { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public string Status { get; set; }

        /// <summary>1 (urgent) to 4 (low). Lower value = higher priority.</summary>
        [DataMember] public int Priority { get; set; }

        [DataMember] public DateTime DateCreated { get; set; }
        [DataMember] public DateTime DateUpdated { get; set; }
        [DataMember] public string AttachmentPath { get; set; }

        /// <summary>IDs of requests that must be finished before this one.</summary>
        [DataMember] public List<int> Dependencies { get; set; }

        public ServiceRequest()
        {
            Status = RequestStatus.Pending;
            Priority = Priorities.Medium;
            DateCreated = DateTime.Now;
            DateUpdated = DateCreated;
            Dependencies = new List<int>();
        }

        public string PriorityLabel => Priorities.Label(Priority);

        public bool IsOpen => RequestStatus.IsOpen(Status);

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // The serializer bypasses the constructor, so repair anything missing from older files.
            if (Dependencies == null) Dependencies = new List<int>();
            if (string.IsNullOrEmpty(Status)) Status = RequestStatus.Pending;
            if (DateUpdated == default(DateTime)) DateUpdated = DateCreated;
        }
    }

    public static class RequestStatus
    {
        public const string Pending = "Pending";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string OnHold = "On Hold";
        public const string WaitingForApproval = "Waiting for Approval";
        public const string Rejected = "Rejected";

        public static readonly string[] All =
        {
            Pending, InProgress, Completed, OnHold, WaitingForApproval, Rejected
        };

        /// <summary>A request is open until it is completed or rejected.</summary>
        public static bool IsOpen(string status) => status != Completed && status != Rejected;
    }

    public static class Priorities
    {
        public const int Urgent = 1;
        public const int High = 2;
        public const int Medium = 3;
        public const int Low = 4;

        public static readonly string[] Labels = { "Urgent", "High", "Medium", "Low" };

        public static string Label(int priority)
        {
            return priority >= Urgent && priority <= Low ? Labels[priority - 1] : "Unknown";
        }
    }

    public static class RequestCategories
    {
        public static readonly string[] All =
        {
            "Sanitation", "Roads", "Utilities", "Streetlights", "Parks", "Traffic Signals"
        };
    }
}
