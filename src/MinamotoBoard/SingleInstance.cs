using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace MinamotoBoard
{
    // Shared by installed and portable builds, isolated to this user and desktop session.
    internal sealed class SingleInstance : IDisposable
    {
        private readonly Mutex mutex;
        private readonly string pipeName;
        private readonly SecurityIdentifier user;
        private readonly CancellationTokenSource stopping = new CancellationTokenSource();
        private NamedPipeServerStream server;
        private bool ownsMutex;

        public SingleInstance()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) user = identity.User;
            using (Process process = Process.GetCurrentProcess())
                pipeName = "app.minamotodev.board." + user.Value + "." + process.SessionId;
            mutex = new Mutex(false, "Local\\" + pipeName);
        }

        public bool TryAcquire()
        {
            if (ownsMutex) return true;
            try { ownsMutex = mutex.WaitOne(0); }
            catch (AbandonedMutexException) { ownsMutex = true; }
            return ownsMutex;
        }

        public void Listen(Action<string> received)
        {
            PipeSecurity security = new PipeSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));
            server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous, 4096, 4096, security);
            Task.Run(async delegate
            {
                while (!stopping.IsCancellationRequested)
                {
                    try
                    {
                        await server.WaitForConnectionAsync(stopping.Token).ConfigureAwait(false);
                        using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token))
                        {
                            timeout.CancelAfter(5000);
                            byte[] header = await ReadExact(server, 4, timeout.Token).ConfigureAwait(false);
                            int length = BitConverter.ToInt32(header, 0);
                            if (length < 0 || length > 65536) throw new IOException("Invalid launch message.");
                            byte[] data = await ReadExact(server, length, timeout.Token).ConfigureAwait(false);
                            received(Encoding.UTF8.GetString(data));
                        }
                    }
                    catch (IOException) { }
                    catch (OperationCanceledException) { }
                    catch (ObjectDisposedException) { break; }
                    finally
                    {
                        if (!stopping.IsCancellationRequested)
                        {
                            try { if (server.IsConnected) server.Disconnect(); }
                            catch (IOException) { }
                            catch (ObjectDisposedException) { }
                        }
                    }
                }
            });
        }

        private static async Task<byte[]> ReadExact(Stream stream, int length, CancellationToken token)
        {
            byte[] data = new byte[length];
            int position = 0;
            while (position < length)
            {
                int read = await stream.ReadAsync(data, position, length - position, token).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException();
                position += read;
            }
            return data;
        }

        public async Task<bool> NotifyExisting(string path)
        {
            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                using (CancellationTokenSource timeout = new CancellationTokenSource(10000))
                {
                    await client.ConnectAsync(10000, timeout.Token).ConfigureAwait(false);
                    uint process;
                    if (GetNamedPipeServerProcessId(client.SafePipeHandle, out process)) AllowSetForegroundWindow(process);
                    byte[] content = Encoding.UTF8.GetBytes(path ?? "");
                    if (content.Length > 65536) return false;
                    byte[] message = new byte[content.Length + 4];
                    Buffer.BlockCopy(BitConverter.GetBytes(content.Length), 0, message, 0, 4);
                    Buffer.BlockCopy(content, 0, message, 4, content.Length);
                    await client.WriteAsync(message, 0, message.Length, timeout.Token).ConfigureAwait(false);
                    return true;
                }
            }
            catch (IOException) { return false; }
            catch (TimeoutException) { return false; }
            catch (OperationCanceledException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public void Dispose()
        {
            stopping.Cancel();
            if (server != null) server.Dispose();
            if (ownsMutex) { ownsMutex = false; mutex.ReleaseMutex(); }
            mutex.Dispose();
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint process);
        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(uint process);
    }
}
