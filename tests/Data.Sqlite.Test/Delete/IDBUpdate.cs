
//using System.Collections;
//using System;
//using LWJ;
//using LWJ.Data.Sqlite;

//namespace LWJ.Data
//{


//    /// <summary>
//    /// 数据更新接口
//    /// </summary>
//    public interface IUpdater : IRoutine<int>
//    {

//        int UpdateTotal { get; }

//        /// <summary>
//        /// 更新错误信息
//        /// </summary>
//        Exception UpdateError { get; }

//        /// <summary>
//        /// 是否需要更新
//        /// </summary>
//        /// <param name="db"></param>
//        /// <returns></returns>
//        bool CanUpdate(SqliteDatabase db, string version);


//        /// <summary>
//        /// 更新数据
//        /// </summary>
//        /// <returns></returns>
//        IEnumerator StartUpdate(SqliteDatabase db, string version);


//        /// <summary>
//        /// 清理数据
//        /// </summary>
//        /// <returns></returns>
//        //IEnumerator DBClear(SqliteDatabase db);




//    }

//}