You are working in the Notrelix backend.

Before modifying code, you must read and follow:

* backend/RULE.md
* docs/backend/*
* backend/src/Notrelix.Application/README.md

Hard constraints:

1. Do not create files outside the paths allowed by backend/RULE.md.
2. Application use cases must follow:

   * Features/{BoundedContext}/{Module}/Commands/{UseCase}
   * Features/{BoundedContext}/{Module}/Queries/{UseCase}
3. Do not use legacy paths:

   * Features/{Context}/Commands/{Module}
   * Features/{Context}/Queries/{Module}
   * Application/{Context}
   * Application/Commands
   * Application/Queries
4. Handlers must not call SaveChangesAsync directly.
5. Mutating commands must implement ITransactionalRequest.
6. Workspace-scoped requests must implement IWorkspaceRequest.
7. Permission-protected requests must implement IRequirePermission.
8. Cross-bounded-context writes are forbidden.
9. Cross-context effects must use Outbox, IntegrationEvent, Consumer, or Saga/Process Manager.
10. Do not put business logic in API or Infrastructure.
11. Do not create vague Service/Helper/Manager classes.
12. Update docs/backend when changing event, transaction, outbox, or bounded-context communication rules.

After implementation, report:

* files created
* files modified
* bounded context touched
* module touched
* use cases changed
* markers used
* outbox/integration event impact
* tests added
* remaining risks
