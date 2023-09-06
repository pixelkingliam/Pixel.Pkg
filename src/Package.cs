using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using SharpCompress.Writers;

//using ICSharpCode.SharpZipLib.Tar;
using SharpCompress.Archives.Tar;
using System.Runtime.InteropServices;


namespace Pixel.Pkg;
public class Package
{
    private byte[] _compressedData;
    public PackageMetadata Metadata;
    private Package(Stream compressedBytes)
    {

        MemoryStream data = new();
        compressedBytes.Seek(0, SeekOrigin.Begin);
        compressedBytes.CopyTo(data);
        _compressedData = data.ToArray();

        Metadata = new();
    }
    public Stream ToStream()
    {
        var md = JObject.FromObject(Metadata).ToString();
        var outStream = new MemoryStream();
        outStream.Write(Encoding.UTF8.GetBytes(md)); // write metadata in UTF-8
        outStream.Write(new byte[] { 0x00, 0x00, 0x00 }, 0, 3); // Write seperator
        outStream.Write(_compressedData); // write compressed data
        return outStream;
    }
    public byte[] ToBytes()
    {
        using MemoryStream stream = (MemoryStream)ToStream();
        return stream.ToArray();
    }


    public static Package Read(Stream stream)
    {
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return LoadPackage(memoryStream.ToArray());
        }
    }
    public static Package Read(byte[] bytes)
    {
        return LoadPackage(bytes);
    }
    public static Package Read(string path)
    {
        return LoadPackage(File.ReadAllBytes(path));
    }
    /// <summary>
    /// Creates a new Package with data from a byte array, which will get compressed.
    /// </summary>
    /// <param name="data">The uncompressed bytes</param>
    /// <returns>A new Package with compressed data<cref</returns>
    public static Package Create(byte[] data)
    {
        using var compressStream = new MemoryStream();
        using var compressor = new DeflateStream(compressStream, CompressionMode.Compress);
        new MemoryStream(data).CopyTo(compressor);
        return new Package(compressStream);

    }
    public static Package Create(Stream data)
    {
        using var compressStream = new MemoryStream();
        using var compressor = new DeflateStream(compressStream, CompressionMode.Compress);
        data.CopyTo(compressor);
        return new Package(compressStream);

    }
    private static Package LoadPackage(byte[] bytes)
    {
        int sepIndex = -1;
        for (int index = 0; index < bytes.Length; index++)
        {
            if(bytes[index] != 0x00)
                continue;
            if (bytes[index + 1] != 0x00)
                continue;
            if (bytes[index + 2] != 0x00)
                continue;
            sepIndex = index;
            break;

        }
        var mdBytes = bytes.Take(sepIndex).ToArray();
        var dataBytes = bytes.Skip(sepIndex + 3).ToArray();


        var pkg = new Package(new MemoryStream(dataBytes));
        
        var md = Encoding.UTF8.GetString(mdBytes);
        pkg.Metadata = JsonConvert.DeserializeObject<PackageMetadata>(md);

        return pkg;
    }
    public MemoryStream GetCompressedData()
    {
        return new MemoryStream(_compressedData);
    }
    public MemoryStream GetUncompressedData()
    {
        var rawBytes = new MemoryStream();
        var decompressor = new DeflateStream(new MemoryStream(_compressedData), CompressionMode.Decompress);
        var data = new MemoryStream(_compressedData);
        decompressor.CopyTo(rawBytes);
        decompressor.Close();
        return rawBytes;
    }

}
public struct PackageMetadata
{
    public string PackageName;
    public string PackageVersion;
    public string PackageLicense;
    public string PackageDescription;
    public string AuthorName;
    public string[] AuthorContacts;
    public string Type;
    public JObject MetaMeta;

}
