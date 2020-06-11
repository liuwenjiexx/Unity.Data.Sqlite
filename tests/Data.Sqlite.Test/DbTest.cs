using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace System.Data.Sqlite.Test
{
    [TestClass]
    public class DbTest
    {

        [TestMethod]
        public void Scalar()
        {

            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    db.NonQuery("insert into table1 (stringField) values(@stringField)", "text");
                    string result = db.Scalar<string>("select stringField from table1 where rowid=@rowid", db.GetLastInsertRowID());
                    Assert.AreEqual("text", result);
                }
            }

        }

        [TestMethod]
        public void QueryFirst()
        {

            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    db.NonQuery("insert into table1 (stringField) values(@stringField)", "text");
                    var result = db.QueryFirst<Dictionary<string, object>>("select stringField from table1 where rowid=@rowid", db.GetLastInsertRowID());
                    Assert.AreEqual("text", result["stringField"]);
                }
            }

        }


        [TestMethod]
        public void LastInsertID()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Assert.IsTrue(db.NonQuery("insert into table1 (stringField) values(@stringField)", "Last Insert ID") > 0);
                    object id = db.GetLastInsertID("table1", "id");
                    Assert.IsNotNull(id);
                    Assert.AreEqual("Last Insert ID", db.Scalar<string>("select stringField from table1 where id=@id", id));
                }
            }
        }

        [TestMethod]
        public void LastRowId()
        {
            using (var db = new FileDB())
            {
                db.Open();
                using (db.BeginTransaction())
                {
                    Assert.IsTrue(db.NonQuery("insert into table1 (stringField) values(@stringField)", "Last Row ID") > 0);

                    long rowId = db.GetLastInsertRowID();
                    Assert.AreEqual("Last Row ID", db.Scalar<string>("select stringField from table1 where rowid=@rowid", rowId));
                }
            }
        }



        SqliteDatabase GetTestDatabase()
        {
            SqliteDatabase db = new SqliteDatabase("../../db/test.db", 0);
            return db;
        }
        [TestMethod]
        public void CreateDB_FileVersion()
        {
            if (File.Exists(FileVersionDB.Location))
                File.Delete(FileVersionDB.Location);

            var db = new FileVersionDB();
            db.Open();
            Assert.IsTrue(db.IsOpened);
            Assert.AreEqual(0, db.Scalar<int>("select count(*) from table1"));
            db.NonQuery("insert into table1 (float32Field) values(@float32Field)", 1);
            Assert.AreEqual(1, db.Scalar<int>("select count(*) from table1"));
        }
        [TestMethod]
        public void OpenDB_FileVersion()
        {
            var db = new OpenFileVersionDB();
            db.Open();
            Assert.IsTrue(db.IsOpened);
        }
    }


    class FileDB : SqliteDatabase
    {
        public FileDB()
            : base("test.db", "../../db/test.db")
        {
        }

    }

    class FileVersionDB : SqliteDatabase
    {
        public FileVersionDB()
            : base(Location, 1)
        {
        }

        public static string Location = "filever.db";

        protected override void OnCreateDatabase()
        {
            NonQuery("  CREATE TABLE [table1] (  [id] INTEGER PRIMARY KEY AUTOINCREMENT,  [stringField] VARCHAR(50),  [int32Field] INT,  [int64Field] INT64,  [textField] TEXT,  [boolField] BOOL,  [float32Field] FLOAT,  [float64Field] DOUBLE)");
        }
    }

    class OpenFileVersionDB : SqliteDatabase
    {
        public OpenFileVersionDB()
            : base(Location, V)
        {
        }
         
        public static string Location = "C:/Users/liuwenjie/AppData/LocalLow/LWJ/My Space/local.sqlite";
        public static int V = 17;
         
    }

    class MemoryDB : SqliteDatabase
    {
        public MemoryDB()
            : base(MemoryDBName, 0)
        {
        }

        protected override void OnCreateDatabase()
        {
            NonQuery("  CREATE TABLE [table1] (  [id] INTEGER PRIMARY KEY AUTOINCREMENT,  [stringField] VARCHAR(50),  [int32Field] INT,  [int64Field] INT64,  [textField] TEXT,  [boolField] BOOL,  [float32Field] FLOAT,  [float64Field] DOUBLE)");
        }
    }

    class FileToMemoryDB : SqliteDatabase
    {
        public FileToMemoryDB()
            : base(MemoryDBName, "../../db/test.db")
        {
        }

    }

}
