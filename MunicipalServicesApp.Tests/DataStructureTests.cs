using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MunicipalServicesApp.Tests
{
    [TestClass]
    public class AvlTreeTests
    {
        private static ServiceRequest Req(int id, int priority = 3)
        {
            return new ServiceRequest { RequestId = id, Priority = priority };
        }

        [TestMethod]
        public void InOrderReturnsRequestsSortedById()
        {
            var tree = new AvlTree();
            foreach (int id in new[] { 5, 2, 8, 1, 9, 3 }) tree.Insert(Req(id));

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 5, 8, 9 }, tree.InOrder().Select(r => r.RequestId).ToArray());
        }

        [TestMethod]
        public void InsertingDuplicateIdIsIgnored()
        {
            var tree = new AvlTree();
            Assert.IsTrue(tree.Insert(Req(1)));
            Assert.IsFalse(tree.Insert(Req(1)));
            Assert.AreEqual(1, tree.Count);
        }

        [TestMethod]
        public void FindReturnsNullWhenMissing()
        {
            var tree = new AvlTree();
            tree.Insert(Req(4));
            Assert.IsNotNull(tree.Find(4));
            Assert.IsNull(tree.Find(5));
        }

        [TestMethod]
        public void StaysBalancedWhenIdsArriveInOrder()
        {
            var tree = new AvlTree();
            for (int i = 1; i <= 1000; i++) tree.Insert(Req(i));

            // An unbalanced tree would have height 1000; AVL guarantees roughly 1.44 * log2(n).
            Assert.IsTrue(tree.Height <= 14, "Height was " + tree.Height);
            Assert.AreEqual(1000, tree.Count);
        }

        [TestMethod]
        public void RemoveKeepsOrderAndBalance()
        {
            var tree = new AvlTree();
            for (int i = 1; i <= 100; i++) tree.Insert(Req(i));

            Assert.IsTrue(tree.Remove(50));
            Assert.IsFalse(tree.Remove(50));
            Assert.IsNull(tree.Find(50));
            Assert.AreEqual(99, tree.Count);
            Assert.IsTrue(tree.InOrder().Select(r => r.RequestId).SequenceEqual(Enumerable.Range(1, 100).Where(i => i != 50)));
            Assert.IsTrue(tree.Height <= 9);
        }
    }

    [TestClass]
    public class MinHeapTests
    {
        [TestMethod]
        public void ExtractsMostUrgentFirstThenLowestId()
        {
            var heap = new MinHeap();
            heap.Insert(new ServiceRequest { RequestId = 1, Priority = 3 });
            heap.Insert(new ServiceRequest { RequestId = 2, Priority = 1 });
            heap.Insert(new ServiceRequest { RequestId = 3, Priority = 1 });
            heap.Insert(new ServiceRequest { RequestId = 4, Priority = 2 });

            Assert.AreEqual(2, heap.ExtractMin().RequestId);
            Assert.AreEqual(3, heap.ExtractMin().RequestId);
            Assert.AreEqual(4, heap.ExtractMin().RequestId);
            Assert.AreEqual(1, heap.ExtractMin().RequestId);
            Assert.IsNull(heap.ExtractMin());
        }

        [TestMethod]
        public void SortByPriorityMatchesLinqOrdering()
        {
            var requests = Enumerable.Range(1, 50)
                .Select(i => new ServiceRequest { RequestId = i, Priority = (i * 7) % 4 + 1 })
                .ToList();

            var expected = requests.OrderBy(r => r.Priority).ThenBy(r => r.RequestId).Select(r => r.RequestId).ToList();
            var actual = MinHeap.SortByPriority(requests).Select(r => r.RequestId).ToList();

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    [TestClass]
    public class GraphTests
    {
        [TestMethod]
        public void BreadthFirstVisitsNearestFirst()
        {
            var g = new Graph();
            g.AddEdge(1, 2);
            g.AddEdge(1, 3);
            g.AddEdge(2, 4);
            g.AddEdge(3, 4);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, g.BreadthFirst(1));
        }

        [TestMethod]
        public void DepthFirstGoesDeepBeforeWide()
        {
            var g = new Graph();
            g.AddEdge(1, 2);
            g.AddEdge(1, 3);
            g.AddEdge(2, 4);

            CollectionAssert.AreEqual(new[] { 1, 2, 4, 3 }, g.DepthFirst(1));
        }

        [TestMethod]
        public void RelatedIgnoresEdgeDirectionAndExcludesStart()
        {
            var g = new Graph();
            g.AddEdge(1, 2);   // 1 depends on 2
            g.AddEdge(3, 1);   // 3 depends on 1
            g.AddNode(9);      // unconnected

            CollectionAssert.AreEquivalent(new[] { 2, 3 }, g.Related(1));
            Assert.AreEqual(0, g.Related(9).Count);
        }

        [TestMethod]
        public void DetectsCycles()
        {
            var g = new Graph();
            g.AddEdge(1, 2);
            g.AddEdge(2, 3);
            Assert.IsFalse(g.HasCycle());

            g.AddEdge(3, 1);
            Assert.IsTrue(g.HasCycle());
        }

        [TestMethod]
        public void SelfLoopsAndDuplicatesAreIgnored()
        {
            var g = new Graph();
            g.AddEdge(1, 1);
            g.AddEdge(1, 2);
            g.AddEdge(1, 2);
            Assert.AreEqual(1, g.Edges.Count());
        }

        [TestMethod]
        public void FromRequestsSkipsDependenciesOutsideTheSet()
        {
            var requests = new[]
            {
                new ServiceRequest { RequestId = 1, Dependencies = { 2, 99 } },
                new ServiceRequest { RequestId = 2 }
            };

            var g = Graph.FromRequests(requests);
            Assert.AreEqual(2, g.NodeCount);
            Assert.IsFalse(g.ContainsNode(99));
        }
    }
}
