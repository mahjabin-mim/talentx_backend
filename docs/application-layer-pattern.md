# Endpoint Implementation Pattern (Clean + MediatR)

Use this checklist for every new endpoint:

1. **Controller stays thin**
    - Extract identity claims (if needed)
    - Call `IMediator.Send(...)`
    - Return HTTP response only

2. **Create command/query in Modules**
    - `app/Core/Modules/<Feature>/<Action>/<Action>Command.cs`

3. **Create FluentValidation validator**
    - `app/Core/Modules/<Feature>/<Action>/<Action>CommandValidator.cs`

4. **Create handler with business logic**
    - `app/Core/Modules/<Feature>/<Action>/<Action>CommandHandler.cs`
    - Keep security controls in handler logic as required

5. **Throw application exceptions for expected failures**
    - Derive from `ApplicationExceptionBase`
    - Let global middleware convert to consistent problem responses

6. **Add tests**
    - Validator tests (invalid + valid cases)
    - Handler tests (success + core failure cases)

7. **Preserve contracts**
    - Keep endpoint paths, status codes, and response DTO shape stable unless explicitly changing API contract.
