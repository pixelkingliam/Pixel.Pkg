using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;


namespace Pixel.Pkg;
public class Package
{
    public const string Version = "1.0.0";
    private byte[] _compressedData;
    /// <summary>
    /// Metadata of the package
    /// </summary>
    public PackageMetadata Metadata;
    private Package(Stream compressedBytes)
    {

        MemoryStream data = new();
        //compressedBytes.Seek(0, SeekOrigin.Begin);
        compressedBytes.CopyTo(data);
        _compressedData = data.ToArray();

        Metadata = new() { FormatVersion = Version };
    }
    /// <summary>
    /// Creates a stream out of the compressed package, ready for writing.
    /// </summary>
    /// <returns>The pkg file as a Stream.</returns>
    public Stream ToStream()
    {
        var md = JObject.FromObject(Metadata).ToString();
        var mdBytes = Encoding.UTF8.GetBytes(md);
        var outStream = new MemoryStream();
        outStream.Write("ppkg"u8.ToArray(), 0, 4); // Write magic bytes
        var mdLength = BitConverter.GetBytes(mdBytes.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(mdLength);
        outStream.Write(mdLength, 0, 4); // write metadata length
        outStream.Write(mdBytes, 0, mdBytes.Length); // write metadata in UTF-8
        outStream.Write(_compressedData, 0, _compressedData.Length); // write compressed data

        return outStream;
    }
    /// <summary>
    /// Creates a bytes array out of the compressed package, ready for writing
    /// </summary>
    /// <returns></returns>
    public byte[] ToBytes()
    {
        using MemoryStream stream = (MemoryStream)ToStream();
        return stream.ToArray();
    }

    /// <summary>
    /// Reads a pkg file represented as a Stream.
    /// </summary>
    /// <param name="stream">The stream of the pkg file.</param>
    /// <returns>The pkg</returns>
    public static Package Read(Stream stream)
    {
        return LoadPackage(stream);
    }
    /// <summary>
    /// Reads a pkg file represented as a byte array.
    /// </summary>
    /// <param name="bytes">The byte array of the pkg file.</param>
    /// <returns>The pkg</returns>
    public static Package Read(byte[] bytes)
    {
        return LoadPackage(new MemoryStream(bytes));
    }
    /// <summary>
    /// Reads a pkg file from a path.
    /// </summary>
    /// <param name="path">The path of the pkg file.</param>
    /// <returns>The pkg</returns>
    public static Package Read(string path)
    {
        return LoadPackage(File.OpenRead(path));
    }
    /// <summary>
    /// Creates a new Package with data from a byte array.
    /// </summary>
    /// <param name="data">The uncompressed byte array</param>
    /// <returns>A new Package<cref</returns>
    public static Package Create(byte[] data)
    {
        using var compressStream = new MemoryStream();
        using var compressor = new DeflateStream(compressStream, CompressionMode.Compress);
        new MemoryStream(data).CopyTo(compressor);
        compressStream.Seek(0, SeekOrigin.Begin);
        return new Package(compressStream);

    }
    /// <summary>
    /// Creates a new Package with data from a Stream.
    /// </summary>
    /// <param name="data">The uncompressed stream</param>
    /// <returns>A new Package<cref</returns>
    public static Package Create(Stream data)
    {
        using var compressStream = new MemoryStream();
        using var compressor = new DeflateStream(compressStream, CompressionMode.Compress);
        data.CopyTo(compressor);
        return new Package(compressStream);

    }
    private static Package LoadPackage(Stream stream)
    {
        int mdLength = 0;


        if (stream.Length < 4)
            throw new FileLoadException("PKG file is too short for magic bytes!");
        byte[] buffer = new byte[4];
        stream.Read(buffer, 0, 4);
        if (!buffer.SequenceEqual("ppkg"u8.ToArray()))
            throw new FileLoadException("PKG file doesn't start with correct magic bytes!");
        stream.Read(buffer, 0, 4); // int 32 length of metadata
        if (BitConverter.IsLittleEndian)
            Array.Reverse(buffer);

        mdLength = BitConverter.ToInt32(buffer);
        
        byte[] mdBytes = new byte[mdLength];
        stream.Read(mdBytes, 0, mdLength);
        MemoryStream dataBytes = new();
        stream.CopyTo(dataBytes);
        stream.Dispose();
        dataBytes.Seek(0, SeekOrigin.Begin);
        var pkg = new Package(dataBytes);

        var md = Encoding.UTF8.GetString(mdBytes.ToArray());
        pkg.Metadata = JsonConvert.DeserializeObject<PackageMetadata>(md);

        return pkg;
    }
    /// <summary>
    /// Gets the compressed data of the Package.
    /// </summary>
    /// <returns>The compressed data (Deflate).</returns>
    public MemoryStream GetCompressedData()
    {
        return new MemoryStream(_compressedData);
    }
    /// <summary>
    /// Uncompresses the Package's data and returns it.
    /// </summary>
    /// <returns>Uncompressed data.</returns>
    public MemoryStream GetUncompressedData()
    {
        var rawBytes = new MemoryStream();
        var decompressor = new DeflateStream(new MemoryStream(_compressedData), CompressionMode.Decompress);        
        decompressor.CopyTo(rawBytes);
        decompressor.Close();
        return rawBytes;
    }

}
/// <summary>
/// Metadata for Package
/// </summary>
public struct PackageMetadata
{
    /// <summary>
    /// The name of the package.
    /// </summary>
    public string PackageName;
    /// <summary>
    /// The version of the package.
    /// </summary>
    public string PackageVersion;
    /// <summary>
    /// License of the package.
    /// </summary>
    public string PackageLicense;
    /// <summary>
    /// Description of the package.
    /// </summary>
    public string PackageDescription;
    /// <summary>
    /// Name of the author(s);
    /// </summary>
    public string AuthorName;
    /// <summary>
    /// Contacts of the author(s).
    /// </summary>
    public string[] AuthorContacts;
    /// <summary>
    /// Version of the package format used.
    /// </summary>
    public string FormatVersion;
    /// <summary>
    /// What this package is, used in conjonction with MetaMeta for storing additional metadata
    /// <para>
    /// Example:
    /// Type could be GameMap
    /// 
    /// and MetaMeta would contain the Gamemode Key.
    /// </para>
    /// <para>
    /// A map loader that uses the pkg loader could check for Type and expect MetaMeta to contain Gamemode as a key.
    /// </para>
    /// </summary>
    public string Type;
    /// <summary>
    /// See PackageMetadata.Type
    /// </summary>
    public JObject MetaMeta;

}
