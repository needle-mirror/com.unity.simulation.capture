using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Simulation
{
    /// <summary>
    /// Event class which contains the aggregated data.
    /// </summary>
    [Serializable]
    public class AggregationEvent : EventBase
    {
        // Public Members

        /// <summary>
        /// The min value collected over the aggregation period.
        /// </summary>
        public double min; //{ get; protected set; }

        /// <summary>
        /// The max value collected over the aggregation period.
        /// </summary>
        public double max; //{ get; protected set; }

        /// <summary>
        /// The mean value collected over the aggregation period.
        /// </summary>
        public double mean; //{ get; protected set; }

        /// <summary>
        /// The variance of the values collected over the aggregation period.
        /// </summary>
        public double variance; //{ get; protected set; }

        /// <summary>
        /// Constucts an Event that collects a metric each interval and aggregates over a period.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="interval">The interval in which to collect the metric, in seconds.</param>
        /// <param name="period">The aggregation period in seconds.</param>
        /// <param name="collector">A delegate to collect the metric.</param>
        /// <returns>A newly constructed Event instance.</returns>
        public AggregationEvent(string eventName, float interval, ContinuousEvents.EventCollectionDelegate collector = null, bool resetOnEachSample = true) : base(eventName, interval, collector)
        {
            resetOnEachSampleCollection = resetOnEachSample;
        }
        
        public override void Reset()
        {
            min      = double.MaxValue;
            max      = double.MinValue;
            mean     = 0;
            variance = 0;
            _count   = 0;
        }

        public override void IngestValue(double value)
        {
            if (Math.Abs(value - PerfStatsManager.Instance.DEFAULT_ERROR_SAMPLE_VALUE) < 2.0)
                return;
            
            min = value < min ? value : min;
            max = value > max ? value : max;

            ++_count;

            double delta, delta2;

            if (value > mean)
            {
                delta  = value - mean;
                mean += delta / _count;
                delta2 = value - mean;
            }
            else
            {
                delta  = mean - value;
                mean -= delta / _count;
                delta2 = mean - value;
            }

            variance += delta * delta2;
        }

        int _count;
    }
}
