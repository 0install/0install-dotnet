// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Multiple computers are trying to sync with the same account at the same time.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class SyncRaceException : WebException
{
    public SyncRaceException()
        : base("Multiple computers are trying to sync with the same account at the same time.")
    {}

#if !NET8_0_OR_GREATER
    protected SyncRaceException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {}
#endif
}
