using System;
using System.Collections;
namespace Program {
    class Program : Printable<Program> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            var (start, end) = (0, 23);
            _ = (new Utils()).SumIntervalIsEven(start, ref end);
            _ = await Utils.SumIntervalIsEvenAsync(start, end);
            foreach(var d in Utils.SumIntervalIsEvenEnum(start, end)) {
                Console.WriteLine(d);
            }
        }
        
        class Utils {
            [ElapsedTime, Memoize]
            public static bool SumIntervalIsEven(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result % 2 == 0;
            }

            [Memoize]
            public static async Task<int> SumIntervalIsEvenAsync(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result;
            }

            [LogEntrency]
            public bool SumIntervalIsEven(int start, ref int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result % 2 == 0;
            }

            [Memoize]
            public static IEnumerable<int> SumIntervalIsEvenEnum(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                    yield return result;
                }
            }
        }
    }
}