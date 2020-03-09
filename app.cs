using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

class App
{
    public static void Main(string[] args)
    {
        var path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        var framework = Path.GetFileName(Path.GetDirectoryName(path));
        var target = Path.GetFileName(path);

        var counts = new[] {     1,    10,  100, 1000, 10000, 100000 }; // various sizes for source type to be copied
        var inners = new[] { 10000, 10000, 1000,  100,    10,     10 }; // "unroll" to avoid nearing resolution of timer
        var outers = 100;                                               // pick the minimum time of this many runs as the perf stat

        if (args.Length > 0)
            outers = int.Parse(args[0]);

        for (var i = 0; i < counts.Length; ++i)
        {
            var count = counts[i];

            void Test(IEnumerable<int> enumerable, string name)
            {
                var timings = Bench(enumerable, inners[i], outers);

                var fields = new object[]
                {
                    framework,
                    target,
                    name,
                    count,
                    inners[i],
                    outers,
                    timings.Baseline.TotalMilliseconds,
                    timings.ToList.TotalMilliseconds,
                    timings.ToArray.TotalMilliseconds,
                };

                Console.WriteLine(string.Join(",", fields));
            }

            Test(Enumerable.Range(0, count), "Enumerable.Range");
            Test(YieldRange(count), "YieldRange");
            Test(YieldRange(count).ToList(), "List");
            Test(YieldRange(count).ToArray(), "Array");
        }
    }

    static IEnumerable<int> YieldRange(int count)
    {
        for (var i = 0; i < count; ++i)
            yield return i;
    }

    static void Minimize(ref TimeSpan timeSpan, in TimeSpan other)
    {
        if (timeSpan > other)
            timeSpan = other;
    }

    static double GetRatio(in TimeSpan baseline, in TimeSpan other) =>
        baseline.Ticks / (double)other.Ticks;

    class Timings
    {
        public TimeSpan Baseline = TimeSpan.MaxValue;
        public TimeSpan ToList = TimeSpan.MaxValue;
        public TimeSpan ToArray = TimeSpan.MaxValue;
    }

    static Timings Bench(IEnumerable<int> enumerable, int innerIters, int outerIters)
    {
        var timer = new Stopwatch();
        var timings = new Timings();

        // Baseline

        var array = enumerable.ToArray();

        for (var outerIter = 0; outerIter < outerIters; ++outerIter)
        {
            timer.Restart();

            for (var innerIter = 0; innerIter < innerIters; ++innerIter)
            {
                // get a reasonable baseline that does the basic operations (single alloc, copy known len). let's
                // call this the speed of light for a ToArray implementation. it's easily possible to detect cases where say
                // a memcpy could be used, but wait for an impl to actually do this before lowering the baseline.

                var len = array.Length;
                var temp = new int[len];
                Array.Copy(array, temp, len);
            }

            timer.Stop();
            Minimize(ref timings.Baseline, timer.Elapsed);
        }

        // ToList

        for (var outerIter = 0; outerIter < outerIters; ++outerIter)
        {
            timer.Restart();

            for (var innerIter = 0; innerIter < innerIters; ++innerIter)
                enumerable.ToList();

            timer.Stop();
            Minimize(ref timings.ToList, timer.Elapsed);
        }

        // ToArray

        for (var outerIter = 0; outerIter < outerIters; ++outerIter)
        {
            timer.Restart();

            for (var innerIter = 0; innerIter < innerIters; ++innerIter)
                enumerable.ToArray();

            timer.Stop();
            Minimize(ref timings.ToArray, timer.Elapsed);
        }

        return timings;
    }
}
