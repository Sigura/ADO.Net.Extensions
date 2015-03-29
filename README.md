# ADO.Net Extensions

For simplify access to sql server & usings

MIT licence

## features
* create connection & open in one line
* create command from connection & add params in same line
* setup app role
* setup deadlock priority

## samples

### create connection

    using(var con = new SqlConnection(connString).OpenIt())
    {
    }

### create command

    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"select top 1 getutcdate()"))
    {
    }

### create command to execute sproc

    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateSProcExecCommand(@"[sp_who2]"))
    {
    }

### add param to command

    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"select top 1 @param")
                       .AddParam(@"param", 1))
    {
    }

### add out param to command

    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"set @param = 10;")
                       .AddOutParam(@"param", 1))
    {
        command.Execute();

        var param = command.Parameters[@"param"] as SqlParameter;

    }

### add data table as param

    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"select ID from simpleTableWithOneColumnLikeID")
                       .AddParam(
                            @"simpleTableWithOneColumnLikeID",
                            new []{1,2,3,4}.ToDataTable(@"ID"),
                            @"dbo.IDColumnTableType"
                        )
         )
    {
        command.Execute();

        var param = command.Parameters[@"param"] as SqlParameter;
    }

### use approle context

    using(var con = new SqlConnection(connString).OpenIt())
    using(con.SetAppRole(@"appRole", superSicretSrting))
    {
    }

### set deadlock priority

    using(var con = new SqlConnection(connString).OpenIt())
    using(con.SetDeadlockPriority(9))
    {
    }
