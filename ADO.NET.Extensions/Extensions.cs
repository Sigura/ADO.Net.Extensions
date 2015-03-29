using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Security;

namespace System.Data.SqlClient
{
    public static partial class Extensions
    {
        public static T ExecuteScalar<T>(this IDbCommand command)
        {
            return (T)command.ExecuteScalar();
        }

        static bool HasColumn(this IDataRecord sqlDataReader, string name)
        {
            try
            {
                for (var i = 0; i < sqlDataReader.FieldCount; ++i)
                {
                    if (sqlDataReader.GetName(i).Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        public static IDbConnection SetDeadlockPriority(this IDbConnection conn, int priority = 10)
        {
            using (var comand = conn.CreateCommand(string.Format(@"SET DEADLOCK_PRIORITY {0}", priority)))
                comand.ExecuteNonQuery();
            return conn;
        }

        public static SqlConnection SetDeadlockPriority(this SqlConnection conn, int priority = 10)
        {
            using (var comand = conn.CreateCommand(string.Format(@"SET DEADLOCK_PRIORITY {0}", priority)))
                comand.ExecuteNonQuery();
            return conn;
        }

        public static bool IsDBNull(this IDataRecord sqlDataReader, string name)
        {
            var ordinal2 = sqlDataReader.GetOrdinal2(name);
            return ordinal2 <= -1 || sqlDataReader.IsDBNull(ordinal2);
        }
        public static int GetOrdinal2(this IDataRecord sqlDataReader, string name)
        {
            try
            {
                if (sqlDataReader.HasColumn(name))
                    return sqlDataReader.GetOrdinal(name);
                return -1;
            }
            catch (IndexOutOfRangeException)
            {
                return -1;
            }
        }

        public static System.Guid GetGuid(this IDataRecord sqlDataReader, string name)
        {
            var index = sqlDataReader.GetOrdinal2(name);
            return index > -1 || sqlDataReader.IsDBNull(index) ? default(System.Guid) : sqlDataReader.GetGuid(index);
        }

        public static System.Boolean GetBoolean(this IDataRecord sqlDataReader, string name)
        {
            var index = sqlDataReader.GetOrdinal2(name);

            return index > -1 && !sqlDataReader.IsDBNull(index) && sqlDataReader.GetBoolean(index);
        }

        public static System.String GetString(this IDataRecord sqlDataReader, string name)
        {
            var index = sqlDataReader.GetOrdinal2(name);

            return index <= -1 || sqlDataReader.IsDBNull(index) ? null : sqlDataReader.GetString(index);
        }

        public static System.Int32 GetInt32(this IDataRecord sqlDataReader, string name)
        {
            var index = sqlDataReader.GetOrdinal2(name);

            return index <= -1 || sqlDataReader.IsDBNull(index) ? default(Int32) : sqlDataReader.GetInt32(index);
        }

        public static object GetValue(this IDataRecord sqlDataReader, string name)
        {
            var index = sqlDataReader.GetOrdinal2(name);
            
            return index > -1 ? sqlDataReader.GetValue(index) : null;
        }

        public static System.Int64 GetInt64(this IDataRecord sqlDataReader, string name)
        {
            var index = sqlDataReader.GetOrdinal2(name);

            return index <= -1 || sqlDataReader.IsDBNull(index) ? default(Int64) : sqlDataReader.GetInt64(index);
        }

        public static DateTime GetDateTime(this IDataRecord sqlDataReader, string name)
        {
            var index = sqlDataReader.GetOrdinal2(name);

            return index <= -1 || sqlDataReader.IsDBNull(index) ? default(DateTime) : sqlDataReader.GetDateTime(index);
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> array, string name = @"ID")
        {
            if (array == null) return null;

            var table = new DataTable();
            table.Columns.Add(new DataColumn(name, typeof(T)));
            table.BeginLoadData();

            foreach (var login in array)
            {
                table.Rows.Add(login);
            }

            return table;
        }

        public static T FirstOrDefault<T>(this IDbCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                var result = default(T);

                if (reader.Read())
                    result = Map<T>(reader);

                reader.Close();

                return result;
            }
        }

        internal class FieldNameComparier : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return obj.ToLowerInvariant().GetHashCode();
            }
        }
        private static T Map<T>(IDataReader reader)
            //where T : class, new()
        {
            var result = Activator.CreateInstance<T>();
            var type = result.GetType();
            var fieldCount = reader.FieldCount;
            var names = Chunk(fieldCount).Select(reader.GetName);
            var fieldNameComparier = new FieldNameComparier();

            var fields = type
                .GetFields()
                .Where(f => f.IsPublic)
                .Where(f => !f.IsStatic)
                .Where(f => names.Contains(f.Name, fieldNameComparier))
                .Count(f => SetField(result, reader, f));

            var props = type
                .GetProperties()
                .Where(f => f.CanWrite)
                .Where(f => f.CanRead)
                .Where(f => names.Contains(f.Name, fieldNameComparier))
                .Count(f => SetProperty(result, reader, f));

            return fields + props > 0 ? result : default(T);
        }

        private static IEnumerable<int> Chunk(int count)
        {
            for (var i = 0; i < count; ++i)
                yield return i;
        }

        private static bool SetProperty<T>(T result, IDataReader reader, PropertyInfo propertyInfo)
        {
            var index = reader.GetOrdinal(propertyInfo.Name);
            var isNull = reader.IsDBNull(index);
            
            if (isNull) return false;
            
            //var declaringType = propertyInfo.DeclaringType;
            //var dbType = GetDBType(declaringType);
            var value = reader.GetValue(index);

            propertyInfo.SetValue(result, value, null);

            return true;
        }

        private static bool SetField<T>(T result, IDataReader reader, FieldInfo fieldInfo)
        {
            var index = reader.GetOrdinal(fieldInfo.Name);
            var isNull = reader.IsDBNull(index);

            if (isNull) return false;

            var value = reader.GetValue(index);

            fieldInfo.SetValue(result, value);

            return true;
        }

        public static IEnumerable<T> Execute<T>(this IDbCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    yield return Map<T>(reader);

                reader.Close();
            }
        }

        public static IEnumerable<T> Execute<T>(this IDbCommand command, Func<IDataRecord, T> mapper)
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    yield return mapper(reader);

                reader.Close();
            }
        }

        public static IEnumerable<T> Execute<T>(this IDataReader reader, Func<IDataRecord, T> mapper)
        {
            while (reader.Read())
                yield return mapper(reader);

            reader.Close();
        }

        static string AddA(this string str)
        {
            return str.StartsWith(@"@") ? str : string.Format(@"@{0}", str);
        }

        public static T AddParam<T>(this T command, string name)
            where T : IDbCommand
        {
            command.Parameters.Add(new SqlParameter(name.AddA(), DBNull.Value));

            return command;
        }

        public static TT AddParam<TT, T>(this TT command, string name, int size, T value)
            where TT : IDbCommand
        {
            var param = new SqlParameter(name.AddA(), GetDBType(typeof(T)), size)
                            {Value = (object) value ?? DBNull.Value};

            command.Parameters.Add(param);

            return command;
        }

        public static TT AddParam<TT, T>(this TT command, string name)
            where TT : IDbCommand
        {
            command.Parameters.Add(new SqlParameter(name.AddA(), DBNull.Value));

            return command;
        }

        public static TT AddParam<TT, T>(this TT command, string name, T value)
            where TT : IDbCommand
        {
            var val = value as object;
            
            command.Parameters.Add(val != null
                                       ? new SqlParameter(name.AddA(), val)
                                       : new SqlParameter(name.AddA(), DBNull.Value));

            return command;
        }
        public static T AddParam<T>(this T command, string name, SqlDbType type, int size, object value, ParameterDirection? direction)
            where T : IDbCommand
        {
            var param = new SqlParameter(name.AddA(), type)
                            {
                                Value = value ?? DBNull.Value,
                                Direction = direction ?? ParameterDirection.Input
                            };

            if (size != 0)
                param.Size = size;

            command.Parameters.Add(param);

            return command;
        }

        public static T AddParam<T>(this T command, string name, DataTable array, string typeName = null)
            where T : IDbCommand
        {
            var param = new SqlParameter(name.AddA(), SqlDbType.Structured)
            {
                Value = array,
                Direction = ParameterDirection.Input,
                TypeName = typeName ?? string.Empty
            };

            command.Parameters.Add(param);

            return command;
        }

        public static T AddParam<T>(this T command, string name, IEnumerable<T> array, string fieldName = @"ID", string typeName = null)
            where T : IDbCommand
        {
            var param = new SqlParameter(name.AddA(), SqlDbType.Structured)
            {
                Value = array.ToDataTable(fieldName),
                Direction = ParameterDirection.Input,
                TypeName = typeName ?? string.Empty
            };

            command.Parameters.Add(param);

            return command;
        }

        public static T AddParam<T>(this T command, string name, SqlDbType type, object value, ParameterDirection? direction = null)
            where T : IDbCommand
        {
            var param = new SqlParameter(name.AddA(), type)
            {
                Value = value,
                Direction = direction ?? ParameterDirection.Input
            };

            command.Parameters.Add(param);

            return command;
        }
        public static T AddParam<T>(this T command, string name, SqlDbType type, int size, object value)
            where T : IDbCommand
        {
            return command.AddParam(name, type, size, value, null);
        }

        public static T AddParam<T>(this T command, string name, Type type, object value)
            where T : IDbCommand
        {
            return command.AddParam(name, GetDBType(type), 0, value);
        }

        public static T AddParam<T>(this T command, string name, SqlDbType type, int size)
            where T : IDbCommand
        {
            return command.AddParam(name, type, size, DBNull.Value);
        }

        public static T AddOutParam<T>(this T command, string name, SqlDbType type, int size, object value)
            where T : IDbCommand
        {
            return command.AddParam(name, type, size, value, ParameterDirection.InputOutput);
        }

        static SqlDbType GetDBType(Type type)
        {
            var param = new SqlParameter();
            var converter = TypeDescriptor.GetConverter(param.DbType);

            if (converter != null && converter.CanConvertFrom(type))
            {
                param.DbType = (DbType)converter.ConvertFrom(type.Name);
            }
            else if (converter != null)
            {
                // try to forcefully convert
                try
                {
                    param.DbType = (DbType)converter.ConvertFrom(type.Name);
                }
                catch (Exception)
                {
                    return DbTypes[type];
                }
            }
            return param.SqlDbType;
        }

        static readonly Dictionary<Type, SqlDbType> DbTypes = new Dictionary<Type, SqlDbType> {
            {typeof(Int64?), SqlDbType.BigInt},
            {typeof(Int64), SqlDbType.BigInt},
            {typeof(int?), SqlDbType.Int},
            {typeof(int), SqlDbType.Int},
            {typeof(string), SqlDbType.NVarChar},
            {typeof(DataTable), SqlDbType.Structured},
            {typeof(byte[]), SqlDbType.VarBinary},
        };

        static readonly Dictionary<SqlDbType, Type> NetTypes = new Dictionary<SqlDbType, Type> {
            {SqlDbType.BigInt, typeof(long)},
            {SqlDbType.TinyInt, typeof(short)},
            {SqlDbType.SmallInt, typeof(int)},
            {SqlDbType.Int, typeof(int)},
            {SqlDbType.DateTime, typeof(DateTime)},
            {SqlDbType.DateTime2, typeof(DateTime)},
            {SqlDbType.NVarChar, typeof(string)},
            {SqlDbType.VarChar, typeof(string)},
            {SqlDbType.Char, typeof(string)},
            {SqlDbType.NChar, typeof(string)},
            {SqlDbType.Text, typeof(string)},
            {SqlDbType.VarBinary, typeof(byte[])}
        };

        public static DbParameter SqlParameter<T>(string name, T value)
        {
            return new SqlParameter(name.AddA(), GetDBType(typeof(T)))
            {
                Direction = ParameterDirection.Input,
                Value = (object)value ?? DBNull.Value
            };
        }

        public static TT AddOutParam<TT, T>(this TT command, string name, T value)
            where TT : IDbCommand
        {
            return command.AddOutParam(name, GetDBType(typeof(T)), null, value);
        }

        public static TT AddOutParam<TT, T>(this TT command, string name, int? size)
            where TT : IDbCommand
        {
            return command.AddOutParam(name, GetDBType(typeof(T)), size);
        }

        public static T AddOutParam<T>(this T command, string name, SqlDbType type, int? size, object value = null)
            where T : IDbCommand
        {
            var param = size.HasValue
                    ? new SqlParameter(name.AddA(), type, size.Value)
                    : new SqlParameter(name.AddA(), type);
            param.Direction = ParameterDirection.InputOutput;
            if (value != null)
                param.Value = value;

            command.Parameters.Add(param);

            return command;
        }

        public static T AddParam<T>(this T command, SqlParameter parameter)
            where T : IDbCommand
        {
            command.Parameters.Add(parameter);

            return command;
        }

        public static IDbCommand Execute(this IDbCommand command)
        {
            command.ExecuteNonQuery();
            
            return command;
        }

        public static IDbCommand CreateSProcExecCommand(this IDbConnection connection, string name)
        {
            var command = connection.CreateCommand();

            command.CommandText = name;
            command.CommandType = CommandType.StoredProcedure;

            return command;
        }

        public static T GetSProcParams<TT, T>(this TT connection, string name)
            where T : IDbCommand
            where TT : IDbConnection
        {
            var command = connection.CreateCommand();

            command.CommandText = name;
            command.CommandType = CommandType.StoredProcedure;

            var sqlCommand = command as SqlCommand;

            if (sqlCommand != null)
                SqlCommandBuilder.DeriveParameters(sqlCommand);

            return (T)command;
        }

        public static IDbCommand CreateCommand(this IDbConnection connection, string sqlCommand)
        {
            var command = connection.CreateCommand();

            command.CommandText = sqlCommand;

            return command;
        }

        public static IDbCommand CreateCommand(this IDbConnection connection, IDbTransaction transaction, string sqlCommand)
        {
            var command = connection.CreateCommand(sqlCommand);

            command.Transaction = transaction;

            return command;
        }

        public static T OpenIt<T>(this T conn)
            where T : IDbConnection
        {
            conn.Open();
            return conn;
        }

        public static AppRoleContext SetAppRole<T>(this T conn, string appRoleName, SecureString appRolePassword)
            where T : IDbConnection
        {
            return string.IsNullOrEmpty(appRoleName) ? new AppRoleContext() : new AppRoleContext(conn, appRoleName, appRolePassword);
        }

        public static Type ToNet(this SqlDbType dbType)
        {
            return NetTypes[dbType];
        }
    }
}
