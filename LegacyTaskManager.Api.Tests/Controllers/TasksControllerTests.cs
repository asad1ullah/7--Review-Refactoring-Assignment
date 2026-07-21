using LegacyTaskManager.Api.Controllers;
using LegacyTaskManager.Api.Data;
using LegacyTaskManager.Api.Models;
using LegacyTaskManager.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegacyTaskManager.Api.Tests.Controllers
{
    public class TasksControllerTests
    {
        private static TasksController CreateController(AppDbContext db) =>
            new TasksController(db, new Mock<ILogger<TasksController>>().Object);

        // --- GetTasks ---

        [Fact]
        public void GetTasks_ReturnsOkWithAllTasksInDatabase()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9001, Title = "Task A" });
            db.Tasks.Add(new TaskItem { Id = 9002, Title = "Task B" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.GetTasks();

            var ok = Assert.IsType<OkObjectResult>(result);
            var tasks = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(ok.Value);
            Assert.Contains(tasks, t => t.Id == 9001 && t.Title == "Task A");
            Assert.Contains(tasks, t => t.Id == 9002 && t.Title == "Task B");
        }

        // --- GetTaskById ---

        [Fact]
        public void GetTaskById_ExistingId_ReturnsOkWithThatTask()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9010, Title = "Findable" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.GetTaskById(9010);

            var ok = Assert.IsType<OkObjectResult>(result);
            var task = Assert.IsType<TaskItem>(ok.Value);
            Assert.Equal("Findable", task.Title);
        }

        [Fact]
        public void GetTaskById_MissingId_ReturnsNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.GetTaskById(999999);

            Assert.IsType<NotFoundResult>(result);
        }

        // --- AddNewTask ---

        [Fact]
        public void AddNewTask_NullBody_ReturnsBadRequestWithNoDataMessage()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.AddNewTask(null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("no data", badRequest.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddNewTask_MissingTitle_ReturnsBadRequestWithTitleRequiredMessage(string? title)
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var task = new TaskItem { Title = title! };

            var result = controller.AddNewTask(task);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("title required", badRequest.Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void AddNewTask_PriorityOutOfRange_DefaultsToMedium(int invalidPriority)
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var task = new TaskItem { Title = "Some task", Priority = invalidPriority };

            var result = controller.AddNewTask(task);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<TaskItem>(ok.Value);
            Assert.Equal(2, saved.Priority);
        }

        [Fact]
        public void AddNewTask_ValidTaskWithoutCreatedBy_SetsPendingStatusAndDefaultsCreatedByToSystem()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var task = new TaskItem { Title = "New task", Priority = 3, CreatedBy = "" };

            var result = controller.AddNewTask(task);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<TaskItem>(ok.Value);
            Assert.Equal("Pending", saved.Status);
            Assert.False(saved.IsCompleted);
            Assert.Equal("system", saved.CreatedBy);
            Assert.Equal(3, saved.Priority);
            Assert.Contains(db.Tasks, t => t.Title == "New task" && t.Status == "Pending");
        }

        [Fact]
        public void AddNewTask_CreatedByProvided_KeepsProvidedValue()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);
            var task = new TaskItem { Title = "New task", CreatedBy = "alice" };

            var result = controller.AddNewTask(task);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<TaskItem>(ok.Value);
            Assert.Equal("alice", saved.CreatedBy);
        }

        // --- EditTask ---

        [Fact]
        public void EditTask_ExistingId_UpdatesTitleDescriptionAndPriority()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9020, Title = "Old", Description = "Old desc", Priority = 1 });
            db.SaveChanges();
            var controller = CreateController(db);
            var updated = new TaskItem { Title = "New", Description = "New desc", Priority = 3 };

            var result = controller.EditTask(9020, updated);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<TaskItem>(ok.Value);
            Assert.Equal("New", saved.Title);
            Assert.Equal("New desc", saved.Description);
            Assert.Equal(3, saved.Priority);
        }

        [Fact]
        public void EditTask_MissingId_ReturnsNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.EditTask(999999, new TaskItem { Title = "X" });

            Assert.IsType<NotFoundResult>(result);
        }

        // --- RemoveTask ---

        [Fact]
        public void RemoveTask_ExistingId_RemovesTaskAndReturnsOk()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9030, Title = "To delete" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.RemoveTask(9030);

            Assert.IsType<OkResult>(result);
            Assert.DoesNotContain(db.Tasks, t => t.Id == 9030);
        }

        [Fact]
        public void RemoveTask_MissingId_ReturnsNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.RemoveTask(999999);

            Assert.IsType<NotFoundResult>(result);
        }

        // --- AssignTask ---

        [Fact]
        public void AssignTask_TaskNotFound_ReturnsNotFoundWithMessage()
        {
            using var db = TestDbContextFactory.Create();
            db.Users.Add(new User { Id = 9040, Name = "U", Email = "u@x.com", userType = "Admin" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(999999, 9040);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("task not found", notFound.Value);
        }

        [Fact]
        public void AssignTask_UserNotFound_ReturnsNotFoundWithMessage()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9041, Title = "T", IsCompleted = false });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(9041, 999999);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("user not found", notFound.Value);
        }

        [Fact]
        public void AssignTask_TaskAlreadyCompleted_ReturnsBadRequest()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9042, Title = "T", IsCompleted = true });
            db.Users.Add(new User { Id = 9042, Name = "U", Email = "u@x.com", userType = "Admin" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(9042, 9042);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("cant assign a completed task", badRequest.Value);
        }

        [Fact]
        public void AssignTask_AdminUser_AssignsEvenWhenAlreadyOverloaded()
        {
            using var db = TestDbContextFactory.Create();
            var admin = new User { Id = 9043, Name = "Admin", Email = "admin@x.com", userType = "Admin" };
            db.Users.Add(admin);
            for (int i = 0; i < 5; i++)
            {
                db.Tasks.Add(new TaskItem { Id = 9100 + i, Title = $"Existing {i}", AssignedUserId = admin.Id, IsCompleted = false });
            }
            var newTask = new TaskItem { Id = 9044, Title = "New", IsCompleted = false };
            db.Tasks.Add(newTask);
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(newTask.Id, admin.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<TaskItem>(ok.Value);
            Assert.Equal(admin.Id, saved.AssignedUserId);
            Assert.Equal("Pending", saved.Status);
        }

        [Fact]
        public void AssignTask_NormalUserWithFourActiveTasks_AssignsTheFifth()
        {
            using var db = TestDbContextFactory.Create();
            var normal = new User { Id = 9045, Name = "Normal", Email = "normal@x.com", userType = "Normal" };
            db.Users.Add(normal);
            for (int i = 0; i < 4; i++)
            {
                db.Tasks.Add(new TaskItem { Id = 9200 + i, Title = $"Existing {i}", AssignedUserId = normal.Id, IsCompleted = false });
            }
            var newTask = new TaskItem { Id = 9046, Title = "New", IsCompleted = false };
            db.Tasks.Add(newTask);
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(newTask.Id, normal.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<TaskItem>(ok.Value);
            Assert.Equal(normal.Id, saved.AssignedUserId);
        }

        [Fact]
        public void AssignTask_NormalUserWithFiveActiveTasks_ReturnsBadRequest()
        {
            using var db = TestDbContextFactory.Create();
            var normal = new User { Id = 9047, Name = "Normal", Email = "normal2@x.com", userType = "Normal" };
            db.Users.Add(normal);
            for (int i = 0; i < 5; i++)
            {
                db.Tasks.Add(new TaskItem { Id = 9300 + i, Title = $"Existing {i}", AssignedUserId = normal.Id, IsCompleted = false });
            }
            var newTask = new TaskItem { Id = 9048, Title = "New", IsCompleted = false };
            db.Tasks.Add(newTask);
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(newTask.Id, normal.Id);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("user has too many tasks", badRequest.Value);
        }

        [Fact]
        public void AssignTask_CompletedExistingTasksDoNotCountTowardTheLimit()
        {
            using var db = TestDbContextFactory.Create();
            var normal = new User { Id = 9049, Name = "Normal", Email = "normal3@x.com", userType = "Normal" };
            db.Users.Add(normal);
            for (int i = 0; i < 5; i++)
            {
                db.Tasks.Add(new TaskItem { Id = 9400 + i, Title = $"Done {i}", AssignedUserId = normal.Id, IsCompleted = true });
            }
            var newTask = new TaskItem { Id = 9050, Title = "New", IsCompleted = false };
            db.Tasks.Add(newTask);
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(newTask.Id, normal.Id);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void AssignTask_UnknownUserType_ReturnsBadRequest()
        {
            using var db = TestDbContextFactory.Create();
            var weirdUser = new User { Id = 9051, Name = "Weird", Email = "weird@x.com", userType = "SuperAdmin" };
            db.Users.Add(weirdUser);
            db.Tasks.Add(new TaskItem { Id = 9052, Title = "T", IsCompleted = false });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.AssignTask(9052, weirdUser.Id);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("unknown user type", badRequest.Value);
        }

        // --- CompleteTask ---

        [Fact]
        public void CompleteTask_ExistingId_MarksCompletedAndSetsStatus()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9060, Title = "T", IsCompleted = false, Status = "Pending" });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.CompleteTask(9060);

            var ok = Assert.IsType<OkObjectResult>(result);
            var saved = Assert.IsType<TaskItem>(ok.Value);
            Assert.True(saved.IsCompleted);
            Assert.Equal("Completed", saved.Status);
        }

        [Fact]
        public void CompleteTask_MissingId_ReturnsNotFound()
        {
            using var db = TestDbContextFactory.Create();
            var controller = CreateController(db);

            var result = controller.CompleteTask(999999);

            Assert.IsType<NotFoundResult>(result);
        }

        // --- GetTasksForUser ---

        [Fact]
        public void GetTasksForUser_ReturnsOnlyTasksAssignedToThatUser()
        {
            using var db = TestDbContextFactory.Create();
            db.Tasks.Add(new TaskItem { Id = 9070, Title = "Mine 1", AssignedUserId = 9070 });
            db.Tasks.Add(new TaskItem { Id = 9071, Title = "Mine 2", AssignedUserId = 9070 });
            db.Tasks.Add(new TaskItem { Id = 9072, Title = "Someone else's", AssignedUserId = 9999 });
            db.SaveChanges();
            var controller = CreateController(db);

            var result = controller.GetTasksForUser(9070);

            var ok = Assert.IsType<OkObjectResult>(result);
            var tasks = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(ok.Value).ToList();
            Assert.Equal(2, tasks.Count);
            Assert.All(tasks, t => Assert.Equal(9070, t.AssignedUserId));
        }
    }
}
