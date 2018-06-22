using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LWJ.Data.Sqlite.Test
{
    /// <summary>
    /// UpgradeTest 的摘要说明
    /// </summary>
    [TestClass]
    public class UpgradeTest : IDisposable
    {
        public UpgradeTest()
        {
            Console.WriteLine("co");
        }

        public void Dispose()
        {
            Console.WriteLine("dis");
        }

        [TestInitialize]
        public void TestBefore()
        {
            Console.WriteLine("delete db file");
            System.IO.File.Delete(UpgradeDB1.Path);
        }

        [TestMethod]
        public void Upgrade()
        {
            using (var db = new UpgradeDB1())
            {
                db.Open();
                db.NonQuery("insert into table1 (id,name) values(1,'s')");
                Assert.AreEqual(1, db.Scalar<int>("select count(*) from table1 where id=1"));
                db.NonQuery("insert into tableOld (id) values(1)");
                Assert.AreEqual(1, db.Scalar<int>("select count(*) from tableOld where id=1"));
            }

            using (var db = new UpgradeDB2())
            {
                db.Open();
                db.NonQuery("insert into table1 (id,name,field_new) values(2,'abc',10)");
                Assert.AreEqual(10, db.Scalar<int>("select field_new from table1 where id=2"));

                db.NonQuery("insert into tableNew (id) values(2)");
                 
                Assert.AreEqual(1, db.Scalar<int>("select count(*) from tableNew where id=2"));
                Assert.AreEqual(2, db.Scalar<int>("select count(*) from tableNew"));
            }

        }

        class UpgradeDB1 : SqliteDatabase
        {
            public const string Path = "../../db/upgrade.db";
            public UpgradeDB1()
                : base(Path, 1)
            {

            }
            protected override void OnCreateDatabase()
            {
                Console.WriteLine("CreateDatabase");
                NonQuery("DROP TABLE IF EXISTS table1");
                NonQuery("CREATE TABLE table1 (id int, name nvarchar(50))");
                NonQuery("DROP TABLE IF EXISTS tableOld");
                NonQuery("CREATE TABLE tableOld (id int)");
            }

            protected override void OnUpgradeDatabase(int oldVersion, int newVersion)
            {
                throw new Exception("UpgradeDatabase");
            }
            protected override void OnDowngradeDatabase(int oldVersion, int newVersion)
            {
                throw new Exception("DowngradeDatabase");
            }
        }

        class UpgradeDB2 : SqliteDatabase
        {
            public UpgradeDB2()
                : base(UpgradeDB1.Path, 2)
            {

            }
            protected override void OnCreateDatabase()
            {
                throw new Exception("UpgradeDatabase");
            }

            protected override void OnUpgradeDatabase(int oldVersion, int newVersion)
            {
                Console.WriteLine("UpgradeDatabase " + newVersion);
                if (oldVersion == 1)
                {
                    NonQuery("ALTER TABLE [table1] ADD [field_new] INTEGER NOT NULL Default(0)");
                    NonQuery("ALTER TABLE tableOld RENAME TO tableNew");
                }
            }
            protected override void OnDowngradeDatabase(int oldVersion, int newVersion)
            {
                throw new Exception("DowngradeDatabase");
            }

        }

    }
}
