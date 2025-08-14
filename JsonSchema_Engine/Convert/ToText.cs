/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.JsonSchema;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace BH.Engine.JsonSchema
{
    public static partial class Convert
    {
        /*******************************************/
        /**** Public Methods                    ****/
        /*******************************************/

        [Description("Converts a Schema to a JSON string representation.")]
        [Input("schema", "The Schema to convert to a JSON string.")]
        [Output("json", "The JSON string representation of the Schema.")]
        public static string ToText(this oM.JsonSchema.JsonSchema schema)
        {
            BsonDocument document = new BsonDocument();
            schema.Serialise(new MongoDB.Bson.IO.BsonDocumentWriter(document), null);
            var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict, Indent = true };
            return document.ToJson<BsonDocument>(jsonWriterSettings);
        }

        /*******************************************/
        /**** Private Methods                   ****/
        /*******************************************/

        private static void Serialise(this oM.JsonSchema.JsonSchema value, BsonDocumentWriter writer, Type targetType)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartDocument();
            foreach (ISchemaKeyWord keyWord in value.Keywords)
            {
                ISerialise(keyWord, writer);
            }
            writer.WriteEndDocument();
        }

        /*******************************************/
        /**** Private Methods -  SchemaKeyWords ****/
        /*******************************************/

        private static void ISerialise(this ISchemaKeyWord value, BsonDocumentWriter writer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            Serialise(value as dynamic, writer);
        }

        /*******************************************/

        private static void Serialise(this IdKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("$id");
            writer.WriteString(value.Uri.OriginalString);
        }

        /*******************************************/

        private static void Serialise(this RefKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("$ref");
            writer.WriteString(value.Uri.OriginalString);
        }

        /*******************************************/

        private static void Serialise(this TypeKeyword value, BsonDocumentWriter writer)
        {
            if (value?.Type == null || value.Type.Length == 0)
                return;

            writer.WriteName("type");
            if (value.Type.Length == 1)
                writer.WriteString(value.Type[0].ToString().Replace("_", "-"));
            else
            {
                writer.WriteStartArray();
                foreach (var item in value.Type)
                {
                    writer.WriteString(item.ToString().Replace("_", "-"));
                }
                writer.WriteEndArray();
            }
        }

        /*******************************************/

        private static void Serialise(this FormatKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("format");
            writer.WriteString(value.Format.ToString().Replace("_", "-"));
        }

        /*******************************************/

        private static void Serialise(this PropertiesKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("properties");
            writer.WriteStartDocument();

            foreach (var property in value.Properties)
            {
                writer.WriteName(property.Key);
                Serialise(property.Value, writer, null);
            }
            writer.WriteEndDocument();
        }

        /*******************************************/

        private static void Serialise(this DefsKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("$defs");
            writer.WriteStartDocument();

            foreach (var property in value.Definitions)
            {
                writer.WriteName(property.Key);
                Serialise(property.Value, writer, null);
            }
            writer.WriteEndDocument();
        }

        /*******************************************/

        private static void Serialise(this ItemKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("items");
            Serialise(value.Item, writer, null);
        }

        /*******************************************/

        private static void Serialise(this PrefixItemsKeyword value, BsonDocumentWriter writer)
        {

            writer.WriteName("prefixItems");
            writer.WriteStartArray();
            foreach (var item in value.PreFixItems)
            {
                Serialise(item, writer, null);
            }
            writer.WriteEndArray();

            if (!value.AllowAdditional)
            {
                //https://json-schema.org/understanding-json-schema/reference/array#additionalitems
                writer.WriteName("items");
                writer.WriteBoolean(false);
            }
        }

        /*******************************************/

        private static void Serialise(this EnumKeyword enumKeyword, BsonDocumentWriter writer)
        {
            writer.WriteName("enum");
            writer.WriteStartArray();
            foreach (string value in enumKeyword.Values)
            {
                writer.WriteString(value);
            }
            writer.WriteEndArray();
        }

        /*******************************************/

        private static void Serialise(this RequiredKeyword required, BsonDocumentWriter writer)
        {
            writer.WriteName("required");
            writer.WriteStartArray();
            foreach (string value in required.Required)
            {
                writer.WriteString(value);
            }
            writer.WriteEndArray();
        }

        /*******************************************/

        private static void Serialise(this TitleKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("title");
            writer.WriteString(value.Title);
        }

        /*******************************************/

        private static void Serialise(this DescriptionKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("description");
            writer.WriteString(value.Description);
        }

        /*******************************************/

        private static void Serialise(this MaximumKeyword value, BsonDocumentWriter writer)
        {
            if (value.Exclusive)
                writer.WriteName("exclusiveMaximum");
            else
                writer.WriteName("maximum");

            writer.WriteDouble(value.Value);
        }

        /*******************************************/

        private static void Serialise(this MinimumKeyword value, BsonDocumentWriter writer)
        {
            if (value.Exclusive)
                writer.WriteName("exclusiveMinimum");
            else
                writer.WriteName("minimum");

            writer.WriteDouble(value.Value);
        }

        /*******************************************/

        private static void Serialise(this OneOfKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("oneOf");
            writer.WriteStartArray();

            foreach (var item in value.Options)
            {
                Serialise(item, writer, null);
            }

            writer.WriteEndArray();
        }

        /*******************************************/

        private static void Serialise(this AllOfKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("allOf");
            writer.WriteStartArray();

            foreach (var item in value.Options)
            {
                Serialise(item, writer, null);
            }

            writer.WriteEndArray();
        }

        /*******************************************/

        private static void Serialise(this AnyOfKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("anyOf");
            writer.WriteStartArray();

            foreach (var item in value.Options)
            {
                Serialise(item, writer, null);
            }

            writer.WriteEndArray();
        }

        /*******************************************/

        private static void Serialise(this AdditionalPropertiesKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("additionalProperties");
            writer.WriteBoolean(value.AllowAdditionalProperties);
        }

        /*******************************************/

        private static void Serialise(this ConstKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("const");

            string s = value.Value.ToString();

            if (int.TryParse(s, out int i))
                writer.WriteInt32(i);
            else if (double.TryParse(s, out double d))
                writer.WriteDouble(d);
            else
                writer.WriteString(s);
        }

        /*******************************************/

        private static void Serialise(this PatternKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("pattern");
            writer.WriteString(value.Value);
        }

        /*******************************************/

        private static void Serialise(this IfKeyword value, BsonDocumentWriter writer)
        {
            writer.WriteName("if");
            Serialise(value.If, writer, null);
            writer.WriteName("then");
            Serialise(value.Then, writer, null);
            if (value.Else != null)
            {
                writer.WriteName("else");
                Serialise(value.Else, writer, null);
            }
        }

        /*******************************************/
    }
}


