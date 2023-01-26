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
    struct Program {
        public string Name { get; set; }
        async static Task Main(string[] args) {
            var p = new Program();
            p.Name = "Hello";
            foreach (var item in p.TestEnumI(100))
            {
                Console.WriteLine(item);
            }
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