using System.Collections.Generic;
using UnityEngine;

public class SignalProvider : MonoBehaviour, ILearningSignalProvider
{
    [SerializeField] private Sensors sensors;
    [SerializeField] private LearningState learningState;

    public void CollectSignals(List<Signal> signals)
    {
        if (signals == null)
        {
            return;
        }

        if (sensors != null)
        {
            IReadOnlyList<Signal> sensorSignals = sensors.Signals;
            for (int i = 0; i < sensorSignals.Count; i++)
            {
                Signal signal = sensorSignals[i];

                if (string.IsNullOrWhiteSpace(signal.Id))
                {
                    continue;
                }

                signals.Add(signal);
            }
        }

        if (learningState != null)
        {
            IReadOnlyList<Signal> learningSignals = learningState.Signals;
            for (int i = 0; i < learningSignals.Count; i++)
            {
                Signal signal = learningSignals[i];

                if (string.IsNullOrWhiteSpace(signal.Id))
                {
                    continue;
                }

                signals.Add(signal);
            }
        }
    }
}