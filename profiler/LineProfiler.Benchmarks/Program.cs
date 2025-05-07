// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using LineProfiler.Benchmarks;


var summary = BenchmarkRunner.Run<MethodBenchmarks>();
