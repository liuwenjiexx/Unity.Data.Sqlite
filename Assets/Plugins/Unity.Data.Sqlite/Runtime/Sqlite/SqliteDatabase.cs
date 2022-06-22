using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Runtime.InteropServices;
using System.Data;
using System.Data.Common;

namespace Yanmonet.Data.Sqlite
{


    public partial class SqliteDatabase : IDisposable
    {

        private string name;
        private string source;
        private int version;
        private SqliteConnection conn;
        private bool isOpened;
        private TransactionScope transScope;
        private bool isMemoryDB;

        private const int dbVersion = 3;
        private const string FileDbFormat = "URI=file:{0}";
        private const string FileAndVersionDBFormat = "URI=file:{0},version={1}";
        private const string MetadataTableName = "__metadata";
        private const string Metadata_Version = "_db_version_";
        private const string Metadata_Owner_ID = "_db_owner_id";
        private const string DeleteMetadataCmdText = "delete from " + MetadataTableName + " where key=@key";
        private const string DeleteAllMetadataCmdText = "delete from " + MetadataTableName;
        private const string InsertMetadataCmdText = "INSERT INTO " + MetadataTableName + " (key, value) VALUES (@key, @value)";


        public const string MemoryDBName = "Data Source=:memory:";

        /// <summary>
        /// 是否是内存数据库，否则为文件数据库
        /// </summary>
        public bool IsMemoryDB
        {
            get { return isMemoryDB; }
        }

        private bool runInMemory;

        /// <summary>
        /// load database memory
        /// </summary>
        public bool RuntInMemory
        {
            get { return runInMemory; }
        }

        private bool changed;

        /// <summary>
        /// 标识数据库内容是否改变
        /// </summary>
        public bool Changed
        {
            get { return changed; }
        }

        public bool HasTransation
        {
            get
            {
                return transScope != null;
            }
        }
        public void ClearChanged()
        {
            changed = false;
        }



        public SqliteDatabase(string dst, string src)
        {

            this.name = dst;
            this.source = src;
            isMemoryDB = IsMemoryName(dst);
            if (!isMemoryDB)
            {
                InitDatabase(dst, src);
            }
            else
            {
                Open(true);
            }

        }

        public bool debug;

        public SqliteDatabase(string name, int version)
        {
            if (name == null)
                throw new System.ArgumentNullException("name");

            //switch (Application.platform)
            //{
            //    case RuntimePlatform.OSXWebPlayer:
            //    case RuntimePlatform.WindowsWebPlayer:
            //        throw new Exception("sqlite not support platform :" + Application.platform);


            //}

            this.name = name;
            this.version = version;

            isMemoryDB = IsMemoryName(name);

            if (!isMemoryDB)
            {
                InitDatabase(name, version);
            }

        }


        public static bool IsMemoryName(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            return name.IndexOf(":memory:") >= 0;
        }

        public int Version { get { return version; } }
        public string Name { get { return name; } }


        public string OwnerID
        {
            get { return GetProperty<string>(Metadata_Owner_ID); }
            set
            {
                string oldId = OwnerID;
                if (oldId != value)
                {
                    SetProperty(Metadata_Owner_ID, value);
                    OnOwnerIDChanged(oldId, value);

                }
            }
        }


        protected virtual void OnOwnerIDChanged(string oldId, string ownerId)
        {

        }


        protected virtual void OnUpgradeDatabase(int oldVersion, int newVersion)
        {

        }
        protected virtual void OnDowngradeDatabase(int oldVersion, int newVersion)
        {
        }

        protected virtual void OnCreateDatabase()
        {
            //throw new NotImplementedException(Resource1.NotImplementCreateDB);
        }



        private void CopyDatabase(string srcPath, string dstPath)
        {
            if (File.Exists(dstPath))
                File.Delete(dstPath);

            File.Copy(srcPath, dstPath);
        }

        int GetDatabaseVersion(string name)
        {
            int ver = -1;
            try
            {
                using (SqliteConnection conn = new SqliteConnection(FileDbFormat.FormatArgs(name)))
                {
                    conn.Open();

                    ver = GetProperty<int>(conn, Metadata_Version, 0, true);
                }
            }
            catch (Exception ex)
            {
            }
            return ver;
        }




        void InitDatabase(string dst, string src)
        {

            int currentVersion = -1;
            version = -1;
            try
            {

                version = GetDatabaseVersion(src);

                currentVersion = GetDatabaseVersion(dst);

                if (currentVersion < version)
                {
                    CopyDatabase(src, dst);
                    OnUpgradeDatabase(currentVersion, version);
                }
                else if (currentVersion > version)
                {
                    OnDowngradeDatabase(currentVersion, version);
                }

                Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SetProperty<T>(string name, T value)
        {

            SetProperty(conn, name, value);
        }

        public T GetProperty<T>(string name)
        {

            return GetProperty<T>(conn, name, default(T), true);
        }

        public T GetProperty<T>(string name, T defaultValue)
        {
            return GetProperty<T>(conn, name, defaultValue, false);
        }

        string MetadataNameToString(Properties name)
        {
            return name.ToString();
        }


        public void SetProperty<T>(Properties name, T value)
        {
            SetProperty(MetadataNameToString(name), value);
        }
        public T GetProperty<T>(Properties name)
        {
            return GetProperty<T>(MetadataNameToString(name));
        }

        public T GetProperty<T>(Properties name, T defaultValue)
        {
            return GetProperty(MetadataNameToString(name), defaultValue);
        }

        private T GetProperty<T>(SqliteConnection conn, string name, T defaultValue, bool hasDefaultValue)
        {
            T value;
            string sqlText = "select value from " + MetadataTableName + " where key=@key";
            using (var cmd = new SqliteCommand(sqlText, conn))
            {
                AttachParameters(cmd, new object[] { name });
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {

                        object val = reader.GetValue(0);
                        value = ConvertFromDBValue<T>(reader.GetValue(0));
                    }
                    else
                    {
                        value = defaultValue;
                    }
                }
            }

            return value;
        }

        public void SetProperty<T>(SqliteConnection conn, string name, T value)
        {
            using (var cmd = new SqliteCommand(conn))
            {
                cmd.CommandText = DeleteMetadataCmdText;
                AttachParameters(cmd, new object[] { name });
                cmd.ExecuteNonQuery();

                cmd.CommandText = InsertMetadataCmdText;
                //?
                //string stringValue = value == null ? null : value.ToString();
                // AttachParameters(cmd, new object[] { name, stringValue });
                AttachParameters(cmd, new object[] { name, value });
                cmd.ExecuteNonQuery();
            }

        }


        public void DeleteProperty(SqliteConnection conn, string name)
        {
            using (var cmd = new SqliteCommand(conn))
            {
                cmd.CommandText = DeleteMetadataCmdText;
                AttachParameters(cmd, new object[] { name });
                cmd.ExecuteNonQuery();
            }
        }
        public void DeleteAllProperty(SqliteConnection conn)
        {
            using (var cmd = new SqliteCommand(conn))
            {
                cmd.CommandText = DeleteAllMetadataCmdText;
                cmd.ExecuteNonQuery();
            }
        }


        void InitDatabase(string name, int version)
        {

            string cmdText;
            try
            {
                Open();
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(name))
                        File.Delete(name);
                }
                catch (Exception ex2)
                {
                    throw ex2;
                }
            }
            try
            {
                Open();

                cmdText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@name";

                if (Scalar<int>(cmdText, MetadataTableName) > 0)
                {
                    int currentVersion;

                    currentVersion = GetProperty<int>(Metadata_Version);

                    if (currentVersion != version)
                    {
                        using (BeginTransaction())
                        {

                            //DeleteAllMetadata(conn);
                            SetProperty(Metadata_Version, version);

                            if (currentVersion < version)
                            {

                                OnUpgradeDatabase(currentVersion, version);
                            }
                            else
                            {

                                OnDowngradeDatabase(currentVersion, version);
                            }
                            Commit();
                        }
                    }

                }
                else
                {

                    using (BeginTransaction())
                    {
                        CreateMetadataTable();

                        OnCreateDatabase();
                        Commit();
                    }
                }


                conn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Close();
            }


        }

        protected void CreateMetadataTable()
        {
            string cmdText;
            cmdText = "CREATE TABLE " + MetadataTableName + " ([key] NVARCHAR(50), [value] NVARCHAR(1024),PRIMARY KEY ([key]))";

            NonQuery(cmdText);

            SetProperty(Metadata_Version, version);

        }


        /// <summary>
        /// 运行在内存
        /// </summary>
        [Obsolete]
        public void OpenInMemory()
        {
            _OpenInMemory();
        }
        private void _OpenInMemory()
        {
            if (isOpened)
            {
                if (runInMemory)
                    return;

                Close();

            }

            SqliteConnection destConn;

            if (isMemoryDB && source == null)
            {
                this.conn = destConn = new SqliteConnection(name);
                destConn.Open();
                using (BeginTransaction())
                {
                    CreateMetadataTable();

                    OnCreateDatabase();
                    Commit();
                }
            }
            else
            {
                this.conn = destConn = new SqliteConnection(MemoryDBName);
                destConn.Open();
                string src = source;
                if (source != null)
                {
                    src = source;
                }
                else
                {
                    src = name;
                }
                if (!IsMemoryName(src))
                {
                    src = FileDbFormat.FormatArgs(src, dbVersion);
                }

                using (SqliteConnection sourceConn = new SqliteConnection(src))
                {
                    sourceConn.Open();
                    BackupDatabase(sourceConn, destConn);
                }
            }
            //this.conn = destConn;
            //jit
            //   conn.Update += DB_Update;
            runInMemory = true;
            isOpened = true;
        }

        public void BackupDatabase()
        {
            if (isMemoryDB)
                throw new Exception("该数据库为内存数据库，无法保存");
            if (!isOpened)
                return;
            if (!runInMemory)
                return;

            using (SqliteConnection sourceConn = new SqliteConnection(FileDbFormat.FormatArgs(name, dbVersion)))
            {
                sourceConn.Open();
                BackupDatabase(conn, sourceConn);
            }

        }
        void DB_Update(object sender, UpdateEventArgs e)
        {
            changed |= true;
        }

        #region BackupDatabase


        static IntPtr GetDBPtr(SqliteConnection conn)
        {
            IntPtr ptr;

            BindingFlags flags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo fInfo = typeof(SqliteConnection).GetField("_sql", flags);

            object _sql = fInfo.GetValue(conn);
            fInfo = _sql.GetType().GetField("_sql", flags);

            _sql = fInfo.GetValue(_sql);
            fInfo = _sql.GetType().GetField("handle", flags);
            ptr = (IntPtr)fInfo.GetValue(_sql);

            return ptr;
        }

        private static void BackupDatabase(SqliteConnection source, SqliteConnection dest)
        {
            IntPtr ptrSrc = GetDBPtr(source);
            if (ptrSrc == IntPtr.Zero)
                throw new Exception("ptr zero");

            IntPtr ptrDest = GetDBPtr(dest);
            if (ptrDest == IntPtr.Zero)
                throw new Exception("ptr zero");

            IntPtr ptrBk = sqlite3_backup_init(ptrDest, "main", ptrSrc, "main");
            if (ptrBk == IntPtr.Zero)
                throw new Exception("backup error");

            sqlite3_backup_step(ptrBk, -1);
            sqlite3_backup_finish(ptrBk);
        }

        public void BackupDatabase(SqliteDatabase dest)
        {
            bool isCloseFrom = false, isCloseDest = false;
            if (!IsOpened)
            {
                Open();
                isCloseFrom = true;
            }
            if (!dest.IsOpened)
            {
                isCloseDest = true;
            }

            BackupDatabase(conn, dest.conn);

            if (isCloseFrom)
                Close();
            if (isCloseDest)
                dest.Close();

        }

        public void BackupDatabase(string backupPath)
        {
            string filePath = Name;

            if (!File.Exists(filePath))
                throw new Exception("db file not extis: " + filePath);


            Close();

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Copy(filePath, backupPath);

        }

        #endregion BackupDatabase

        public void Recovery(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");
            if (!File.Exists(filePath))
                throw new Exception("Recovery db fail, file not exists path:" + filePath);

            byte[] data = File.ReadAllBytes(filePath);

            Recovery(data);

        }


        public void Recovery(byte[] file)
        {
            if (isMemoryDB)
                throw new Exception("is memory db");

            string filePath = Name;
            if (IsOpened)
                Close();

            if (File.Exists(filePath))
                File.Delete(filePath);

            File.WriteAllBytes(filePath, file);

            if (!isMemoryDB)
            {
                InitDatabase(name, version);
            }
        }


        #region MyRegion
        private const string ParamNameRegex = @"(@)([@\w]*)";

        static Dictionary<string, CommandTextRef> cachedCmdTexts = new Dictionary<string, CommandTextRef>();

        private class CommandTextRef
        {
            public string CommandText;

            /// <summary>
            /// @paramName
            /// </summary>
            public string[] rawParamNames;

            /// <summary>
            /// paramName
            /// </summary>
            public string[] paramNames;

        }

        CommandTextRef ParseCommandText(string cmdText)
        {
            CommandTextRef cmdTextRef;
            int i = 0;
            if (!cachedCmdTexts.TryGetValue(cmdText, out cmdTextRef))
            {

                cmdTextRef = new CommandTextRef();
                cmdTextRef.CommandText = cmdText;

                Regex ex = new Regex(ParamNameRegex);
                MatchCollection mc = ex.Matches(cmdText);
                string[] rawParamNames = new string[mc.Count];
                string[] paramNames = new string[mc.Count];
                foreach (Match m in mc)
                {
                    string rawParamName = m.Value.Trim();
                    if (rawParamNames.Contains(rawParamName))
                        continue;

                    rawParamNames[i] = rawParamName;
                    paramNames[i] = rawParamName.Substring(1);

                    // Debug.Log("name: " + "|" + m.Value + "|");
                    i++;
                }
                cmdTextRef.rawParamNames = rawParamNames;
                cmdTextRef.paramNames = paramNames;
                cachedCmdTexts[cmdText] = cmdTextRef;
            }
            return cmdTextRef;
        }
        /*
        protected object[] GetEntityParameterList(string cmdText, object entity)
        {
            if (cmdText == null)
                throw new ArgumentNullException("cmdText");


            CommandTextRef cmdTextRef = ParseCommandText(cmdText);

            if (cmdTextRef.rawParamNames.Length <= 0)
                return new object[0];


            if (entity == null)
                throw new ArgumentNullException("entity");

            Type objType = entity.GetType();

            if (debug)
                Debug.Log("attach param by entity, type:{0}".FormatString(objType));
            DataMemberInfo[] members = GetMembers(objType);



            object[] paramList = new object[cmdTextRef.rawParamNames.Length];


            DataMemberInfo member;
            object value;
            int i = 0;
            foreach (var paramName in cmdTextRef.paramNames)
            {

                member = null;
                for (int j = 0; j < members.Length; j++)
                {
                    if (members[j].LowerMemberName == paramName.ToLowerInvariant())
                    {
                        member = members[j];
                        break;
                    }
                }

                if (member == null)
                    throw new Exception("not find parameter member: {0} ,type: {1} ".FormatString(paramName, objType));

                if (member.Property != null)
                {
                    value = member.Property.GetValueUnity(entity, null);
                }
                else if (member.Field != null)
                {
                    value = member.Field.GetValue(entity);
                }
                else
                {
                    throw new Exception("parameter member {0}, type: {1}, property and field null".FormatString(paramName, objType));
                }

                if (debug)
                    Debug.Log("param index:{0} name:{1}, value:{2}".FormatString(i, paramName, value));

                paramList[i] = value;
                i++;
            }

            return paramList;
        }
        */
        //protected void AttachParametersByEntity(SqliteCommand cmd, object entity)
        //{
        //    if (cmd == null)
        //        throw new ArgumentNullException("cmd");
        //    if (entity == null)
        //        throw new ArgumentNullException("entity");

        //    Type objType = entity.GetType();

        //    if (debug)
        //        Debug.Log("attach param by entity, type:{0}".FormatString(objType));
        //    DataMemberInfo[] members = DataMemberInfo.GetDataMembers(objType);

        //    string cmdText = cmd.CommandText;

        //    CommandTextRef cmdTextRef = ParseCommandText(cmdText);

        //    SqliteParameterCollection coll = cmd.Parameters;
        //    coll.Clear();

        //    SqliteParameter param;
        //    DataMemberInfo member;
        //    object value;
        //    int i = 0;
        //    foreach (var paramName in cmdTextRef.paramNames)
        //    {
        //        param = new SqliteParameter();
        //        param.ParameterName = cmdTextRef.rawParamNames[i];

        //        member = null;
        //        for (int j = 0; j < members.Length; j++)
        //        {
        //            if (members[j].LowerMemberName == paramName.ToLowerInvariant())
        //            {
        //                member = members[j];
        //                break;
        //            }
        //        }

        //        if (member == null)
        //            throw new Exception("not find parameter member: {0} ,type: {1} ".FormatString(paramName, objType));

        //        if (member.Property != null)
        //        {
        //            value = member.Property.GetValueUnity(entity, null);
        //        }
        //        else if (member.Field != null)
        //        {
        //            value = member.Field.GetValue(entity);
        //        }
        //        else
        //        {
        //            throw new Exception("parameter member {0}, type: {1}, property and field null".FormatString(paramName, objType));
        //        }
        //        param.Value = ToDBValue(value);
        //        if (debug)
        //            Debug.Log("param index:{0} name:{1}, value:{2}".FormatString(i, param.ParameterName, param.Value));
        //        coll.Add(param);
        //        i++;
        //    }

        //}


        protected SqliteParameterCollection AttachParameters(SqliteCommand cmd, object[] paramList)
        {
            if (paramList == null || paramList.Length == 0) return null;

            SqliteParameterCollection coll = cmd.Parameters;
            coll.Clear();
            string cmdText = cmd.CommandText;

            CommandTextRef cmdTextRef = ParseCommandText(cmdText);
            int i;
            i = 0;
            Type valueType = null;
            foreach (object value in paramList)
            {

                if (value == null)
                {
                    valueType = null;
                }
                else
                {
                    valueType = value.GetType();
                }


                SqliteParameter parm = new SqliteParameter();

                parm.ParameterName = cmdTextRef.rawParamNames[i];

                parm.Value = ConvertToDBValue(value, typeof(object));

                coll.Add(parm);
                i++;
            }
            return coll;
        }

        #endregion

        public bool IsOpened
        {
            get { return isOpened; }
        }


        public void Open()
        {
            if (isOpened)
                return;
            if (isMemoryDB)
            {
                _OpenInMemory();
                return;
            }

            if (this.conn == null)
            {

                string connStr = null;
#if UNITY_IPHONE1
            SqliteConnectionStringBuilder scsb=new SqliteConnectionStringBuilder();
            //scsb.DataSource=name;
            scsb.Uri=name; 
            connStr=scsb.ToString();
#else
                //connStr = OpenDbAndVersionFormat.FormatString(name, dbVersion);
                connStr = FileDbFormat.FormatArgs(name, dbVersion);

#endif

                SqliteConnection conn = new SqliteConnection(connStr);
                // conn.StateChange += StateChanged;
                //jit
                // conn.Update += DB_Update;

                conn.Open();
                this.conn = conn;
            }


            isOpened = true;
            runInMemory = false;
        }

        public void Open(bool inMemory)
        {
            if (inMemory)
                _OpenInMemory();
            else
                Open();
        }

        public void Close()
        {
            if (!isOpened)
                return;

            isOpened = false;
            runInMemory = false;

            if (conn != null)
            {
                if (transScope != null)
                    Rollback();

                var c = conn;
                conn = null;
                c.Close();
            }

        }

        public SqliteConnection Connection => conn;


        #region Transaction


        class TransactionScope : IDisposable
        {
            private SqliteDatabase db;
            public TransactionScope previous;
            public TransactionScope next;
            public SqliteTransaction trans;
            private bool isExecuted;


            public TransactionScope(SqliteDatabase db)
            {
                this.db = db;
                if (db.transScope != null)
                {
                    this.previous = db.transScope;
                }
                else
                {
                    trans = db.conn.BeginTransaction();
                }

            }

            private void SetNextCancel()
            {
                var current = next;
                while (current != null)
                {
                    if (!current.isExecuted)
                    {
                        current.isExecuted = true;
                    }
                    current = current.next;
                }
            }

            public void Commit()
            {
                if (isExecuted)
                    return;
                isExecuted = true;
                if (trans != null)
                {
                    var t = trans;
                    t.Commit();
                    trans = null;
                }
                db.transScope = previous;
                SetNextCancel();
            }

            public void Rollback()
            {
                if (isExecuted)
                    return;
                isExecuted = true;
                if (trans != null)
                {
                    var t = trans;
                    trans = null;
                    t.Rollback();
                }
                db.transScope = previous;
                SetNextCancel();
            }

            public void Dispose()
            {
                Rollback();
            }
        }


        void CheckTransaction()
        {

            if (transScope == null)
                throw new InvalidOperationException("Resource1.CommitNotTransaction");
        }


        public IDisposable BeginTransaction()
        {
            CheckOpen();

            transScope = new TransactionScope(this);

            return transScope;
        }

        public void Rollback()
        {
            CheckOpen();
            CheckTransaction();

            transScope.Rollback();
        }

        public void Commit()
        {
            CheckOpen();
            CheckTransaction();

            transScope.Commit();
        }

        protected SqliteTransaction GetTransaction()
        {
            if (transScope != null)
                return transScope.trans;
            return null;
        }

        #endregion

        void CheckOpen()
        {
            if (conn == null)
                throw new InvalidOperationException("Resource1.NotOpenDB");
            if (!(conn.State == ConnectionState.Open || conn.State == ConnectionState.Connecting))
                throw new InvalidOperationException("Resource1.NotOpenDB");
        }

        void ExecuteBefore()
        {
            CheckOpen();
        }

        //private void DebugInfo(string action, string sqlText, object[] parameters)
        //{
        //    Log.I("{0} cmdText:{1} \nparameter:{2}".FormatString(action, sqlText, string.Join(",", parameters.Select(o => o.ToStringOrEmpty()).ToArray())));
        //}
        /*
                private void EntityDebugInfo(string action, string sqlText, object entity)
                {
                    Debug.Log("{0} cmdText:{1} ".FormatString(action, sqlText));
                }*/
        public SqliteDataReader Reader(string cmdText, params object[] parameters)
        {
            ExecuteBefore();
            using (var cmd = GetCommand(cmdText, parameters))
            {
                try
                {
                    return cmd.ExecuteReader();
                }
                catch (Exception ex)
                {
                    throw new CommandException(cmd, ex);
                }
            }
        }


        protected SqliteCommand GetCommand(string cmdText, params object[] parameters)
        {
            var cmd = new SqliteCommand(cmdText, conn, GetTransaction());
            if (parameters != null && parameters.Length > 0)
                AttachParameters(cmd, parameters);
            return cmd;
        }


        public IEnumerable<T> Query<T>(string cmdText, params object[] parameters)
        {
            ExecuteBefore();

            T obj;
            using (var cmd = GetCommand(cmdText, parameters))
            using (var reader = cmd.ExecuteReader())
            {
                Type type = typeof(T);

                while (true)
                {
                    try
                    {
                        if (!reader.Read())
                            break;
                    }
                    catch (Exception ex)
                    {
                        throw new CommandException(cmd, ex);
                    }

                    if (type.IsPrimitive || type == typeof(string))
                    {
                        obj = ConvertFromDBValue<T>(reader[0]);
                    }
                    else
                    {
                        obj = Activator.CreateInstance<T>();
                        Fill(reader, obj);
                    }

                    yield return obj;
                }
            }

        }

        /// <summary>
        /// query first row
        /// </summary>
        public T QueryFirst<T>(string cmdText, params object[] parameters)
        {
            ExecuteBefore();

            using (var cmd = GetCommand(cmdText, parameters))
            using (var reader = cmd.ExecuteReader())
            {
                bool hasResult;
                try
                {
                    hasResult = reader.Read();
                }
                catch (Exception ex)
                {
                    throw new CommandException(cmd, ex);
                }

                if (hasResult)
                {
                    T obj = Activator.CreateInstance<T>();
                    Fill(reader, obj);
                    return obj;
                }
            }

            return default(T);
        }

        //private class SqliteDataReaderWrap : DbDataReader, IDisposable
        //{
        //    public SqliteDataReaderWrap(SqliteDataReader reader)
        //    {
        //        this.reader = reader;
        //    }
        //    private SqliteDataReader reader;
        //    public override void Close()
        //    {
        //        reader.Close();
        //    }
        //    public override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType)
        //    {
        //        return reader.CreateObjRef(requestedType);
        //    }
        //    public override int Depth
        //    {
        //        get { return reader.Depth; }
        //    }


        public int NonQuery(string cmdText, params object[] parameters)
        {
            ExecuteBefore();

            using (var cmd = GetCommand(cmdText, parameters))
            {
                try
                {
                    return cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new CommandException(cmd, ex);
                }
            }
        }


        public object Scalar(string cmdText, params object[] parameters)
        {
            ExecuteBefore();

            using (var cmd = GetCommand(cmdText, parameters))
            {
                try
                {
                    return cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw new CommandException(cmd, ex);
                }
            }
        }

        public T Scalar<T>(string cmdText, params object[] parameters)
        {
            return (T)ConvertFromDBValue(Scalar(cmdText, parameters), typeof(T));
        }

        public object Scalar(Type valueType, string cmdText, params object[] parameters)
        {
            return ConvertFromDBValue(Scalar(cmdText, parameters), valueType);
        }




        #region Entity

        //public void EntityInsert(object entity)
        //{
        //    if (entity == null)
        //        return;
        //    Type objType = entity.GetType();
        //    var members = DataMemberInfo.GetDataMembers(objType);
        //    if (members == null || members.Length <= 0)
        //        throw new Exception("type: {0} no data member".FormatString(objType));



        //}



        //public void EntityUpdate(object entity)
        //{

        //}
        /*  private static Type classAttrType = typeof(EntityTableAttribute);
         private static Type memberAttrType = typeof(EntityMemberAttribute);
         private static Type memberIgnoreAttrType = typeof(IgnoreEntityMemberAttribute);

         public void EntityDelete(object entity)
         {
             if (entity == null)
                 return;
             Type objType = entity.GetType();
             var members = GetEntityMembers(objType);
             var id = members.Where(o => o.ID).FirstOrDefault();
             if (id == null)
                 throw new Exception("id null");


         }
         DataMemberDescription GetDataDesc(Type type)
         {
             return DataMemberInfo.GetDataMembers(type, classAttrType, memberAttrType, memberIgnoreAttrType);
         }
         DataMemberInfo[] GetMembers(Type type)
         {
             return GetDataDesc(type).Members;
         }*/
        /*  EntityMemberAttribute[] GetEntityMembers(Type type)
          {
              return GetDataDesc(type).Members.Where(o => o.MemberAttribute != null).Select(o => (EntityMemberAttribute)o.MemberAttribute).ToArray();
          }

          public int EntityNonQuery(string cmdText, object entity)
          {
              return _ExecuteNonQuery(cmdText, GetEntityParameterList(cmdText, entity));
          }

          public object EntityScalar(string cmdText, object entity)
          {
              return _ExecuteScalar(cmdText, GetEntityParameterList(cmdText, entity));
          }


          public T EntityScalar<T>(string cmdText, object entity)
          {
              return _ExecuteScalar<T>(cmdText, GetEntityParameterList(cmdText, entity));
          }
          */
        /*
        public IEnumerable<T> EntityQuery<T>(string cmdText, object entity)
        {
            return _ExecuteQuery<T>(cmdText, GetEntityParameterList(cmdText, entity));
        }
        public T EntityQueryFirst<T>(string cmdText, object entity)
        {
            return _ExecuteQueryFirst<T>(cmdText, GetEntityParameterList(cmdText, entity));
        }*/
        #endregion



        object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }



        public object ConvertToDBValue(object value, Type dbValueType)
        {
            if (value == null)
                return DBNull.Value;
            Type valueType = value.GetType();
            /*     if (ArrayToStringConverter.instance.CanConvert(valueType, dbValueType))
                     return ArrayToStringConverter.instance.ConvertToDBValue(valueType, value, dbValueType);*/

            switch (valueType.ToString())
            {
                case "System.DateTime":
                    {
                        DateTime dt = (DateTime)value;
                        //dt = TimeZone.CurrentTimeZone.ToUniversalTime(dt);
                        return dt.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                    }
            }
            return value;
        }

        public T ConvertFromDBValue<T>(object dbValue)
        {
            return (T)ConvertFromDBValue(dbValue, typeof(T));
        }

        public virtual object ConvertFromDBValue(object dbValue, Type valueType)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return GetDefaultValue(valueType);

            Type dbValueType = dbValue.GetType();
            object value;
            if (valueType.IsAssignableFrom(dbValueType))
            {
                value = dbValue;
                //if (valueType == typeof(DateTime))
                //{
                //    value = TimeZone.CurrentTimeZone.ToLocalTime((DateTime)dbValue);
                //}
                return value;
            }
            /*
            if (ArrayToStringConverter.instance.CanConvert(dbValueType, valueType))
                return ArrayToStringConverter.instance.ConvertToValue(dbValue, valueType);*/

            string str;

            if (valueType.IsEnum)
            {
                str = dbValue == null ? "" : dbValue.ToString();
                ulong n;
                if (ulong.TryParse(str, out n))
                {
                    return Enum.ToObject(valueType, n);
                }
                return Enum.Parse(valueType, str, true);
            }


            if (valueType == typeof(DateTime))
            {

                if (dbValueType == typeof(long))
                {
                    DateTime dt = DateTime.FromFileTime((long)dbValue);
                    //value = TimeZone.CurrentTimeZone.ToLocalTime(dt);
                    value = dt;
                }
                else
                {
                    value = Convert.ChangeType(dbValue, valueType);
                }
                //value = TimeZone.CurrentTimeZone.ToLocalTime((DateTime)value);
            }
            else if (valueType == typeof(Guid))
            {
                if (dbValueType == typeof(string))
                {
                    value = new Guid((string)dbValue);
                }
                else
                {
                    value = Convert.ChangeType(dbValue, valueType);
                }
            }
            else
            {
                value = Convert.ChangeType(dbValue, valueType);
            }
            return value;
        }


        public long GetLastInsertRowID()
        {
            return Scalar<long>("select last_insert_rowid()");
        }

        public void UpdateChildCountField(string tableName, string idField, string parentIDField, string childCountField, string where = null)
        {
            string cmdText = string.Format("update [{0}]  set {3}=(select count(*) from [{0}] t2 where t2.[{2}]=[{0}].[{1}] ) ", tableName, idField, parentIDField, childCountField);

            if (!string.IsNullOrEmpty(where))
            {
                cmdText += " where " + where;
            }

            NonQuery(cmdText);
        }
        //public SqliteCommand BuildSelectCommand(string table, string select, string where, string orderby, string start, int limit, object[] args)
        //{
        //    return BuildSelectCommand(table, select, where, orderby, start, limit, "", args);
        //}
        //public SqliteCommand BuildSelectCommand(string table, string select, string where, string orderby, string start, int limit, string rowid, object[] args)
        //{
        //    if (string.IsNullOrEmpty(select))
        //        select = "*";

        //    if (!string.IsNullOrEmpty(where))
        //        where = " where " + where;
        //    if (string.IsNullOrEmpty(rowid))
        //        rowid = "rowid";

        //    //string cmdText = string.Format("select {1} from {0} {2} {3} limit ( select IFNULL(max(rowid),0) from ( select {6},* from {0}  {2}  {3} ) as t where t.{4}  ) , {5} ",
        //    string cmdText = string.Format("select {1} from {0} {2} {3} limit ( select IFNULL(max(rowid),0) from ( select {6},* from {0}  {2}  {3} ) as t where t.{4}  ) , {5} ",
        //        table, select, where, orderby, start, limit, rowid);

        //    SqliteCommand cmd = new SqliteCommand(cmdText, conn, trans);

        //    AttachParameters(cmd, args);

        //    return cmd;

        //}
        public SqliteCommand BuildSelectCommand(string table, string select, string where, string orderby, int limit, object[] args)
        {
            if (string.IsNullOrEmpty(select))
                select = "*";

            if (!string.IsNullOrEmpty(where))
                where = " where " + where;

            string cmdText = string.Format("select {1} from {0} {2} {3} limit {4}", table, select, where, orderby, limit);

            SqliteCommand cmd = new SqliteCommand(cmdText, conn, GetTransaction());

            AttachParameters(cmd, args);

            return cmd;
        }

        public SqliteCommand BuildSelectCommand(string table, string select, string where, string orderby, object[] args)
        {
            if (string.IsNullOrEmpty(select))
                select = "*";

            if (!string.IsNullOrEmpty(where))
                where = " where " + where;

            string cmdText = string.Format("select {1} from {0} {2} {3}", table, select, where, orderby);
            using (SqliteCommand cmd = new SqliteCommand(cmdText, conn, GetTransaction()))
            {
                AttachParameters(cmd, args);

                return cmd;
            }


        }



        //BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.SetProperty | BindingFlags.SetField;
        /*
        static Dictionary<Type, IDataConverter> converterCached = new Dictionary<Type, IDataConverter>();

        static IDataConverter GetConverter(Type type)
        {
            IDataConverter conv;
            if (converterCached.TryGetValue(type, out conv))
                return conv;

            conv = (IDataConverter)type.GetConstructor(Type.EmptyTypes).Invoke(null);
            converterCached[type] = conv;

            return conv;
        }*/

        public static IEnumerable<KeyValuePair<string, object>> ReaderToKeyValuePairs(SqliteDataReader reader)
        {
            int fieldCount = reader.FieldCount;
            string fieldName;
            object[] values = new object[fieldCount];
            reader.GetValues(values);
            for (int i = 0; i < fieldCount; i++)
            {
                fieldName = reader.GetName(i);

                yield return new KeyValuePair<string, object>(fieldName, values[i]);
            }
        }

        public void Fill(SqliteDataReader reader, object obj)
        {
            if (obj == null)
                return;
            Type objType = obj.GetType();

            try
            {

                if (typeof(IDictionary).IsAssignableFrom(objType))
                {
                    object val = null;
                    int fieldCount = reader.FieldCount;
                    string fieldName;
                    object[] values = new object[fieldCount];
                    reader.GetValues(values);
                    IDictionary dic = (IDictionary)obj;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        fieldName = reader.GetName(i);
                        val = values[i];
                        dic[fieldName] = val;
                    }
                    return;
                }
                else
                {
                    object val = null;
                    int fieldCount = reader.FieldCount;
                    string fieldName;
                    object[] values = new object[fieldCount];
                    reader.GetValues(values);

                    var members = DataFillHelper.GetDataMemberToMemberMapping(objType);
                    DataFillHelper.MappingInfo mapping;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        fieldName = reader.GetName(i);
                        if (members.TryGetValue(fieldName, out mapping))
                        {
                            val = ConvertFromDBValue(values[i], mapping.memberType);
                            if (mapping.pInfo != null)
                            {
                                mapping.pInfo.GetSetMethod().Invoke(obj, new object[] { val });
                            }
                            else
                            {
                                mapping.fInfo.SetValue(obj, val);
                            }
                        }
                    }
                    //***DataMemberAttribute.Fill(obj, ReaderToKeyValuePairs(reader));
                }

            }
            catch (Exception ex)
            {
                /*  if (member != null)
                  {
                      string msg = "";
                      if (member.pInfo != null)
                      {
                          msg = "type:" + member.pInfo.DeclaringType.Name + " property:" + member.pInfo.Name + ", type:" + member.pInfo.PropertyType;
                      }
                      else
                      {
                          msg = "type:" + member.fInfo.DeclaringType.Name + " field:" + member.fInfo.Name + ", type:" + member.fInfo.FieldType;
                      }
                      msg += ", dbValue:" + val + "," + ex;
                      Debug.LogError(msg);
                  }*/
                throw ex;
            }


        }
        //public bool CopyError(Result result)
        //{

        //    if (HasError)
        //    {
        //        result.Success = false;
        //        result.Error = LastError == null ? "" : LastError.ToString();
        //        return true;

        //    }
        //    return false;
        //}

        /// <summary>
        /// default id field name 'id'
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public object GetLastInsertID(string tableName)
        {
            return GetLastInsertID(tableName, "id");
        }

        public object GetLastInsertID(string tableName, string idName)
        {
            object id = Scalar("select {1} from {0} where rowid=(select last_insert_rowid())".FormatArgs(tableName, idName));
            return id;
        }

        public enum Properties
        {
            DataVersion,
        }

        class DataFillHelper
        {

            public class MappingInfo
            {
                public string memberName;
                public string mappingName;
                public Type memberType;
                public FieldInfo fInfo;
                public PropertyInfo pInfo;
            }

            static Dictionary<Type, Dictionary<string, MappingInfo>> toDataMemberMappings;
            static Dictionary<Type, Dictionary<string, MappingInfo>> toMemberMappings;

            public static Dictionary<string, MappingInfo> GetMemberToDataMemberMapping(Type type)
            {
                Dictionary<string, MappingInfo> toDataMemberMapping, toMemberMapping;
                GetDataMapping(type, out toDataMemberMapping, out toMemberMapping);
                return toDataMemberMapping;
            }
            public static Dictionary<string, MappingInfo> GetDataMemberToMemberMapping(Type type)
            {
                Dictionary<string, MappingInfo> toDataMemberMapping, toMemberMapping;
                GetDataMapping(type, out toDataMemberMapping, out toMemberMapping);
                return toMemberMapping;
            }



            static void GetDataMapping(Type type, out Dictionary<string, MappingInfo> toDataMemberMapping, out Dictionary<string, MappingInfo> toMemberMapping)
            {

                if (toDataMemberMappings == null)
                {
                    toDataMemberMappings = new Dictionary<Type, Dictionary<string, MappingInfo>>();
                    toMemberMappings = new Dictionary<Type, Dictionary<string, MappingInfo>>();
                }

                if (toDataMemberMappings.TryGetValue(type, out toDataMemberMapping))
                {
                    toMemberMappings.TryGetValue(type, out toMemberMapping);
                    return;
                }

                toDataMemberMapping = new Dictionary<string, MappingInfo>();
                toMemberMapping = new Dictionary<string, MappingInfo>();

                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty;

                MappingInfo info;
                foreach (var m in type.GetMembers(bindingFlags))
                {
                    var attrs = m.GetCustomAttributes(true);
                    Attribute dataAttr = (Attribute)attrs.Where(o => o.GetType().Name == "DataMemberAttribute").FirstOrDefault();
                    info = new MappingInfo();
                    info.memberName = m.Name;
                    info.mappingName = m.Name;

                    if (m.MemberType == MemberTypes.Property)
                    {
                        info.pInfo = (PropertyInfo)m;
                        info.memberType = info.pInfo.PropertyType;
                    }
                    else if (m.MemberType == MemberTypes.Field)
                    {
                        info.fInfo = (FieldInfo)m;
                        info.memberType = info.fInfo.FieldType;
                    }
                    else
                        continue;
                    if (dataAttr != null)
                    {
                        var nameProp = dataAttr.GetType().GetProperty("Name");
                        if (nameProp != null)
                        {
                            info.mappingName = nameProp.GetGetMethod().Invoke(dataAttr, null) as string;
                        }
                    }
                    else
                    {
                        if (info.pInfo != null)
                        {

                            if (info.pInfo.GetSetMethod() == null || !info.pInfo.GetSetMethod().IsPublic)
                                continue;
                        }
                        else
                        {
                            if (!info.fInfo.IsPublic || info.fInfo.IsInitOnly)
                                continue;

                        }
                    }
                    if (string.IsNullOrEmpty(info.mappingName))
                        info.mappingName = m.Name;
                    toDataMemberMapping[info.memberName] = info;
                    toMemberMapping[info.mappingName] = info;
                }

                toDataMemberMappings[type] = toDataMemberMapping;
                toMemberMappings[type] = toMemberMapping;
            }
        }


        #region IDisposable 成员

        public void Dispose()
        {
            Close();
        }

        ~SqliteDatabase()
        {
            if (transScope != null)
            {
                Rollback();
            }
            if (conn != null)
            {
                Dispose();
            }

        }
        #endregion
    }











}




