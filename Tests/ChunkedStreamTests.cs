#if !UNITY_SIMULATION_SDK_DISABLED
using System;
using System.Collections;
using System.Text;
using System.IO;

using Unity.Simulation;

using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

public class ChunkedStreamTests
{
    [UnityTest]
    [Timeout(10000)]
    public IEnumerator ChunkedStream_AppendsBytesToBuffer_FlushesToFileSystem()
    {
        string path = Path.Combine(Application.persistentDataPath, "log_0.txt");
        if (File.Exists(path))
            File.Delete(path);
        ChunkedStream producer = new ChunkedStream(8, 1, functor:(AsyncRequest<object> request) =>
        {
            FileProducer.Write(path, request.data as Array, false);
            return AsyncRequest.Result.Completed;
        });
        producer.Append(Encoding.ASCII.GetBytes("Test"));
        producer.Append(Encoding.ASCII.GetBytes("Unit"));
        while (!System.IO.File.Exists(path))
            yield return null;
        Assert.True(System.IO.File.ReadAllText(path) == "TestUnit");
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
