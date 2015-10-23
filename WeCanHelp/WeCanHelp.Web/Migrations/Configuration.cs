using WeCanHelp.Web.Models;

namespace WeCanHelp.Web.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<WeCanHelp.Web.Models.WeCanHelpContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "WeCanHelp.Web.Models.WeCanHelpContext";
        }

        protected override void Seed(WeCanHelp.Web.Models.WeCanHelpContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //

            context.Applications.AddOrUpdate(new Application[]
                {
                    new Application() { Name = "Excel" },
                    new Application() {Name = "Word" },
                    new Application() {Name = "PowerPoint" }
                }
            );
        }
    }
}
