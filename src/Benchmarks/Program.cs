using System;
using BenchmarkDotNet.Running;

#if DEBUG
#warning Benchmarking software ran in DEBUG mode
#else
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
