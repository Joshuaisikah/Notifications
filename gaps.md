# Gaps

- **Delivery is at-least-once, and the receiver is not idempotent.** If the dispatcher sends a notification and crashes (or times out) before marking the outbox row as sent, the row is retried and the notification is delivered twice. Same root cause as JobRunner. To close: project 08 (idempotency keys).
