namespace Angara.Data.TestsF

open System
open System.Reflection
open NUnit.Framework
open FsUnit
open Angara.Data

[<AbstractClass; Sealed>]
type ColumnTestsF =

    static member TestColumnType<'T>(column:Column) : unit =
        Assert.AreEqual(typeof<'T>, Column.Type column)

    static member TestColumnTrySubPass<'T when 'T : equality>(data:'T[]) (column:Column) (random:Random) (recurse:bool) : unit =
        Assert.Greater(data.Length / 4, 1)
        let subLength = random.Next(data.Length / 4, data.Length / 2)

        Assert.Greater(data.Length - 1 - subLength, 1)
        let subStart = random.Next(1, data.Length - 1 - subLength)

        let subData = data.[subStart..(subStart + subLength - 1)]

        // Test 4 overloads of TrySub
        if recurse then
            let subTryColumnC:Column option = Column.TrySub<Column> subStart subLength column
            Assert.IsTrue(subTryColumnC.IsSome)
            ColumnTestsF.TestColumn<'T> subData subTryColumnC.Value random false

        let subTryColumnI:IRArray<'T> option = Column.TrySub<IRArray<'T>> subStart subLength column
        Common.testOptionArraySome<'T, IRArray<'T>> subData subTryColumnI

        let subTryColumnT:'T[] option = Column.TrySub<'T[]> subStart subLength column
        Common.testOptionArraySome<'T, 'T[]> subData subTryColumnT

        let subTryColumnA:Array option = Column.TrySub<Array> subStart subLength column
        Common.testOptionArraySome<'T, Array> subData subTryColumnA

    static member TestColumnTrySubPassFull<'T>(data:'T[]) (column:Column) : unit =
        let subColumnC:Column option = Column.TrySub<Column> 0 data.Length column
        Assert.IsTrue(subColumnC.IsSome)
        Assert.AreEqual(Column.ToArray<Array> column, Column.ToArray<Array> subColumnC.Value);

        let subTryColumnI:IRArray<'T> option = Column.TrySub<IRArray<'T>> 0 data.Length column
        Common.testOptionArraySome<'T, IRArray<'T>> data subTryColumnI

        let subTryColumnT:'T[] option = Column.TrySub<'T[]> 0 data.Length column
        Common.testOptionArraySome<'T, 'T[]> data subTryColumnT

        let subTryColumnA:Array option = Column.TrySub<Array> 0 data.Length column
        Common.testOptionArraySome<'T, Array> data subTryColumnA

    static member TestColumnTrySubFailBounds<'T>(data:'T[]) (column:Column) (subStart:int) (subLength:int) : unit =
        let subColumnC:Column option = Column.TrySub<Column> subStart subLength column
        Assert.IsTrue(subColumnC.IsNone)

        let subTryColumnI:IRArray<'T> option = Column.TrySub<IRArray<'T>> subStart subLength column
        Assert.IsTrue(subTryColumnI.IsNone)

        let subTryColumnT:'T[] option = Column.TrySub<'T[]> subStart subLength column
        Assert.IsTrue(subTryColumnT.IsNone)

        let subTryColumnA:Array option = Column.TrySub<Array> subStart subLength column
        Assert.IsTrue(subTryColumnA.IsNone)

    static member TestColumnTrySubFailTypes<'T, 'U>(data:'T[]) (column:Column) (random:Random) (recurse:bool) : unit =
        Assert.Greater(data.Length / 4, 1)
        let subLength = random.Next(data.Length / 4, data.Length / 2)

        Assert.Greater(data.Length - 1 - subLength, 1)
        let subStart = random.Next(1, data.Length - 1 - subLength)

        let subTryColumnI:IRArray<'U> option = Column.TrySub<IRArray<'U>> subStart subLength column
        Assert.IsTrue(subTryColumnI.IsNone)

        let subTryColumnT:'U[] option = Column.TrySub<'U[]> subStart subLength column
        Assert.IsTrue(subTryColumnT.IsNone)

    static member TestColumnTrySub<'T when 'T : equality>(data:'T[]) (column:Column) (random:Random) (recurse:bool) (validTypes:Type[]) : unit =
        validTypes
        |> Array.iter
            (fun validType ->
                if typeof<'T> = validType then
                    ColumnTestsF.TestColumnTrySubPass<'T> data column random recurse
                    ColumnTestsF.TestColumnTrySubPassFull<'T> data column

                    ColumnTestsF.TestColumnTrySubFailBounds<'T> data column -1 1
                    ColumnTestsF.TestColumnTrySubFailBounds<'T> data column 1 data.Length
                else
                    let method':MethodInfo = typeof<ColumnTestsF>.GetMethod("TestColumnTrySubFailTypes")
                    let generic:MethodInfo = method'.MakeGenericMethod(typeof<'T>, validType)
                    let objs:obj[] =[| data; column; random; recurse |]
                    generic.Invoke(null, objs)
                    |> ignore
            )

    static member TestColumnSubPass<'T when 'T : equality>(data:'T[]) (column:Column) (random:Random) (recurse:bool) : unit =
        Assert.Greater(data.Length / 4, 1)
        let subLength = random.Next(data.Length / 4, data.Length / 2)

        Assert.Greater(data.Length - 1 - subLength, 1)
        let subStart = random.Next(1, data.Length - 1 - subLength)

        let subData = data.[subStart..(subStart + subLength - 1)]

        // Test 4 overloads of Sub
        if recurse then
            let subColumnC:Column = Column.Sub<Column> subStart subLength column
            ColumnTestsF.TestColumn<'T> subData subColumnC random false

        let subColumnI:IRArray<'T> = Column.Sub<IRArray<'T>> subStart subLength column
        Assert.AreEqual(subData, subColumnI.ToArray())

        let subColumnT:'T[] = Column.Sub<'T[]> subStart subLength column
        Assert.AreEqual(subData, subColumnT)

        let subColumnA:Array = Column.Sub<Array> subStart subLength column
        Assert.AreEqual(subData, subColumnA :?> 'T[])

    static member TestColumnSubPassFull<'T when 'T : equality> (data:'T[]) (column:Column) : unit =
            let subColumnC:Column = Column.Sub<Column> 0 data.Length column
            Assert.AreEqual(Column.ToArray<Array> column, Column.ToArray<Array> subColumnC)

            let subColumnI:IRArray<'T> = Column.Sub<IRArray<'T>> 0 data.Length column
            Assert.AreEqual(data, subColumnI.ToArray())

            let subColumnT:'T[] = Column.Sub<'T[]> 0 data.Length column
            Assert.AreEqual(data, subColumnT)

            let subColumnA:Array = Column.Sub<Array> 0 data.Length column
            Assert.AreEqual(data, subColumnA :?> 'T[])

    static member TestColumnSubFailBounds<'T> (data:'T[]) (column:Column) (subStart:int) (subLength:int) : unit =
            (fun () -> Column.Sub<Column> subStart subLength column |> ignore) |> should throw typeof<System.Exception>
            (fun () -> Column.Sub<IRArray<'T>> subStart subLength column |> ignore) |> should throw typeof<System.Exception>
            (fun () -> Column.Sub<'T[]> subStart subLength column |> ignore) |> should throw typeof<System.Exception>
            (fun () -> Column.Sub<Array> subStart subLength column |> ignore) |> should throw typeof<System.Exception>

    static member TestColumnSubFailTypes<'T, 'U>(data:'T[]) (column:Column) (random:Random) (recurse:bool) : unit =
        Assert.Greater(data.Length / 4, 1)
        let subLength = random.Next(data.Length / 4, data.Length / 2)

        Assert.Greater(data.Length - 1 - subLength, 1)
        let subStart = random.Next(1, data.Length - 1 - subLength)

        (fun () -> Column.Sub<IRArray<'U>> subStart subLength column |> ignore) |> should throw typeof<System.Exception>
        (fun () -> Column.Sub<'U[]> subStart subLength column |> ignore) |> should throw typeof<System.Exception>

    static member TestColumnSub<'T when 'T : equality>(data:'T[]) (column:Column) (random:Random) (recurse:bool) (validTypes:Type[]) : unit =
        validTypes
        |> Array.iter
            (fun validType ->
                if typeof<'T> = validType then
                    ColumnTestsF.TestColumnSubPass<'T> data column random recurse
                    ColumnTestsF.TestColumnSubPassFull<'T> data column

                    ColumnTestsF.TestColumnSubFailBounds<'T> data column -1 1
                    ColumnTestsF.TestColumnSubFailBounds<'T> data column 1 data.Length
                else
                    let method':MethodInfo = typeof<ColumnTestsF>.GetMethod("TestColumnSubFailTypes")
                    let generic:MethodInfo = method'.MakeGenericMethod(typeof<'T>, validType)
                    let objs:obj[] =[| data; column; random; recurse |]
                    generic.Invoke(null, objs)
                    |> ignore
            )

    static member TestColumnCount<'T>(data:'T[]) (column:Column) : unit =
        Assert.AreEqual(data.Length, Column.Count column)

    static member TestColumnTryItemPass<'T when 'T : equality>(data:'T[]) (column:Column) : unit =
        Assert.IsTrue((Column.TryItem<'T> -1 column).IsNone)

        data
        |> Seq.iteri (
            fun i d ->
                let v:'T option = Column.TryItem<'T> i column
                Assert.IsTrue(v.IsSome)
                Assert.AreEqual(d, v.Value))

        Assert.IsTrue((Column.TryItem<'T> data.Length column).IsNone)

    static member TestColumnTryItemFail<'T, 'U>(data:'T[]) (column:Column) : unit =
        Assert.IsTrue((Column.TryItem<'U> -1 column).IsNone)

        data
        |> Seq.iteri (
            fun i d ->
                let v:'U option = Column.TryItem<'U> i column
                Assert.IsTrue(v.IsNone))

        Assert.IsTrue((Column.TryItem<'U> data.Length column).IsNone)

    static member TestColumnTryItem<'T when 'T : equality>(data:'T[]) (column:Column) (validTypes:Type[]) : unit =
        validTypes
        |> Array.iter
            (fun validType ->
                if typeof<'T> = validType then
                    ColumnTestsF.TestColumnTryItemPass<'T> data column
                else
                    let method':MethodInfo = typeof<ColumnTestsF>.GetMethod("TestColumnTryItemFail")
                    let generic:MethodInfo = method'.MakeGenericMethod(typeof<'T>, validType)
                    let objs:obj[] =[| data; column |]
                    generic.Invoke(null, objs)
                    |> ignore
            )

    static member TestColumnItemPass<'T when 'T : equality>(data:'T[]) (column:Column) : unit =
        (fun () -> Column.Item<'T> -1 column |> ignore) |> should throw typeof<System.Exception>

        data
        |> Seq.iteri (fun i d -> Assert.AreEqual(d, Column.Item<'T> i column))

        (fun () -> Column.Item<'T> data.Length column |> ignore) |> should throw typeof<System.Exception>

    static member TestColumnItemFail<'T, 'U>(data:'T[]) (column:Column) : unit =
        (fun () ->  Column.Item<'U> -1 column |> ignore) |> should throw typeof<System.Exception>

        data
        |> Seq.iteri (fun i _ -> (fun () ->  Column.Item<'U> i column |> ignore) |> should throw typeof<System.Exception>)

        (fun () ->  Column.Item<'U> data.Length column |> ignore) |> should throw typeof<System.Exception>

    static member TestColumnItem<'T when 'T : equality>(data:'T[]) (column:Column) (validTypes:Type[]) : unit =
        validTypes
        |> Array.iter
            (fun validType ->
                if typeof<'T> = validType then
                    ColumnTestsF.TestColumnItemPass<'T> data column
                else
                    let method':MethodInfo = typeof<ColumnTestsF>.GetMethod("TestColumnItemFail")
                    let generic:MethodInfo = method'.MakeGenericMethod(typeof<'T>, validType)
                    let objs:obj[] =[| data; column |]
                    generic.Invoke(null, objs)
                    |> ignore
            )

    static member TestColumnTryToArrayPass<'T>(data:'T[]) (column:Column) : unit =
        let columnC:Column option = Column.TryToArray<Column> column
        Assert.IsTrue(columnC.IsSome)
        Assert.AreEqual(column, columnC.Value)

        let tryColumnI:IRArray<'T> option = Column.TryToArray<IRArray<'T>> column
        Common.testOptionArraySome<'T, IRArray<'T>> data tryColumnI

        let tryColumnT:'T[] option = Column.TryToArray<'T[]> column
        Common.testOptionArraySome<'T, 'T[]> data tryColumnT

        let tryColumnA:Array option = Column.TryToArray<Array> column
        Common.testOptionArraySome<'T, Array> data tryColumnA

    static member TestColumnTryToArrayFail<'T, 'U>(data:'T[]) (column:Column) : unit =
        let tryColumnI:IRArray<'U> option = Column.TryToArray<IRArray<'U>> column
        Assert.IsTrue(tryColumnI.IsNone)

        let tryColumnT:'U[] option = Column.TryToArray<'U[]> column
        Assert.IsTrue(tryColumnT.IsNone)

    static member TestColumnTryToArray<'T>(data:'T[]) (column:Column) (validTypes:Type[]) : unit =
        validTypes
        |> Array.iter
            (fun validType ->
                if typeof<'T> = validType then
                    ColumnTestsF.TestColumnTryToArrayPass<'T> data column
                else
                    let method':MethodInfo = typeof<ColumnTestsF>.GetMethod("TestColumnTryToArrayFail")
                    let generic:MethodInfo = method'.MakeGenericMethod(typeof<'T>, validType)
                    let objs:obj[] =[| data; column |]
                    generic.Invoke(null, objs)
                    |> ignore
            )

    static member TestColumnToArrayPass<'T when 'T : equality>(data:'T[]) (column:Column) : unit =
        let columnC:Column = Column.ToArray<Column> column
        Assert.AreEqual(column, columnC)

        let columnI:IRArray<'T> = Column.ToArray<IRArray<'T>> column
        Assert.AreEqual(data, columnI.ToArray())

        let columnT:'T[] = Column.ToArray<'T[]> column
        Assert.AreEqual(data, columnT)

        let columnA:Array = Column.ToArray<Array> column
        Assert.AreEqual(data, columnA :?> 'T[])

    static member TestColumnToArrayFail<'T, 'U>(data:'T[]) (column:Column) : unit =
        (fun () ->  Column.ToArray<IRArray<'U>> column |> ignore) |> should throw typeof<System.Exception>
        (fun () ->  Column.ToArray<'U[]> column |> ignore) |> should throw typeof<System.Exception>

    static member TestColumnToArray<'T when 'T : equality>(data:'T[]) (column:Column) (validTypes:Type[]) : unit =
        validTypes
        |> Array.iter
            (fun validType ->
                if typeof<'T> = validType then
                    ColumnTestsF.TestColumnToArrayPass<'T> data column
                else
                    let method':MethodInfo = typeof<ColumnTestsF>.GetMethod("TestColumnToArrayFail")
                    let generic:MethodInfo = method'.MakeGenericMethod(typeof<'T>, validType)
                    let objs:obj[] =[| data; column |]
                    generic.Invoke(null, objs)
                    |> ignore
            )

    static member TestColumn<'T when 'T : equality> (data:'T[]) (column:Column) (random:Random) (recurse:bool) : unit =
        let validTypes = Column.ValidTypes

        ColumnTestsF.TestColumnType<'T> column
        ColumnTestsF.TestColumnTrySub<'T> data column random recurse validTypes
        ColumnTestsF.TestColumnSub<'T> data column random recurse validTypes
        ColumnTestsF.TestColumnTryToArray<'T> data column validTypes
        ColumnTestsF.TestColumnToArray<'T> data column validTypes
        ColumnTestsF.TestColumnCount<'T> data column
        ColumnTestsF.TestColumnTryItem<'T> data column validTypes
        ColumnTestsF.TestColumnItem<'T> data column validTypes

    static member TestIntMap1 (data:int[]) (column:Column) : unit =
        let map (x:int) : double = (double)x + 0.5

        let expected = Array.map map data
        let actual = Column.Map map [column]

        Assert.AreEqual(expected, actual)

    [<Test; Category("CI")>]
    static member ColumnF_TestIntRealColumnMap2() =
        let data1 = Common.randomIntArray Common.random 100
        let column1 = Column.New<int[]> data1

        let data2 = Common.randomDoubleArray Common.random 100
        let column2 = Column.New<double[]> data2

        let map (x:int) (y:double) : double = (double)x * y
        let expected = Array.map2 map data1 data2
        let actual = Column.Map map [column1; column2]
        Assert.AreEqual(expected, actual)


    [<Test; Category("CI")>]
    static member ColumnF_TestColumnMapsIntDblPass() =
        let data1 = Common.randomIntArray Common.random 100
        let column1 = Column.New<int[]> data1

        let data2 = Common.randomDoubleArray Common.random 100
        let column2 = Column.New<double[]> data2

        let map (x1:int) (x2:double) : double = double(x1)+x2
        let expected = Array.map2 map data1 data2
        let actual = Column.Map map [| column1; column2 |]
        Assert.AreEqual(expected, actual)

    [<Test; Category("CI")>]
    static member ColumnF_TestColumnMapsDoublePass() =
        let data1 = Common.randomDoubleArray Common.random 100
        let column1 = Column.New<double[]> data1

        let data2 = Common.randomDoubleArray Common.random 100
        let column2 = Column.New<double[]> data2

        let map (x1:double) (x2:double) :  double = x1+x2
        let expected = Array.map2 map data1 data2
        let actual = Column.Map map [| column1; column2 |]
        Assert.AreEqual(expected, actual)

    [<Test; Category("CI")>]
    static member ColumnF_TestMultiMap1() =
        let data1 = Common.randomIntArray Common.random 100
        let column1 = Column.New<int[]> data1

        let columns = [| column1 |]

        let map (x:int) : double = (double)x + 0.5

        let expected = Array.map map data1
        let actual = Column.Map map columns

        Assert.AreEqual(expected, actual)


    [<Test; Category("CI")>]
    static member ColumnF_TestMultiMapi() =
        let data1 = Common.randomIntArray Common.random 100
        let column1 = Column.New<int[]> data1

        let map (i:int) (x:int) : double = (double)x + 0.5 + (double)i

        let expected = Array.mapi map data1
        let actual = Column.Mapi map [| column1 |]

        Assert.AreEqual(expected, actual)

    [<Test; Category("CI")>]
    static member ColumnF_TestMultiMap2() =

        let data1 = Common.randomIntArray Common.random 100
        let column1 = Column.New<int[]> data1

        let data2 = Common.randomDoubleArray Common.random 100
        let column2 = Column.New<double[]> data2

        let map(x:int) (y:double) : double = (double)x * y

        let expected = Array.map2 map data1 data2
        let actual = Column.Map map [| column1; column2 |]
        Assert.AreEqual(expected, actual)

    [<Test; Category("CI")>]
    static member ColumnF_TestMultiMapi2() =

        let data1 = Common.randomIntArray Common.random 100
        let column1 = Column.New<int[]> data1

        let data2 = Common.randomDoubleArray Common.random 100
        let column2 = Column.New<double[]> data2

        let map (i:int) (x:int) (y:double) : double = (double)x * y + (double)i

        let expected = Array.mapi2 map data1 data2
        let actual = Column.Mapi map [| column1; column2 |]
        Assert.AreEqual(expected, actual)

    [<Test; Category("CI")>]
    static member ColumnF_TestIntColumn() =
        let data = Common.randomIntArray Common.random 100
        let column = Column.New<int[]> data

        ColumnTestsF.TestColumn<int> data column Common.random true
        ColumnTestsF.TestIntMap1 data column

    [<Test; Category("CI")>]
    static member ColumnF_TestRealColumn() =
        let data = Common.randomDoubleArray Common.random 100
        let column0 = Column.New<double[]> data

        ColumnTestsF.TestColumn<double> data column0 Common.random true

    [<Test; Category("CI")>]
    static member ColumnF_TestStringColumn() =
        let data = Common.randomStringArray Common.random 100 12
        let column0 = Column.New<string[]> data

        ColumnTestsF.TestColumn<string> data column0 Common.random true

    [<Test; Category("CI")>]
    static member ColumnF_TestDateColumn() =
        let data = Common.randomDateArray Common.random 100
        let column0 = Column.New<DateTime[]> data

        ColumnTestsF.TestColumn<DateTime> data column0 Common.random true

