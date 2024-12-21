using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Data.Sqlite
{

    public partial class SqliteDatabase
    {
        #region Async


        //public Task<IEnumerable<T>> QueryAsync<T>(string cmdText, params object[] parameters)
        //{
        //    var result = Task.Run<IEnumerable<T>>(() => Query<T>(cmdText, parameters));
        //    return result;
        //}

        //public Task<T> QueryFirstAsync<T>(string cmdText, params object[] parameters)
        //{
        //    var result = Task.Run<T>(() => QueryFirst<T>(cmdText, parameters));
        //    return result;
        //}

        //public Task<int> NonQueryAsync(string cmdText, params object[] parameters)
        //{
        //    var result = Task.Run<int>(() => NonQuery(cmdText, parameters));
        //    return result;
        //}

        //public Task<object> ScalarAsync(string cmdText, params object[] parameters)
        //{
        //    var result = Task.Run<object>(() => Scalar(cmdText, parameters));
        //    return result;
        //}

        //public Task<T> ScalarAsync<T>(string cmdText, params object[] parameters)
        //{
        //    var result = Task.Run<T>(() => Scalar<T>(cmdText, parameters));
        //    return result;
        //}


        #endregion

    }


}
