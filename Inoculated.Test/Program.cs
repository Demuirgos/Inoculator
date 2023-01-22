using System.Collections;
namespace Program {
    class Program {
        async static Task Main(string[] args) {
            await TestAsyncI();
            await TestAsyncI();
            await TestAsyncI();
            await TestAsyncI();
        }

        [ElapsedTime, LogEntrency]
        public static int TestS(int k, int m) {
            int i = 0;
            for (int j = 0; j < k; j++) {
                i += m + j;
            }
            return i;
        }

        [ElapsedTime, LogEntrency]
        public int TestI(int k, int m) {
            int i = 0;
            for (int j = 0; j < k; j++) {
                i += m + j;
            }
            return i;
        }

        [ElapsedTime, LogEntrency]
        public static async Task TestAsyncI() {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            Console.WriteLine(i);
        }

        [ElapsedTime, LogEntrency]
        public static async Task TestAsyncS() {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            Console.WriteLine(i);
        }

        [ElapsedTime, LogEntrency]
        public IEnumerable<string> TestEnumI(int k) {
            for (int j = 0; j < k; j++) {
                yield return $"Iteration {j}";
            }
        }
        
        [ElapsedTime, LogEntrency]
        public static IEnumerable<string> TestEnumS(int k) {
            for (int j = 0; j < k; j++) {
                yield return $"Iteration {j}";
            }
        }

        [ElapsedTime, LogEntrency]
        public static IEnumerable TestEnumSU(int k) {
            for (int j = 0; j < k; j++) {
                yield return $"Iteration {j}";
            }
        }
    }
}