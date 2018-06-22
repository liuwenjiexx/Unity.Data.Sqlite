using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LWJ.Data.Sqlite.Test
{

    [TestClass]
    public class TransationTest
    {
        [TestMethod]
        public void Transation_Commit()
        {
            int count;
            string testValue = "Test Transation";
            using (var db = new FileDB())
            {
                db.Open();

                db.NonQuery("delete from table1 where stringField=@stringField", testValue);

                using (db.BeginTransaction())
                {
                    db.NonQuery("insert into table1 (stringField) values(@stringField)", testValue);
                    count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                    Assert.IsTrue(count > 0);
                    db.Commit();
                }

                count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                Assert.IsTrue(count > 0);

                db.NonQuery("delete from table1 where stringField=@stringField", testValue);
            }
        }
        [TestMethod]
        public void Transation_Rollback()
        {
            using (var db = new FileDB())
            {
                db.OpenInMemory();
                int count;
                string testValue = "Test Transation";
                db.NonQuery("delete from table1 where stringField=@stringField", testValue);

                using (db.BeginTransaction())
                {
                    db.NonQuery("insert into table1 (stringField) values(@stringField)", testValue);
                    count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                    Assert.IsTrue(count > 0);
                }

                count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                Assert.IsTrue(count == 0);
            }
        }

        [TestMethod]
        public void Transation_Rollback_Commit()
        {
            using (var db = new FileDB())
            {
                db.OpenInMemory();
                int count;
                string testValue = "Test Transation";
                db.NonQuery("delete from table1 where stringField=@stringField", testValue);

                using (db.BeginTransaction())
                {
                    using (db.BeginTransaction())
                    {
                        db.NonQuery("insert into table1 (stringField) values(@stringField)", testValue);
                        db.Commit();
                    }

                    count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                    Assert.IsTrue(count > 0);
                }

                count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                Assert.IsTrue(count == 0);
            }
        }


        [TestMethod]
        public void Transation_Commit_Rollback()
        {
            using (var db = new FileDB())
            {
                db.OpenInMemory();
                int count;
                string testValue = "Test Transation";
                db.NonQuery("delete from table1 where stringField=@stringField", testValue);

                using (db.BeginTransaction())
                {
                    Assert.IsTrue(db.HasTransation);
                    using (db.BeginTransaction())
                    {
                        db.NonQuery("insert into table1 (stringField) values(@stringField)", testValue);
                    }
                    count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                    Assert.IsTrue(count > 0, "Rollback");
                    db.Commit();
                }

                count = db.Scalar<int>("select * from table1 where stringfield=@stringField", testValue);
                Assert.IsTrue(count > 0, "Commit");
            }
        }

    }
}
