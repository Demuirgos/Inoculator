namespace Program {
    class Program {
        async static Task Main(string[] args) {
            Test();
            await TestAsync();
        }

        [ElapsedTime, LogEntrency]
        public static void Test() {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            Console.WriteLine(i);
        }

        public static async Task TestAsync() {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            Console.WriteLine(i);
        }
    }
}