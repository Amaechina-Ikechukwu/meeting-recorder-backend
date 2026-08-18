# Meeting Recorder Backend

.NET 10 (ASP.NET Core) implementation of the architecture described in
`meeting-recorder-backend-design.md`: Clean Architecture (Domain / Application /
Infrastructure / Api / Workers), Firestore, Firebase Auth, RabbitMQ, SignalR, and
Minimal APIs documented via Scalar.

## Projects

- `src/MeetingRecorder.Domain` — entities, enums, value objects. No external dependencies.
- `src/MeetingRecorder.Application` — use cases, interfaces (`IMeetingRepository`,
  `ITranscriptionEngine`, `IDiarizationEngine`, ...), DTOs, the diarization/transcript
  merge algorithm.
- `src/MeetingRecorder.Infrastructure` — Firestore repositories, GCS-backed signed-URL
  storage, RabbitMQ publisher, Deepgram-based transcription/diarization engines,
  FirebaseAdmin user lookup, ZeptoMail sender, OpenXml .docx export, the SignalR hub
  and notifier.
- `src/MeetingRecorder.Api` — Minimal API endpoints, Firebase Auth JWT bearer auth,
  Scalar docs, SignalR hub mapping.
- `src/MeetingRecorder.Workers` — RabbitMQ consumers (transcription, diarization,
  merge, notification) as `BackgroundService`s, with retry/backoff and dead-lettering.

## Configuration

Both `MeetingRecorder.Api` and `MeetingRecorder.Workers` read the same configuration
shape (`appsettings.json`, environment variables, or user-secrets):

| Section | Purpose |
|---|---|
| `FirebaseAuth:ProjectId` | Firebase project — validates JWTs from `securetoken.google.com` (Api only). |
| `Firestore:ProjectId` | GCP project hosting Firestore. |
| `Storage:BucketName` | GCS bucket for audio and derived artifacts. |
| `RabbitMQ:ConnectionString` | Broker connection string (`amqp://user:pass@host:port`), plus `ExchangeName` and `MaxRetryAttempts` before dead-lettering. |
| `Deepgram:ApiKey` | STT/diarization provider credential — swappable via `ITranscriptionEngine`. `UseSignedSourceUrl` (default on) hands Deepgram a signed GCS URL so it reads the audio straight out of the bucket instead of this service downloading and re-uploading it; `RequestTimeoutMinutes` caps a single transcription call. |
| `ZeptoMail:ApiKey` | Transactional email for the "transcript ready" notification. |

Google credentials are resolved via Application Default Credentials
(`GOOGLE_APPLICATION_CREDENTIALS` env var pointing at a service account key, or
`gcloud auth application-default login` for local dev). The same credential is used
for Firestore, GCS signed URLs, and Firebase Admin.

## Running locally

```bash
# start RabbitMQ locally, e.g.:
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management

dotnet run --project src/MeetingRecorder.Api
dotnet run --project src/MeetingRecorder.Workers
```

The Api serves Scalar docs at `/scalar/v1` and the SignalR hub at `/hubs/meeting`.

## Notes on provider choices

The design doc leaves STT/diarization provider selection as an open question. This
implementation wires up Deepgram's prerecorded `/v1/listen` endpoint (with
`diarize=true`) behind both `ITranscriptionEngine` and `IDiarizationEngine` as a
working default — swap in another provider by implementing those two interfaces in
`Infrastructure/SpeechToText` and updating `DependencyInjection.AddInfrastructure`.
