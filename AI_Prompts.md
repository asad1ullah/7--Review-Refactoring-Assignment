# AI Prompts Used — LegacyTaskManager.Api Review & Refactoring Assignment

Prompts listed in the order they were used. One prompt asking for a refactor of "the first issue" was interrupted before completion and superseded by the next prompt below — excluded here since it was never carried out.

## 1. Initial code review

```
Review this .NET project as a senior software engineer.

Your task is to identify code quality issues only.

Requirements:

- Find at least 10 code smells.
- Prioritize the most important five.
- Explain why each issue is a problem.
- Mention which Clean Code or SOLID principle is being violated.
- Do NOT modify any code yet.
- Do NOT add new features.
- Do NOT change application behaviour.
```

## 2. Refactoring plan

```
Based on the issues you identified, create a refactoring plan.

Requirements:

- Keep existing behaviour unchanged.
- Do not add features.
- Prioritize only the highest-impact improvements.
- Explain the order in which the refactoring should be done.
- Estimate risk for every change.
```

## 3. Second code review (5+ issues, Clean Code focus)

```
Review this .NET project and identify at least five code quality issues.

Requirements:
- Find a minimum of five problems.
- Focus on Clean Code issues such as long methods, duplicate code, poor naming, large classes, complex conditionals, magic numbers, or poor error handling.
- Explain why each issue is a problem.
- Suggest how each issue can be refactored.
- Do not modify any code yet.
- Do not add new features.
- Do not change the application's behaviour.
```

## 4. Refactor the project

```
Refactor this .NET project based on the identified issues.

Requirements:
- Fix only the identified code quality problems.
- Follow Clean Code principles.
- Preserve the existing behaviour of the application.
- Do not add new features.
- Do not remove existing functionality.
- Keep the changes as small and focused as possible.
- After refactoring, explain each change that was made.
```

## 5. Generate unit tests

```
Generate xUnit unit tests for the refactored code.

Requirements:
- Ensure the application's behaviour remains unchanged.
- Cover the main functionality and important edge cases.
- Use Moq where dependencies need to be mocked.
- Do not modify the production code unless absolutely necessary for testing.
- Explain what each test verifies.
```

## 6. Summarize the refactoring changes

```
Create a summary of all refactoring changes made.

For each change include:
- The original problem.
- The refactoring performed.
- The benefit of the change.

Keep the summary clear and concise.
```

## 7. Document all prompts used

```
Create a document containing all AI prompts used during this assignment in the order they were used.
```
