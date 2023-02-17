using System;
using System.Collections;
namespace Program {
    class Entry : Printable<Entry> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            try {
                Console.WriteLine(await Utils.Factorial(10));
            } catch (Exception e) {
                Console.WriteLine(e.Message + " " + e.StackTrace);
            }
            foreach (var value in Utils.Fibbonacci(10))
            {
                    Console.WriteLine(value);
            }
        }
        
        class Utils {
            [LogEntrency]
            public static int SumUpTo(int lim) => (lim + 1) * lim / 2;
            [Retry<Entry>(10)]
            public static IEnumerable<int> Fibbonacci(int value) {
                int a = 0, b = 1;
                for (int i = 0; i < value; i++) {
                    yield return a;
                    int temp = a;
                    a = b;
                    b = temp + b;
                }
            } 
            static int random = 0;
            [Retry<Entry>(10)]
            public static async Task<int> Factorial(int value) {
                if(random++ == 3) {
                    throw new Exception("Random exception");
                }
                return value <= 0 ? 1 : value * (await Factorial(value - 1));
            } 
        }
    }
}