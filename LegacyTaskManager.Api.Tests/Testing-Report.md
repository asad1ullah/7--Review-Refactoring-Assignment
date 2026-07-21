# Testing Report — LegacyTaskManager.Api.Tests

## What we did

We wrote automated tests for the two controllers (`TasksController` and `UsersController`) to make sure the refactoring didn't break anything. All 42 tests pass.

## Quick facts

| | |
|---|---|
| Test project | `LegacyTaskManager.Api.Tests` |
| Test framework | xUnit |
| Fake database | EF Core InMemory (`AppDbContext`) — no real database needed |
| Fake logger | Moq (`ILogger<T>` mocked) |
| Total tests | 42 |
| Passed | 42 |
| Failed | 0 |
| Skipped | 0 |
| How long it takes | about half a second |
| Build warnings | 0 |

To run the tests yourself, go to the solution root folder and run `dotnet test`. Last time we ran it, all 42 tests passed.

## What's tested

**TasksController — 24 tests**

Covers getting tasks, adding tasks, editing tasks, deleting tasks, assigning tasks, completing tasks, and getting a user's tasks. This includes normal cases (it works) and edge cases (bad input, missing records, business rules like task limits per user type).

- `GetTasks`, `GetTaskById` — task exists, task doesn't exist
- `AddNewTask` — empty request, missing title, invalid priority value (falls back to medium), normal valid request, request with a specific `createdBy`
- `EditTask` — task exists, task doesn't exist
- `RemoveTask` — task exists, task doesn't exist
- `AssignTask` — task not found, user not found, task already completed, Admin user (no task limit), Normal user with 4 tasks (allowed), Normal user with 5 tasks (blocked), completed tasks don't count toward the limit, unknown user type
- `CompleteTask` — task exists, task doesn't exist
- `GetTasksForUser` — only returns tasks belonging to that user

**UsersController — 18 tests**

Covers getting users, creating users, updating users, and deleting users, plus validation rules like required fields and duplicate emails.

- `GetAllUsers`
- `GetUserById` — invalid id (0 or negative), user exists, user doesn't exist
- `CreateUser` — empty request, missing name, name too short, missing email, email without "@", duplicate email (not case-sensitive), no user type given (defaults to Normal), specific user type given
- `UpdateUser` — user exists, user doesn't exist
- `DeleteUser` — user exists, user doesn't exist

## Why this matters

Every controller method that was touched during the refactor (see Issues 1–6 in `Refactoring-Changes.md`) has tests for both its normal behavior and its edge cases. We ran the same tests before and after each refactoring step, and got the same result every time: 42/42 passing, same status codes, same messages. That proves the refactor changed *how* the code is written internally, without changing *what* it does or returns to the caller.

## What we didn't test (on purpose)

- We don't check the exact text of log messages. The Issue 3 fix was about making sure exceptions get logged at all, not about wording.
- These are unit tests at the controller level, not full end-to-end HTTP tests. They use an in-memory database instead of a real one.
