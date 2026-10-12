using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MunicipalServicesApp.Tests
{
    [TestClass]
    public class RequestRepositoryTests
    {
        private string path;

        [TestInitialize]
        public void Setup()
        {
            path = Path.Combine(Path.GetTempPath(), "msa-tests-" + Guid.NewGuid() + ".json");
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (string file in new[] { path, path + ".bak", path + ".tmp" })
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        [TestMethod]
        public void SeedsSampleDataOnFirstRun()
        {
            var repo = new RequestRepository(path);
            Assert.IsTrue(repo.All.Count > 0);
            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod]
        public void AddedRequestsSurviveAReload()
        {
            var repo = new RequestRepository(path);
            var added = repo.Add(new ServiceRequest
            {
                Category = "Roads",
                Location = "Test Street",
                Description = "Pothole",
                Priority = Priorities.High,
                Dependencies = { 1 }
            });

            var reloaded = new RequestRepository(path).Get(added.RequestId);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual("Test Street", reloaded.Location);
            Assert.AreEqual(RequestStatus.Pending, reloaded.Status);
            CollectionAssert.AreEqual(new[] { 1 }, reloaded.Dependencies);
        }

        [TestMethod]
        public void NewIdsAreUnique()
        {
            var repo = new RequestRepository(path);
            int a = repo.Add(new ServiceRequest()).RequestId;
            int b = repo.Add(new ServiceRequest()).RequestId;
            Assert.AreNotEqual(a, b);
            Assert.AreEqual(repo.All.Count, repo.All.Select(r => r.RequestId).Distinct().Count());
        }

        [TestMethod]
        public void UpdateStatusPersistsAndRaisesChanged()
        {
            var repo = new RequestRepository(path);
            int raised = 0;
            repo.Changed += (s, e) => raised++;

            repo.UpdateStatus(1, RequestStatus.Completed);

            Assert.AreEqual(1, raised);
            Assert.AreEqual(RequestStatus.Completed, new RequestRepository(path).Get(1).Status);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UpdateStatusRejectsUnknownStatus()
        {
            new RequestRepository(path).UpdateStatus(1, "Bogus");
        }

        [TestMethod]
        public void CorruptFileIsBackedUpAndReplacedWithSampleData()
        {
            File.WriteAllText(path, "{ this is not json");

            var repo = new RequestRepository(path);

            Assert.IsTrue(repo.All.Count > 0);
            Assert.IsTrue(File.Exists(path + ".bak"));
        }
    }
}
