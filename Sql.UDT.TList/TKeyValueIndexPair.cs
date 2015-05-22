using System;
using System.Collections.Generic;
using System.Text;

public struct KeyValueIndexPair<TKey, TValue, TIndex>
{
  public TKey   Key;
  public TValue Value;
  public TIndex Index;

  public KeyValueIndexPair(TKey key, TValue value, TIndex index)
  {
    Key   = key;
    Value = value;
    Index = index;
  }
}
