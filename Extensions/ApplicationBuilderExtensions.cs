using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CabaVS.Shared.AspNetCore.EFCore.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        private const string DatabaseNamePattern = ".+Database=(.*?);.+";

        public static void RunMigrations<TContext>(this IApplicationBuilder app, bool createDbIfNotExists = false, string connectionString = null) where TContext : DbContext
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var serviceScopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            using var scope = serviceScopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetService<TContext>();
            if (context == null)
            {
                throw new ApplicationException("Unable to run migrations. Chosen DbContext is not available from Service Provider.");
            }

            if (createDbIfNotExists)
            {
                if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

                var masterConnString = ReplaceDatabaseInConnString(connectionString, "master", out var originalDatabase);
                
                using var sqlConnection = new SqlConnection(masterConnString);
                sqlConnection.Open();

                using var sqlCommand = sqlConnection.CreateCommand();
                sqlCommand.CommandText = $"CREATE DATABASE [{originalDatabase}]";
                sqlCommand.ExecuteNonQuery();

                sqlConnection.Close();
            }
            else
            {
                if (!context.Database.CanConnect())
                {
                    throw new ApplicationException("Unable to run migrations. Database is not available.");
                }
            }

            context.Database.Migrate();
        }

        private static string ReplaceDatabaseInConnString(string connectionString, string database, out string originalDatabase)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            if (database == null) throw new ArgumentNullException(nameof(database));

            originalDatabase = ExtractDatabaseName(connectionString);

            return connectionString.Replace(originalDatabase, database);
        }

        private static string ExtractDatabaseName(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

            var regex = new Regex(DatabaseNamePattern);
            var match = regex.Match(connectionString);

            return match.Success
                ? match.Groups[1].Value
                : throw new ApplicationException("Invalid connection string. Unable to find 'Database' part.");
        }
    }
}