using System;
using System.Collections.Generic;
using Xunit;
using System.Diagnostics;

namespace RobustCollections.Tests
{
    public class RobustDictionaryTests
    {
        [Fact]
        public void LookupWithExistingDictionary()
        {
            var dict = new RobustDictionary(new Dictionary<string, string>() {
                { "a", "b" },
                { "b", "c" },
                { "xx", "yy" }
            });

            string res = null;
            Assert.True(dict.TryGetValue("a", out res));
            Assert.Equal("b", res);

            Assert.True(dict.TryGetValue("b", out res));
            Assert.Equal("c", res);

            Assert.True(dict.TryGetValue("xx", out res));
            Assert.Equal("yy", res);

            Assert.False(dict.TryGetValue("yy", out res));
            Assert.Null(res);

        }

        Dictionary<string, string> MkDictionary (int size) {
            Dictionary<string, string> res = new Dictionary<string, string>();
            for (int i = 0; i < size; ++i)
                res[i.ToString()] = (i* 1199).ToString();
            return res;
        }

        long GcAndMeasure(out long gcTime) {
            var s = Stopwatch.StartNew();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            s.Stop();
            gcTime = s.ElapsedMilliseconds;
            return GC.GetTotalMemory(false);
        }

        [Fact]
        public void CheckMemUsage (){
            var regularStart = GcAndMeasure(out long _ignore);
            var dicts = new List<object> ();
            for(int i = 0; i < 50; ++i)
                dicts.Add(MkDictionary(100_000));
            var regularEnd = GcAndMeasure(out long regularGcTime);
            dicts = new List<object>();

            var robustStart = GcAndMeasure(out long _ignore2);
            dicts = new List<object> ();
            for(int i = 0; i < 50; ++i)
                dicts.Add(new RobustDictionary(MkDictionary(100_000)));
            var robustEnd = GcAndMeasure(out long robustGcTime);

            Console.WriteLine($"regular mem: {regularEnd - regularStart} gc: {regularGcTime}");
            Console.WriteLine($"robust  mem: {robustEnd - robustStart} gc: {robustGcTime}");

        }
    }
}
