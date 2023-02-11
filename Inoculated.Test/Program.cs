﻿using System;
using System.Collections;
namespace Program {
    class Program : Printable<Program> {
        public string Name {get; set;} = "Program";
        async static Task Main(string[] args) {
            _ = await Utils.SumIntervalIsEvenAsync(0, 0);
            foreach(var d in Utils.SumIntervalIsEvenEnum(0, 23)) {
                Console.WriteLine(d);
            }
        }
        
        static class Utils {
            [ElapsedTime, Reflective]
            public static bool SumIntervalIsEven(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result % 2 == 0;
            }

            [Reflective]
            public static async Task<int> SumIntervalIsEvenAsync(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                return result;
            }

            [LogEntrency]
            public static bool SumIntervalIsEven<T>(T gen, int start, ref int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                }
                end = 123;
                return result % 2 == 0;
            }

            [Reflective]
            public static IEnumerable<bool> SumIntervalIsEvenEnum(int start, int end) {
                int result = 0;
                for (int j = start; j < end; j++) {
                    result += j;
                    yield return result % 2 == 0;
                }
            }
        }
    }
}