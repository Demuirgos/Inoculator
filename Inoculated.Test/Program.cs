namespace Program {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
        }

        [LogEntrency]
        public static void Test() {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            Console.WriteLine(i);
        }
    }
}