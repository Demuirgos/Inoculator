using System.Collections;
namespace Program {
    struct Program {
        async static Task Main(string[] args) {
           TestS(out int start, 10);
        }

        [ElapsedTime, LogEntrency]
        public static int TestS(out int k, int m) {
            int i = 0;
            k = 23;
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
        public async Task<Student> TestAsyncS(int l) {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            return new Student(Name, l);
        }

        [ElapsedTime, LogEntrency]
        public IEnumerable<(string, int)> TestEnumI(int k) {
            for (int j = 0; j < k; j++) {
                yield return ($"Iteration {Name}", 23);
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