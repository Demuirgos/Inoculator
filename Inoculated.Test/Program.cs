namespace Program {
    class Program {
        async static Task Main(string[] args) {
            var p = new Program();
            foreach (var item in p.TestEnumI(100))
            {
                Console.WriteLine(item);
            }

            foreach (var item in TestEnumS(100))
            {
                Console.WriteLine(item);
            }

            p.TestI(100, 10);
            TestS(100, 10);
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
        public static async Task TestAsync() {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            Console.WriteLine(i);
        }

        [ElapsedTime, LogEntrency]
        public IEnumerable<(string, int)> TestEnumI(int k) {
            for (int j = 0; j < k; j++) {
                yield return ($"Iteration {j}", 23);
            }
        }
        
        [ElapsedTime, LogEntrency]
        public static IEnumerable<string> TestEnumS(int k) {
            for (int j = 0; j < k; j++) {
                yield return $"Iteration {j}";
            }
        }
    }
}