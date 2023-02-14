using System;
using System.Collections;
namespace Program {
    class Entry : Printable<Entry> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            _ = Utils.SumUpTo(10);
            _ = Utils.Factorial(10);
            _ = Utils.SumUpToRef(10, out int result);
            _ = Utils.FactorialRef(10, ref result);
            _ = await Utils.SumUpToAsync(10);
            _ = await Utils.FactorialAsync(10);
        }
        
        class Utils {
            [Duration, LogEntrency]
            public static int SumUpTo(int lim) => (lim + 1) * lim / 2;

            [ElapsedTime, LogEntrency]
            public static int Factorial(int value) 
                => value <= 0 ? 1 : value * Factorial(value - 1);

                
            [Duration, LogEntrency]
            public static int SumUpToRef(int lim, out int result) {
                result = (lim + 1) * lim / 2;
                return result;
            } 

            [ElapsedTime, LogEntrency]
            public static int FactorialRef(int value, ref int result) {
                result = value <= 0 ? 1 : value * FactorialRef(value - 1, ref result);
                return result;
            } 

            [ElapsedTime, LogEntrency]
            public static async Task<int> SumUpToAsync(int lim) {
                return (lim + 1) * lim / 2;
            }

            [Duration, LogEntrency]
            public static async Task<int> FactorialAsync(int lim) {
                int result = lim <= 0 ? 1 : lim * await FactorialAsync(lim - 1);
                throw new Exception("test");
                return result;
            }

            
            [ElapsedTime, LogEntrency]
            public static IEnumerable<int> SumUpToEnum(int lim) {
                yield return (lim + 1) * lim / 2;
            }

            [Duration, LogEntrency]
            public static IEnumerable<int> FibbEnum(int lim) {
                int a = 0, b = 1, result = 0;
                for (int i = 0; i < lim; i++) {
                    result = a + b;
                    a = b;
                    b = result;
                    yield return result;
                }
            }
        }
    }
}