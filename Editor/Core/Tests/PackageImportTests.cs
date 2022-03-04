using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Simulation;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace Unity.Simulation
{
    public class PackageImportTests : MonoBehaviour
    {
        private static string[] packagesToTest = new[] {"com.unity.perception", "com.unity.simulation.games"};
        public IEnumerator PerceptionPackageUponImportShouldNotConflictWithNewtonsoftJson([ValueSource("packagesToTest")] string package)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var req = UnityEditor.PackageManager.Client.Add(package);
            while (!req.IsCompleted)
            {
                if (stopWatch.ElapsedMilliseconds > 10000)
                {
                    Log.E("Failed to import the package");
                    break;
                }
            
                yield return null;
            }
        
            stopWatch.Reset();
        
            if (req.IsCompleted)
                Log.I("FinishedImporting");
        
            LogAssert.Expect(LogType.Log, new Regex("FinishedImporting"));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
