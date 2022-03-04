using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Simulation;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Simulation.Tests
{
    public class SimulationProfilerTests
    {
        [UnityTest]
        public IEnumerator CollectorWithAggregationEvent_ShouldGenerateReport()
        {
            var collector = new TestCollector();
            PerfStatsManager.Instance.StartProfiling(new List<CollectorBase>() { collector});
            yield return new WaitForSeconds(4);

            var path = Path.Combine(Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs), "TestCollector.json");
            Assert.True(File.Exists(path), "No Collector report found at path : " + path);

            var data = DeserializeObjects<SampleReport>(File.ReadAllText(path));
            Assert.True(data.Any(), "Json report is empty");
            foreach (var entry in data)
            {
                Assert.True(entry.name.Equals("TestCollector"), "The name of the collector doesnt match. Actual value: " + entry.name + " Expected value : TestCollector");
                Assert.True(entry.events.Length > 0, "No events are populated.");
                Assert.True(entry.category == 0, "Expected Category: 0, Actual Category: "+entry.category);
            }
            
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator CollectorWithProfilingSampleEvent_ShouldGenerateReport()
        {
            var collector = new TestProfileSampleEventCollector("TestProfileSampleEventCollector");
            var path = Path.Combine(Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs), "TestProfileSampleEventCollector.json");
            PerfStatsManager.Instance.StartProfiling(new List<CollectorBase>() {collector});

            PerfStatsManager.Instance.BeginSample("TestSample", typeof(TestProfileSampleEventCollector));
            Thread.Sleep(4000);
            PerfStatsManager.Instance.EndSample("TestSample", typeof(TestProfileSampleEventCollector));
            
            yield return new WaitForSeconds(2);
            
            Assert.True(File.Exists(path), "No report generated at path: " + path);
            var data = DeserializeObjects<SampleReportProfileSampling>(File.ReadAllText(path));
            Assert.True(data.Any(), "No entries found in the report.");
            
            PerfStatsManager.Instance.StopProfiling();
            
            var testSampleName = "TestProfileSampleEventCollector_TestSample";
            foreach (var entry in data)
            {
                Assert.True(entry.events.Any(), "No samples found.");
                var samples = entry.events;
                
                foreach (var keyValuePair in samples)
                {
                    Assert.True(keyValuePair.eventName.Equals("profileSampleEvent"), "Value found : " + keyValuePair.eventName + " ExpectedValue: profileSampleEvent");
                    foreach (var e in keyValuePair.AggregatedSamples)
                    {
                        Assert.True(e.Key.Equals(testSampleName), "Sample name does not match. Actual value: " + e.Key + " Expected : "+ testSampleName);
                        Assert.True(e.Value.avg > 4.0f, "Value doesnt not as expected. Actual value : " + e.Value.avg);
                    }
                }
            }
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator CollectorWithProfilingSampleEventOnBackgroundThread_ShouldGenerateReport()
        {
            var collector = new TestProfileSampleEventCollector("TestProfileSampleEventCollector");
            var path = Path.Combine(Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs), "TestProfileSampleEventCollector.json");
            PerfStatsManager.Instance.StartProfiling(new List<CollectorBase>() {collector});

            PerfStatsManager.Instance.BeginSample("TestSample", typeof(TestProfileSampleEventCollector));
            Thread.Sleep(4000);
            PerfStatsManager.Instance.EndSample("TestSample", typeof(TestProfileSampleEventCollector));
            var backgroundActivityStarted = true;
            ThreadPool.QueueUserWorkItem((callback) =>
            {
                PerfStatsManager.Instance.BeginSample("TestSample", typeof(TestProfileSampleEventCollector));
                Thread.Sleep(6000);
                PerfStatsManager.Instance.EndSample("TestSample", typeof(TestProfileSampleEventCollector));
                backgroundActivityStarted = false;
            });

            while (backgroundActivityStarted) ;
            
            yield return new WaitForSeconds(2);
            
            Assert.True(File.Exists(path), "No report generated at path: " + path);
            var data = DeserializeObjects<SampleReportProfileSampling>(File.ReadAllText(path));
            Assert.True(data.Any(), "No entries found in the report.");
            
            var testSampleName = "TestProfileSampleEventCollector_TestSample";
            foreach (var entry in data)
            {
                Assert.True(entry.events.Any(), "No samples found.");
                var samples = entry.events;
                
                foreach (var keyValuePair in samples)
                {
                    Assert.True(keyValuePair.eventName.Equals("profileSampleEvent"), "Value found : " + keyValuePair.eventName + " ExpectedValue: profileSampleEvent");
                    foreach (var e in keyValuePair.AggregatedSamples)
                    {
                        Assert.True(e.Key.Equals(testSampleName), "Sample name does not match. Actual value: " + e.Key + " Expected : "+ testSampleName);
                        Assert.True(e.Value.avg >= 5.0f, "Value doesnt not as expected. Actual value : " + e.Value.avg);
                    }
                }
            }
            File.Delete(path);
        }

        [UnityTest]
        public IEnumerator CollectorWithCounterEven_ShouldGenerateReport()
        {
            var collector = new TestCounterEventCollector();
            var path = Path.Combine(Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs), "TestCounterEventCollector.json");
            PerfStatsManager.Instance.StartProfiling(new List<CollectorBase>() { collector });

            PerfStatsManager.Instance.TrackAPIUsage("CollectorWithCounterEven_ShouldGenerateReport", typeof(TestCounterEventCollector));
            
            yield return new WaitForSeconds(5);
            
            Assert.True(File.Exists(path), "No report generated");
            
            File.Delete(path);
        }
        
        private IEnumerable<T> DeserializeObjects<T>(string input)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (var streamReader = new StringReader(input)) 
            using (var jsonReader = new JsonTextReader(streamReader))
            { 
                jsonReader.SupportMultipleContent = true;
                while (jsonReader.Read()) 
                {                       
                    yield return  serializer.Deserialize<T>(jsonReader);
                }
            }
        }
    }

    [Serializable]
    public struct SampleReport
    {
        public string name;
        public int category;
        public AggregationEvent[] events;
    }

    [Serializable]
    public struct SampleReportProfileSampling
    {
        public string name;
        public int category;
        public ProfilerSamplingEvent[] events;
    }

    [IgnoreCollectorAttribute]
    [Serializable]
    public class TestCollector : CollectorBase
    {
        public override void Initialize()
        {
            base.Initialize();

            period = 1;
        }

        public override void PopulateEvents()
        {
            AddEvent(new AggregationEvent("TestEvent",  0,
                () => UnityEngine.Random.Range(2, 4)));
        }
    }
    
    [IgnoreCollectorAttribute]
    public class TestProfileSampleEventCollector : CollectorBase
    {
        public TestProfileSampleEventCollector(string collectorName) : base()
        {
            name = collectorName;
        }
        public override void PopulateEvents()
        {
            period = 4;
            AddEvent(new ProfilerSamplingEvent("profileSampleEvent"));
        }
    }

    [IgnoreCollectorAttribute]
    public class TestCounterEventCollector : CollectorBase
    {
        public override void PopulateEvents()
        {
            period = 4;
            AddEvent(new CounterEvent("testFunctionCounter", 1));
        }
    }
}