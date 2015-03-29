using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security;

namespace Samples
{
    class Program
    {
        private const string ConnString = @"Data Source=.;Initial Catalog=test-db;Integrated Security=True;";

        /// <summary>
        /// CREATE DATABASE [test-db];
        /// CREATE TYPE dbo.SimpleTableWithOneColumnLikeID AS TABLE 
        /// (
        /// 	ID int not null
        ///     PRIMARY KEY(ID)
        /// )
        /// </summary>
        static void Main()
        {
            using (var con = new SqlConnection(ConnString).OpenIt())
            {
                Console.WriteLine(@"All is ok {0}", con.State);
            }

            using (var con = new SqlConnection(ConnString).OpenIt())
            using (var cmd = con.CreateCommand(@"select top 1 getutcdate()"))
            {
                var utcDateTime = cmd.ExecuteScalar<DateTime>();

                Console.WriteLine(@"Scalar {0}", utcDateTime);
            }

            using (var con = new SqlConnection(ConnString).OpenIt())
            using (var cmd = con.CreateSProcExecCommand(@"[sp_who2]"))
            using (var res = cmd.ExecuteReader())
            {
                while (res.Read())
                {
                    Console.WriteLine(@"SPID: {0}, Login: {1}", res.GetString(@"SPID").Trim(), res.GetString(@"Login"));
                }
            }

            using (var con = new SqlConnection(ConnString).OpenIt())
            using (var cmd = con.CreateSProcExecCommand(@"[sp_who2]"))
            {
                var rows = cmd.Execute(x => new DataRow
                    {
                        ID = Convert.ToInt32(x.GetString(@"SPID").Trim()),
                        Description = x.GetString(@"Login").Trim()
                    })
                    .Reverse()
                    .Take(10);
                foreach (var row in rows)
                {
                    Console.WriteLine(@"SPID: {0}, Login: {1}", row.ID, row.Description);
                }
            }

            using (var con = new SqlConnection(ConnString).OpenIt())
            using (var cmd = con.CreateCommand(@"select top 1 @param")
                               .AddParam(@"param", 1))
            {
                var param = cmd.ExecuteScalar<int>();

                Console.WriteLine(@"Param added {0}", param);
            }

            using (var con = new SqlConnection(ConnString).OpenIt())
            using (var cmd = con.CreateCommand(@"set @param = 10;")
                               .AddOutParam("param", 1))
            {
                cmd.Execute();

                var param = cmd.Parameters["@param"] as SqlParameter;

                Console.WriteLine(@"out param {0}", param.Value);
            }

            using (var con = new SqlConnection(ConnString).OpenIt())
            using (var cmd = con.CreateCommand(@"select id from @simpleTableWithOneColumnLikeID")
                               .AddParam(
                                    @"simpleTableWithOneColumnLikeID",
                                    new[] { 1, 2, 3, 4 }.ToDataTable(@"id"),
                                    @"dbo.SimpleTableWithOneColumnLikeID"
                                )
                 )
            {
                var rows = cmd.Execute<DataRow>();

                foreach (var row in rows)
                {
                    Console.WriteLine(@"id: {0}", row.ID);
                }
            }

            using (var con = new SqlConnection(ConnString).OpenIt())
            using (con.SetDeadlockPriority(9))
            {
            }

            var superSicretSrting = new SecureString();

            /// CREATE APPLICATION ROLE appRole WITH PASSWORD = ''
            using (var con = new SqlConnection(ConnString).OpenIt())
            using (con.SetAppRole(@"appRole", superSicretSrting))
            {
                Console.WriteLine(@"All is ok {0}", con.State);
            }

        }


        public class DataRow
        {
            public int ID;
            public string Description;
        }
    }
}
