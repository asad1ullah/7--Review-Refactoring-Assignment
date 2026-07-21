using LegacyTaskManager.Api.Controllers;
using LegacyTaskManager.Api.Data;
using LegacyTaskManager.Api.Models;
using LegacyTaskManager.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegacyTaskManager.Api.Tests.Controllers
{
    public class UsersControllerTests
    {
        private static UsersController CreateController(AppDbContext db) =>
            new UsersController(db, new Mock<ILogger<UsersController>>().Object);

        // --- GetAllUsers ---

        [Fact]
        public void GetAllUsers_ReturnsOkWithAllUsersInDatabase()
        {
            using var db = TestDbContextFactory.Create();
            db.Users.Add(new User { Id = 8001, Name = "Alice", Email = "alice@x.com" });
            db.Users.Add(new User { Id = 8002, Name = "Bob", Email = "bob@x.com" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.GetAllUsers();

            var ok = Assert.IsType<OkObjectResult>(result);
            var users = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value);
            Assert.Contains(users, u => u.Id == 8001 && u.Name == "Alice");
            Assert.Contains(users, u => u.Id == 8002 && u.Name == "Bob");
        }

        // --- GetUserById ---

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetUserById_NonPositiveId_ReturnsBadRequest(int id)
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.GetUserById(id);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("bad id", badRequest.Value);
        }

        [Fact]
        public void GetUserById_ExistingId_ReturnsOkWithThatUser()
        {
            using var db = TestDbContextFactory.Create();
            db.Users.Add(new User { Id = 8010, Name = "Findable", Email = "findable@x.com" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.GetUserById(8010);

            var ok = Assert.IsType<OkObjectResult>(result);
            var user = Assert.IsType<User>(ok.Value);
            Assert.Equal("Findable", user.Name);
        }

        [Fact]
        public void GetUserById_MissingId_ReturnsNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.GetUserById(999999);

            Assert.IsType<NotFoundResult>(result);
        }

        // --- CreateUser ---

        [Fact]
        public void CreateUser_NullBody_ReturnsBadRequestWithNoDataMessage()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.CreateUser(null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("no data", badRequest.Value);
        }

        [Fact]
        public void CreateUser_MissingName_ReturnsBadRequestWithNameRequiredMessage()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var user = new User { Name = "", Email = "a@x.com" };

            var result = controller.CreateUser(user);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("name required", badRequest.Value);
        }

        [Fact]
        public void CreateUser_NameTooShort_ReturnsBadRequestWithNameTooShortMessage()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var user = new User { Name = "A", Email = "a@x.com" };

            var result = controller.CreateUser(user);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("name too short", badRequest.Value);
        }

        [Fact]
        public void CreateUser_MissingEmail_ReturnsBadRequestWithEmailRequiredMessage()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var user = new User { Name = "Valid Name", Email = "" };

            var result = controller.CreateUser(user);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("email required", badRequest.Value);
        }

        [Fact]
        public void CreateUser_EmailWithoutAtSymbol_ReturnsBadRequestWithEmailNeedsAtMessage()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var user = new User { Name = "Valid Name", Email = "not-an-email" };

            var result = controller.CreateUser(user);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("email needs @", badRequest.Value);
        }

        [Fact]
        public void CreateUser_DuplicateEmailCaseInsensitive_ReturnsBadRequestWithEmailAlreadyUsedMessage()
        {
            using var db = TestDbContextFactory.Create();
            db.Users.Add(new User { Id = 8020, Name = "Existing", Email = "Taken@Example.com" });
            db.SaveChanges();
            var controller = CreateController(db);
            var user = new User { Name = "New Person", Email = "taken@example.com" };

            var result = controller.CreateUser(user);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("email already used", badRequest.Value);
        }

        [Fact]
        public void CreateUser_ValidUserWithoutUserType_DefaultsToNormal()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var user = new User { Name = "New Person", Email = "new@x.com", userType = "" };

            var result = controller.CreateUser(user);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<User>(ok.Value);
            Assert.Equal("Normal", saved.userType);
            Assert.Contains(db.Users, u => u.Email == "new@x.com" && u.userType == "Normal");
        }

        [Fact]
        public void CreateUser_ValidUserWithUserTypeProvided_KeepsProvidedType()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var user = new User { Name = "New Admin", Email = "newadmin@x.com", userType = "Admin" };

            var result = controller.CreateUser(user);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<User>(ok.Value);
            Assert.Equal("Admin", saved.userType);
        }

        // --- UpdateUser ---

        [Fact]
        public void UpdateUser_ExistingId_UpdatesAllEditableFields()
        {
            using var db = TestDbContextFactory.Create();
            db.Users.Add(new User { Id = 8030, Name = "Old", Email = "old@x.com", Phone = "000", userType = "Normal" });
            db.SaveChanges();
            var controller = CreateController(db);
            var updated = new User { Name = "New", Email = "new@x.com", Phone = "111", userType = "Admin" };

            var result = controller.UpdateUser(8030, updated);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<User>(ok.Value);
            Assert.Equal("New", saved.Name);
            Assert.Equal("new@x.com", saved.Email);
            Assert.Equal("111", saved.Phone);
            Assert.Equal("Admin", saved.userType);
        }

        [Fact]
        public void UpdateUser_MissingId_ReturnsNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.UpdateUser(999999, new User { Name = "X", Email = "x@x.com" });

            Assert.IsType<NotFoundResult>(result);
        }

        // --- DeleteUser ---

        [Fact]
        public void DeleteUser_ExistingId_RemovesUserAndReturnsOk()
        {
            using var db = TestDbContextFactory.Create();
            db.Users.Add(new User { Id = 8040, Name = "To delete", Email = "delete@x.com" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.DeleteUser(8040);

            Assert.IsType<OkResult>(result);
            Assert.DoesNotContain(db.Users, u => u.Id == 8040);
        }

        [Fact]
        public void DeleteUser_MissingId_ReturnsNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.DeleteUser(999999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
