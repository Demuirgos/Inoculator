﻿using System;
using System.Collections;
namespace Program {
    class Entry : Printable<Entry> {
        public string Name {get; set;} = "Program";
        public static int RandomExceptionControl = 0;
        async static Task Main(string[] args) {
            try {
                Console.WriteLine(await Utils.Factorial(10));
                foreach (var value in Utils.Fibbonacci(10))
                {
                    Console.WriteLine(value);
                }

                await foreach (var value in Utils.Primes(10))
                {
                    Console.WriteLine(value);
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message + " " + e.StackTrace);
            }
        }
        
        class Utils {
            [LogEntrency]
            public static int SumUpTo(int lim) => (lim + 1) * lim / 2;
            [Retry<Entry>(10)]
            public static IEnumerable<int> Fibbonacci(int value) {
                int a = 0, b = 1;
                for (int i = 0; i < value; i++) {
                    yield return a;
                    int temp = a;
                    a = b;
                    b = temp + b;
                }
            } 
            [Retry<Entry>(10)]
            public static async Task<int> Factorial(int value) {
                return value <= 0 ? 1 : value * (await Factorial(value - 1));
            } 

            [LogEntrency]
            [Retry<Entry>(10)]
            public static async IAsyncEnumerable<int> Primes(int value) {
                static Task<bool> IsPrime(int n) => Task.Run(() => {
                    if(n % 2 == 0) return n == 2;
                    if(n % 3 == 0) return n == 3;
                    for (int i = 3; i <= Math.Sqrt(n); i+=2) {
                        if (n % i == 0) return false;
                    }
                    return true;
                });
                yield return 2;
                for (int i = 3; i < value; i+=2) {
                    if(await IsPrime(i)) yield return i;
                }
            } 
        }
    }
}