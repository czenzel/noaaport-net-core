using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;

namespace ChrisZenzel.Noaaport.Receiver
{
    class NBSPClient
    {
        /// <summary>
        /// Function called when a file is successfully received from NOAAPORT
        /// </summary>
        /// <param name="fileName">The name of the file provided by NOAAPORT</param>
        /// <param name="fileContents">The contents of the file provided by NOAAPORT in a binary format</param>
        public delegate void ReceivedFile(string fileName, byte[] fileContents);

        /// <summary>
        /// Connect to a NBSP Master Server and Download Data
        /// </summary>
        /// <param name="MasterServerDNS">Specify the DNS Name for the Master Server on the NOAAPORT Broadcast System</param>
        /// <param name="FileReceivedCallback">Specify the callback to use when a file is successfully received from NOAAPORT</param>
        public static void OpenConnection(string MasterServerDNS, ReceivedFile FileReceivedCallback)
        {
            // Specify the TCP Client
            TcpClient nbsp = new TcpClient();

            // Connect to a Tier Server
            nbsp.Client.Connect(MasterServerDNS, 2210);

            if (nbsp.Connected)
                Console.WriteLine("[NBSPClient] Success! Connecting as NBS1 Slave!");

            // Connect to NOAAPORT
            NetworkStream ns = nbsp.GetStream();

            try
            {
                do
                {
                    // Advertise to the NBSP Server we are a NBS1 Slave Server
                    byte[] strProtocol = System.Text.Encoding.ASCII.GetBytes("NBS1");
                    for (int i = 0; i < strProtocol.Length; i++)
                    {
                        ns.WriteByte(strProtocol[i]);
                    }

                    // Tell user if we are connected to a NBSP Server as NBS1 Slave
                    Console.WriteLine("[NBSPClient] Connected as NBS1 Slave Server!");

                    // Track bytes and loops
                    int bytes = 0;

                    // Read the header for each NOAAPORT Broadcast
                    // through the NBSP system.
                    byte[] buffer = new byte[66];
                    bytes = ns.Read(buffer, 0, buffer.Length);

                    // Keep track of the bytes we are reading for each
                    // NOAAPORT product
                    int p = 0;

                    // Check the NBSP Server's Data Identifier.
                    long data_id = unpack_uint32(buffer, 0);

                    // Check to make sure NBSP Server provided us with a correct NOAAPORT
                    // product. NBSP provides data identifier of 1
                    if (data_id == 1)
                    {
                        // Increase here. We don't need these 12 bytes.
                        p += 12;

                        // Determine the Sequence Number of Products
                        long seq_number = unpack_uint32(buffer, p);
                        p += 4;

                        // Determine the NOAAPORT Product Type
                        byte product_type = buffer[p];
                        p++;

                        // Determine the NOAAPORT Product Category
                        byte product_category = buffer[p];
                        p++;

                        // Determine the NOAAPORT product code
                        byte product_code = buffer[p];
                        p++;

                        // Determine which NOAAPORT Channel this product is from
                        // Example Channel 1 is mostly dedicated to text based products
                        // from the National Weather Service
                        byte np_channel_index = buffer[p];
                        p++;

                        // Get the filename NOAAPORT wishes to use from the server
                        string strFileName = System.Text.Encoding.ASCII.GetString(buffer, p, 37);
                        p += 37;

                        // Determine if the product is compressed from the NOAAPORT Stream
                        byte f_zip = buffer[p];
                        p++;

                        // Read which block we currently are on from the NOAAPORT Stream
                        uint num_block = unpack_uint16(buffer, p);
                        p += 2;

                        // Read the current block number (how many blocks make up this product)
                        uint block_number = unpack_uint16(buffer, p);
                        p += 2;

                        // Read the NOAAPORT Block Size
                        long block_size = unpack_uint32(buffer, p);
                        p += 4;

                        // Read the data from the network stream
                        if ((int)block_size > 0)
                        {
                            int data_bytes = 0;

                            // Get the data specified to this product from NOAAPORT
                            MemoryStream ms_data = new MemoryStream();
                            for (int i = 0; i < (int)block_size; i++)
                            {
                                byte[] data_buffer = new byte[1];
                                int c_bytes = ns.Read(data_buffer, 0, 1);
                                ms_data.Write(data_buffer, 0, c_bytes);
                                data_bytes++;
                            }

                            // Received the file. Get a string and provide it to the consumer.
                            byte[] dataReceived = ms_data.ToArray();
                            FileReceivedCallback(strFileName, dataReceived);
                        }
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                // Received an exception. Give details.
                Console.WriteLine("[NBSPClient] Error " + ex.Message);
            }
            finally
            {
                // Close the NBSP Client if we error to not keep the server hung
                nbsp.Dispose();
            }

            // Specify the connection has been closed.
            if (!nbsp.Connected)
                Console.WriteLine("[NBSPClient] Connection Closed!");
        }

        /// <summary>
        /// Pack a byte buffer to a Base 64 data string
        /// </summary>
        /// <param name="buffer">Data represented by array of bytes</param>
        /// <returns>A Base64 String representing the data</returns>
        static string pack_base64(byte[] buffer)
        {
            return System.Convert.ToBase64String(buffer);
        }

        /// <summary>
        /// Unpacks Unsigned Integer 16s from Byte Array
        /// </summary>
        /// <param name="buffer">Array containing buffer of bytes from the network stream</param>
        /// <param name="start">Index of where to start parsing for the unsigned integer 16</param>
        /// <returns>Specified Unsigned Integer 16</returns>
        static uint unpack_uint16(byte[] buffer, int start)
        {
            long u;
            u = (buffer[start] << 8);
            u += (buffer[start + 1]);
            return (uint)u;
        }

        /// <summary>
        /// Unpacks Unsigned Integer 32s from Byte Array
        /// </summary>
        /// <param name="buffer">Array containing buffer of bytes from the network stream</param>
        /// <param name="start">Index of where to start parsing for the unsigned integer 32</param>
        /// <returns>Specified Unsigned Integer 32</returns>
        static long unpack_uint32(byte[] buffer, int start)
        {
            long u;
            u = (buffer[start] << 24);
            u |= (long)(buffer[start + 1] << 16);
            u |= (long)(buffer[start + 2] << 8);
            u |= (long)(buffer[start + 3]);
            return u;
        }
    }
}
