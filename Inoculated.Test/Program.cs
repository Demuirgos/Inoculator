using System.Collections;
namespace Program {
    struct Program {
        async static Task Main(string[] args) {
            // Gen region
            var rg = await NGen.TestC(3, 0);
            Console.WriteLine(rg);
        }
    }
    class Gen<P> {
        [ElapsedTime, LogEntrency]
        public static async Task<int> TestA<T, U>(T k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                i += m + j;
            }
            return i;
        }

        [ElapsedTime, LogEntrency]
        public static IEnumerable<int> TestE<T>(T k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                yield return j;
            }
        }

        
        [ElapsedTime, LogEntrency]
        public static int TestS<T>(T k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                i += m + j;
            }
            return i;
        }

        [ElapsedTime, LogEntrency]
        public static int TestSR<T>(ref T k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                i += m + j;
            }
            return i;
        }
    }

    class NGen {
        [ElapsedTime, LogEntrency]
        public static async Task<int> SumUntil1(int k) {
            int i = 0;
            for (int j = 0; j <= k; j++) {
                i += j;
            }
            return i;
        }

        [ElapsedTime, LogEntrency]
        public static async Task<int> SumUntil2(int k) {
            int i = k * (k + 1) / 2;
            return i;
        }

        [ElapsedTime, LogEntrency]
        public static async Task<int> TestC(int k, int m) {
            int r1 = await SumUntil1(k);
            int r2 = await SumUntil2(k);
            return r1 + r2 + m;
        }

        [ElapsedTime, LogEntrency]
        public static IEnumerable<int> TestE(int k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                yield return j;
            }
        }

        
        [ElapsedTime, LogEntrency]
        public static int TestS(int k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                i += m + j;
            }
            return i;
        }
    }
}