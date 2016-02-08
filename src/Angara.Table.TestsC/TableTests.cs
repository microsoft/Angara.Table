using Angara.Data;
using Microsoft.FSharp.Core;
using NUnit.Framework;
using System;

namespace Angara.Data.TestsC
{
    [TestFixture]
    public class TableTests
    {
        [Test]
        public void TableC_TestEmpty()
        {
            var table0 = Table.Empty;

            Assert.AreEqual(new string[] {}, table0.Names);
            Assert.AreEqual(new Column[] {}, table0.Columns);
            Assert.AreEqual(new Type[] {}, table0.Types);
            Assert.AreEqual(0, table0.Count);
        }

        public void TestColumn(int[] data, Table table, string columnName)
        {
            Assert.AreEqual(typeof(Int32), Table.Type(columnName, table));
            Assert.AreEqual(FSharpOption<int>.None, Table.TryItem<int>(columnName, -1, table));
            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(FSharpOption<int>.Some(data[i]), Table.TryItem<int>(columnName, i, table));
            }
            Assert.AreEqual(FSharpOption<int>.None, Table.TryItem<int>(columnName, data.Length, table));

            for (int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(data[i], Table.Item<int>(columnName, i, table));
            }
        }

        [Test]
        public void TableC_TestAddOneColumn()
        {
            var data = new int[100];
            var random = new Random();
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = random.Next();
            }

            var column0 = Column.New<int[]>(data);

            var table0 = Table.Empty;
            var table1 = Table.Add("col1", column0, table0);

            Assert.AreEqual(new string[] { "col1" }, table1.Names);
            Assert.AreEqual(new Column[] { column0 } , table1.Columns);
            Assert.AreEqual(new Type[] { typeof(Int32) }, table1.Types);
            Assert.AreEqual(data.Length, table1.Count);

            var column01 = Table.Column("col1", table1);
            Assert.AreEqual(column0, column01);

            TestColumn(data, table1, "col1");
        }
    }
}
