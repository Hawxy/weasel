using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

namespace Weasel.Postgresql.Functions
{
    /// <summary>
    /// Base class for an ISchemaObject manager for a Postgresql function
    /// </summary>
    public abstract class OldFunction: ISchemaObject
    {
        public DbObjectName Identifier { get; }
        public bool IsRemoved { get; }

        protected OldFunction(DbObjectName identifier, bool isRemoved = false)
        {
            Identifier = identifier;
            IsRemoved = isRemoved;
        }

        /// <summary>
        /// Override to write the actual DDL code
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="writer"></param>
        public abstract void WriteCreateStatement(DdlRules rules, TextWriter writer);

        public void ConfigureQueryCommand(CommandBuilder builder)
        {
            var schemaParam = builder.AddParameter(Identifier.Schema).ParameterName;
            var nameParam = builder.AddParameter(Identifier.Name).ParameterName;

            builder.Append($@"
SELECT pg_get_functiondef(pg_proc.oid)
FROM pg_proc JOIN pg_namespace as ns ON pg_proc.pronamespace = ns.oid WHERE ns.nspname = :{schemaParam} and proname = :{nameParam};

SELECT format('DROP FUNCTION IF EXISTS %s.%s(%s);'
             ,n.nspname
             ,p.proname
             ,pg_get_function_identity_arguments(p.oid))
FROM   pg_proc p
LEFT JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
WHERE  p.proname = :{nameParam}
AND    n.nspname = :{schemaParam};
");
        }

        public Task<ISchemaObjectDelta> CreateDelta(DbDataReader reader)
        {
            throw new System.NotImplementedException();
        }

        public async Task<SchemaPatchDifference> CreatePatch(DbDataReader reader, SchemaPatch patch, AutoCreate autoCreate)
        {
            var diff = await fetchDelta(reader, patch.Rules);

            if (diff == null && IsRemoved)
            {
                return SchemaPatchDifference.None;
            }

            if (diff == null)
            {
                WriteCreateStatement(patch.Rules, patch.UpWriter);
                WriteDropStatement(patch.Rules, patch.DownWriter);

                return SchemaPatchDifference.Create;
            }

            // if (diff.Removed)
            // {
            //     WriteCreateStatement(patch.Rules, patch.UpWriter);
            //     return SchemaPatchDifference.Update;
            // }
            //
            // if (diff.AllNew)
            // {
            //     WriteCreateStatement(patch.Rules, patch.UpWriter);
            //     WriteDropStatement(patch.Rules, patch.DownWriter);
            //
            //     return SchemaPatchDifference.Create;
            // }
            //
            // if (diff.HasChanged)
            // {
            //     diff.WritePatch(patch);
            //
            //     return SchemaPatchDifference.Update;
            // }

            return SchemaPatchDifference.None;
        }

        public IEnumerable<DbObjectName> AllNames()
        {
            yield return Identifier;
        }

        protected async Task<FunctionDelta> fetchDelta(DbDataReader reader, DdlRules rules)
        {
            if (!await reader.ReadAsync())
            {
                await reader.NextResultAsync();
                return null;
            }

            var existingFunction = await reader.GetFieldValueAsync<string>(0);

            var expectedBody = IsRemoved ? null : ToBody(rules);
            
            if (string.IsNullOrEmpty(existingFunction))
            {
                throw new NotImplementedException();
                //return new FunctionDelta(expectedBody, null);
            }

            await reader.NextResultAsync();
            var drops = new List<string>();
            while (await reader.ReadAsync())
            {
                drops.Add(await reader.GetFieldValueAsync<string>(0));
            }

            var actualBody = new FunctionBody(Identifier, drops.ToArray(), existingFunction.TrimEnd() + ";");


            throw new NotImplementedException();
            //return new FunctionDelta(expectedBody, actualBody);
        }

        public FunctionBody ToBody(DdlRules rules)
        {
            var dropSql = toDropSql();

            var writer = new StringWriter();
            WriteCreateStatement(rules, writer);

            return new FunctionBody(Identifier, new string[] { dropSql }, writer.ToString());
        }

        /// <summary>
        /// Override to customize the DROP statements for this function
        /// </summary>
        /// <returns></returns>
        protected abstract string toDropSql();

        public void WriteDropStatement(DdlRules rules, TextWriter writer)
        {
            var dropSql = toDropSql();
            writer.WriteLine(dropSql);
        }

        public async Task<FunctionDelta> FetchDelta(NpgsqlConnection conn, DdlRules rules)
        {
            var cmd = conn.CreateCommand();
            var builder = new CommandBuilder(cmd);

            ConfigureQueryCommand(builder);

            cmd.CommandText = builder.ToString();

            using var reader = await cmd.ExecuteReaderAsync();
            return await fetchDelta(reader, rules);
        }
    }
}