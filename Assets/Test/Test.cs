using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Sqlite;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Test : MonoBehaviour
{
    SqliteDatabase db;
    bool diried;
    Table1[] rows;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("DB location:" + TestDB.Location);
        db = new TestDB();
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
                db.NonQuery("insert into table1 (float32Field) values(@float32Field)", Random.value);
                diried = true;
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
                GUILayout.Label("float32Field");
                GUILayout.Label(row.float32Field.ToString());
            }
        }

    }


    class Table1
    {
        public int id;
        public float float32Field;
    }

    private class TestDB : SqliteDatabase
    {
        public TestDB()
            : base(Location, 1)
        {

        }

        public static string Location = Application.persistentDataPath + "/local";
        protected override void OnCreateDatabase()
        {
            NonQuery("  CREATE TABLE [table1] (  [id] INTEGER PRIMARY KEY AUTOINCREMENT,  [stringField] VARCHAR(50),  [int32Field] INT,  [int64Field] INT64,  [textField] TEXT,  [boolField] BOOL,  [float32Field] FLOAT,  [float64Field] DOUBLE)");
        }
    }
}
