using Mono.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Data.Sqlite
{
    public static class SqliteDatabaseExtensions
    {
        public static IEnumerable<IEnumerable<KeyValuePair<string, object>>> ToRows(this SqliteDataReader reader)
        {
            while (reader.Read())
            {
                yield return ToRow(reader);
            }
        }

        private static IEnumerable<KeyValuePair<string, object>> ToRow(this SqliteDataReader reader)
        {
            int fieldCount;
            fieldCount = reader.FieldCount;
            for (int i = 0; i < fieldCount; i++)
            {
                yield return new KeyValuePair<string, object>(reader.GetName(i), reader.GetValue(i));
            }
        }


        //public static Result<bool> ReadAsync(this SqliteDataReader reader)
        //{
        //    Result<bool> result = new Result<bool>();
        //    result.SetRoutine(ToNonBlocking(reader, result));
        //    return result;
        //}

        //static IEnumerator ToNonBlocking(SqliteDataReader reader, Result<bool> result)
        //{
        //    yield return null;
        //    try
        //    {
        //        result.Data = reader.Read();
        //    }
        //    catch (Exception ex)
        //    {
        //        result.Error = ex.Message;
        //    }
        //}

    }

    class SqliteDataReaderDictionary : Dictionary<string, object>
    {

        public SqliteDataReaderDictionary(SqliteDataReader reader)
        {
            int fieldCount = reader.FieldCount;
            object[] values = new object[fieldCount];
            reader.GetValues(values);
            string fieldName;
            for (int i = 0; i < fieldCount; i++)
            {
                fieldName = reader.GetName(i);
                this[fieldName] = values[i];
            }
        }



    }

}
