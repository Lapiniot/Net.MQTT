using System.Net.Mqtt.Benchmarks.Extensions;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<TopicMatchingBenchmarks>();

internal sealed partial class Program { }