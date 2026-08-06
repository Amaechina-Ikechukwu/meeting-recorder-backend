# Meeting Recorder Backend — Technical Design Document

## 1. Overview

This document describes the backend architecture for a meeting recording platform that ingests audio, produces speaker-attributed transcripts, and stores recordings and derived artifacts for retrieval. The backend is built on **.NET 8 (ASP.NET Core)** using **Clean Architecture**, following the same conventions as the Asire backend: layered separation of Domain/Application/Infrastructure/API, Firestore as the primary datastore, Firebase Auth for identity, RabbitMQ for async processing, SignalR for real-time client updates, and Minimal APIs exposed via Scalar.

Core features:
- **Recording ingestion & storage** — upload, chunked/streamed audio capture, durable blob storage
- **Transcription** — speech-to-text pipeline, async, queued
- **Diarization** — speaker segmentation and labeling, merged with transcript output
- **Retrieval** — searchable meeting transcripts, exports, playback with synced captions
- **Real-time updates** — SignalR pushes processing status and live transcript segments to connected clients

---

## 2. Architecture

### 2.1 Layered structure

```
src/
  MeetingRecorder.Domain/         # Entities, value objects, domain events
  MeetingRecorder.Application/    # Use cases, interfaces, DTOs, validators
  MeetingRecorder.Infrastructure/ # Firestore, Storage, RabbitMQ, external APIs
  MeetingRecorder.Api/            # Minimal API endpoints, Scalar docs, DI wiring
  MeetingRecorder.Workers/        # Background consumers (transcription, diarization)
```

- **Domain** has no external dependencies — `Meeting`, `Recording`, `Transcript`, `Speaker`, `TranscriptSegment`.
- **Application** defines interfaces (`IRecordingStorage`, `ITranscriptionEngine`, `IDiarizationEngine`, `IMeetingRepository`) and orchestrates use cases (`StartMeetingRecording`, `ProcessRecordingUploaded`, `GetMeetingTranscript`).
- **Infrastructure** implements those interfaces against real providers (cloud storage, Firestore, third-party STT/diarization APIs).
- **Api** hosts Minimal API endpoints and auth middleware.
- **Workers** hosts RabbitMQ consumers that run the actual transcription/diarization pipeline off the request path.

### 2.2 High-level flow

1. Client uploads/streams audio to the API → stored in blob storage, `Recording` record created in Firestore with status `Uploaded`.
2. API publishes a `RecordingUploaded` event to RabbitMQ.
3. A **Transcription Worker** consumes the event, runs STT, writes raw transcript segments, publishes `TranscriptionCompleted`.
4. A **Diarization Worker** consumes `RecordingUploaded` in parallel (or chained after transcription, depending on provider), producing speaker turn boundaries, publishes `DiarizationCompleted`.
5. A **Merge Worker** consumes both completion events, aligns diarization speaker turns with transcript segments (by timestamp overlap), writes the final `Transcript` document, updates `Meeting.Status = Ready`, and optionally emails the user via ZeptoMail.
6. Client subscribes to a **SignalR hub** for live status/progress updates, then fetches the finished transcript once `meeting.ready` fires.

Running transcription and diarization as parallel, independently-queued jobs (rather than one sequential pipeline) keeps latency down and lets either stage retry independently on failure.

---

## 3. Domain Model

```csharp
public class Meeting
{
    public string Id { get; init; }
    public string OwnerId { get; init; }
    public string Title { get; set; }
    public MeetingStatus Status { get; set; } // Recording, Processing, Ready, Failed
    public DateTimeOffset CreatedAt { get; init; }
    public List<string> ParticipantHints { get; set; } = new(); // optional, for speaker naming
}

public class Recording
{
    public string Id { get; init; }
    public string MeetingId { get; init; }
    public string StorageKey { get; set; }        // path in blob storage
    public string ContentType { get; set; }
    public long DurationMs { get; set; }
    public RecordingStatus Status { get; set; }    // Uploading, Uploaded, Processing, Processed, Failed
}

public class TranscriptSegment
{
    public string Id { get; init; }
    public string TranscriptId { get; init; }
    public string SpeakerId { get; set; }          // resolved after diarization merge
    public string Text { get; set; }
    public long StartMs { get; set; }
    public long EndMs { get; set; }
    public double Confidence { get; set; }
}

public class Speaker
{
    public string Id { get; init; }
    public string MeetingId { get; init; }
    public string Label { get; set; }              // "Speaker 1" or resolved display name
    public double TotalSpeakingMs { get; set; }
}
```

---

## 4. Feature Design

### 4.1 Storage

- **Blob provider**: Firebase Storage (or GCS bucket directly, since Firestore/Firebase Auth are already in the stack) for raw audio and processed artifacts (transcripts as JSON, optional SRT/VTT exports).
- **Upload paths**:
  - **Direct upload**: client requests a signed upload URL from the API (`POST /recordings/upload-url`), uploads directly to storage, then confirms via `POST /recordings/{id}/complete`. Avoids proxying large audio files through the API server.
  - **Streamed capture**: for in-progress recording (e.g. live meeting bot), the API accepts chunked uploads over a resumable session, appending to a single object.
- **Storage key convention**: `recordings/{meetingId}/{recordingId}/audio.{ext}`, `recordings/{meetingId}/{recordingId}/transcript.json`.
- **Lifecycle**: raw audio retained per a configurable retention policy (e.g. 90 days) with a background cleanup job; transcripts retained indefinitely unless the user deletes the meeting.
- **Access control**: signed, time-limited URLs for playback/export; no public buckets. Ownership checked against Firebase Auth UID on every request via Firestore security rules and API-level authorization.

### 4.2 Transcription

- `ITranscriptionEngine` abstracts the STT provider (e.g. a hosted API such as Whisper-based service, Deepgram, or Azure Speech) so the provider can be swapped without touching Application logic.
- Long recordings are chunked (e.g. 5–10 minute windows with small overlaps) to stay within provider limits and to allow partial/incremental results for near-real-time transcripts.
- Output normalized into `TranscriptSegment` objects with timestamps in milliseconds relative to recording start, before diarization merge.
- Retries: transient provider failures retried with exponential backoff at the RabbitMQ consumer level (dead-letter queue after N attempts, surfaced as `Meeting.Status = Failed` with a reason code).

### 4.3 Diarization

- `IDiarizationEngine` abstracts the diarization provider, returning a list of speaker turns: `{ StartMs, EndMs, SpeakerLabel }`.
- Two supported approaches, selected per deployment:
  1. **Provider-native diarization** — some STT providers return diarization inline with transcription (simplest, single call).
  2. **Separate diarization pass** — a dedicated model/service run independently, then merged.
- **Merge algorithm**: for each `TranscriptSegment`, find the diarization turn with maximum time overlap and assign its `SpeakerId`. Segments spanning a speaker change are split at the boundary.
- **Speaker identity resolution**: diarization produces anonymous labels (`Speaker 1`, `Speaker 2`); if `Meeting.ParticipantHints` or a known-voice-print feature is available, speakers can optionally be matched to real names post-hoc. Otherwise the user renames speakers manually in the UI, which persists back to the `Speaker` entity.

### 4.4 Retrieval & Export

- `GET /meetings/{id}/transcript` — full transcript with speaker-attributed segments.
- `GET /meetings/{id}/transcript/search?q=` — full-text search across segments (Firestore doesn't do native full-text well at scale, so this is backed by a lightweight search index — e.g. Typesense/Meilisearch — kept in sync via the same event pipeline).
- `GET /meetings/{id}/export?format=srt|vtt|docx|txt` — generated on demand or cached in storage after first request.
- `GET /meetings/{id}/audio-url` — returns a short-lived signed playback URL.

---

## 5. API Surface (Minimal API, representative)

```csharp
app.MapPost("/meetings", CreateMeeting).RequireAuthorization();
app.MapPost("/meetings/{meetingId}/recordings/upload-url", GetUploadUrl).RequireAuthorization();
app.MapPost("/meetings/{meetingId}/recordings/{recordingId}/complete", CompleteUpload).RequireAuthorization();
app.MapGet("/meetings/{meetingId}", GetMeeting).RequireAuthorization();
app.MapGet("/meetings/{meetingId}/transcript", GetTranscript).RequireAuthorization();
app.MapGet("/meetings/{meetingId}/status", GetMeetingStatus).RequireAuthorization();
app.MapGet("/meetings/{meetingId}/export", ExportTranscript).RequireAuthorization();
app.MapPatch("/meetings/{meetingId}/speakers/{speakerId}", RenameSpeaker).RequireAuthorization();
```

Documented via Scalar (`app.MapScalarApiReference()`), consistent with existing project conventions.

---

## 6. Real-Time Updates (SignalR)

Server-side SignalR ships as part of the ASP.NET Core shared framework — no separate server NuGet package is required, just `builder.Services.AddSignalR()` and `app.MapHub<T>()`. For .NET clients (e.g. a desktop/companion app) or any client outside the browser, use the latest **`Microsoft.AspNetCore.SignalR.Client`** NuGet package, currently **10.0.10**; browser clients use the **`@microsoft/signalr`** npm package, currently at **10.0.x**. Both are cross-compatible with a .NET 8 server — the client package version isn't required to match the server's .NET version.

```csharp
public class MeetingHub : Hub
{
    public async Task JoinMeetingGroup(string meetingId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(meetingId));

    public async Task LeaveMeetingGroup(string meetingId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(meetingId));

    private static string GroupName(string meetingId) => $"meeting:{meetingId}";
}

public interface IMeetingNotifier
{
    Task NotifyStatusChanged(string meetingId, MeetingStatus status);
    Task NotifyTranscriptSegmentReady(string meetingId, TranscriptSegment segment);
}

public class MeetingNotifier(IHubContext<MeetingHub> hubContext) : IMeetingNotifier
{
    public Task NotifyStatusChanged(string meetingId, MeetingStatus status) =>
        hubContext.Clients.Group($"meeting:{meetingId}")
            .SendAsync("meetingStatusChanged", new { meetingId, status });

    public Task NotifyTranscriptSegmentReady(string meetingId, TranscriptSegment segment) =>
        hubContext.Clients.Group($"meeting:{meetingId}")
            .SendAsync("transcriptSegmentReady", segment);
}
```

- **Wiring**: `app.MapHub<MeetingHub>("/hubs/meeting")`, authorized via the same Firebase Auth JWT bearer scheme used by the REST API (`RequireAuthorization()` on the hub).
- **Where it's called from**: the RabbitMQ workers (§6 Async Processing) inject `IMeetingNotifier` and push a status event after each stage — `Processing`, per-segment `transcriptSegmentReady` as chunks finish (enables a live-updating transcript UI rather than a single blocking wait), and `Ready`/`Failed` at the end.
- **Scale-out**: if the API/workers run on multiple instances, add a SignalR backplane (Redis backplane via `AddStackExchangeRedis(...)`) so a message published by one worker instance reaches clients connected to a different API instance.
- **Fallback**: clients that don't maintain a persistent connection (or reconnect after a drop) can still call `GET /meetings/{id}/status` to resync state — SignalR is an optimization for immediacy, not the source of truth (Firestore is).

---

## 7. Async Processing (RabbitMQ)

| Exchange/Queue | Publisher | Consumer | Purpose |
|---|---|---|---|
| `recording.uploaded` | API | Transcription Worker, Diarization Worker | Kick off both pipelines in parallel |
| `transcription.completed` | Transcription Worker | Merge Worker | Raw transcript ready |
| `diarization.completed` | Diarization Worker | Merge Worker | Speaker turns ready |
| `meeting.ready` | Merge Worker | Notification Worker | Triggers ZeptoMail "your transcript is ready" email + SignalR `meetingStatusChanged` push |
| `*.dlq` | — | Ops/alerting | Dead-lettered failed jobs after retry exhaustion |

Using RabbitMQ v7+ async consumer patterns (`IAsyncBasicConsumer` / `AsyncEventingBasicConsumer`), each worker acknowledges only after successful persistence to Firestore, so a crash mid-processing safely redelivers.

---

## 8. Data Store (Firestore)

Collections:
- `meetings/{meetingId}`
- `meetings/{meetingId}/recordings/{recordingId}`
- `meetings/{meetingId}/transcriptSegments/{segmentId}`
- `meetings/{meetingId}/speakers/{speakerId}`

Firestore security rules restrict all reads/writes to `request.auth.uid == resource.data.ownerId` (or shared-access lists if collaborative meetings are supported later).

---

## 9. Status & Error Model

`Meeting.Status`: `Recording → Uploaded → Processing → Ready | Failed`

Failure reasons are stored as a structured code (`TranscriptionFailed`, `DiarizationFailed`, `StorageError`) so the client can show actionable messages and the API can expose a `POST /meetings/{id}/retry` endpoint that re-publishes the relevant queue message.

---

## 10. Non-functional Considerations

- **Scalability**: workers scale independently and horizontally since they're stateless queue consumers; storage and Firestore scale natively.
- **Cost control**: chunked transcription avoids re-processing entire long recordings on retry — only the failed chunk is redriven.
- **Observability**: structured logs correlated by `meetingId`/`recordingId`, queue depth and DLQ alerts, processing-time metrics per stage (upload → transcript ready).
- **Security**: signed URLs everywhere for audio access, no direct client-to-Firestore audio blobs, Firebase Auth-verified JWTs on every API call.

---

## 11. Open Questions

- Which STT/diarization providers to standardize on (cost vs. accuracy vs. latency tradeoffs) — worth a short bake-off before locking `ITranscriptionEngine`/`IDiarizationEngine` implementations.
- Real-time (live) transcription during an in-progress meeting vs. post-processing only — affects whether chunked streaming upload is required for v1.
- Whether full-text search needs a dedicated index (Typesense/Meilisearch) or can be deferred until transcript volume justifies it.
