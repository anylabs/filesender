using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace FileSender {
    internal class Program {
        private static void Main(string[] args) {
            var path = args.First();
            var filePropertyName = ConfigurationManager.AppSettings["filePropertyName"];
            var files = new List<UploadFile>();

            if (!File.Exists(path) && !Directory.Exists(path)) {
                Console.Error.WriteLine(path + " not found!");
                Console.ReadLine();

                return;
            }

            try {
                if (File.Exists(path)) {
                    files.Add(ReadFile(path, filePropertyName));
                }
                else {
                    files.AddRange(Directory.EnumerateFiles(path).Select(file => ReadFile(file, filePropertyName)));
                }

                Console.WriteLine(SendFiles(files));
                Console.ReadLine();
            }
            catch (Exception exception) {
                Console.Error.WriteLine("Exception thrown!");
                Console.Error.WriteLine(exception);
                Console.ReadLine();
            }
        }

        private static string SendFiles(IEnumerable<UploadFile> files) {
            var endpoint = ConfigurationManager.AppSettings["endpointUrl"];
            var response = UploadFiles(endpoint, files, new NameValueCollection());

            return Encoding.UTF8.GetString(response);
        }

        private static UploadFile ReadFile(string path, string propertyName) {
            return new UploadFile {
                Stream = File.OpenRead(path),
                Name = propertyName,
                Filename = Path.GetFileName(path)
            };
        }

        public class UploadFile {
            public UploadFile() {
                ContentType = "application/octet-stream";
            }
            public string Name { get; set; }
            public string Filename { get; set; }
            public string ContentType { get; set; }
            public Stream Stream { get; set; }
        }

        public static byte[] UploadFiles(string address, IEnumerable<UploadFile> files, NameValueCollection values) {
            var request = WebRequest.Create(address);
            request.Method = "POST";
            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            boundary = "--" + boundary;

            using (var requestStream = request.GetRequestStream()) {
                // Write the values
                foreach (string name in values.Keys) {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", name, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(values[name] + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                // Write the files
                foreach (var file in files) {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", file.Name, file.Filename, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Type: {0}{1}{1}", file.ContentType, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);
                    file.Stream.CopyTo(requestStream);
                    buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                var boundaryBuffer = Encoding.ASCII.GetBytes(boundary + "--");
                requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
            }

            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var stream = new MemoryStream()) {
                responseStream.CopyTo(stream);
                return stream.ToArray();
            }
        }
    }
}