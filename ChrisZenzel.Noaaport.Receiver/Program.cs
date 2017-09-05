using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChrisZenzel.Noaaport.Receiver
{
    class Program
    {
        /// <summary>
        /// Main Program
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            // Create a data directory
            string temporaryDirectory = Path.GetTempPath();
            temporaryDirectory += "nbsp" + Path.DirectorySeparatorChar;

            if (!Directory.Exists(temporaryDirectory))
            {
                Directory.CreateDirectory(temporaryDirectory);
            }

            // Consume from the main NOAAPORT server
            NBSPClient.OpenConnection("1.nbsp.inoaaport.net", new NBSPClient.ReceivedFile((string fileName, byte[] fileContents) =>
            {
                // Get a SHA-256 hash of the file name
                string fileHash = HashSHA256(fileName);

                // Create a path of the file
                string fileWritePath = temporaryDirectory + fileHash + ".bin";

                // Write the contents of this NOAAPORT file to a hashed index file
                // Process later with a filter using a directory watcher
                var fileWriter = File.Create(fileWritePath);
                fileWriter.Write(fileContents, 0, fileContents.Length);
                fileWriter.Dispose();

                // Display file path
                Console.WriteLine("Processed {0}", fileName);
            }));

            // Console.ReadLine to allow people to see the output
            Console.ReadLine();
        }

        /// <summary>
        /// Create an SHA 256 hash from a string
        /// </summary>
        /// <param name="value">The string to create the hash from</param>
        /// <returns></returns>
        static string HashSHA256(string value)
        {
            // Create the hasher
            using (SHA256 hash = SHA256.Create())
            {
                // Get the UTF-8 Encoding from the System
                Encoding enc = Encoding.UTF8;

                // Compute the hash
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                // Build a string
                StringBuilder sb = new StringBuilder();
                foreach (byte b in result)
                {
                    sb.Append(b.ToString("x2"));
                }

                // Return the hash
                return sb.ToString();
            }
        }
    }
}
