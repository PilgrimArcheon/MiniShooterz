﻿using System.IO;
using System.Xml.Serialization;

public static class Helper 
{
    // Serialize
    public static string Serialize<T>(this T toSerialize)
    {
        XmlSerializer xml = new XmlSerializer(typeof(T));
        StringWriter writer = new StringWriter();
        xml.Serialize(writer, toSerialize);
        return writer.ToString();
    }

    // De-Serialize 
    public static T Deserialize<T>(this string toDeSerialize)
    {
        XmlSerializer xml = new XmlSerializer(typeof(T));
        StringReader reader = new StringReader(toDeSerialize);
        return (T)xml.Deserialize(reader);
    } 
}
