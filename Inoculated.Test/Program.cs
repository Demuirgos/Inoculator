using System.Collections;
namespace Program {
    struct  Student {
        public Student(string name, int age) {
            Name = name;
            Age = age;
        }
        public string Name;
        public int Age;
    }
    class Program {
        private string Name { get; set; } = "Test";
        async static Task Main(string[] args) {
            await TestAsyncI();
            await TestAsyncI();
            var p = new Program();
            var result = await p.TestAsyncS(23);
            Console.WriteLine(result);
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
        public async Task<Student> TestAsyncS(int l) {
            int i = 0;
            for (int j = 0; j < 100; j++) {
                i++;
            }
            return new Student(Name, l);
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