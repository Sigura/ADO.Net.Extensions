# ADO.Net Extensions

For simplify access to sql server & usings

MIT licence

## features
* [create connection & open in one line](#create-connection)
* [create command from connection & add params in same line](#create-command)
* [add params in one string](#add-params-to-command)
* [add out params in one string](#add-out-param-to-command)
* [simple mapping](#create-command-to-execute-sproc-with-simple-mapping)
* [setup app role](#use-approle-context)
* [setup deadlock priority](#set-deadlock-priority)

## Samples

### create connection

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    {
        Console.WriteLine(@"All is ok {0}", con.State);
    }
```

### create command

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"select top 1 getutcdate()"))
    {
        var utcDateTime = cmd.ExecuteScalar<DateTime>();

        Console.WriteLine(@"Scalar {0}", utcDateTime);
    }
```

### create command to execute sproc

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateSProcExecCommand(@"[sp_who2]"))
    using(var res = cmd.ExecuteReader())
    {
        while (res.Read())
        {
            Console.WriteLine(@"SPID: {0}, Login: {1}", res.GetString(@"SPID").Trim(), res.GetString(@"Login"));
        }
    }
```

### create command to execute sproc with simple mapping

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateSProcExecCommand(@"[sp_who2]"))
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
```

### add params to command

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"select top 1 @param")
                       .AddParam(@"param", 1))
    {
        var param = cmd.ExecuteScalar<int>();

        Console.WriteLine(@"Param added {0}", param);
    }
```

### add out param to command

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"set @param = 10;")
                       .AddOutParam(@"param", 1))
    {
        cmd.Execute();

        var param = cmd.Parameters["@param"] as SqlParameter;

        Console.WriteLine(@"out param {0}", param.Value);
    }
```

### add data table as param with mapping for simple result
_do not use that map way in production_

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(var cmd = con.CreateCommand(@"select ID from simpleTableWithOneColumnLikeID")
                       .AddParam(
                            @"simpleTableWithOneColumnLikeID",
                            new []{1,2,3,4}.ToDataTable(@"ID"),
                            @"dbo.IDColumnTableType"
                        )
         )
    {
        var rows = cmd.Execute<DataRow>();

        foreach (var row in rows)
        {
            Console.WriteLine(@"id: {0}", row.ID);
        }
    }
```

### use approle context

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(con.SetAppRole(@"appRole", superSicretSrting))
    {
    }
```

### set deadlock priority

```c#
    using(var con = new SqlConnection(connString).OpenIt())
    using(con.SetDeadlockPriority(9))
    {
    }
```