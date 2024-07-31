global using PublishStateMap =
#if NET9_0_OR_GREATER
System.Collections.Generic.OrderedDictionary<ushort, string>;
#else
OOs.Collections.Generic.OrderedHashMap<ushort, string>;
#endif