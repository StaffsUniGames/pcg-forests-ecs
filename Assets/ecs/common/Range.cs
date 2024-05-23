using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Range<T>
{
    public T min;
    public T max;

    public Range(T min, T max)
    {
        this.min = min;
        this.max = max;
    }

    public void Set(T min, T max)
    {
        this.min = min;
        this.max = max;
    }

    public void Set((T min, T max) arg)
    {
        this.min = arg.min;
        this.max = arg.max;
    }

    public static implicit operator Range<T>((T min, T max) arg) => new Range<T>(arg.min, arg.max);
    public static implicit operator (T min, T max)(Range<T> arg) => (arg.min, arg.max);
}