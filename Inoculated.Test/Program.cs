using System;
using System.Collections;
namespace Program {
    class Entry : Printable<Entry> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            _ = Utils.SumUpTo(10);
           _ = Utils.Factorial(10);
        }
        
        class Utils {
            [LogEntrency, Wire(23), DateAndTime]
            public static int SumUpTo(int lim) => (lim + 1) * lim / 2;
            static int random = 3;
            [LogEntrency, Retry(10)]
            public static int Factorial(int value) {
                if(random != 0) {
                    random--;
                    throw new Exception("Random exception");
                }
                return value <= 0 ? 1 : value * Factorial(value - 1);
            }
        }
    }
}