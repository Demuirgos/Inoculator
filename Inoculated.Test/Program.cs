using System.Collections;
namespace Program {
    struct Program {
        async static Task Main(string[] args) {
            // Gen region
            _ = SlideSumWindow(out _);
            foreach(var kvp in CallCountAttribute.CallCounter) {
                Console.WriteLine($"{kvp.Key}: called {kvp.Value}: times");
            }
            Console.WriteLine(MetadataSink.CallCount);
        }
        

        [ElapsedTime, LogEntrency, CallCount]
        public static bool SumIntervalIsEven(int start, int end, out int r) {
            int result = 0;
            for (int j = start; j < end; j++) {
                result += j;
            }
            r = result;
            return r % 2 == 0;
        }

        [ElapsedTime, UpdateStaticClass<MetadataSink>]
        public static bool SlideSumWindow(out int r) {
            r = 0;
            for (int j = 7; j < 13; j++) {
                _ = SumIntervalIsEven(j, j + 23, out int result);
                r += result;
            }
            return r % 2 == 0;
        }
    }
}