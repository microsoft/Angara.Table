using Angara.Data;
using Microsoft.FSharp.Core;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace Angara.Data.TestsC
{
    [TestFixture]
    public class ColumnTests
    {
        public static void TestColumnType<T>(Column column)
        {
            Assert.AreEqual(typeof(T), Column.Type(column));
        }

        public static void TestColumnTrySubPass<T>(T[] data, Column column, Random random, bool recurse)
        {
            Assert.Greater(data.Length / 4, 1);
            var subLength = random.Next(data.Length / 4, data.Length / 2);

            Assert.Greater(data.Length - 1 - subLength, 1);
            var subStart = random.Next(1, data.Length - 1 - subLength);

            var subData = new T[subLength];
            Array.Copy(data, subStart, subData, 0, subLength);

            // Test 4 overloads of TrySub
            if (recurse == true)
            {
                FSharpOption<Column> subTryColumnC = Column.TrySub<Column>(subStart, subLength, column);
                Assert.IsTrue(FSharpOption<Column>.get_IsSome(subTryColumnC));
                TestColumn<T>(subData, subTryColumnC.Value, random, false);
            }

            FSharpOption<IRArray<T>> subTryColumnI = Column.TrySub<IRArray<T>>(subStart, subLength, column);
            Common.TestOptionArraySome<T, IRArray<T>>(subData, subTryColumnI);

            FSharpOption<T[]> subTryColumnT = Column.TrySub<T[]>(subStart, subLength, column);
            Common.TestOptionArraySome<T, T[]>(subData, subTryColumnT);

            FSharpOption<Array> subTryColumnA = Column.TrySub<Array>(subStart, subLength, column);
            Common.TestOptionArraySome<T, Array>(subData, subTryColumnA);
        }

        public static void TestColumnTrySubPassFull<T>(T[] data, Column column)
        {
            FSharpOption<Column> subColumnC = Column.TrySub<Column>(0, data.Length, column);
            Assert.IsTrue(FSharpOption<Column>.get_IsSome(subColumnC));
            Assert.AreEqual(Column.ToArray<Array>(column), Column.ToArray<Array>(subColumnC.Value));

            FSharpOption<IRArray<T>> subTryColumnI = Column.TrySub<IRArray<T>>(0, data.Length, column);
            Common.TestOptionArraySome<T, IRArray<T>>(data, subTryColumnI);

            FSharpOption<T[]> subTryColumnT = Column.TrySub<T[]>(0, data.Length, column);
            Common.TestOptionArraySome<T, T[]>(data, subTryColumnT);

            FSharpOption<Array> subTryColumnA = Column.TrySub<Array>(0, data.Length, column);
            Common.TestOptionArraySome<T, Array>(data, subTryColumnA);
        }

        public static void TestColumnTrySubFailBounds<T>(T[] data, Column column, int subStart, int subLength)
        {
            FSharpOption<Column> subColumnC = Column.TrySub<Column>(subStart, subLength, column);
            Assert.IsTrue(FSharpOption<Column>.get_IsNone(subColumnC));

            FSharpOption<IRArray<T>> subTryColumnI = Column.TrySub<IRArray<T>>(subStart, subLength, column);
            Assert.IsTrue(FSharpOption<IRArray<T>>.get_IsNone(subTryColumnI));

            FSharpOption<T[]> subTryColumnT = Column.TrySub<T[]>(subStart, subLength, column);
            Assert.IsTrue(FSharpOption<T[]>.get_IsNone(subTryColumnT));

            FSharpOption<Array> subTryColumnA = Column.TrySub<Array>(subStart, subLength, column);
            Assert.IsTrue(FSharpOption<Array>.get_IsNone(subTryColumnA));
        }

        public static void TestColumnTrySubFailTypes<T, U>(T[] data, Column column, Random random, bool recurse)
        {
            Assert.Greater(data.Length / 4, 1);
            var subLength = random.Next(data.Length / 4, data.Length / 2);

            Assert.Greater(data.Length - 1 - subLength, 1);
            var subStart = random.Next(1, data.Length - 1 - subLength);

            FSharpOption<IRArray<U>> subTryColumnI = Column.TrySub<IRArray<U>>(subStart, subLength, column);
            Assert.IsTrue(FSharpOption<IRArray<U>>.get_IsNone(subTryColumnI));

            FSharpOption<U[]> subTryColumnT = Column.TrySub<U[]>(subStart, subLength, column);
            Assert.IsTrue(FSharpOption<U[]>.get_IsNone(subTryColumnT));
        }

        public static void TestColumnTrySub<T>(T[] data, Column column, Random random, bool recurse, Type[] validTypes)
        {
            foreach (var validType in validTypes)
            {
                if (typeof(T) == validType)
                {
                    TestColumnTrySubPass<T>(data, column, random, recurse);
                    TestColumnTrySubPassFull<T>(data, column);

                    TestColumnTrySubFailBounds<T>(data, column, -1, 1);
                    TestColumnTrySubFailBounds<T>(data, column, 1, data.Length);
                }
                else
                {
                    MethodInfo method = typeof(ColumnTests).GetMethod("TestColumnTrySubFailTypes");
                    MethodInfo generic = method.MakeGenericMethod(typeof(T), validType);
                    generic.Invoke(null, new object[] { data, column, random, recurse });
                }
            }
        }

        public static void TestColumnSubPass<T>(T[] data, Column column, Random random, bool recurse)
        {
            Assert.Greater(data.Length / 4, 1);
            var subLength = random.Next(data.Length / 4, data.Length / 2);

            Assert.Greater(data.Length - 1 - subLength, 1);
            var subStart = random.Next(1, data.Length - 1 - subLength);

            var subData = new T[subLength];
            Array.Copy(data, subStart, subData, 0, subLength);

            // Test 4 overloads of Sub
            if (recurse == true)
            {
                Column subColumnC = Column.Sub<Column>(subStart, subLength, column);
                TestColumn<T>(subData, subColumnC, random, false);
            }

            IRArray<T> subColumnI = Column.Sub<IRArray<T>>(subStart, subLength, column);
            Assert.AreEqual(subData, subColumnI);

            T[] subColumnT = Column.Sub<T[]>(subStart, subLength, column);
            Assert.AreEqual(subData, subColumnT);

            Array subColumnA = Column.Sub<Array>(subStart, subLength, column);
            Assert.AreEqual(subData, subColumnA);
        }

        public static void TestColumnSubPassFull<T>(T[] data, Column column)
        {
            Column subColumnC = Column.Sub<Column>(0, data.Length, column);
            Assert.AreEqual(Column.ToArray<Array>(column), Column.ToArray<Array>(subColumnC));

            IRArray<T> subColumnI = Column.Sub<IRArray<T>>(0, data.Length, column);
            Assert.AreEqual(data, subColumnI);

            T[] subColumnT = Column.Sub<T[]>(0, data.Length, column);
            Assert.AreEqual(data, subColumnT);

            Array subColumnA = Column.Sub<Array>(0, data.Length, column);
            Assert.AreEqual(data, subColumnA);
        }

        public static void TestColumnSubFailBounds<T>(T[] data, Column column, int subStart, int subLength)
        {
            Assert.Throws<System.Exception>(delegate { Column.Sub<Column>(subStart, subLength, column); });
            Assert.Throws<System.Exception>(delegate { Column.Sub<IRArray<T>>(subStart, subLength, column); });
            Assert.Throws<System.Exception>(delegate { Column.Sub<T[]>(subStart, subLength, column); });
            Assert.Throws<System.Exception>(delegate { Column.Sub<Array>(subStart, subLength, column); });
        }

        public static void TestColumnSubFailTypes<T, U>(T[] data, Column column, Random random, bool recurse)
        {
            Assert.Greater(data.Length / 4, 1);
            var subLength = random.Next(data.Length / 4, data.Length / 2);

            Assert.Greater(data.Length - 1 - subLength, 1);
            var subStart = random.Next(1, data.Length - 1 - subLength);

            Assert.Throws<System.Exception>(delegate { Column.Sub<IRArray<U>>(subStart, subLength, column); });
            Assert.Throws<System.Exception>(delegate { Column.Sub<U[]>(subStart, subLength, column); });
        }

        public static void TestColumnSub<T>(T[] data, Column column, Random random, bool recurse, Type[] validTypes)
        {
            foreach (var validType in validTypes)
            {
                if (typeof(T) == validType)
                {
                    TestColumnSubPass<T>(data, column, random, recurse);
                    TestColumnSubPassFull<T>(data, column);

                    TestColumnSubFailBounds<T>(data, column, -1, 1);
                    TestColumnSubFailBounds<T>(data, column, 1, data.Length);
                }
                else
                {
                    MethodInfo method = typeof(ColumnTests).GetMethod("TestColumnSubFailTypes");
                    MethodInfo generic = method.MakeGenericMethod(typeof(T), validType);
                    generic.Invoke(null, new object[] { data, column, random, recurse });
                }
            }
        }

        public static void TestColumnCount<T>(T[] data, Column column)
        {
            Assert.AreEqual(data.Length, Column.Count(column));
        }

        public static void TestColumnTryItemPass<T>(T[] data, Column column)
        {
            Assert.AreEqual(FSharpOption<T>.None, Column.TryItem<T>(-1, column));
            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(FSharpOption<T>.Some(data[i]), Column.TryItem<T>(i, column));
            }
            Assert.AreEqual(FSharpOption<T>.None, Column.TryItem<T>(data.Length, column));
        }

        public static void TestColumnTryItemFail<T, U>(T[] data, Column column)
        {
            Assert.AreEqual(FSharpOption<int>.None, Column.TryItem<U>(-1, column));
            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(FSharpOption<int>.None, Column.TryItem<U>(i, column));
            }
            Assert.AreEqual(FSharpOption<int>.None, Column.TryItem<U>(data.Length, column));
        }

        public static void TestColumnTryItem<T>(T[] data, Column column, Type[] validTypes)
        {
            foreach (var validType in validTypes)
            {
                if (typeof(T) == validType)
                {
                    TestColumnTryItemPass<T>(data, column);
                }
                else
                {
                    MethodInfo method = typeof(ColumnTests).GetMethod("TestColumnTryItemFail");
                    MethodInfo generic = method.MakeGenericMethod(typeof(T), validType);
                    generic.Invoke(null, new object[] { data, column });
                }
            }
        }

        public static void TestColumnItemPass<T>(T[] data, Column column)
        {
            Assert.Throws<System.Exception>(delegate { Column.Item<T>(-1, column); });
            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(data[i], Column.Item<T>(i, column));
            }
            Assert.Throws<System.Exception>(delegate { Column.Item<T>(data.Length, column); });
        }

        public static void TestColumnItemFail<T, U>(T[] data, Column column)
        {
            Assert.Throws<System.Exception>(delegate { Column.Item<U>(-1, column); });
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Throws<System.Exception>(delegate { Column.Item<U>(i, column); });
            }
            Assert.Throws<System.Exception>(delegate { Column.Item<U>(data.Length, column); });
        }

        public static void TestColumnItem<T>(T[] data, Column column, Type[] validTypes)
        {
            foreach (var validType in validTypes)
            {
                if (typeof(T) == validType)
                {
                    TestColumnItemPass<T>(data, column);
                }
                else
                {
                    MethodInfo method = typeof(ColumnTests).GetMethod("TestColumnItemFail");
                    MethodInfo generic = method.MakeGenericMethod(typeof(T), validType);
                    generic.Invoke(null, new object[] { data, column });
                }
            }
        }

        public static void TestColumnTryToArrayPass<T>(T[] data, Column column)
        {
            FSharpOption<Column> columnC = Column.TryToArray<Column>(column);
            Assert.IsTrue(FSharpOption<Column>.get_IsSome(columnC));
            Assert.AreEqual(column, columnC.Value);

            FSharpOption<IRArray<T>> tryColumnI = Column.TryToArray<IRArray<T>>(column);
            Common.TestOptionArraySome<T, IRArray<T>>(data, tryColumnI);

            FSharpOption<T[]> tryColumnT = Column.TryToArray<T[]>(column);
            Common.TestOptionArraySome<T, T[]>(data, tryColumnT);

            FSharpOption<Array> tryColumnA = Column.TryToArray<Array>(column);
            Common.TestOptionArraySome<T, Array>(data, tryColumnA);
        }

        public static void TestColumnTryToArrayFail<T, U>(T[] data, Column column)
        {
            FSharpOption<IRArray<U>> tryColumnI = Column.TryToArray<IRArray<U>>(column);
            Assert.IsTrue(FSharpOption<IRArray<U>>.get_IsNone(tryColumnI));

            FSharpOption<U[]> tryColumnT = Column.TryToArray<U[]>(column);
            Assert.IsTrue(FSharpOption<U[]>.get_IsNone(tryColumnT));
        }

        public static void TestColumnTryToArray<T>(T[] data, Column column, Type[] validTypes)
        {
            foreach (var validType in validTypes)
            {
                if (typeof(T) == validType)
                {
                    TestColumnTryToArrayPass<T>(data, column);
                }
                else
                {
                    MethodInfo method = typeof(ColumnTests).GetMethod("TestColumnTryToArrayFail");
                    MethodInfo generic = method.MakeGenericMethod(typeof(T), validType);
                    generic.Invoke(null, new object[] { data, column });
                }
            }
        }

        public static void TestColumnToArrayPass<T>(T[] data, Column column)
        {
            Column columnC = Column.ToArray<Column>(column);
            Assert.AreEqual(column, columnC);

            IRArray<T> columnI = Column.ToArray<IRArray<T>>(column);
            Assert.AreEqual(data, columnI);

            T[] columnT = Column.ToArray<T[]>(column);
            Assert.AreEqual(data, columnT);

            Array columnA = Column.ToArray<Array>(column);
            Assert.AreEqual(data, columnA);
        }

        public static void TestColumnToArrayFail<T, U>(T[] data, Column column)
        {
            Assert.Throws<System.Exception>(delegate { Column.ToArray<IRArray<U>>(column); });
            Assert.Throws<System.Exception>(delegate { Column.ToArray<U[]>(column); });
        }

        public static void TestColumnToArray<T>(T[] data, Column column, Type[] validTypes)
        {
            foreach (var validType in validTypes)
            {
                if (typeof(T) == validType)
                {
                    TestColumnToArrayPass<T>(data, column);
                }
                else
                {
                    MethodInfo method = typeof(ColumnTests).GetMethod("TestColumnToArrayFail");
                    MethodInfo generic = method.MakeGenericMethod(typeof(T), validType);
                    generic.Invoke(null, new object[] { data, column });
                }
            }
        }

        public static void TestColumn<T>(T[] data, Column column, Random random, bool recurse)
        {
            Type[] validTypes = Column.ValidTypes;

            TestColumnType<T>(column);
            TestColumnTrySub<T>(data, column, random, recurse, validTypes);
            TestColumnSub<T>(data, column, random, recurse, validTypes);
            TestColumnTryToArray<T>(data, column, validTypes);
            TestColumnToArray<T>(data, column, validTypes);
            TestColumnCount<T>(data, column);
            TestColumnTryItem<T>(data, column, validTypes);
            TestColumnItem<T>(data, column, validTypes);
        }

        public static void TestIntMap1(int[] data, Column column)
        {
            Func<int, double> map = x => (double)x + 0.5;
            var fmap = FSharpFunc<int, double>.FromConverter(new Converter<int, double>(map));

            var expected = data.Select(map);
            var actual = Column.Map<int, double, double> (fmap, new[] { column });
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ColumnC_TestIntRealColumnMap2()
        {
            var data1 = Common.RandomIntArray(Common.random, 100);
            var column1 = Column.New<int[]>(data1);

            var data2 = Common.RandomDoubleArray(Common.random, 100);
            var column2 = Column.New<double[]>(data2);

            Func<int, double, double> map = (x, y) => (double)x * y;

            FSharpFunc<int, FSharpFunc<double, double>> fmap =
                FuncConvert.ToFSharpFunc<int, FSharpFunc<double, double>>(
                    new Converter<int, FSharpFunc<double, double>>(
                        i =>
                            FuncConvert.ToFSharpFunc(
                                new Converter<double, double>(
                                    x =>
                                        map(i, x)))));

            var expected = data1.Zip(data2, map);

            var actual = Column.Map<int, FSharpFunc<double, double>, double>(fmap, new[] { column1, column2 });
            Assert.AreEqual(expected, actual);
        }

        //[Test]
        //public void ColumnC_TestColumnMapsFail()
        //{
        //    var data1 = Common.RandomIntArray(Common.random, 100);
        //    var column1 = Column.New<int[]>(data1);

        //    var data2 = Common.RandomDoubleArray(Common.random, 100);
        //    var column2 = Column.New<double[]>(data2);

        //    var columns = new Column[] { column1, column2 };

        //    Func<IEnumerable<int>, double> map = xs => xs.Sum();

        //    FSharpFunc<IEnumerable<int>, double> fmap =
        //        FuncConvert.ToFSharpFunc<IEnumerable<int>, double>(
        //            new Converter<IEnumerable<int>, double>(map));

        //    var expected = new double[] { };
        //    var actual = Column.Maps<int, double>(fmap, columns);
        //    Assert.AreEqual(expected, actual);
        //}

        //[Test]
        //public void ColumnC_TestColumnMapsObjPass()
        //{
        //    var data1 = Common.RandomIntArray(Common.random, 100);
        //    var column1 = Column.New<int[]>(data1);

        //    var data2 = Common.RandomDoubleArray(Common.random, 100);
        //    var column2 = Column.New<double[]>(data2);

        //    var columns = new Column[] { column1, column2 };

        //    Func<IEnumerable<Object>, double> map = xs =>
        //        {
        //            var xss = xs.ToArray();
        //            var x0 = Operators.Unbox<int>(xss[0]);
        //            var x1 = Operators.Unbox<double>(xss[1]);

        //            return (double)x0 + x1;
        //        };

        //    FSharpFunc<IEnumerable<Object>, double> fmap =
        //        FuncConvert.ToFSharpFunc<IEnumerable<Object>, double>(
        //            new Converter<IEnumerable<Object>, double>(map));

        //    var expected = data1.Zip(data2, (i, x) => map(new Object[] { i, x} ));

        //    var actual = Column.Maps<Object, double>(fmap, columns).ToArray();
        //    Assert.AreEqual(expected, actual);
        //}

        //[Test]
        //public void ColumnC_TestColumnMapsDoublePass()
        //{
        //    var data1 = Common.RandomDoubleArray(Common.random, 100);
        //    var column1 = Column.New<double[]>(data1);

        //    var data2 = Common.RandomDoubleArray(Common.random, 100);
        //    var column2 = Column.New<double[]>(data2);

        //    var columns = new Column[] { column1, column2 };

        //    Func<IEnumerable<double>, double> map = xs => xs.Sum();

        //    FSharpFunc<IEnumerable<double>, double> fmap =
        //        FuncConvert.ToFSharpFunc<IEnumerable<double>, double>(
        //            new Converter<IEnumerable<double>, double>(map));

        //    var expected = data1.Zip(data2, (x, y) => map(new double[] { x, y }));

        //    var actual = Column.Maps<double, double>(fmap, columns).ToArray();
        //    Assert.AreEqual(expected, actual);
        //}

        [Test]
        public void ColumnC_TestMultiMap1()
        {
            var data1 = Common.RandomIntArray(Common.random, 100);
            var column1 = Column.New<int[]>(data1);

            var columns = new Column[] { column1 };

            Func<int, double> map = x => (double)x + 0.5;
            var fmap = FSharpFunc<int, double>.FromConverter(new Converter<int, double>(map));

            var expected = data1.Select(map);
            var actual = Column.Map<int, double, double>(fmap, columns);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ColumnC_TestMultiMap2()
        {
            var data1 = Common.RandomIntArray(Common.random, 100);
            var column1 = Column.New<int[]>(data1);

            var data2 = Common.RandomDoubleArray(Common.random, 100);
            var column2 = Column.New<double[]>(data2);

            var columns = new Column[] { column1, column2 };

            Func<int, double, double> map = (x, y) => (double)x * y;

            FSharpFunc<int, FSharpFunc<double, double>> fmap =
                FuncConvert.ToFSharpFunc<int, FSharpFunc<double, double>>(
                    new Converter<int, FSharpFunc<double, double>>(
                        i =>
                            FuncConvert.ToFSharpFunc(
                                new Converter<double, double>(
                                    x =>
                                        map(i, x)))));

            var expected = data1.Zip(data2, map);

            var actual = Column.Map<int, FSharpFunc<double, double>, double>(fmap, columns);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ColumnC_TestIntColumn()
        {
            var data = Common.RandomIntArray(Common.random, 100);
            var column = Column.New<int[]>(data);

            TestColumn<int>(data, column, Common.random, true);
            TestIntMap1(data, column);
        }

        [Test]
        public void ColumnC_TestRealColumn()
        {
            var data = Common.RandomDoubleArray(Common.random, 100);
            var column0 = Column.New<double[]>(data);

            TestColumn<double>(data, column0, Common.random, true);
        }

        [Test]
        public void ColumnC_TestStringColumn()
        {
            var data = Common.RandomStringArray(Common.random, 100, 12);
            var column0 = Column.New<string[]>(data);

            TestColumn<string>(data, column0, Common.random, true);
        }

        [Test]
        public void ColumnC_TestDateColumn()
        {
            var data = Common.RandomDateArray(Common.random, 100);
            var column0 = Column.New<DateTime[]>(data);

            TestColumn<DateTime>(data, column0, Common.random, true);
        }
    }
}
