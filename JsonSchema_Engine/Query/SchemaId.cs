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
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BH.Engine.JsonSchema
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Gets the full ID path URI to the type based on its assembly and namespace. This method returns the full ID path URI to the type, which can be used to reference the schema in a JSON Schema document.")]
        public static Uri SchemaId(this Type type, string branch)
        {
            if(!(typeof(IObject).IsAssignableFrom(type) || type.IsEnum))
                return null;    

            string basePath = $"https://raw.githubusercontent.com/BHoM/BHoM_JSONSchema/{branch}/";
            string relativeSchema = type.RelativeSchemaId();
            if(relativeSchema != null)
                return new Uri($"{basePath}{relativeSchema}");
            else
                return null;
        }

        /***************************************************/

        [Description("Gets the path to the type based on its assembly and namespace. This method just returns the relative path, not the full ID path URI to the type.")]
        public static string RelativeSchemaId(this Type type)
        {
            if (!(typeof(IObject).IsAssignableFrom(type) || (type.IsEnum && type.Namespace.StartsWith("BH.oM"))))
                return null;

            Assembly assembly = type.Assembly;
            if(type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            //Get rid of initial either BH.oM.Adapters or BH.oM and split by .
            //Replacing the adapters version first, in case it is there to avoid blanket replace any "Adapters." in the string, in case it shows up later
            //Skip 1 to ignore the main namespace as that is taken care of by using the assembly name, avoinding for example Geometry_oM/Geometry
            string namePath = string.Join("/", type.FullName.Replace("BH.oM.Adapters.","").Replace("BH.oM.", "").Split('.').Skip(1));

            return $"{assembly.RelativeAssemblySchemaId()}/{namePath}.json";
        }

        /***************************************************/

        [Description("Gets the path to the type to be used for a particular assembly. Currently set to simply be the name of the assembly.")]
        public static string RelativeAssemblySchemaId(this Assembly assembly)
        {
            return $"{assembly.GetName().Name}";
        }

        /***************************************************/
    }
}
