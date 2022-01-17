// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Serialization;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Multiple computers are trying to sync with the same account at the same time.
/// </summary>
[Serializable]
public class SyncRaceException : WebException
{
    public SyncRaceException(WebException ex)
        : base("Multiple computers are trying to sync with the same account at the same time.", ex, ex.Status, ex.Response)
    {}

    protected SyncRaceException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {}
}
