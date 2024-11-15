using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// tracks debug values over time and logs a summary when asked.
/// </summary>
public class DebugCollector<IdentifierType, ValueType>
{
    private Dictionary<IdentifierType, List<ValueType>> collectedValues;

    public DebugCollector()
    {
        collectedValues = new Dictionary<IdentifierType, List<ValueType>>();
    }

    public void CollectLog(IdentifierType identifier, ValueType value)
    {
        if (collectedValues.ContainsKey(identifier))
        {
            if(collectedValues[identifier] == null)
            {
                collectedValues[identifier] = new List<ValueType>();
            }
            collectedValues[identifier].Add(value);
        }
        else
        {
            List<ValueType> list = new List<ValueType>();
            list.Add(value);
            collectedValues.Add(identifier, list);
        }
    }

    public void SendDebug(IdentifierType identifier, Func<List<ValueType>, ValueType> aggregateFunction)
    {
        if (collectedValues.ContainsKey(identifier))
        {
            Debug.Log(identifier.ToString() + aggregateFunction(collectedValues[identifier]).ToString());
            collectedValues[identifier].Clear();
        }
    }

    public void SendDebug(Func<List<ValueType>, ValueType> aggregateFunction)
    {
        foreach (var identifier in collectedValues.Keys)
        {
            if (collectedValues[identifier] != null && collectedValues[identifier].Count != 0)
                SendDebug(identifier, aggregateFunction);
        }
    }
}
