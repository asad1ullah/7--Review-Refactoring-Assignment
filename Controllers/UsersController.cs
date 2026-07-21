using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegacyTaskManager.Api.Data;
using LegacyTaskManager.Api.Models;

namespace LegacyTaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext db;
        private readonly ILogger<UsersController> logger;

        public UsersController(AppDbContext context, ILogger<UsersController> logger)
        {
            db = context;
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = db.Users.ToList();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            if (id > 0)
            {
                var u = FindUserById(id);
                if (u != null)
                {
                    return Ok(u);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest("bad id");
            }
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] User newUser)
        {
            if (newUser != null)
            {
                if (!string.IsNullOrEmpty(newUser.Name))
                {
                    if (newUser.Name.Length > 1)
                    {
                        if (!string.IsNullOrEmpty(newUser.Email))
                        {
                            if (newUser.Email.Contains("@"))
                            {
                                var all = db.Users.ToList();
                                var dupe = false;
                                for (int i = 0; i < all.Count; i++)
                                {
                                    if (all[i].Email.ToLower() == newUser.Email.ToLower())
                                    {
                                        dupe = true;
                                    }
                                }
                                if (dupe == false)
                                {
                                    if (newUser.userType == null || newUser.userType == "")
                                    {
                                        newUser.userType = UserTypes.Normal;
                                    }
                                    db.Users.Add(newUser);
                                    try
                                    {
                                        db.SaveChanges();
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError(ex, "Failed to save new user");
                                    }
                                    return Ok(newUser);
                                }
                                else
                                {
                                    return BadRequest("email already used");
                                }
                            }
                            else
                            {
                                return BadRequest("email needs @");
                            }
                        }
                        else
                        {
                            return BadRequest("email required");
                        }
                    }
                    else
                    {
                        return BadRequest("name too short");
                    }
                }
                else
                {
                    return BadRequest("name required");
                }
            }
            else
            {
                return BadRequest("no data");
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] User updated)
        {
            var existing = FindUserById(id);
            if (existing == null) return NotFound();

            existing.Name = updated.Name;
            existing.Email = updated.Email;
            existing.Phone = updated.Phone;
            existing.userType = updated.userType;
            db.SaveChanges();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var existing = FindUserById(id);
            if (existing == null)
            {
                return NotFound();
            }
            db.Users.Remove(existing);
            db.SaveChanges(); db.SaveChanges();
            return Ok();
        }

        private List<User> GetAdmins()
        {
            var result = new List<User>();
            var all = db.Users.ToList();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].userType == "Admin")
                {
                    result.Add(all[i]);
                }
            }
            return result;
        }

        private bool CheckEmailFormat(string email)
        {
            if (email.Contains("@"))
            {
                if (email.Contains("."))
                {
                    return true;
                }
            }
            return false;
        }

        private User? FindUserById(int id) => db.Users.FirstOrDefault(u => u.Id == id);
    }
}
