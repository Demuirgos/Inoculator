using System.Collections;
namespace Program {
    struct Program {
        async static Task Main(string[] args) {
            // Gen region
            var rg = await Gen<int>.TestA<int, int>(23, 100);
            Console.WriteLine(rg);
            foreach (var itemg in Gen<int>.TestE<int>(23, 100)) {
                Console.WriteLine(itemg);
            }
            var sg = Gen<int>.TestS<int>(23, 100); 
            var srg = Gen<int>.TestSR<int>(ref sg, 100);   
            // NGen region
            var rng = await NGen.TestA(23, 100);
            Console.WriteLine(rng);
            foreach (var itemng in NGen.TestE(23, 100)) {
                Console.WriteLine(itemng);
            }
            var sng = NGen.TestS(23, 100);
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
        public static async Task<int> TestA(int k, int m) {
            int i = 0;
            for (int j = 0; j < m; j++) {
                i += m + j;
            }
            return i;
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