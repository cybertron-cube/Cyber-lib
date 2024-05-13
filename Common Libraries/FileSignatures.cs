using System.Numerics;

namespace Cybertron;

public static class FileSignatures
{
    public const ulong ZipSignature = 0x04034b50;
    public const ulong GZipSignature = 0x8b1f;
    
    /// <summary>
    /// Determines the minimum number of bits needed to represent the number
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static int MinSize(ulong number)
        => 64 - BitOperations.LeadingZeroCount(number);
    
    public static bool IsZip(string filePath)
        => MatchSignature(filePath, ZipSignature);
    
    public static bool IsGZip(string filePath)
        => MatchSignature(filePath, GZipSignature);
    
    public static bool MatchSignature(string filePath, ulong signature, int offset = 0)
    {
        var bits = MinSize(signature);
        var length = (long)Math.Ceiling((double)bits / 8);
        
        var buffer = new byte[length];
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            var read = fs.Read(buffer, offset, buffer.Length);
            if (read < buffer.Length)
                return false;
        }
        
        var actualSignature = length switch
        {
            < 4 => BitConverter.ToUInt16(buffer, 0),
            < 8 => BitConverter.ToUInt32(buffer, 0),
            _ => BitConverter.ToUInt64(buffer, 0)
        };
        
        return actualSignature == signature;
    }
}
