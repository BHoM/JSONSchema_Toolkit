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

        [Description("Adds an ID keyword to the schema, based on the type's SchemaId method. The schema is not modified if no SchemaId cant be found.")]
        [Input("schema", "The JSON Schema to which the ID will be added.")]
        [Input("type", "The type for which the ID is being added. The type must implement IObject or be an enum in the BH.oM namespace.")]
        [Input("branchName", "The branch name to be used in the schema ID. This is typically the branch of the BHoM repository where the schema is located, e.g., 'deveop' or 'v8.2'.")]
        public static void AddId(this oM.JsonSchema.JsonSchema schema, Type type, string branchName)
        {
            var id = type.SchemaId(branchName);
            if (id != null)
            {
                schema.Keywords.Add(new IdKeyword() { Uri = id });
            }
        }

        /*******************************************/
    }
}
