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


        private static Schema TypeSchema(Type type = null)
        {
            Schema schema = Create.Schema(SchemaType.@object);
            Schema nameSchema = ToJsonSchema(typeof(string), false, false, "", false, new HashSet<Type>());
            if(type != null)
                nameSchema.Keywords.Add(new ConstKeyword { Value = type.FullName });

            PropertiesKeyword properties = new PropertiesKeyword
            {
                Properties = new Dictionary<string, Schema>
                {
                    {m_TypeDescriminator, TypeDisciminatorSchema(typeof(Type)) },
                    {"Name", nameSchema  }
                }
            };

            schema.Keywords.Add(properties);
            return schema;
        }

        /*******************************************/

        private static Schema ColourSchema()
        {
            Schema schema = Create.Schema(SchemaType.@object, false);
            PropertiesKeyword properties = new PropertiesKeyword
            {
                Properties = new Dictionary<string, Schema>
                {
                    {m_TypeDescriminator, TypeDisciminatorSchema(typeof(System.Drawing.Color)) },
                    {"A", ToJsonSchema(typeof(int), false, false, "", false, new HashSet<Type>()) },
                    {"R", ToJsonSchema(typeof(int), false, false, "", false, new HashSet<Type>()) },
                    {"G", ToJsonSchema(typeof(int), false, false, "", false, new HashSet<Type>()) },
                    {"B", ToJsonSchema(typeof(int), false, false, "", false, new HashSet<Type>()) },
                }
            };

            schema.Keywords.Add(properties);
            schema.Keywords.Add(new RequiredKeyword() { Required = new List<string> { "A", "R", "G", "B" } });
            return schema;


        }
        /*******************************************/

        private static Schema DataTableSchema()
        {
            Schema schema = Create.Schema(SchemaType.array);

            ItemKeyword items = Create.ItemKeyword(SchemaType.@object);

            schema.Keywords.Add(items);
            return schema;
        }

        /*******************************************/

        private static Schema TupleSchema(this Type tupleType, bool typeAsRef, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            Schema schema = Create.Schema(SchemaType.array);

            PrefixItemsKeyword items = new PrefixItemsKeyword { AllowAdditional = false };
            Type[] typeArgs = tupleType.GenericTypeArguments;
            items.PreFixItems = new Schema[typeArgs.Length];

            for (int i = 0; i < typeArgs.Length; i++)
            {
                if (typeArgs[i].IsGenericParameter)
                    items.PreFixItems[i] = GenericParameterTypeSchema(typeArgs[i], typeAsRef, includeInnerIds, visitedTypes);
                else
                    items.PreFixItems[i] = ToJsonSchema(typeArgs[i], includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes);
            }

            schema.Keywords.Add(items);
            return schema;

        }

        /*******************************************/

        private static Schema GenericParameterTypeSchema(this Type genericParameterType, bool typeAsRef, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            Type[] constraints = genericParameterType.GetGenericParameterConstraints();
            if (constraints.Length == 0)
                return new Schema();    //Empty doc, no limitation
            else if (constraints.Length == 1)
                return ToJsonSchema(constraints[0], includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes);
            else
            {
                Schema schema = new Schema();
                AllOfKeyword allOfKeyword = new AllOfKeyword();
                foreach (Type type in constraints)
                {
                    allOfKeyword.Options.Add(ToJsonSchema(type, includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes));
                }
                schema.Keywords.Add(allOfKeyword);
                return schema;
            }
        }
        /*******************************************/

        private static Schema DecimalSchema()
        {
            Schema schema = Create.Schema(SchemaType.@object, false);
            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, Schema> { { "$numberDecimal", ToJsonSchema(typeof(string), false, false, "", false, new HashSet<Type>()) } } });
            return schema;
        }

        /*******************************************/

        private static Schema DateTimeSchema()
        {
            Schema schema = Create.Schema(SchemaType.@object, false);

            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, Schema> { { "$date", ToJsonSchema(typeof(int), false, false, "", false, new HashSet<Type>()) } } });
            return schema;
        }

        /*******************************************/

        private static Schema DateTimeOffsetSchema()
        {
            Schema schema = Create.Schema(SchemaType.array);

            PrefixItemsKeyword items = new PrefixItemsKeyword { AllowAdditional = false };

            items.PreFixItems = new Schema[2];
            items.PreFixItems[0] = ToJsonSchema(typeof(long), false, false, "", false, new HashSet<Type>());
            items.PreFixItems[1] = ToJsonSchema(typeof(int), false, false, "", false, new HashSet<Type>());
            schema.Keywords.Add(items);
            return schema;
        }

        /*******************************************/

        private static Schema IComparableSchema()
        {
            Schema schema = new Schema();
            TypeKeyword typeKeyword = new TypeKeyword { Type = new SchemaType[] { SchemaType.@object, SchemaType.integer, SchemaType.@string, SchemaType.number, SchemaType.@null } };
            schema.Keywords.Add(typeKeyword);
            return schema;
        }

        /*******************************************/

        private static Schema SystemObjectSchema()
        {
            Schema schema = new Schema();
            return schema;
        }

        /*******************************************/

        private static Schema NoUpdateExceptionSchema(Type type)
        {
            Schema schema = Create.Schema(SchemaType.@object, false);
            var id = type.SchemaId();
            if (id != null)
            {
                schema.Keywords.Add(new IdKeyword() { Uri = id });
            }
            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, Schema> { { "Message", ToJsonSchema(typeof(string), false, false, "", false, new HashSet<Type>()) } } });
            schema.Keywords.Add(new RequiredKeyword { Required = new List<string> { "Message" } });
            return schema;
        }

        /*******************************************/
    }
}





