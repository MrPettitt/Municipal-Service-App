using System;
using System.Collections.Generic;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Binary min-heap of service requests. Requests with the lowest priority number
    /// (most urgent) come out first; ties are broken by oldest request ID.
    /// </summary>
    public class MinHeap
    {
        private readonly List<ServiceRequest> items = new List<ServiceRequest>();

        public int Count => items.Count;

        public void Insert(ServiceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            items.Add(request);
            SiftUp(items.Count - 1);
        }

        public ServiceRequest Peek()
        {
            return items.Count == 0 ? null : items[0];
        }

        /// <summary>Removes and returns the most urgent request, or null if empty.</summary>
        public ServiceRequest ExtractMin()
        {
            if (items.Count == 0) return null;

            ServiceRequest min = items[0];
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);
            if (items.Count > 0) SiftDown(0);
            return min;
        }

        /// <summary>Returns the requests ordered most urgent first (heap sort; the heap is built fresh).</summary>
        public static List<ServiceRequest> SortByPriority(IEnumerable<ServiceRequest> requests)
        {
            var heap = new MinHeap();
            foreach (ServiceRequest request in requests) heap.Insert(request);

            var sorted = new List<ServiceRequest>(heap.Count);
            while (heap.Count > 0) sorted.Add(heap.ExtractMin());
            return sorted;
        }

        private static bool Before(ServiceRequest a, ServiceRequest b)
        {
            if (a.Priority != b.Priority) return a.Priority < b.Priority;
            return a.RequestId < b.RequestId;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!Before(items[index], items[parent])) break;
                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = 2 * index + 1;
                int right = left + 1;
                int smallest = index;

                if (left < items.Count && Before(items[left], items[smallest])) smallest = left;
                if (right < items.Count && Before(items[right], items[smallest])) smallest = right;
                if (smallest == index) return;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            ServiceRequest temp = items[a];
            items[a] = items[b];
            items[b] = temp;
        }
    }
}
