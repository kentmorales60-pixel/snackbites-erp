namespace SnackBites.Erp2.Migrations
{
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using SnackBites.Erp2.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            string[] roles = { "RequisitionStaff", "CoOwner1", "CoOwner2", "InventoryStaff", "MainOwner" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExists(role))
                    roleManager.Create(new IdentityRole(role));
            }

            // One test user per role. Password: Test@1234
            var users = new[]
            {
                new { Email = "staff@snackbites.com",     Role = "RequisitionStaff" },
                new { Email = "coowner1@snackbites.com",  Role = "CoOwner1" },
                new { Email = "coowner2@snackbites.com",  Role = "CoOwner2" },
                new { Email = "inventory@snackbites.com", Role = "InventoryStaff" },
                new { Email = "owner@snackbites.com",     Role = "MainOwner" }
            };

            foreach (var u in users)
            {
                var user = userManager.FindByEmail(u.Email);
                if (user == null)
                {
                    user = new ApplicationUser { UserName = u.Email, Email = u.Email, EmailConfirmed = true };
                    userManager.Create(user, "Test@1234");
                }
                if (!userManager.IsInRole(user.Id, u.Role))
                    userManager.AddToRole(user.Id, u.Role);
            }

            // Sample items: only added when the table is empty, so re-running
            // Update-Database never resets your stock numbers.
            if (!context.Items.Any())
            {
                context.Items.AddRange(new[]
                {
                    new Item { Name = "Beef Patty",    Category = "Meat",   Unit = "pcs",    QtyOnHand = 50, ReorderLevel = 20, UnitCost = 25 },
                    new Item { Name = "Burger Bun",    Category = "Bakery", Unit = "pcs",    QtyOnHand = 60, ReorderLevel = 20, UnitCost = 8 },
                    new Item { Name = "French Fries",  Category = "Frozen", Unit = "kg",     QtyOnHand = 10, ReorderLevel = 5,  UnitCost = 120 },
                    new Item { Name = "Fresh Milk",    Category = "Dairy",  Unit = "L",      QtyOnHand = 12, ReorderLevel = 6,  UnitCost = 95 },
                    new Item { Name = "Caramel Syrup", Category = "Syrup",  Unit = "bottle", QtyOnHand = 4,  ReorderLevel = 2,  UnitCost = 350 }
                });
            }

            context.SaveChanges();
        }
    }
}