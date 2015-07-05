using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;

namespace Tests
{
    // NOTE: Blatantly based of Bugsnag's test server: https://github.com/bugsnag/bugsnag-dotnet/blob/master/test/FunctionalTests/TestServer.cs
    public class TestServer : IDisposable
    {
        public static string EndpointUri = "http://localhost:8181/";
        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private HttpListener _listener;

        public TestServer()
        {
            Start();
        }

        public void Dispose()
        {
            Stop();
        }

        public async void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(EndpointUri);
            _listener.Start();

            while (true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _messageQueue.Enqueue(ProcessRequest(context));
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
                catch
                {
                    return;
                }
            }
        }

        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Abort();
            }
        }

        private static string ProcessRequest(HttpListenerContext context)
        {
            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            return body;
        }

        public string GetLastResponse()
        {
            string result = null;
            _messageQueue.TryDequeue(out result);
            return result;
        }
    }
}