# Troubleshooting Guide

Common issues and solutions for Notrelix development.

## Docker Issues

### Containers Won't Start

**Symptom:** `docker-compose up` fails or containers exit immediately

**Solutions:**

```bash
# 1. Clean everything and restart
make clean
make dev-up

# 2. Check logs for specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f backend
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f frontend

# 3. Rebuild images
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build --no-cache

# 4. Check port conflicts
lsof -i :3000  # Frontend
lsof -i :5000  # Backend
lsof -i :5432  # PostgreSQL
lsof -i :6379  # Redis
```

### Database Connection Failed

**Symptom:** Backend can't connect to PostgreSQL

**Solutions:**

```bash
# 1. Check if PostgreSQL is running
docker ps | grep postgres

# 2. Check connection string in appsettings.json
# Should be: Host=postgres;Database=notrelix;Username=notrelix;Password=...

# 3. Reset database
make dev-down
docker volume rm todo-app_postgres_data
make dev-up

# 4. Connect manually to verify
docker exec -it todo-app-postgres-1 psql -U notrelix -d notrelix
```

### Redis Connection Failed

**Symptom:** Backend can't connect to Redis

**Solutions:**

```bash
# 1. Check if Redis is running
docker ps | grep redis

# 2. Test Redis connection
docker exec -it todo-app-redis-1 redis-cli ping
# Should return: PONG

# 3. Check connection string in appsettings.json
# Should be: redis:6379
```

---

## Backend Issues

### Migration Fails

**Symptom:** `dotnet ef database update` fails

**Solutions:**

```bash
# 1. Check migration list
cd backend
dotnet ef migrations list --project Notrelix.Infrastructure --startup-project Notrelix.API

# 2. View SQL that would be executed
dotnet ef migrations script --project Notrelix.Infrastructure --startup-project Notrelix.API

# 3. Rollback to previous migration
dotnet ef database update {PreviousMigrationName} --project Notrelix.Infrastructure --startup-project Notrelix.API

# 4. Remove failed migration
dotnet ef migrations remove --project Notrelix.Infrastructure --startup-project Notrelix.API

# 5. Reset database (WARNING: deletes all data)
make dev-down
docker volume rm todo-app_postgres_data
make dev-up
```

### CS0108 Warning (Hidden Member)

**Symptom:** Warning about hiding inherited member

**Problem:**
```csharp
public class Card : AuditableEntity
{
    public Guid? CreatedBy { get; set; }  // ❌ Hides AuditableEntity.CreatedBy
}
```

**Solution:**
```csharp
public class Card : AuditableEntity
{
    // ✅ Don't redeclare inherited properties
    // CreatedBy is already in AuditableEntity
}
```

### Build Fails

**Symptom:** `dotnet build` fails

**Solutions:**

```bash
# 1. Clean and restore
cd backend
dotnet clean
dotnet restore
dotnet build

# 2. Check for missing packages
dotnet restore --force

# 3. Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
```

### Tests Fail

**Symptom:** `dotnet test` fails

**Solutions:**

```bash
# 1. Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"

# 2. Run specific test
dotnet test --filter "FullyQualifiedName~CreateCardCommandHandlerTests"

# 3. Check test database connection
# Tests should use in-memory database or separate test database
```

---

## Frontend Issues

### Build Fails

**Symptom:** `bun run build` fails

**Solutions:**

```bash
cd frontend

# 1. Clean and reinstall
rm -rf node_modules .next
bun install
bun run build

# 2. Check for TypeScript errors
bun run type-check

# 3. Check for linting errors
bun run lint

# 4. Clear Next.js cache
rm -rf .next
```

### Dev Server Won't Start

**Symptom:** `bun run dev` fails

**Solutions:**

```bash
# 1. Check port 3000 is available
lsof -i :3000
# If occupied, kill the process or change port

# 2. Clear cache and restart
rm -rf .next
bun run dev

# 3. Check environment variables
cat .env.local
# Should have NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

### API Calls Fail (CORS)

**Symptom:** Browser console shows CORS errors

**Solutions:**

1. Check backend CORS configuration in `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

2. Ensure CORS middleware is before routing:
```csharp
app.UseCors();
app.UseRouting();
```

### TanStack Query Issues

**Symptom:** Data not fetching or stale data shown

**Solutions:**

```typescript
// 1. Check query keys are correct
import { queryKeys } from '@/lib/query/query-keys';
useQuery({
  queryKey: queryKeys.boards.detail(boardId),  // ✅ Use factory
  // NOT: queryKey: ['boards', boardId],       // ❌ Hardcoded
});

// 2. Invalidate queries after mutations
queryClient.invalidateQueries({
  queryKey: queryKeys.boards.list(workspaceId),
});

// 3. Check staleTime is appropriate
useQuery({
  queryKey: queryKeys.boards.detail(boardId),
  queryFn: () => boardsApi.getBoard(boardId),
  staleTime: 30 * 1000,  // 30 seconds
});

// 4. Enable React Query DevTools
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
// Add to layout.tsx
```

### Next.js 16 Params Issue

**Symptom:** Error accessing params in page components

**Problem:**
```typescript
// ❌ Wrong (Next.js 16)
export default function Page({ params }: { params: { boardId: string } }) {
  const { boardId } = params;  // Error: params is Promise
}
```

**Solution:**
```typescript
// ✅ Correct (Next.js 16)
export default async function Page({ params }: { params: Promise<{ boardId: string }> }) {
  const { boardId } = await params;  // Must await
}
```

---

## Database Issues

### Position Field Issues

**Symptom:** Position ordering not working correctly

**Problem:**
```sql
-- ❌ Wrong: Using INTEGER
position INTEGER NOT NULL DEFAULT 0
```

**Solution:**
```sql
-- ✅ Correct: Using FLOAT8 for fractional indexing
position FLOAT8 NOT NULL DEFAULT 0
```

### Soft Delete Not Working

**Symptom:** Deleted items still appearing in queries

**Problem:**
```csharp
// ❌ Missing soft delete filter
var cards = await _context.Cards
    .Where(c => c.ListId == listId)
    .ToListAsync();
```

**Solution:**
```csharp
// ✅ Include soft delete filter
var cards = await _context.Cards
    .Where(c => c.ListId == listId && !c.IsDeleted)
    .ToListAsync();
```

### Index Not Used

**Symptom:** Slow queries despite having indexes

**Solutions:**

```sql
-- 1. Check if index exists
SELECT * FROM pg_indexes WHERE tablename = 'cards';

-- 2. Analyze query plan
EXPLAIN ANALYZE
SELECT * FROM cards WHERE list_id = 'xxx' AND is_deleted = false;

-- 3. Ensure partial index matches query
CREATE INDEX idx_cards_list_pos ON cards(list_id, position)
  WHERE is_deleted = false;  -- Must match WHERE clause in query

-- 4. Update statistics
ANALYZE cards;
```

---

## Authentication Issues

### Access Token Expired

**Symptom:** 401 Unauthorized errors

**Solution:**

The axios interceptor should automatically refresh the token. If not:

```typescript
// Check interceptor in lib/api/api-client.ts
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Try to refresh token
      const refreshed = await authApi.refresh();
      if (refreshed) {
        // Retry original request
        return apiClient.request(error.config);
      }
    }
    return Promise.reject(error);
  }
);
```

### Refresh Token Not Working

**Symptom:** Can't refresh access token

**Solutions:**

1. Check refresh token cookie is set:
```typescript
// Backend should set httpOnly cookie
Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.UtcNow.AddDays(30)
});
```

2. Check cookie is sent with requests:
```typescript
// Frontend axios should include credentials
const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
  withCredentials: true,  // Important for cookies
});
```

---

## Performance Issues

### Slow Queries

**Solutions:**

```sql
-- 1. Add missing indexes
CREATE INDEX idx_cards_list_pos ON cards(list_id, position)
  WHERE is_deleted = false;

-- 2. Use EXPLAIN ANALYZE to identify bottlenecks
EXPLAIN ANALYZE
SELECT * FROM cards WHERE list_id = 'xxx';

-- 3. Avoid N+1 queries with Include
var cards = await _context.Cards
    .Include(c => c.Labels)
    .Include(c => c.Members)
    .Where(c => c.ListId == listId && !c.IsDeleted)
    .ToListAsync();
```

### Frontend Slow Loading

**Solutions:**

```typescript
// 1. Use Server Components for data fetching
export default async function Page() {
  const data = await fetchData();  // Server-side
  return <Component data={data} />;
}

// 2. Prefetch data in layout/page
const queryClient = getQueryClient();
await queryClient.prefetchQuery({
  queryKey: queryKeys.boards.detail(boardId),
  queryFn: () => boardsApi.getBoard(boardId),
});

// 3. Use dynamic imports for heavy components
const BlockEditor = dynamic(
  () => import('@/components/docs/block-editor'),
  { ssr: false, loading: () => <EditorSkeleton /> }
);

// 4. Optimize images
import Image from 'next/image';
<Image src="/image.jpg" width={500} height={300} alt="..." />
```

---

## Common Error Messages

### "List with id 'xxx' not found"

**Cause:** Trying to create card in non-existent or deleted list

**Solution:** Verify list exists and is not deleted before creating card

### "You don't have access to this workspace"

**Cause:** User is not a member of the workspace

**Solution:** Check workspace membership or invite user

### "Validation failed"

**Cause:** Invalid input data

**Solution:** Check validation rules in command validator

### "Conflict: A board with this slug already exists"

**Cause:** Duplicate slug in workspace

**Solution:** Generate unique slug or let user choose different name

---

## Development Tips

### Hot Reload Not Working

**Docker:**
```yaml
# Ensure volumes are mounted correctly in docker-compose.dev.yml
volumes:
  - ./backend:/app
  - ./frontend:/app
```

**Backend:**
```bash
# Use dotnet watch
cd backend
dotnet watch run --project Notrelix.API
```

**Frontend:**
```bash
# Next.js dev server has hot reload by default
cd frontend
bun run dev
```

### Debugging Backend

```csharp
// 1. Add breakpoints in Visual Studio / Rider

// 2. Use logging
_logger.LogInformation("Card created: {CardId}", card.Id);

// 3. Check logs
docker-compose logs -f backend
```

### Debugging Frontend

```typescript
// 1. Use browser DevTools

// 2. Add console.log
console.log('Board data:', board);

// 3. Use React DevTools extension

// 4. Use TanStack Query DevTools
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
```

---

## Getting Help

1. **Check existing documentation:**
   - [AGENTS.md](../AGENTS.md) — Comprehensive rules
   - [CLAUDE.md](../CLAUDE.md) — Quick start guide
   - [domains.md](./domains.md) — Domain structure

2. **Check git history:**
   ```bash
   git log --grep="keyword"
   git log --all -- path/to/file
   ```

3. **Search codebase:**
   ```bash
   grep -r "pattern" backend/
   grep -r "pattern" frontend/
   ```

4. **Check recent changes:**
   ```bash
   git diff HEAD~5
   git log --oneline -10
   ```

---

## See Also

- [CLAUDE.md](../CLAUDE.md) — Quick start and common tasks
- [AGENTS.md](../AGENTS.md) — Comprehensive project rules
- [conventions.md](./conventions.md) — Naming conventions
