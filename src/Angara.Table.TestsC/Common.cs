using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.FSharp.Core;
using NUnit.Framework;

namespace Angara.Data.TestsC
{
    public static class Common
    {
        public static Random random = new Random();

        public static string RandomString(Random random, int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789";

            var rndChars = Enumerable.Range(0, length).Select(i => chars[random.Next(chars.Length)]).ToArray();

            return new string(rndChars);
        }

        public static DateTime RandomDate(Random random)
        {
            var start = new DateTime(0);
            var range = (DateTime.Today - start).Days;

            return start.AddDays(random.Next(range));
        }

        public static double[] RandomDoubleArray(Random random, int length)
        {
            return Enumerable.Range(0, length).Select(i => random.NextDouble()).ToArray();
        }

        public static int[] RandomIntArray(Random random, int length)
        {
            return Enumerable.Range(0, length).Select(i => random.Next()).ToArray();
        }

        public static string[] RandomStringArray(Random random, int length1, int length2)
        {
            return Enumerable.Range(0, length1).Select(i => RandomString(random, length2).ToLower()).ToArray();
        }

        public static DateTime[] RandomDateArray(Random random, int length1)
        {
            return Enumerable.Range(0, length1).Select(i => RandomDate(random)).ToArray();
        }

        public static void TestOptionArraySome<T, U>(T[] expected, FSharpOption<U> actual)
        {
            Assert.IsTrue(FSharpOption<U>.get_IsSome(actual));
            Assert.AreEqual(expected, actual.Value);
        }

    }
}
