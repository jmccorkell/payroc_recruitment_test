using System;
using Microsoft.AspNetCore.WebUtilities;

public class ShortenURL
{
    public string GetUrlChunk()
    {
        // Turn "id" of the object into short text
        return WebEncoders.Base64UrlEncode(BitConverter.GetBytes(Id));
    }

    public static int GetId(string urlChunk)
    {
        // Reverse text back into an int Id
        return BitConverter.ToInt32(WebEncoders.Base64UrlDecode(urlChunk));
    }

    public int Id { get; set; }

    public string Url { get; set; }
}