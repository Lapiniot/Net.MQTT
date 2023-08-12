global using Utf8StringPair = (System.ReadOnlyMemory<byte> Name, System.ReadOnlyMemory<byte> Value);
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: CLSCompliant(false)]
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]
