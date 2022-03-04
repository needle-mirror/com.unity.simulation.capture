using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Simulation;
using UnityEngine;


namespace Unity.Simulation
{
    [Serializable]
public abstract class EventBase 
{
    // Public Members
    
    /// <summary>
    /// The delegate to use for dispatching events.
    /// </summary>
    [JsonIgnore] 
    public ContinuousEvents.EventDispatchDelegate dispatchDelegate { get; set; }

    /// <summary>
    /// The delegate to use for collecting a metric to aggregate.
    /// </summary>
    [JsonIgnore]
    public ContinuousEvents.EventCollectionDelegate collector { get; set; }

    /// <summary>
    /// Name of the event.
    /// </summary>
    public string eventName; //{ get; protected set; }
    
    protected bool allowMultipledEventsOfType = true;

    [JsonIgnore]
    public bool resetOnEachSampleCollection = true;
    public bool IsMultipleEventsInstancesAllowed()
    {
        return allowMultipledEventsOfType;
    }

    /// <summary>
    /// Constucts an Event that collects a metric each interval and aggregates over a period.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="interval">The interval in which to collect the metric, in seconds.</param>
    /// <param name="period">The aggregation period in seconds.</param>
    /// <param name="collector">A delegate to collect the metric.</param>
    /// <returns>A newly constructed Event instance.</returns>
    public EventBase(string eventName, float interval, ContinuousEvents.EventCollectionDelegate collector = null)
    {
        this.eventName = eventName;
        _interval = interval;
        this.collector = collector;
    }

    /// <summary>
    /// Implement this method to override the reset logic for the event before each sample capture.
    /// </summary>
    public virtual void Reset() { }

    /// <summary>
    /// Override this method to provide implementation for ingesting the value to perform some aggregation.
    /// </summary>
    /// <param name="value">Value ingested from the data source.</param>
    /// <exception cref="NotImplementedException"></exception>
    public virtual void IngestValue(double value) { }

    public virtual void Update(float deltaTime)
    {
        _elapsedInterval += deltaTime;
        _elapsedPeriod   += deltaTime;

        if (_interval >= 0 && _elapsedInterval >= _interval)
        {
            if (collector != null)
                IngestValue(collector.Invoke());
            _elapsedInterval -= _interval;
        }
    }

    protected float _elapsedInterval;
    protected float _elapsedPeriod;
    protected float _interval;
    protected float _period;
}   
}
