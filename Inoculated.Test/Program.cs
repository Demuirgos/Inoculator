using System;
using System.Collections;
namespace Program {
    class Program : Printable<Program> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            _ = await Utils<int>.SumIntervalIsEvenAsync<int>(0, 0, 23);
        }
        
        static class Utils<T> {
            [ElapsedTime, Reflective]
            public static bool SumIntervalIsEven(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result % 2 == 0;
            }

            [Reflective]
            public static async Task<int> SumIntervalIsEvenAsync<U>(U gen, int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result;
            }

            [LogEntrency, Reflective]
            public static bool SumIntervalIsEven<T>(T gen, int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result % 2 == 0;
            }

            [Reflective]
            public static IEnumerable<bool> SumIntervalIsEvenEnum(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                    yield return result % 2 == 0;
                }
            }
        }
    }
}