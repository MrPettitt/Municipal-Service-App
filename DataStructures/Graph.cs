using System.Collections.Generic;
using System.Linq;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Directed graph (adjacency list) of request IDs. An edge A -> B means "A depends on B".
    /// </summary>
    public class Graph
    {
        private readonly Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();

        public IEnumerable<int> Nodes => adjacency.Keys;

        public int NodeCount => adjacency.Count;

        public bool ContainsNode(int nodeId) => adjacency.ContainsKey(nodeId);

        public void AddNode(int nodeId)
        {
            if (!adjacency.ContainsKey(nodeId))
                adjacency[nodeId] = new List<int>();
        }

        /// <summary>Adds a directed edge, creating the nodes if needed. Self-loops and duplicates are ignored.</summary>
        public void AddEdge(int from, int to)
        {
            AddNode(from);
            AddNode(to);
            if (from != to && !adjacency[from].Contains(to))
                adjacency[from].Add(to);
        }

        /// <summary>Outgoing neighbours (the requests that <paramref name="nodeId"/> depends on).</summary>
        public IReadOnlyList<int> Neighbors(int nodeId)
        {
            List<int> list;
            return adjacency.TryGetValue(nodeId, out list) ? list : (IReadOnlyList<int>)new int[0];
        }

        /// <summary>All edges as (from, to) pairs.</summary>
        public IEnumerable<KeyValuePair<int, int>> Edges =>
            adjacency.SelectMany(kv => kv.Value.Select(to => new KeyValuePair<int, int>(kv.Key, to)));

        /// <summary>Breadth-first traversal following edge direction. Includes the start node.</summary>
        public List<int> BreadthFirst(int start)
        {
            var order = new List<int>();
            if (!ContainsNode(start)) return order;

            var visited = new HashSet<int> { start };
            var queue = new Queue<int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                order.Add(current);
                foreach (int next in adjacency[current])
                {
                    if (visited.Add(next)) queue.Enqueue(next);
                }
            }
            return order;
        }

        /// <summary>Depth-first traversal following edge direction. Includes the start node.</summary>
        public List<int> DepthFirst(int start)
        {
            var order = new List<int>();
            if (!ContainsNode(start)) return order;

            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                int current = stack.Pop();
                if (!visited.Add(current)) continue;
                order.Add(current);

                // Push in reverse so the first neighbour is visited first.
                var next = adjacency[current];
                for (int i = next.Count - 1; i >= 0; i--)
                {
                    if (!visited.Contains(next[i])) stack.Push(next[i]);
                }
            }
            return order;
        }

        /// <summary>
        /// Every request connected to <paramref name="start"/> by dependencies in either direction,
        /// nearest first (breadth-first over the undirected view). Excludes the start node.
        /// </summary>
        public List<int> Related(int start)
        {
            var related = new List<int>();
            if (!ContainsNode(start)) return related;

            var undirected = new Dictionary<int, List<int>>();
            foreach (int node in adjacency.Keys) undirected[node] = new List<int>();
            foreach (var edge in Edges)
            {
                undirected[edge.Key].Add(edge.Value);
                undirected[edge.Value].Add(edge.Key);
            }

            var visited = new HashSet<int> { start };
            var queue = new Queue<int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in undirected[current].OrderBy(n => n))
                {
                    if (!visited.Add(next)) continue;
                    related.Add(next);
                    queue.Enqueue(next);
                }
            }
            return related;
        }

        /// <summary>True if following dependencies can lead back to where it started (an impossible schedule).</summary>
        public bool HasCycle()
        {
            // 0 = unvisited, 1 = on current path, 2 = finished
            var state = adjacency.Keys.ToDictionary(k => k, k => 0);
            foreach (int node in adjacency.Keys)
            {
                if (state[node] == 0 && VisitHasCycle(node, state)) return true;
            }
            return false;
        }

        private bool VisitHasCycle(int node, Dictionary<int, int> state)
        {
            state[node] = 1;
            foreach (int next in adjacency[node])
            {
                if (state[next] == 1) return true;
                if (state[next] == 0 && VisitHasCycle(next, state)) return true;
            }
            state[node] = 2;
            return false;
        }

        /// <summary>Builds the dependency graph for a set of requests (edges only between requests in the set).</summary>
        public static Graph FromRequests(IEnumerable<ServiceRequest> requests)
        {
            var list = requests.ToList();
            var ids = new HashSet<int>(list.Select(r => r.RequestId));
            var graph = new Graph();

            foreach (ServiceRequest request in list) graph.AddNode(request.RequestId);
            foreach (ServiceRequest request in list)
            {
                foreach (int dependency in request.Dependencies)
                {
                    if (ids.Contains(dependency)) graph.AddEdge(request.RequestId, dependency);
                }
            }
            return graph;
        }
    }
}
