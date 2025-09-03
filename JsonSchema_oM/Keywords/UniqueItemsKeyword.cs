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
    [Description("Represents the 'uniqueItems' keyword in JSON Schema, which is used to validate that all items in an array are unique. When applied to an array schema, this constraint ensures that no two elements in the array have the same value. The uniqueness comparison is performed using deep equality, meaning that objects and arrays are compared by their structure and content, not by reference.")]
    public class UniqueItemsKeyword : ISchemaKeyWord
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        [Description("Specifies whether array items must be unique. When set to true, the array validation will fail if any duplicate items are found. When set to false (or omitted), duplicate items are allowed in the array. This property corresponds to the 'uniqueItems' keyword value in the JSON Schema document. Note that uniqueness is determined by deep equality comparison - two items are considered equal if they have the same type and value (for primitives) or the same structure and content (for objects and arrays).")]
        public virtual bool Unique { get; set; }

        /***************************************************/
    }
}
