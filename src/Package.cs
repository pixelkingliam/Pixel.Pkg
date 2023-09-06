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
    public JObject ExtraJson;
    private Package(Stream compressedBytes)
    {

        MemoryStream data = new();
        compressedBytes.Seek(0, SeekOrigin.Begin);
        compressedBytes.CopyTo(data);
        _compressedData = data.ToArray();
        File.WriteAllBytes("copy3", data.ToArray());
        
        Metadata = new();
        ExtraJson = new();
    }
    public byte[] OldToBytes()
    {
        var pkg = TarArchive.Create();
        var md = JsonConvert.SerializeObject(Metadata);
        Console.WriteLine(md);
        pkg.AddEntry("metadata", new MemoryStream(Encoding.UTF8.GetBytes(md)));
        pkg.AddEntry("data", new MemoryStream(_compressedData));
        pkg.AddEntry("extra", new MemoryStream(Encoding.UTF8.GetBytes(ExtraJson.ToString())));

        var outStream = new MemoryStream();
        pkg.SaveTo(outStream, new WriterOptions(SharpCompress.Common.CompressionType.None));
        return outStream.ToArray();
    }
    public Stream ToStream()
    {
        var pkg = TarArchive.Create();
        var md = JsonConvert.SerializeObject(Metadata);
        pkg.AddEntry("metadata", new MemoryStream(Encoding.UTF8.GetBytes(md)));
        pkg.AddEntry("data", new MemoryStream(_compressedData));
        pkg.AddEntry("extra", new MemoryStream(Encoding.UTF8.GetBytes(ExtraJson.ToString())));
        var outStream = new MemoryStream();
        pkg.SaveTo(outStream, new WriterOptions(SharpCompress.Common.CompressionType.None));
        return outStream;
    }
    public byte[] ToBytes()
    {
        using (MemoryStream stream = (MemoryStream)ToStream())
        {
            return stream.ToArray();
        }
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
        //compressor.Close();
        File.WriteAllBytes("copy2", compressStream.ToArray());
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
        var tpkg = TarArchive.Open(new MemoryStream(bytes));
        foreach (var item in tpkg.Entries)
        {
            Console.WriteLine(item.Key);
        }
        if (tpkg.Entries.Count != 3)
            throw new FileLoadException("Invalid PKG file (Invalid Structure)");
        if (!tpkg.Entries.All(i => i.Key == "metadata" || i.Key == "data" || i.Key == "extra"))
            throw new FileLoadException("Invalid PKG file (Invalid Structure)");


        // Handle metadata
        PackageMetadata metadata;
        try
        {
            string md;
            using (var reader = new StreamReader(tpkg.Entries.First(pkg => pkg.Key == "metadata").OpenEntryStream(), Encoding.UTF8))
            {
                md = reader.ReadToEnd();
            }
            metadata = JsonConvert.DeserializeObject<PackageMetadata>(md);
        }
        catch (Exception)
        {

            throw new FileLoadException("Invalid PKG file (Invalid Metadata)"); ;
        }
        // Handle extra
        JObject extraJson = new();

        try
        {
            string extra;
            using (var reader = new StreamReader(tpkg.Entries.First(pkg => pkg.Key == "extra").OpenEntryStream(), Encoding.UTF8))
            {
                extra = reader.ReadToEnd();
            }
            if (extra != string.Empty)
                extraJson = JObject.Parse(extra);
        }
        catch (System.Exception)
        {
            throw new FileLoadException("Invalid PKG file (Invalid Extra)"); ;

        }

        // Handle data
        var compressedData = tpkg.Entries.First(pkg => pkg.Key == "data").OpenEntryStream();
        var pkg = new Package(compressedData) { Metadata = metadata, ExtraJson = extraJson };
        return pkg;
    }
    public Stream GetCompressedData()
    {
        return new MemoryStream(_compressedData);
    }
    public Stream GetUncompressedData()
    {
        var rawBytes = new MemoryStream();
        var decompressor = new DeflateStream(rawBytes, CompressionMode.Decompress);
        var data = new MemoryStream(_compressedData);
        data.CopyTo(decompressor);
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

}
