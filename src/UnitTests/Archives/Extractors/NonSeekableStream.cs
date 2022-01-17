// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Streams;

namespace ZeroInstall.Archives.Extractors;

public class NonSeekableStream : DelegatingStream
{
    public NonSeekableStream(Stream underlyingStream)
        : base(underlyingStream)
    {}

    public override bool CanSeek => false;

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override long Position { get => base.Position; set => throw new NotSupportedException(); }
}
