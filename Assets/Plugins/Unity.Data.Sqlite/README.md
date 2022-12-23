# Unity.Data.Sqlite

Unity 使用Sqlite数据库，Unity3D usage sqlite database



## 使用

1. 定义数据库

```c#
class MyDB : SqliteDatabase
{
    public MyDB()
    	: base(Location, 1)
    {}

    public static string Location = Application.persistentDataPath + "/local.db";
    protected override void OnCreateDatabase()
    {
        NonQuery("CREATE TABLE [table1] ([id] INTEGER PRIMARY KEY AUTOINCREMENT, [float32] FLOAT)");
    }
}
```

2. 初始化DB

   ```c#
   MyDB db = new MyDB();
   db.Open();
   ```

3. 添加数据

   ```c#
    using (var cmd = db.Connection.CreateCommand())
    {
        cmd.CommandText = "insert into table1 (float32) values(@float32)";
        cmd.Parameters.Add("float32", System.Data.DbType.Single).Value = Random.value;
        cmd.ExecuteNonQuery();
    }
   ```

4. 查询数据

   ```c#
   using (var cmd = db.Connection.CreateCommand())
   {
       cmd.CommandText = "select * from table1";
       using (var reader = cmd.ExecuteReader())
       {
           while (reader.Read())
           {
           	Debug.Log($"id: {reader["id"]}, float32: {reader["float32"]}");
           }
       }
   }
   ```

   ```c#
 using (var reader = cmd.Reader("select * from table1"))
    {
     while (reader.Read())
        {
   		var value = reader["field"];
        }
    }
   ```
   
   
   
   

## SqliteDatabase

定义表结构

```c#
class Table1
{
    public int id;
    public float float32;
}
```

### 添加数据

```c#
db.NonQuery("insert into table1 (float32) values(@float32)", Random.value);
```

声明顺序传递参数  `@param`

### 查询数据

```c#
db.Query<Table1>("select * from table1")
```



## SqliteDatabase 接口

#### int NonQuery(string cmdText, params object[] parameters)

执行SQL语句，返回影响的行数

#### T Scalar<T>(string cmdText, params object[] parameters)

执行 `select` 语句，返回查询值

#### SqliteDataReader Reader(string cmdText, params object[] parameters)

执行`select` 语句，返回 `reader`

#### void Fill(SqliteDataReader reader, object obj)

将 `reader` 数据 填充到`obj`对象

#### IEnumerable<T> Query<T>(string cmdText, params object[] parameters)

查询数据返回对象结构

#### BeginTransaction

开始事务

#### Commit

提交事务

#### Rollback

回滚事务

#### BackupDatabase

备份数据库

#### Recovery

恢复数据库

#### SetProperty

设置元数据表属性

#### GetProperty

获取元数据表属性

#### DeleteProperty

删除元数据表属性



## 编辑器使用

只在编辑器使用 `Sqlite`, 运行时不使用，在 `Player Settings/Scripting Define Symbols` 添加宏 `DATABASE_SQLITE_EDITOR`，在构建时将排除 `Sqlite` 所有程序集 (`.dll`, `.so`)

