// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Publish;

/// <summary>
/// An exit code is returned to the original caller after the application terminates, to indicate success or the reason for failure.
/// </summary>
public enum ExitCode
{
    /// <summary>The operation completed without any problems.</summary>
    OK = 0,

    /// <summary>The operation resulted in no changes. This may be due to a problem with the input or simply indicate that the system is already in the desired state.</summary>
    NoChanges = 1,

    /// <summary>There was a network problem. This may be intermittent and resolve itself e.g. when a Wi-Fi connection is restored.</summary>
    /// <seealso cref="WebException"/>
    WebError = 10,

    /// <summary>You have insufficient access rights. This can potentially be fixed by running the command as an Administrator/root. It may also indicate misconfigured file permissions.</summary>
    /// <seealso cref="UnauthorizedAccessException"/>
    AccessDenied = 11,

    /// <summary>There was an IO problem. This encompasses issues such as missing files or insufficient disk space.</summary>
    /// <seealso cref="IOException"/>
    IOError = 12,

    /// <summary>A data file could not be parsed. This encompasses issues such as damaged configuration files or malformed XML documents (e.g. feeds).</summary>
    /// <seealso cref="InvalidDataException"/>
    InvalidData = 25,

    /// <summary>The <see cref="ManifestDigest"/> of an implementation does not match the expected value. This could be caused by a damaged download or an incorrect feed.</summary>
    /// <seealso cref="DigestMismatchException"/>
    DigestMismatch = 26,

    /// <summary>The operation could not be completed because a feature that is not (yet) supported was requested. Upgrading to a newer version may resolve this issue.</summary>
    /// <seealso cref="NotSupportedException"/>
    NotSupported = 50,

    /// <summary>The command-line arguments passed to the application were invalid.</summary>
    /// <seealso cref="FormatException"/>
    InvalidArguments = 99,

    /// <summary>The user canceled the task.</summary>
    /// <seealso cref="OperationCanceledException"/>
    UserCanceled = 100
}
