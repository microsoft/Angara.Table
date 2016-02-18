using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Angara;

namespace Angara.Data.TestsC
{
    using Angara.Data;

    [TestFixture]
    public static class CsvTests
    {
        public static Stream AsStream(string content)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write(content);
            sw.Flush();
            ms.Position = 0;
            return ms;
        }

        public static string[] SplitRowStr(char delimiter, string row) {
            using (var s = AsStream(row)) {
                var r = new StreamReader(s);
                var items = DelimitedFile.Helpers.splitRow(delimiter, r);
                if (Microsoft.FSharp.Core.FSharpOption<string[]>.get_IsSome(items))
                    return items.Value;
                else
                    return null;
            }
        }



//        public static void TestCsvReadWithUserDefinedTypes()
//        {
//            using (var s = AsStream(
//@"x,y
//1,a
//2,b
//3,c"))
//            {
//                var settings = new Csv.ReadSettings(Csv.Delimiter.Comma, false, null,
//                    new Microsoft.FSharp.Core.FSharpOption((colIndex, colName) => null));
//                var result = Csv.Read(settings, s);
//            }
//        }
    }
}
