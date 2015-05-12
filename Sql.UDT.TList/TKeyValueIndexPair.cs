using System;
using System.Collections.Generic;
using System.Text;

public struct KeyValueIndexPair<TKey, TValue>
{
  public TKey   Key;
  public TValue Value;
  public Int32  Index;

  public KeyValueIndexPair(TKey key, TValue value, Int32 index)
  {
    Key   = key;
    Value = value;
    Index = index;
  }
}
