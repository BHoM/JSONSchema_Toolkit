using BH.oM.Base.Attributes;
using BH.oM.JsonSchema;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace BH.Engine.JsonSchema
{
    public static partial class Modify 
    {
        /*******************************************/
        /**** Public Methods                    ****/
        /*******************************************/

        [Description("Adds a DescriptionKeyword to the schema, using the provided description and the type's DescriptionAttribute if available.")]
        [Input("schema", "The JSON Schema to which the description will be added.")]
        [Input("type", "The type for which the description is being added. If the type has a DescriptionAttribute, its value will be included in the schema description.")]
        [Input("description", "The description text to be added to the schema. If null or empty, only the type's DescriptionAttribute will be used if available.")]
        public static void AddDescription(this oM.JsonSchema.JsonSchema schema, Type type, string description)
        {
            string typeDesc = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
            description = description ?? "";
            if (typeDesc != null)
            {
                description += $" {type.Name}: {typeDesc}";
            }
            description = description.Trim();

            if (!string.IsNullOrWhiteSpace(description))   //If description set, add to schema
                schema.Keywords.Add(new DescriptionKeyword { Description = description });
        }

        /*******************************************/
    }
}
