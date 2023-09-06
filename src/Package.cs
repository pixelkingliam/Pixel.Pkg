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
        compressedBytes.Seek(0, SeekOrigin.Begin);
        compressedBytes.CopyTo(data);
        _compressedData = data.ToArray();

        Metadata = new() {FormatVersion = Version};
    }
    /// <summary>
    /// Creates a stream out of the compressed package, ready for writing.
    /// </summary>
    /// <returns>The pkg file as a Stream.</returns>
    public Stream ToStream()
    {
        var md = JObject.FromObject(Metadata).ToString();
        var outStream = new MemoryStream();
        outStream.Write(Encoding.UTF8.GetBytes(md)); // write metadata in UTF-8
        outStream.Write(new byte[] { 0x00, 0x00, 0x00 }, 0, 3); // Write seperator
        outStream.Write(_compressedData); // write compressed data
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
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return LoadPackage(memoryStream.ToArray());
        }
    }
    /// <summary>
    /// Reads a pkg file represented as a byte array.
    /// </summary>
    /// <param name="bytes">The byte array of the pkg file.</param>
    /// <returns>The pkg</returns>
    public static Package Read(byte[] bytes)
    {
        return LoadPackage(bytes);
    }
    /// <summary>
    /// Reads a pkg file from a path.
    /// </summary>
    /// <param name="path">The path of the pkg file.</param>
    /// <returns>The pkg</returns>
    public static Package Read(string path)
    {
        return LoadPackage(File.ReadAllBytes(path));
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
    private static Package LoadPackage(byte[] bytes)
    {
        int sepIndex = -1;
        for (int index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != 0x00)
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
        var data = new MemoryStream(_compressedData);
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
