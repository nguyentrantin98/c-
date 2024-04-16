using c_.Ext;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace c_.Controller
{
    public class GenericController<T> : ControllerBase where T : class
    {
        IConfiguration configuration;
        public GenericController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet("api/[Controller]/")]
        public async Task<Dictionary<string, object>[][]> Get()
        {
            var sql = $"select * from {typeof(T).Name}";
            var data = await ReadDataSet(sql);
            return data;
        }

        [HttpPut("api/[Controller]/")]
        public async Task<bool> Update([FromBody] T entity)
        {
            var pros = entity.GetType().GetProperties().Where(x => x.CanWrite).ToList();
            var sql = $"update {typeof(T).Name} set";
            var listUpdate = new List<string>();
            foreach (var item in pros)
            {
                listUpdate.Add($" {item.Name} = '{entity.GetPropValue(item.Name)}'");
            }
            sql += string.Join(",", listUpdate);
            sql += $" where Id = {entity.GetPropValue("Id")}";
            return await ExeNonQuery(sql);
        }

        [HttpPost("api/[Controller]/")]
        public async Task<bool> Create([FromBody] T entity)
        {
            var pros = entity.GetType().GetProperties().Where(x => x.CanWrite).ToList();
            var sql = $"insert into [{typeof(T).Name}]({string.Join(",", pros)})";
            var listUpdate = new List<string>();
            foreach (var item in pros)
            {
                listUpdate.Add($"N'{entity.GetPropValue(item.Name)}'");
            }
            sql += $" values ({string.Join(",", listUpdate)})";
            return await ExeNonQuery(sql);
        }

        public async Task<Dictionary<string, object>[][]> ReadDataSet(string query)
        {
            var connStr = configuration.GetConnectionString("Default");
            var con = new SqlConnection(connStr);
            var sqlCmd = new SqlCommand(query, con)
            {
                CommandType = CommandType.Text
            };
            SqlDataReader reader = null;
            var tables = new List<Dictionary<string, object>[]>();
            try
            {
                await con.OpenAsync();
                reader = await sqlCmd.ExecuteReaderAsync();
                while (true)
                {
                    var table = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        table.Add(ReadSqlRecord(reader));
                    }
                    tables.Add([.. table]);
                    var next = await reader.NextResultAsync();
                    if (!next) break;
                }
                return [.. tables];
            }
            catch (Exception e)
            {
                var message = $"{e.Message} {query}";
                return [.. tables];
            }
            finally
            {
                if (reader is not null) await reader.DisposeAsync();
                await sqlCmd.DisposeAsync();
                await con.DisposeAsync();
            }
        }

        protected async Task<bool> ExeNonQuery(string reportQuery)
        {
            var connStr = configuration.GetConnectionString("Default");
            using (SqlConnection connection = new SqlConnection(connStr))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Transaction = transaction;
                        command.Connection = connection;
                        command.CommandText = reportQuery;
                        var rs = await command.ExecuteNonQueryAsync();
                        transaction.Commit();
                        return rs > 0;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        protected static Dictionary<string, object> ReadSqlRecord(IDataRecord reader)
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader[i];
                row[reader.GetName(i)] = val == DBNull.Value ? null : val;
            }
            return row;
        }
    }
}
