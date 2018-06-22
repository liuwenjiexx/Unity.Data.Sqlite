using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LWJ.Data.Sqlite.Test
{
    [TestClass]
    public class DataTypeTest
    {  /*
         sqlite type: 
         int    int32
            long    integer
            float   real
            double  float
             */

        [TestMethod]
        public void DataType_String()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, object, string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( stringField) values(@stringField)", value);
                        object result;
                        result = db.Scalar<string>("select stringField from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        Assert.AreEqual(expected, result, msg);
                    };
                    exec(null, null, "null");
                    exec(DBNull.Value, null, "DBNull");
                    exec(string.Empty, string.Empty, "empty");
                    exec("Hello World", "Hello World", "Hello World");


                }
            }
        }

        [TestMethod]
        public void DataType_Int32()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, object, string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( int32Field) values(@int32Field)", value);
                        object result;
                        result = db.Scalar<int>("select int32Field from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        Assert.AreEqual(expected, result, msg);
                    };
                    exec(null, 0, "null");
                    exec(DBNull.Value, 0, "DBNull");
                    exec(0, 0, "0");
                    exec(int.MinValue, int.MinValue, "int min");
                    exec(int.MaxValue, int.MaxValue, "int max");

                    Assert.ThrowsException<CommandException>(() => exec(long.MaxValue, long.MaxValue, "long max"));

                }
            }
        }


        [TestMethod]
        public void DataType_Int64()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, object, string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( int64Field) values(@int64Field)", value);
                        object result;
                        result = db.Scalar<long>("select int64Field from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        Assert.AreEqual(expected, result, msg);
                    };
                    exec(null, 0L, "null");
                    exec(DBNull.Value, 0L, "DBNull");
                    exec(0L, 0L, "0");
                    exec(long.MinValue, long.MinValue, "long min");
                    exec(long.MaxValue, long.MaxValue, "long max");
                    exec(int.MaxValue, (long)int.MaxValue, "int max");

                    Assert.ThrowsException<AssertFailedException>(() => exec(0f, 0f, "float 0"));

                }
            }
        }

        [TestMethod]
        public void DataType_Float32()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, object, string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( float32Field) values(@float32Field)", value);
                        object result;
                        result = db.Scalar<float>("select float32Field from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        Assert.AreEqual(expected, result, msg);
                    };
                    exec(null, 0f, "null");
                    exec(DBNull.Value, 0f, "DBNull");
                    exec(0f, 0f, "0");
                    exec(float.MinValue, float.MinValue, "float min");
                    exec(float.MaxValue, float.MaxValue, "float max");
                }
            }
        }

        [TestMethod]
        public void DataType_Float64()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, object, string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( float64Field) values(@float64Field)", value);
                        object result;
                        result = db.Scalar<double>("select float64Field from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        Assert.AreEqual(expected, result, msg);
                    };
                    exec(null, 0d, "null");
                    exec(DBNull.Value, 0d, "DBNull");
                    exec(0d, 0d, "0");
                    exec(double.MinValue, double.MinValue, "double min");
                    exec(double.MaxValue, double.MaxValue, "double max");
                }
            }
        }
        [TestMethod]
        public void DataType_Bool()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, object, string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( boolField) values(@boolField)", value);
                        object result;
                        result = db.Scalar<bool>("select boolField from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        Assert.AreEqual(expected, result, msg);
                    };
                    exec(null, false, "null");
                    exec(DBNull.Value, false, "DBNull");
                    exec(false, false, "false");
                    exec(true, true, "true");
                }
            }
        }

        [TestMethod]
        public void DataType_DateTime()
        {

            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, object, string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( datetimeField) values(@datetimeField)", value);
                        object result;
                        result = db.Scalar<DateTime>("select datetimeField from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        Assert.AreEqual(expected, result, msg);
                    };
                    exec(null, DateTime.MinValue, "null");
                    exec(DBNull.Value, DateTime.MinValue, "DBNull");

                    exec(DateTime.MinValue, DateTime.MinValue, "DateTime min");
                    exec(DateTime.MaxValue, DateTime.MaxValue, "DateTime max");

                    Assert.ThrowsException<CommandException>(() => exec(long.MaxValue, long.MaxValue, "long max"));

                }
            }
        }
        [TestMethod]
        public void DataType_IntArray()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Action<object, int[], string> exec = (value, expected, msg) =>
                    {
                        db.NonQuery("insert into table1 ( intArray) values(@intArray)", value);
                        int[] result;
                        result = db.Scalar<int[]>("select intArray from table1 where rowid=@rowid", db.GetLastInsertRowID());
                        CollectionAssert.AreEqual(expected, result, msg);
                    };
                    exec(null, null, "null");
                    exec(DBNull.Value, null, "DBNull");
                    exec(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }, "1,2,3");

                }
            }
        }

    }
}
