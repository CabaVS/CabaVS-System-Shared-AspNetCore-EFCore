using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CabaVS.Shared.AspNetCore.EFCore.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void RunMigrations<TContext>(this IApplicationBuilder app, bool checkDatabase = true) where TContext : DbContext
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var serviceScopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            using var scope = serviceScopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetService<TContext>();
            if (context == null)
            {
                throw new ApplicationException("Unable to run migrations. Chosen DbContext is not available from Service Provider.");
            }

            if (checkDatabase && !context.Database.CanConnect())
            {
                throw new ApplicationException("Unable to run migrations. Database is not available.");
            }

            context.Database.Migrate();
        }
    }
}