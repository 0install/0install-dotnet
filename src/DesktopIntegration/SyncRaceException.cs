// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Multiple computers are trying to sync with the same account at the same time.
/// </summary>
#if !NET
[Serializable]
#endif
public class SyncRaceException : WebException
{
    public SyncRaceException()
        : base("Multiple computers are trying to sync with the same account at the same time.")
    {}

#if !NET
    protected SyncRaceException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {}
#endif
}
