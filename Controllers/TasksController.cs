using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegacyTaskManager.Api.Data;
using LegacyTaskManager.Api.Models;

namespace LegacyTaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private const int MinPriority = 1;
        private const int MaxPriority = 3;
        private const int DefaultPriority = 2;
        private const int MaxActiveTasksPerNormalUser = 5;

        private readonly AppDbContext db;
        private readonly ILogger<TasksController> logger;

        public TasksController(AppDbContext context, ILogger<TasksController> logger)
        {
            db = context;
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult GetTasks()
        {
            var tasks = db.Tasks.ToList();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public IActionResult GetTaskById(int id)
        {
            var task = FindTaskById(id);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }

        [HttpPost]
        public IActionResult AddNewTask([FromBody] TaskItem t)
        {
            if (t == null)
            {
                return BadRequest("no data");
            }
            if (t.Title == null || t.Title == "")
            {
                return BadRequest("title required");
            }
            if (t.Priority < MinPriority || t.Priority > MaxPriority)
            {
                t.Priority = DefaultPriority;
            }
            t.Status = TaskStatuses.Pending;
            t.IsCompleted = false;
            if (t.CreatedBy == null || t.CreatedBy.Length == 0)
            {
                t.CreatedBy = "system";
            }
            db.Tasks.Add(t);
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save new task");
            }
            return Ok(t);
        }

        [HttpPut("{id}")]
        public IActionResult EditTask(int id, [FromBody] TaskItem updated)
        {
            var existing = FindTaskById(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.Priority = updated.Priority;
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save edited task {TaskId}", id);
            }
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult RemoveTask(int id)
        {
            var existing = FindTaskById(id);
            if (existing == null)
            {
                return NotFound();
            }
            db.Tasks.Remove(existing);
            db.SaveChanges();
            return Ok();
        }

        [HttpPost("{id}/assign/{userId}")]
        public IActionResult AssignTask(int id, int userId)
        {
            var task = FindTaskById(id);
            if (task == null)
            {
                return NotFound("task not found");
            }

            var user = FindUserById(userId);
            if (user == null)
            {
                return NotFound("user not found");
            }

            if (task.IsCompleted)
            {
                return BadRequest("cant assign a completed task");
            }

            if (user.userType == UserTypes.Admin)
            {
                task.AssignedUserId = userId;
                task.Status = TaskStatuses.Pending;
                db.SaveChanges();
                return Ok(task);
            }

            if (user.userType == UserTypes.Normal)
            {
                if (CountActiveTasksForUser(userId) >= MaxActiveTasksPerNormalUser)
                {
                    return BadRequest("user has too many tasks");
                }

                task.AssignedUserId = userId;
                task.Status = TaskStatuses.Pending;
                db.SaveChanges();
                return Ok(task);
            }

            return BadRequest("unknown user type");
        }

        [HttpPost("{id}/complete")]
        public IActionResult CompleteTask(int id)
        {
            var task = FindTaskById(id);
            if (task == null)
            {
                return NotFound();
            }
            task.IsCompleted = true;
            task.Status = TaskStatuses.Completed;
            db.SaveChanges();
            return Ok(task);
        }

        [HttpGet("byuser/{userId}")]
        public IActionResult GetTasksForUser(int userId)
        {
            var result = new List<TaskItem>();
            var all = db.Tasks.ToList();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].AssignedUserId == userId)
                {
                    result.Add(all[i]);
                }
            }
            return Ok(result);
        }

        private TaskItem? FindTaskById(int id) => db.Tasks.FirstOrDefault(t => t.Id == id);

        private User? FindUserById(int userId) => db.Users.FirstOrDefault(u => u.Id == userId);

        private int CountActiveTasksForUser(int userId)
        {
            var count = 0;
            var allTasks = db.Tasks.ToList();
            for (int i = 0; i < allTasks.Count; i++)
            {
                if (allTasks[i].AssignedUserId == userId && allTasks[i].IsCompleted == false)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
