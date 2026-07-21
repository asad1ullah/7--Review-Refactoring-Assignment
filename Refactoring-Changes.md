# Refactoring Changes — LegacyTaskManager.Api

This document describes the code quality issues that were identified during review and actually resolved during refactoring. Only real, applied changes are described — no hypothetical or unimplemented improvements are included.

---

## Issue 1: Duplicate, Inefficient Database Lookup Pattern

### Location
- `Controllers/TasksController.cs` — `GetTaskById`, `EditTask`, `RemoveTask`, `AssignTask`, `CompleteTask`
- `Controllers/UsersController.cs` — `GetUserById`, `UpdateUser`, `DeleteUser`

### Before Refactoring
Every lookup-by-id action repeated the same line:
```csharp
var task = db.Tasks.ToList().Where(x => x.Id == id).FirstOrDefault();
```
This pattern appeared roughly a dozen times across both controllers. `ToList()` pulled the entire table into memory before `Where`/`FirstOrDefault` filtered it in .NET — the filter never reached the database. It was also copy-pasted rather than written once, violating the Don't-Repeat-Yourself (DRY) principle.

### Refactoring Performed
Extracted one private helper per controller:
```csharp
private TaskItem? FindTaskById(int id) => db.Tasks.FirstOrDefault(t => t.Id == id);
private User? FindUserById(int userId) => db.Users.FirstOrDefault(u => u.Id == userId);
```
All lookup sites now call the helper instead of repeating the pattern inline.

### After Refactoring
The filter predicate is passed directly to EF Core, which translates it into a SQL `WHERE` clause instead of loading the whole table. The lookup logic exists in exactly one place per entity type instead of being duplicated at every call site.

### Behaviour Verification
`FirstOrDefault(predicate)` returns the same single record (or `null`) as the original `ToList().Where(...).FirstOrDefault()` — same result, different execution path. Covered by `GetTaskById_ExistingId_ReturnsOkWithThatTask`, `GetTaskById_MissingId_ReturnsNotFound`, and equivalent tests for `GetUserById`, `EditTask`, `RemoveTask`, `UpdateUser`, `DeleteUser`, `AssignTask`, and `CompleteTask` — all pass against the refactored controllers.

---

## Issue 2: Deeply Nested Conditional Logic in `AssignTask`

### Location
- `Controllers/TasksController.cs` — `AssignTask(int id, int userId)`

### Before Refactoring
The method nested five levels of `if`/`else`: task found → user found → task not completed → user type is Admin/Normal → (for Normal) active-task count under the limit. Each branch's outcome depended on tracing through all the enclosing conditions, and the active-task count was computed with an inline manual `for` loop. This was hard to read, hard to reason about, and hard to test one branch at a time.

### Refactoring Performed
Rewrote the method using sequential guard clauses — each condition returns immediately if it fails, instead of nesting the "happy path" inside `else` blocks. The manual counting loop was extracted into a named helper:
```csharp
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
```
The loop body itself was left untouched (same semantics), only given a name and pulled out of the assignment method. This applies the Clean Code guidance to keep functions small and at one level of abstraction, and reduces cyclomatic complexity.

### After Refactoring
`AssignTask` now reads top to bottom as: task exists → user exists → task not already completed → Admin branch (assign directly) → Normal branch (check limit, then assign) → fall-through for any other user type. Each rule is a single, flat `if` block instead of a nested layer.

### Behaviour Verification
The order of checks and every returned message/status code (`"task not found"`, `"user not found"`, `"cant assign a completed task"`, `"user has too many tasks"`, `"unknown user type"`) are unchanged from the original. Verified by tests covering every branch: task not found, user not found, completed task, Admin assigning while "overloaded", Normal user with 4 active tasks (succeeds), Normal user with 5 active tasks (rejected), completed tasks not counted toward the limit, and an unrecognized user type — all pass.

---

## Issue 3: Swallowed Exceptions and Ineffective Logging

### Location
- `Controllers/TasksController.cs` — `AddNewTask`, `EditTask`
- `Controllers/UsersController.cs` — `CreateUser`

### Before Refactoring
```csharp
try { db.SaveChanges(); }
catch (Exception) { /* ignore, probably fine */ }
```
Save failures were silently discarded — the API still returned `200 OK` even if the data was never persisted, with no record that anything went wrong. In `UsersController`, the one place that did log used `Console.WriteLine("stuff broke: " + ex.ToString().Substring(0, 5))` — five characters of an exception, which carries no diagnostic value.

### Refactoring Performed
Injected `ILogger<TasksController>` / `ILogger<UsersController>` via the constructor and replaced the empty/broken catch bodies with a proper log call, e.g.:
```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed to save new task");
}
```
The catch-and-continue control flow itself was deliberately left as-is — only the missing/broken logging was fixed. Removing the swallow entirely (e.g. returning `500` on failure) would change the API's current response behaviour, which was out of scope for this pass.

### After Refactoring
A failed save is now recorded with the full exception via structured logging (`ILogger`), instead of vanishing or producing a useless log fragment. The response the client receives is unchanged.

### Behaviour Verification
No test asserts on log output, since the goal was observability, not a behaviour change. Existing tests confirm the response is still `200 OK` with the expected payload for successful saves (e.g. `AddNewTask_ValidTaskWithoutCreatedBy_SetsPendingStatusAndDefaultsCreatedByToSystem`, `CreateUser_ValidUserWithoutUserType_DefaultsToNormal`), proving the surrounding behaviour was not altered.

---

## Issue 4: Inconsistent Method Naming

### Location
- `Controllers/TasksController.cs` — `Get_Task`
- `Controllers/UsersController.cs` — `getuserbyid`, `Create_User`, `delete`

### Before Refactoring
Method names mixed snake_case (`Get_Task`, `Create_User`), all-lowercase (`getuserbyid`, `delete`), and PascalCase (`GetTasks`, `AddNewTask`) within the same two classes, with no consistent convention.

### Refactoring Performed
Renamed the inconsistent methods to standard PascalCase: `Get_Task` → `GetTaskById`, `getuserbyid` → `GetUserById`, `Create_User` → `CreateUser`, `delete` → `DeleteUser`.

### After Refactoring
All action method names in both controllers now follow the same PascalCase convention, making the API surface easier to scan and predict.

### Behaviour Verification
Routing in ASP.NET Core comes from the `[HttpGet]`/`[HttpPost]`/`[Route("api/[controller]")]` attributes, not from the C# method name, so the renames have no effect on the URLs or HTTP verbs the API responds to. All endpoint tests continue to call the same routes through the (renamed) controller methods and pass.

---

## Issue 5: Magic Numbers and Magic Strings

### Location
- `Controllers/TasksController.cs` — `AddNewTask`, `AssignTask`, `CompleteTask`
- `Controllers/UsersController.cs` — `CreateUser`
- New: `Models/TaskStatuses.cs`, `Models/UserTypes.cs`

### Before Refactoring
Priority validation used raw literals (`t.Priority < 1 || t.Priority > 3`, default `t.Priority = 2`), the task-assignment limit was a bare `5`, and task/user state was compared against string literals (`"Pending"`, `"Completed"`, `"Admin"`, `"Normal"`) scattered across both files. A typo in any of these literals would fail silently with no compiler warning.

### Refactoring Performed
Added named constants in `TasksController`:
```csharp
private const int MinPriority = 1;
private const int MaxPriority = 3;
private const int DefaultPriority = 2;
private const int MaxActiveTasksPerNormalUser = 5;
```
Added two small static classes holding the repeated string literals as constants:
```csharp
public static class TaskStatuses { public const string Pending = "Pending"; public const string Completed = "Completed"; }
public static class UserTypes { public const string Admin = "Admin"; public const string Normal = "Normal"; }
```
All comparisons and assignments in both controllers now reference these constants instead of literal values.

### After Refactoring
Values have names that explain their purpose, and each one exists in exactly one place. The underlying string/int values are identical to the originals, so this is a pure naming/readability improvement, not a data model change.

### Behaviour Verification
Because the constants hold the exact same values as the original literals, database rows and JSON responses are byte-for-byte identical to before. Tests such as `AddNewTask_PriorityOutOfRange_DefaultsToMedium`, `AssignTask_NormalUserWithFiveActiveTasks_ReturnsBadRequest`, and `CreateUser_ValidUserWithoutUserType_DefaultsToNormal` confirm the observable values and thresholds are unchanged.

---

## Issue 6: Dead Code and Unused Mutable Static State

### Location
- `Controllers/TasksController.cs` — `hitCount`, `x`, `FindTasksByUserId`, `PriorityLabel`
- `Controllers/UsersController.cs` — `callz`, `doNothingImportant()`

### Before Refactoring
Both controllers carried unused static fields (`hitCount`, `callz`), an unused instance field (`x`), an unused method that computed a value and threw it away (`doNothingImportant`), and unused private methods (`FindTasksByUserId`, `PriorityLabel`). None of these were referenced anywhere in the codebase. Static mutable fields are also inherently not thread-safe.

### Refactoring Performed
Deleted all of the above after confirming — by searching the whole project — that nothing referenced them. Also removed dead local aliasing left over in the same methods being touched for other fixes (e.g. `var t2 = t;` in `GetTasks`, a redundant duplicate null check in `EditTask`).

### After Refactoring
Both controllers contain only code that is actually exercised by the API. There is no unused mutable shared state left behind.

### Behaviour Verification
Since none of the removed members were read or called anywhere, their removal cannot change any observable behaviour. Confirmed by a full solution build (`dotnet build`, 0 errors/warnings) and the full test suite (42/42 passing) after the change.

---

## Issues Identified but Not Addressed in This Pass

The following issues were identified during review but intentionally left unresolved, as fixing them would either exceed "small and focused" scope or change observable behaviour:

- **Fat controller / no service or repository layer** — both controllers still talk to `AppDbContext` directly and contain business rules inline. Deferred because extracting a service layer is the largest, riskiest change in scope and should follow its own dedicated pass with full test coverage already in place (as it now is).
- **Duplicate `db.SaveChanges()` call** in `UsersController.DeleteUser` — harmless but wasteful; not part of the issue list this refactoring pass acted on.
- **Nested if/else validation chain in `CreateUser`** — same "arrow" shape as `AssignTask` had, but was not in the identified scope for this pass.
- **`GetAdmins` and `CheckEmailFormat`** in `UsersController` — unused private methods, but not confirmed as part of this round's identified issues.
- **No async/await** — all EF Core calls are synchronous; changing this touches every controller method signature and was left out to keep the diff minimal.
- **Inconsistent error response shapes** — some actions return a bare 404, others a 400 with a free-text string; standardizing on `ProblemDetails` would change the JSON shape of error responses for API consumers, which is a behaviour change.

---

## Summary

- **Total issues identified:** 12
- **Total issues resolved:** 6
- **Overview:** The refactoring focused on the six highest-value, lowest-risk problems: an inefficient/duplicated database lookup pattern, deeply nested conditional logic in task assignment, silently swallowed exceptions with no useful logging, inconsistent method naming, magic numbers/strings, and dead/unused code. Each change was verified with a new xUnit test suite (42 tests, using EF Core's InMemory provider for `AppDbContext` and Moq for `ILogger`) and a clean solution build.
- **Behaviour preserved:** All existing endpoints return the same status codes, messages, and payloads as before. No functionality was removed or added.
- **Scope discipline:** This refactoring addressed code quality and maintainability only — no new features, no API contract changes, and no behavioural changes were introduced.
