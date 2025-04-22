// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using MoMediatoR.Benchmarks;

Console.WriteLine("Hello, World!");
BenchmarkRunner.Run<MoMediatoRBenchmark>();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

