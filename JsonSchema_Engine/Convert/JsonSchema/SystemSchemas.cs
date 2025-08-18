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


using System.Linq;
using System.Collections.Generic;
using System;
using BH.oM.JsonSchema;


namespace BH.Engine.JsonSchema
{
    public static partial class Convert
    {
        /*******************************************/
        /**** Private Methods                   ****/
        /*******************************************/


        private static oM.JsonSchema.JsonSchema TypeSchema(Type type = null)
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object);
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

        private static oM.JsonSchema.JsonSchema DataTableSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.array);

            ItemKeyword items = Create.ItemKeyword(SchemaType.@object);

            schema.Keywords.Add(items);
            return schema;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema TupleSchema(this Type tupleType, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.array);

            PrefixItemsKeyword items = new PrefixItemsKeyword { AllowAdditional = false };
            Type[] typeArgs = tupleType.GenericTypeArguments;
            items.PreFixItems = new oM.JsonSchema.JsonSchema[typeArgs.Length];

            for (int i = 0; i < typeArgs.Length; i++)
            {
                if (typeArgs[i].IsGenericParameter)
                    items.PreFixItems[i] = GenericParameterTypeSchema(typeArgs[i], config, visitedTypes);
                else
                    items.PreFixItems[i] = ToJsonSchema(typeArgs[i], false, config, "", visitedTypes);
            }

            schema.Keywords.Add(items);
            return schema;

        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema GenericParameterTypeSchema(this Type genericParameterType, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            Type[] constraints = genericParameterType.GetGenericParameterConstraints();
            if (constraints.Length == 0)
                return new oM.JsonSchema.JsonSchema();    //Empty doc, no limitation
            else if (constraints.Length == 1)
                return ToJsonSchema(constraints[0], false, config, "", visitedTypes);
            else
            {
                oM.JsonSchema.JsonSchema schema = new oM.JsonSchema.JsonSchema();
                AllOfKeyword allOfKeyword = new AllOfKeyword();
                foreach (Type type in constraints)
                {
                    allOfKeyword.Options.Add(ToJsonSchema(type, false, config, "", visitedTypes));
                }
                schema.Keywords.Add(allOfKeyword);
                return schema;
            }
        }
        /*******************************************/

        private static oM.JsonSchema.JsonSchema DecimalSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, false);
            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, oM.JsonSchema.JsonSchema> { { "$numberDecimal", ToJsonSchema(typeof(string), false, new ConvertConfig(), "", new HashSet<Type>()) } } });
            return schema;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema DateTimeSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, false);

            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, oM.JsonSchema.JsonSchema> { { "$date", ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>()) } } });
            return schema;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema DateTimeOffsetSchema()
        {
            oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.array);

            PrefixItemsKeyword items = new PrefixItemsKeyword { AllowAdditional = false };

            items.PreFixItems = new oM.JsonSchema.JsonSchema[2];
            items.PreFixItems[0] = ToJsonSchema(typeof(long), false, new ConvertConfig(), "", new HashSet<Type>());
            items.PreFixItems[1] = ToJsonSchema(typeof(int), false, new ConvertConfig(), "", new HashSet<Type>());
            schema.Keywords.Add(items);
            return schema;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema IComparableSchema()
        {
            oM.JsonSchema.JsonSchema schema = new oM.JsonSchema.JsonSchema();
            TypeKeyword typeKeyword = new TypeKeyword { Type = new SchemaType[] { SchemaType.@object, SchemaType.integer, SchemaType.@string, SchemaType.number, SchemaType.@null } };
            schema.Keywords.Add(typeKeyword);
            return schema;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema SystemObjectSchema()
        {
            oM.JsonSchema.JsonSchema schema = new oM.JsonSchema.JsonSchema();
            return schema;
        }

        /*******************************************/

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





