# Implementation Tasks (in progress)

This file tracks remaining tasks while working through the spec.

## Open
- None.

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
