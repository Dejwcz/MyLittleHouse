# Implementation Tasks (in progress)

This file tracks remaining tasks while working through the spec.

## Open
- Capacitor wrapper setup (see `docs/plans/2026-01-03-capacitor-wrapper-plan.md`).
- Add Capacitor config and scripts in `src/web`.
- Ensure static build output for mobile web view.
- Add iOS/Android platforms and run `npx cap sync`.

## Done
- Added SyncRetry + SendEmail jobs and scheduling.
- Added invitation declined notifications + API docs update.
- Added email template renderer + HTML templates for core emails.
- Implemented invitation link/token handling + invite emails.
- Added invitation resend endpoint + docs.
- Build issue: skipped per user (build reported OK).
- ExportData now uploads to storage with presigned download link and optional file payloads.
- SyncRetry now uses real queue processor (sync apply logic + retry queue on exception).
- Email dispatch from endpoints now queues SendEmailJob (with direct-send fallback).
- Storage provider added (S3/local) with avatar resize + thumbnails.
