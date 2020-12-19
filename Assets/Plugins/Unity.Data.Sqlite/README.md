# Unity.Data.Sqlite

Unity 使用Sqlite数据库，Unity3D usage sqlite database



## 使用

### 定义表结构

```c#
class Table1
{
    public int id;
    public float float32Field;
}
```



### 继承 `SqliteDatabase` 实现数据库类

```c#
class MyDB : SqliteDatabase
{
    public MyDB()
    	: base(Location, 1)
    {}

    public static string Location = Application.persistentDataPath + "/local";
    protected override void OnCreateDatabase()
    {
        NonQuery("CREATE TABLE [table1] ([id] INTEGER PRIMARY KEY AUTOINCREMENT, [float32Field] FLOAT)");
    }
}
```



### 初始化DB

```
MyDB db = new MyDB();
db.Open();
```

#### 添加数据

```
db.NonQuery("insert into table1 (float32Field) values(@float32Field)", Random.value);
```

#### 查询数据

```
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