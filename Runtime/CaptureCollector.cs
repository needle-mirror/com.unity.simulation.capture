using System.Collections;
using System.Collections.Generic;
using Unity.Simulation;
using UnityEngine;

public class CaptureCollector : CollectorBase
{
    public override void PopulateEvents()
    {
        AddEvent( new ProfilerSamplingEvent("ImageEncodingAndFileIO"));
    }
}
