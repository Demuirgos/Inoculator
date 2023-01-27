using System.Collections;
namespace Program {
    struct Program {
        async static Task Main(string[] args) {
           var r = Gen.TestS<int>(23, 0);
           Console.WriteLine(r);
        }
    }
    class Gen {
        [ElapsedTime, LogEntrency]
        public static int TestS<T>(T k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                i += m + j;
            }
            return i;
        }
    }
}