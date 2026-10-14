# Foundation API

All responses: `{ "success": bool, "message": string, "data": ..., "errors": [{ "field", "message" }] }`.
Errors: 400 validation (field errors, camelCase), 401 (no/expired token or bad login), 403 (missing permission), 404, 409 (duplicate), 422 (business rule), 500 (generic).
Auth header: `Authorization: Bearer <accessToken>`. Health: `GET /health` (anonymous). Swagger: `/swagger` (Development).

## Auth
- `POST /api/auth/login` (anonymous). Body `{ "username": "admin", "password": "Admin@123" }`.
  Data: `{ accessToken, expiresAt, user: { id, username, displayName, role, permissions: string[], email, farmerId, customerId, employeeId, mustChangePassword } }`.
  Invalid credentials -> 401 "Invalid username or password." (same for unknown/inactive/wrong password). Locked account -> 401 with lock message (5 failed attempts).
- `POST /api/auth/change-password` (any signed-in user). Body `{ currentPassword, newPassword, confirmPassword? }`. Policy: >=8 chars, upper, lower, digit, symbol. Data: null.
- `GET /api/auth/me` (any signed-in user). Data: same `user` object as login (fresh permissions).

## Units (reference module) `/api/inventory/units`
- `GET /api/inventory/units?page&pageSize&search&sortBy(id|code|name|isActive)&sortDir(asc|desc)&isActive` — `inventory.view` — data `PagedResult<{ id, code, name, isActive }>` = `{ items, total, page, pageSize, totalPages }`.
- `GET /api/inventory/units/{id}` — `inventory.view` — data `{ id, code, name, isActive }`.
- `POST /api/inventory/units` — `inventory.create` — body `{ code, name }` — 201, data `{ id, code, name, isActive }`.
- `PUT /api/inventory/units/{id}` — `inventory.update` — body `{ code, name, isActive? }` (isActive=false deactivates) — data `{ id, code, name, isActive }`.
