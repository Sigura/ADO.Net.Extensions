using System.Runtime.InteropServices;
using System.Security;

namespace System.Data.SqlClient
{
    public class AppRoleContext : IDisposable
    {
        const string CookieParamName = @"@cookie";

        private readonly IDbConnection _conn;
        private readonly byte[] _cookie;
        private bool _disposed;
        private readonly object _syncObject = new object();

        public AppRoleContext(IDbConnection conn, string appRoleName, SecureString appRolePassword)
        {
            _conn = conn;
            using (var command = conn.CreateSProcExecCommand(@"sys.sp_SetAppRole")
                .AddParam(@"rolename", appRoleName)
                .AddParam(@"@encrypt", @"none")
                .AddParam(@"fCreateCookie", true)
                .AddOutParam(CookieParamName, SqlDbType.VarBinary, 50)
                .AddParam(@"password", Marshal.PtrToStringBSTR(Marshal.SecureStringToBSTR(appRolePassword))))
            {
                command.ExecuteNonQuery();

                var cookieParam = command.Parameters[CookieParamName] as SqlParameter;
                _cookie = cookieParam != null ? cookieParam.Value as byte[] : null;

                if (_cookie == null)
                    throw new InvalidOperationException(@"app role context cookie is null");
            }
        }

        public AppRoleContext(IDbConnection conn, byte[] cookie)
        {
            _conn = conn;
            _cookie = cookie;
        }

        public AppRoleContext()
        {
        }

        public void Dispose()
        {
            lock (_syncObject)
            {
                if (_disposed || _conn == null || _cookie == null)
                    return;

                _disposed = true;

                using (var command = _conn.CreateSProcExecCommand(@"sys.sp_UnsetAppRole")
                    .AddParam(CookieParamName, SqlDbType.VarBinary, 50, _cookie))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}