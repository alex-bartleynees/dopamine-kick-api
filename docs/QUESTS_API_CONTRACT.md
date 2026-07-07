# Quests API Contract

Hand-off contract for the frontend. Describes the Quests backend endpoints, request/response shapes, and enums. Status: **proposed** (backend implementation in progress on branch `feature/quests`). Ping the backend agent if you need a change.

## Concept

A **Quest** is a one-off task the user completes by a specific time. Unlike a Habit (which repeats forever and only tracks streaks), a Quest has a terminal **Completed** state. A Quest can have **multiple reminders**, each firing once at an absolute time and delivered via web-push.

## Conventions

- Base path: `/api/quests`
- **Auth:** all endpoints require a Bearer JWT (same as Habits). The user id is taken from the token — never send it in the body.
- **Content type:** `application/json`. Property names are **camelCase**.
- **Timestamps:** ISO-8601 `DateTimeOffset` with offset, e.g. `2026-07-10T18:30:00+00:00`.
- **Enums** are serialized as **strings** (see `QuestStatus`).
- **Error body** (returned on 4xx):
  ```json
  { "status": 400, "title": "BadRequest", "detail": "Human readable message" }
  ```

## Enums

### QuestStatus
| Value | Meaning |
|---|---|
| `Pending` | Not yet completed |
| `Completed` | Marked done (terminal) |

## Models

### Quest
```json
{
  "id": "8f3c...uuid",
  "userId": "1a2b...uuid",
  "emoji": "🎯",
  "title": "Submit tax return",
  "description": "Include the freelance invoices",
  "dueAt": "2026-07-15T23:59:00+00:00",
  "status": "Pending",
  "completedAt": null,
  "createdAt": "2026-07-07T09:00:00+00:00",
  "updatedAt": "2026-07-07T09:00:00+00:00",
  "reminders": [ /* QuestReminder[] — included on GET by id; see note */ ]
}
```
- `description` is nullable.
- `completedAt` is `null` until the quest is completed.
- `reminders` is included on `GET /api/quests/{id}`. On the list endpoint it may be omitted/empty for payload size — treat as optional.

### QuestReminder
```json
{
  "id": "c4d5...uuid",
  "questId": "8f3c...uuid",
  "remindAt": "2026-07-15T18:00:00+00:00",
  "timeZone": "Pacific/Auckland",
  "isEnabled": true,
  "createdAt": "2026-07-07T09:05:00+00:00",
  "updatedAt": "2026-07-07T09:05:00+00:00"
}
```
- `remindAt` is an **absolute** one-off time (not a time-of-day). Must be in the future when created.
- `timeZone` is an IANA id (e.g. `Pacific/Auckland`), used for display/scheduling correctness.
- `isEnabled` — a disabled reminder is stored but not scheduled.

## Endpoints

### List my quests
`GET /api/quests`

Query params (optional):
| Param | Type | Description |
|---|---|---|
| `status` | `Pending` \| `Completed` | Filter by status. Omit for all. |

**200** → `Quest[]`

---

### Get a quest by id
`GET /api/quests/{questId}`

**200** → `Quest` (with `reminders` populated)
**404** → `Error` if not found / not owned by caller

---

### Create a quest
`POST /api/quests`

Request body:
```json
{
  "emoji": "🎯",
  "title": "Submit tax return",
  "description": "Include the freelance invoices",
  "dueAt": "2026-07-15T23:59:00+00:00"
}
```
| Field | Type | Required | Rules |
|---|---|---|---|
| `emoji` | string | yes | 1–10 chars |
| `title` | string | yes | 1–100 chars |
| `description` | string \| null | no | ≤ 500 chars |
| `dueAt` | DateTimeOffset | yes | must be in the future |

**201 Created** → `Quest` (Location header points to the new resource)
**400** → `Error` on validation failure

---

### Update a quest
`PUT /api/quests/{questId}`

Request body (same shape as create; all fields replace current values):
```json
{
  "emoji": "🎯",
  "title": "Submit tax return (amended)",
  "description": null,
  "dueAt": "2026-07-16T23:59:00+00:00"
}
```
**200** → `Quest`
**400** → `Error` on validation failure
**404** → `Error` if not found

> Note: status is **not** changed here. Use the complete endpoint to complete a quest.

---

### Complete a quest
`POST /api/quests/{questId}/complete`

No request body.

**200** → `Quest` with `status: "Completed"` and `completedAt` set. Any scheduled reminders for the quest are cancelled server-side.
**404** → `Error` if not found

---

### Delete a quest
`DELETE /api/quests/{questId}`

**204 No Content** on success (reminders and their scheduled notifications are removed too).
**404** → `Error` if not found

---

### Add a reminder to a quest
`POST /api/quests/{questId}/reminders`

Request body:
```json
{
  "remindAt": "2026-07-15T18:00:00+00:00",
  "timeZone": "Pacific/Auckland",
  "isEnabled": true
}
```
| Field | Type | Required | Rules |
|---|---|---|---|
| `remindAt` | DateTimeOffset | yes | must be in the future |
| `timeZone` | string | yes | valid IANA id |
| `isEnabled` | bool | no (default `true`) | |

**201 Created** → the created `QuestReminder`
**400** → `Error` on validation failure
**404** → `Error` if quest not found

Multiple reminders per quest are supported — call this endpoint once per reminder.

---

## Notes for the frontend

- A quest with `dueAt` in the past but `status: "Pending"` is **overdue** — the API does not auto-complete it; render overdue state client-side by comparing `dueAt` to now.
- Reminders fire as web-push notifications (same subscription mechanism as habit reminders). No polling needed on the client for delivery.
- Deleting or completing a quest cancels its pending push notifications automatically.

## Open items / may still change

- Bulk-create endpoints (quests / reminders) are **not** in scope yet — say if you need them.
- A dedicated "reopen/uncomplete" endpoint is not planned — confirm if the UI needs it.
