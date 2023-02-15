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
            [DateAndTime, LogEntrency]
            public static int SumUpTo(int lim) => (lim + 1) * lim / 2;

            [LogEntrency, DurationLogger<Entry>]
            public static int Factorial(int value) 
                => value <= 0 ? 1 : value * Factorial(value - 1);
        }
    }
}