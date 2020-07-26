﻿using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EDennis.AspNet.Base.Security {
    /// <summary>
    /// Explicitly define JsonConverter to 
    ///   (a) prevent circular referencing during Serialization
    ///   (b) pack extra properties into a catch-all Property property as a JSON string
    /// </summary>
    public class DomainOrganizationJsonConverter : JsonConverter<DomainOrganization> {

        public override DomainOrganization Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var obj = new DomainOrganization();
            DeserializeInto(obj, reader);
            return obj;
        }

        /// <summary>
        /// For Patch operations.  Do not include a [FromBody] parameter
        /// First, retrieve obj from store
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="request"></param>
        public static void DeserializeInto(DomainOrganization obj, HttpRequest request) {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var str = Encoding.UTF8.GetBytes(Task.Run(() => reader.ReadToEndAsync()).Result);
            var bytes = new ReadOnlySpan<byte>(str);
            DeserializeInto(obj, new Utf8JsonReader(bytes));
        }

        public static void DeserializeInto(DomainOrganization obj, Utf8JsonReader reader) {
            bool hasWrittenProperties = false;
            using var ms = new MemoryStream();
            using var jw = new Utf8JsonWriter(ms);
            while (reader.Read()) {
                switch (reader.TokenType) {
                    case JsonTokenType.PropertyName:
                        var prop = reader.GetString();
                        reader.Read();
                        switch (prop) {
                            case "Id":
                            case "id":
                                obj.Id = reader.GetGuid();
                                break;
                            case "Name":
                            case "name":
                                obj.Name = reader.GetString();
                                break;
                            case "SysUser":
                            case "sysUser":
                                obj.SysUser = reader.GetString();
                                break;
                            case "SysStatus":
                            case "sysStatus":
                                obj.SysStatus = (SysStatus)Enum.Parse(typeof(SysStatus), reader.GetString());
                                break;
                            case "SysStart":
                            case "sysStart":
                                obj.SysStart = reader.GetDateTime();
                                break;
                            case "SysEnd":
                            case "sysEnd":
                                obj.SysEnd = reader.GetDateTime();
                                break;
                            default:
                                if (!hasWrittenProperties)
                                    jw.WriteStartObject();
                                switch (reader.TokenType) {
                                    case JsonTokenType.Null:
                                        jw.WriteNull(prop);
                                        break;
                                    case JsonTokenType.True:
                                    case JsonTokenType.False:
                                        jw.WriteBoolean(prop, reader.GetBoolean());
                                        break;
                                    case JsonTokenType.Number:
                                        jw.WriteNumber(prop, reader.GetDecimal());
                                        break;
                                    case JsonTokenType.String:
                                        jw.WriteString(prop, reader.GetString());
                                        break;
                                    default:
                                        break;
                                }
                                break;

                        }
                        break;
                    default:
                        break;
                }
            }
            if (hasWrittenProperties) {
                jw.WriteEndObject();
                obj.Properties = Encoding.UTF8.GetString(ms.ToArray());
            }

        }

        public override void Write(Utf8JsonWriter writer, DomainOrganization value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            {
                writer.WriteString("Id", value.Id.ToString());
                writer.WriteString("Name", value.Name);
                writer.WriteString("SysUser", value.SysUser);
                writer.WriteString("SysStatus", value.SysStatus.ToString());
                writer.WriteString("SysStart", value.SysStart.ToString("u"));
                writer.WriteString("SysEnd", value.SysStart.ToString("u"));
                //extract catch-all properties and promote to top-level in JSON
                if (value.Properties != null) {
                    using var doc = JsonDocument.Parse(value.Properties);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        prop.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
        }
    }
}
