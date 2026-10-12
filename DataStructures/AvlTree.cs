using System;
using System.Collections.Generic;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Self-balancing binary search tree (AVL) of service requests keyed by RequestId.
    /// Insert, find and remove are O(log n) even when IDs arrive in ascending order.
    /// </summary>
    public class AvlTree
    {
        private class Node
        {
            public ServiceRequest Value;
            public Node Left;
            public Node Right;
            public int Height = 1;

            public Node(ServiceRequest value)
            {
                Value = value;
            }
        }

        private Node root;

        public int Count { get; private set; }

        public int Height => HeightOf(root);

        /// <summary>Adds the request. Returns false (and changes nothing) if the ID already exists.</summary>
        public bool Insert(ServiceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            bool added = false;
            root = Insert(root, request, ref added);
            if (added) Count++;
            return added;
        }

        public ServiceRequest Find(int requestId)
        {
            Node node = root;
            while (node != null)
            {
                if (requestId == node.Value.RequestId) return node.Value;
                node = requestId < node.Value.RequestId ? node.Left : node.Right;
            }
            return null;
        }

        public bool Remove(int requestId)
        {
            bool removed = false;
            root = Remove(root, requestId, ref removed);
            if (removed) Count--;
            return removed;
        }

        /// <summary>All requests in ascending RequestId order.</summary>
        public List<ServiceRequest> InOrder()
        {
            var result = new List<ServiceRequest>(Count);
            InOrder(root, result);
            return result;
        }

        private static Node Insert(Node node, ServiceRequest request, ref bool added)
        {
            if (node == null)
            {
                added = true;
                return new Node(request);
            }

            if (request.RequestId < node.Value.RequestId)
                node.Left = Insert(node.Left, request, ref added);
            else if (request.RequestId > node.Value.RequestId)
                node.Right = Insert(node.Right, request, ref added);
            else
                return node; // duplicate ID

            return Rebalance(node);
        }

        private static Node Remove(Node node, int requestId, ref bool removed)
        {
            if (node == null) return null;

            if (requestId < node.Value.RequestId)
            {
                node.Left = Remove(node.Left, requestId, ref removed);
            }
            else if (requestId > node.Value.RequestId)
            {
                node.Right = Remove(node.Right, requestId, ref removed);
            }
            else
            {
                removed = true;
                if (node.Left == null) return node.Right;
                if (node.Right == null) return node.Left;

                // Two children: replace with the in-order successor.
                Node successor = node.Right;
                while (successor.Left != null) successor = successor.Left;
                node.Value = successor.Value;
                bool ignored = false;
                node.Right = Remove(node.Right, successor.Value.RequestId, ref ignored);
            }

            return Rebalance(node);
        }

        private static void InOrder(Node node, List<ServiceRequest> result)
        {
            if (node == null) return;
            InOrder(node.Left, result);
            result.Add(node.Value);
            InOrder(node.Right, result);
        }

        private static int HeightOf(Node node) => node == null ? 0 : node.Height;

        private static void UpdateHeight(Node node)
        {
            node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
        }

        private static Node RotateRight(Node y)
        {
            Node x = y.Left;
            y.Left = x.Right;
            x.Right = y;
            UpdateHeight(y);
            UpdateHeight(x);
            return x;
        }

        private static Node RotateLeft(Node x)
        {
            Node y = x.Right;
            x.Right = y.Left;
            y.Left = x;
            UpdateHeight(x);
            UpdateHeight(y);
            return y;
        }

        private static Node Rebalance(Node node)
        {
            UpdateHeight(node);
            int balance = HeightOf(node.Left) - HeightOf(node.Right);

            if (balance > 1)
            {
                if (HeightOf(node.Left.Left) < HeightOf(node.Left.Right))
                    node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }

            if (balance < -1)
            {
                if (HeightOf(node.Right.Right) < HeightOf(node.Right.Left))
                    node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }

            return node;
        }
    }
}
