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

using System;
using System.ComponentModel;

namespace BH.oM.JsonSchema
{
    [Description("Represents the '$schema' keyword in JSON Schema, which specifies the JSON Schema specification version that the schema document conforms to. This keyword is typically used at the root level of a schema to indicate which version of the JSON Schema specification should be used to validate the schema itself.")]
    public class SchemaKeyword : ISchemaKeyWord
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        [Description("The URI that identifies the JSON Schema specification version. Common values include 'https://json-schema.org/draft/2020-12/schema' for JSON Schema Draft 2020-12, 'https://json-schema.org/draft/2019-09/schema' for Draft 2019-09, or 'https://json-schema.org/draft-07/schema' for Draft 7. This property corresponds to the '$schema' keyword in the JSON Schema document.")]
        public virtual string Schema { get; set; }

        /***************************************************/
    }
}
