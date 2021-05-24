// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Model;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// An external solver process controlled via a JSON API (https://docs.0install.net/developers/json-api/).
    /// </summary>
    /// <seealso cref="ExternalSolver"/>
    internal sealed class ExternalSolverSession : Dictionary<string, Func<object[], object?>>, IDisposable
    {
        public const string ApiVersion = "2.7";

        private readonly Process _process;
        private readonly Stream _stdin;
        private readonly Stream _stdout;
        private readonly StreamConsumer _stderr;

        public ExternalSolverSession(ProcessStartInfo startInfo)
        {
            _process = startInfo.Start();

            _stdin = _process.StandardInput.BaseStream;
            _stdout = _process.StandardOutput.BaseStream;
            _stderr = new StreamConsumer(_process.StandardError);

            var apiNotification = GetJsonChunk();
            if (apiNotification == null ||
                apiNotification[0].ToString() != "invoke" ||
                apiNotification[1] != null ||
                apiNotification[2].ToString() != "set-api-version")
                throw new IOException("External solver did not respond correctly to handshake.");

            var apiVersion = new ImplementationVersion(apiNotification[3].ReparseAsJson<string[]>()[0]);
            if (apiVersion >= new ImplementationVersion(ApiVersion))
                Log.Debug("Agreed on 0install slave API version " + apiVersion);
            else throw new IOException("Failed to agree on slave API version. External solver insisted on: " + apiVersion);
        }

        private object[]? GetJsonChunk()
        {
            var chunk = GetChunk();
            if (chunk == null) return null;

            string json = Encoding.UTF8.GetString(chunk);
            return JsonStorage.FromJsonString<object[]>(json);
        }

        private byte[]? GetChunk()
        {
            var preamble = _stdout.TryRead(11);
            if (preamble == null) return null;

            int length = Convert.ToInt32(Encoding.UTF8.GetString(preamble).TrimEnd('\n'), 16);
            return _stdout.Read(length);
        }

        private void SendJsonChunk(params object?[] value)
        {
            string json = value.ToJsonString();
            SendChunk(Encoding.UTF8.GetBytes(json));
        }

        private void SendChunk(byte[] data)
        {
            _stdin.Write(Encoding.UTF8.GetBytes($"0x{data.Length:x8}\n"));
            _stdin.Write(data);
            _stdin.Flush();
        }

        private int _nextTicket;
        private readonly Dictionary<string, Action<object[]>> _callbacks = new();

        public void Invoke(Action<object[]> onSuccess, string operation, params object[] args)
        {
            string ticket = _nextTicket++.ToString();
            _callbacks[ticket] = onSuccess;

            SendJsonChunk("invoke", ticket, operation, args);
        }

        public void HandleStderr()
        {
            string? message;
            while ((message = _stderr.ReadLine()) != null)
            {
                if (message.StartsWith("error: ", out string? error)) Log.Error("External solver: " + error);
                else if (message.StartsWith("warning: ", out string? warning)) Log.Warn("External solver: " + warning);
                else if (message.StartsWith("info: ", out string? info)) Log.Info("External solver: " + info);
                else if (message.StartsWith("debug: ", out string? debug)) Log.Debug("External solver: " + debug);
                else Log.Debug("External solver: " + message);
            }
        }

        public void HandleNextChunk()
        {
            var apiRequest = GetJsonChunk();
            if (apiRequest == null) return;

            string type = (string)apiRequest[0];
            string ticket = (string)apiRequest[1];
            string operation = (string)apiRequest[2];
            var args = apiRequest[3];

            switch (type)
            {
                case "invoke":
                    try
                    {
                        var response = this[operation](args.ReparseAsJson<object[]>());
                        ReplyOK(ticket, response);
                    }
                    catch (Exception ex)
                    {
                        ReplyFail(ticket, ex.Message);
                        throw;
                    }
                    break;

                case "return":
                    switch (operation)
                    {
                        case "ok":
                            _callbacks[ticket](args.ReparseAsJson<object[]>());
                            break;
                        case "ok+xml":
                            // ReSharper disable once AssignNullToNotNullAttribute
                            string xml = Encoding.UTF8.GetString(GetChunk() ?? throw new IOException("Error parsing external solver response."));
                            Log.Debug("XML from external solver: " + xml);
                            _callbacks[ticket](args.ReparseAsJson<object[]>().Append(xml));
                            break;
                        case "fail":
                            throw new IOException(((string)args).Replace("\n", Environment.NewLine));
                    }
                    break;
            }
        }

        private void ReplyOK(string ticket, object? response) => SendJsonChunk("return", ticket, "ok", new[] {response});

        private void ReplyFail(string ticket, string message) => SendJsonChunk("return", ticket, "fail", message);

        public void Dispose()
        {
            _stdin.Close();
            _stdout.Close();
            _process.WaitForExit();
        }
    }
}
