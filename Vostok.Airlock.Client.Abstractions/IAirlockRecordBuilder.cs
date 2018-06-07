﻿using System;

namespace Vostok.Airlock.Client.Abstractions
{
    public interface IAirlockRecordBuilder
    {
        IAirlockRecordBuilder SetTimestamp(DateTimeOffset timestamp);
        IAirlockRecordBuilder Add(string key, byte value);
        IAirlockRecordBuilder Add(string key, short value);
        IAirlockRecordBuilder Add(string key, int value);
        IAirlockRecordBuilder Add(string key, long value);
        IAirlockRecordBuilder Add(string key, bool value);
        IAirlockRecordBuilder Add(string key, float value);
        IAirlockRecordBuilder Add(string key, double value);
        IAirlockRecordBuilder Add(string key, string value);
        IAirlockRecordBuilder Add(string key, byte[] values);
        IAirlockRecordBuilder Add(string key, short[] values);
        IAirlockRecordBuilder Add(string key, int[] values);
        IAirlockRecordBuilder Add(string key, long[] values);
        IAirlockRecordBuilder Add(string key, bool[] values);
        IAirlockRecordBuilder Add(string key, float[] values);
        IAirlockRecordBuilder Add(string key, double[] values);
        IAirlockRecordBuilder Add(string key, string[] values);
    }
}