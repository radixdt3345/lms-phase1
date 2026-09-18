using System;

namespace LMS.Domain.Entities;

/// <summary>
/// Stores metadata for a file attached to a leave request.
/// The actual binary is stored in Azure Blob Storage; only the storage path is kept here.
/// Served via an auth-gated streaming endpoint — never as a static file.
/// </summary>
public class LeaveRequestAttachment
{
    public Guid Id { get; set; }

    /// <summary>The leave request this attachment belongs to.</summary>
    public Guid LeaveRequestId { get; set; }

    /// <summary>Original file name as uploaded (e.g. "medical_certificate.pdf").</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME type (PDF/JPG/PNG accepted; validated server-side via magic bytes).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>File size in bytes — must not exceed 5 MB.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Azure Blob Storage path for the file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>The user who uploaded this attachment.</summary>
    public Guid UploadedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public LeaveRequest LeaveRequest { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
