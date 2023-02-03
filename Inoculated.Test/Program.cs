using System.Collections;
namespace Program {
    struct Program {
        async static Task Main(string[] args) {
            // Gen region
            _ = SumIntervalIsEven(7, 23, out _);
            foreach(var kvp in CallCountAttribute.CallCounter) {
                Console.WriteLine($"{kvp.Key}: called {kvp.Value}: times");
            }
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
    }
}