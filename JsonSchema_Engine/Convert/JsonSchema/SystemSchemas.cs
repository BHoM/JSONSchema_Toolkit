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


using BH.oM.Base.Attributes;
using BH.oM.JsonSchema;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace BH.Engine.JsonSchema
{
    public static partial class Convert
    {
        /*******************************************/
        /**** Private Methods                   ****/
        /*******************************************/

        [Description("Generates a JSON Schema for System.Type objects, with optional constraint to a specific type.")]
        [Input("type", "Optional specific type to constrain the schema to, if null allows any type")]
        [Output("schema", "JsonSchema representing a Type object with type discriminator and name properties")]
        private static oM.JsonSchema.JsonSchema TypeSchema(Type type = null)
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, true);
            oM.JsonSchema.JsonSchema nameSchema = ToJsonSchema(typeof(string), false, new ConvertConfig(), "", new HashSet<Type>());
            if(type != null)
                nameSchema.Keywords.Add(new ConstKeyword { Value = type.FullName });

            PropertiesKeyword properties = new PropertiesKeyword
            {
                Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                {
                    {m_TypeDescriminator, TypeDisciminatorSchema(typeof(Type)) },
                    {"Name", nameSchema  }
                }
            };

            schema.Keywords.Add(properties);
            return schema;
        }

        /*******************************************/

        [Description("Generates a JSON Schema for System.Drawing.Color objects with ARGB properties.")]
        [Output("schema", "JsonSchema representing a Color object with Alpha, Red, Green, and Blue integer properties")]
        private static oM.JsonSchema.JsonSchema ColourSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, false);
            PropertiesKeyword properties = new PropertiesKeyword
            {
                Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                {
                    {m_TypeDescriminator, TypeDisciminatorSchema(typeof(System.Drawing.Color)) },
                    {"A", ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>()) },
                    {"R", ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>()) },
                    {"G", ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>()) },
                    {"B", ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>()) },
                }
            };

            schema.Keywords.Add(properties);
            schema.Keywords.Add(new RequiredKeyword() { Required = new List<string> { "A", "R", "G", "B" } });
            return schema;


        }
        /*******************************************/

        [Description("Generates a JSON Schema for System.Data.DataTable objects, represented as an array of objects.")]
        [Output("schema", "JsonSchema representing a DataTable as an array with object-type items")]
        private static oM.JsonSchema.JsonSchema DataTableSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.array, true);

            ItemKeyword items = Create.ItemKeyword(SchemaType.@object);

            schema.Keywords.Add(items);
            return schema;
        }

        /*******************************************/

        [Description("Generates a JSON Schema for Tuple types, represented as arrays with fixed-length prefix items and no additional items allowed.")]
        [Input("tupleType", "The tuple type to generate schema for")]
        [Input("config", "Configuration settings for schema generation")]
        [Input("visitedTypes", "Set of types already visited to prevent circular references")]
        [Output("schema", "JsonSchema representing a tuple as an array with typed prefix items")]
        private static oM.JsonSchema.JsonSchema TupleSchema(this Type tupleType, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.array);

            PrefixItemsKeyword items = new PrefixItemsKeyword { AllowAdditional = false };
            Type[] typeArgs = tupleType.GenericTypeArguments;
            items.PreFixItems = new oM.JsonSchema.JsonSchema[typeArgs.Length];

            for (int i = 0; i < typeArgs.Length; i++)
            {
                if (typeArgs[i].IsGenericParameter)
                    items.PreFixItems[i] = GenericParameterTypeSchema(typeArgs[i], config, new HashSet<Type>(visitedTypes));
                else
                    items.PreFixItems[i] = ToJsonSchema(typeArgs[i], false, config, "", new HashSet<Type>(visitedTypes));
            }

            schema.Keywords.Add(items);
            return schema;

        }

        /*******************************************/

        [Description("Generates a JSON Schema for generic parameter types, handling type constraints with allOf logic when multiple constraints exist.")]
        [Input("genericParameterType", "The generic parameter type to generate schema for")]
        [Input("config", "Configuration settings for schema generation")]
        [Input("visitedTypes", "Set of types already visited to prevent circular references")]
        [Output("schema", "JsonSchema representing the generic parameter constraints, empty if no constraints")]
        private static oM.JsonSchema.JsonSchema GenericParameterTypeSchema(this Type genericParameterType, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            Type[] constraints = genericParameterType.GetGenericParameterConstraints();
            if (constraints.Length == 0)
                return new oM.JsonSchema.JsonSchema();    //Empty doc, no limitation
            else if (constraints.Length == 1)
                return ToJsonSchema(constraints[0], false, config, "", new HashSet<Type>(visitedTypes));
            else
            {
                oM.JsonSchema.JsonSchema schema = new oM.JsonSchema.JsonSchema();
                AllOfKeyword allOfKeyword = new AllOfKeyword();
                foreach (Type type in constraints)
                {
                    allOfKeyword.Options.Add(ToJsonSchema(type, false, config, "", new HashSet<Type>(visitedTypes)));
                }
                schema.Keywords.Add(allOfKeyword);
                return schema;
            }
        }
        /*******************************************/

        [Description("Generates a JSON Schema for System.Decimal objects, represented using MongoDB's $numberDecimal format.")]
        [Output("schema", "JsonSchema representing a decimal number using MongoDB's extended JSON format")]
        private static oM.JsonSchema.JsonSchema DecimalSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, false);
            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, oM.JsonSchema.JsonSchema> { { "$numberDecimal", ToJsonSchema(typeof(string), false, new ConvertConfig(), "", new HashSet<Type>()) } } });
            return schema;
        }

        /*******************************************/

        [Description("Generates a JSON Schema for System.DateTime objects, represented using MongoDB's $date format.")]
        [Output("schema", "JsonSchema representing a DateTime using MongoDB's extended JSON format")]
        private static oM.JsonSchema.JsonSchema DateTimeSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, false);

            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, oM.JsonSchema.JsonSchema> { { "$date", ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>()) } } });
            return schema;
        }

        /*******************************************/

        [Description("Generates a JSON Schema for System.DateTimeOffset objects, with different representations for nullable and non-nullable versions.")]
        [Input("isNullable", "Whether the DateTimeOffset is nullable, affecting the schema structure")]
        [Output("schema", "JsonSchema representing a DateTimeOffset as either an object (nullable) or array (non-nullable)")]
        private static oM.JsonSchema.JsonSchema DateTimeOffsetSchema(bool isNullable)
        {
            if (isNullable)
            {
                oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, true);
                PropertiesKeyword properties = new PropertiesKeyword
                {
                    Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                    {
                        {m_TypeDescriminator, TypeDisciminatorSchema(typeof(DateTimeOffset)) },
                        {m_ValueProperty, DateTimeOffsetSchema(false) },
                    }
                };
                schema.Keywords.Add(properties);
                schema.Keywords.Add(new RequiredKeyword { Required = new List<string> { m_ValueProperty } });
                return schema;
            }
            else
            {
                oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.array, false);

                PrefixItemsKeyword items = new PrefixItemsKeyword { AllowAdditional = false };

                items.PreFixItems = new oM.JsonSchema.JsonSchema[2];
                items.PreFixItems[0] = ToJsonSchema(typeof(long), false, new ConvertConfig(), "", new HashSet<Type>());
                items.PreFixItems[1] = ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>());
                schema.Keywords.Add(items);
                return schema;
            }
        }

        /*******************************************/

        [Description("Generates a JSON Schema for IComparable and IComparable<T> types, allowing multiple JSON types that can be compared.")]
        [Output("schema", "JsonSchema allowing object, integer, string, number, or null types for comparable values")]
        private static oM.JsonSchema.JsonSchema IComparableSchema()
        {
            oM.JsonSchema.JsonSchema schema = new oM.JsonSchema.JsonSchema();
            TypeKeyword typeKeyword = new TypeKeyword { Type = new SchemaType[] { SchemaType.@object, SchemaType.integer, SchemaType.@string, SchemaType.number, SchemaType.@null } };
            schema.Keywords.Add(typeKeyword);
            return schema;
        }

        /*******************************************/

        [Description("Generates a JSON Schema for System.Object and generic parameter types, allowing any JSON value by providing an empty schema.")]
        [Output("schema", "Empty JsonSchema that allows any JSON value")]
        private static oM.JsonSchema.JsonSchema SystemObjectSchema()
        {
            oM.JsonSchema.JsonSchema schema = new oM.JsonSchema.JsonSchema();
            return schema;
        }

        /*******************************************/

        [Description("Generates a JSON Schema for NoUpdateException objects with a required Message property and optional schema ID.")]
        [Input("type", "The NoUpdateException type to generate schema for")]
        [Input("config", "Configuration settings for schema generation, used for branch information in schema ID")]
        [Output("schema", "JsonSchema representing a NoUpdateException with required Message string property")]
        private static oM.JsonSchema.JsonSchema NoUpdateExceptionSchema(Type type, ConvertConfig config)
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, false);
            var id = type.SchemaId(config.Branch);
            if (id != null)
            {
                schema.Keywords.Add(new IdKeyword() { Uri = id });
            }
            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, oM.JsonSchema.JsonSchema> { { "Message", ToJsonSchema(typeof(string), false, new ConvertConfig(), "", new HashSet<Type>()) } } });
            schema.Keywords.Add(new RequiredKeyword { Required = new List<string> { "Message" } });
            return schema;
        }

        /*******************************************/
    }
}





