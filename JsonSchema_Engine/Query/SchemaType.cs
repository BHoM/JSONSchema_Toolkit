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
using BH.oM.JsonSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BH.Engine.JsonSchema
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public static SchemaType SchemaType(this Type type)
        {

            switch (type.FullName)
            {
                case "System.Boolean":
                    return oM.JsonSchema.SchemaType.boolean;
                case "System.Decimal":
                case "System.Double":
                case "System.Single": // float
                    return oM.JsonSchema.SchemaType.number;
                case "System.Int16": // short
                case "System.Int32": // integer
                case "System.Int64": // long
                case "System.UInt32": // unsigned integer (uint)
                    return oM.JsonSchema.SchemaType.integer;
                case "System.TimeSpan":
                case "System.Guid":
                case "System.Text.RegularExpressions.Regex":
                case "System.DateTime":
                case "System.DateTimeOffset":
                case "System.String":
                case "System.Enum":
                    return oM.JsonSchema.SchemaType.@string;
            }

            if (type.IsEnum)
                return oM.JsonSchema.SchemaType.@string;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            { 
                var constraints = type.GetGenericArguments();
                if (constraints.Length > 0 && constraints[0] == typeof(string))
                    return oM.JsonSchema.SchemaType.@object; //Dictionary<string,T> is serialisaed as an object;
                else
                    return oM.JsonSchema.SchemaType.array;
            }

            if (type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)) || type.IsArray)
            {
                return oM.JsonSchema.SchemaType.array;
            }


            return oM.JsonSchema.SchemaType.@object;
        }
    }
}
