using System;
using System.Collections;
namespace Program {
    class Program : Printable<Program> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            int end = 23;
            _ = Utils.SumIntervalIsEven<object>(new object(), 7, ref end);
            _ = await Utils.SumIntervalIsEvenAsync(7, end);
            foreach(bool value in Utils.SumIntervalIsEvenEnum(7, end)) {
                Console.WriteLine(value);
            }
            Console.WriteLine(end);
        }
        
        static class Utils {

            [LogEntrency]
            public static bool SumIntervalIsEven<T>(T gen, int start, ref int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                end = 123;
                return result % 2 == 0;
            }

            [LogEntrency]
            public static async Task<bool> SumIntervalIsEvenAsync(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result % 2 == 0;
            }

            [LogEntrency]
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