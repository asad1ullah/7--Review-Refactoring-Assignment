# Known Quality Issues

This project is intentionally written like a small legacy codebase for refactoring practice. The app builds, runs, and every endpoint works correctly — the issues below are style/design smells, not bugs. Nothing here should be treated as a defect report; it's a checklist of what a refactor should target.

## 1. Long methods

- `Controllers/UsersController.cs` — `Create_User`: a single method doing null checks, name validation, email validation, duplicate-email lookup, and default-value assignment all in one block.
- `Controllers/TasksController.cs` — `AssignTask`: one method covering task lookup, user lookup, completed-task check, role check, and workload-count logic.

## 2. Duplicate code

- `db.Users.ToList().Where(x => x.Id == id).FirstOrDefault()` (or the `Tasks` equivalent) is repeated in nearly every action of both `UsersController.cs` and `TasksController.cs` instead of being pulled into one lookup.
- `GetTasksForUser` and `FindTasksByUserId` in `Controllers/TasksController.cs` do the exact same filter-by-user-id loop.

## 3. Poor variable and method names

- `Controllers/UsersController.cs`: method names `getuserbyid`, `Create_User`, `delete`, and variable names `u`, `db` mix casing conventions and give no indication of behavior.
- `Controllers/TasksController.cs`: `Get_Task`, `t` (parameter for a `TaskItem`), `p` (parameter for a priority int).
- `Models/User.cs`: property `userType` breaks PascalCase used by every other property.

## 4. Deeply nested if/else statements

- `Controllers/UsersController.cs` — `Create_User` nests 6 levels of `if/else` for validation.
- `Controllers/TasksController.cs` — `AssignTask` nests task/user/role/capacity checks inside each other instead of using guard clauses.

## 5. Magic numbers and hard-coded strings

- `Controllers/TasksController.cs`: literal `5` for the max open-task count in `AssignTask`, `1`/`2`/`3` priority values in `AddNewTask` and `PriorityLabel`, and status strings `"Pending"` / `"Completed"` scattered across multiple methods instead of a shared constant or enum.
- `Controllers/UsersController.cs`: the `"Normal"` / `"Admin"` role strings are hard-coded rather than defined once.
- `Data/AppDbContext.cs`: seed data uses inline literals for every field (acceptable for seed data, but adds to the overall pattern of no shared constants).

## 6. Business logic inside controllers instead of services

- Both controllers talk to `AppDbContext` directly and contain validation, duplicate-checking, and workload rules (`AssignTask`) with no service or repository layer in between.

## 7. Repeated LINQ queries

- `db.Users.ToList()` and `db.Tasks.ToList()` are called multiple times per request instead of being queried once and reused — e.g. `AssignTask` calls `db.Tasks.ToList()` twice and `db.Users.ToList()` once, all inside the same request.

## 8. Unused methods

- `Controllers/UsersController.cs`: `GetAdmins()` and `CheckEmailFormat()` are private methods with no callers.
- `Controllers/TasksController.cs`: `FindTasksByUserId()` and `PriorityLabel()` are private methods with no callers.

## 9. Inconsistent formatting and naming

- Route/action naming style flips between `PascalCase` (`AddNewTask`, `EditTask`), `snake_case`-ish (`Create_User`, `Get_Task`), and all-lowercase (`getuserbyid`, `delete`) within the same controllers.
- Boolean comparisons are inconsistent: `if (dupe == false)` and `if (task.IsCompleted == false)` alongside plain `if (existing == null)` checks elsewhere.

## 10. Methods that should be split into smaller methods

- `Create_User` (`UsersController.cs`) should be split into separate validation and persistence steps.
- `AssignTask` (`TasksController.cs`) should be split into lookup, eligibility-check, and workload-check steps.
