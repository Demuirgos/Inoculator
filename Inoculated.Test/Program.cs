namespace Program {
    class Program {
        async static Task Main(string[] args) {
            foreach (var item in TestEnum())
            {
                Console.WriteLine(item);
            }
        }

        [ElapsedTime, LogEntrency]
        public static int Test(int k, int m) {
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
        public static IEnumerable<int> TestEnum() {
            for (int j = 0; j < 100; j++) {
                yield return j;
            }
        }
    }
}