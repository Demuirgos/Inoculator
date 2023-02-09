using System;
using System.Collections;
namespace Program {
    class Program : Printable<Program> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            _ = (new Program()).SumIntervalIsEven(7, 23);
        }
        

        [InvokeReflectiveAttribute]
        public bool SumIntervalIsEven(int start, int end) {
            int result = 0;
            for (int j = start; j < end; j++) {
                result += j;
            }
            Console.WriteLine(Name);
            return result % 2 == 0;
        }
    }
}