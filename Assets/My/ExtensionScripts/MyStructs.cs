using UnityEngine;

public class MyStructs
{
    
}

[System.Serializable]
public struct HandData<T>
{
    public T Left;
    public T Right;

    public HandData(T left, T right)
    {
        Left = left;
        Right = right;
    }
}