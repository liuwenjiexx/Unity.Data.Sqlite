using System;
using System.Collections;
using System.Collections.Generic;
using Yanmonet.Data.Sqlite;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Demo1 : MonoBehaviour
{
    SqliteDatabase db;
    bool diried;
    bool diried2;
    Table1[] rows;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("DB location:" + MyDB.Location);
        db = new MyDB();
        db.Open();
        diried = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnGUI()
    {
        GUI.matrix = Matrix4x4.Scale(Vector3.one * 2);

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear"))
            {
                db.NonQuery("delete from table1");
                diried = true;
            }
            if (GUILayout.Button("+"))
            {
                db.NonQuery("insert into table1 (float32Field) values(@float32)", Random.value);
                diried = true;
            }
            if (GUILayout.Button("+ conn"))
            {
                using (var cmd = db.Connection.CreateCommand())
                {
                    cmd.CommandText = "insert into table1 (float32) values(@float32)";
                    cmd.Parameters.Add("float32", System.Data.DbType.Single).Value = Random.value;
                    cmd.ExecuteNonQuery();
                }
                diried2 = true;
                using (var cmd = db.Connection.CreateCommand())
                {
                    cmd.CommandText = "select * from table1";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Debug.Log($"data, float32: {reader["float32"]}");
                        }
                    }
                }
            }

        }

        if (diried)
        {
            rows = db.Query<Table1>("select * from table1").ToArray();
        }


        foreach (var row in rows)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("id");
                GUILayout.Label(row.id.ToString());
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("float32");
                GUILayout.Label(row.float32.ToString());
            }
        }



    }


    class Table1
    {
        public int id;
        public float float32;
    }

    private class MyDB : SqliteDatabase
    {
        public MyDB()
            : base(Location, 1)
        {

        }

        public static string Location = Application.persistentDataPath + "/local";
        protected override void OnCreateDatabase()
        {
            NonQuery("CREATE TABLE [table1] (  [id] INTEGER PRIMARY KEY AUTOINCREMENT, [float32] FLOAT)");
            //NonQuery("  CREATE TABLE [table1] (  [id] INTEGER PRIMARY KEY AUTOINCREMENT,  [stringField] VARCHAR(50),  [int32Field] INT,  [int64Field] INT64,  [textField] TEXT,  [boolField] BOOL,  [float32Field] FLOAT,  [float64Field] DOUBLE)");
        }
    }
}
