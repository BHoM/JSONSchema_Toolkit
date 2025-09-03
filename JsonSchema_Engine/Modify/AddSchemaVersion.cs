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

        [Description("Adds a SchemaKeyword to the schema, setting the schema version to 2020-12. This is the latest version of JSON Schema and is used by default.")]
        [Input("schema", "The JSON Schema to which the schema version will be added.")]
        public static void AddSchemaVersion(this oM.JsonSchema.JsonSchema schema)
        {
            //Set the schema version to 2020-12
            //This is the latest version of JSON Schema and is used by default
            //Setting this as hardcoded here, and not in the config, as this is the latest version and should be used by default
            //Can be overridden in the config if needed at a later date.
            schema.Keywords.Add(new SchemaKeyword { Schema = "https://json-schema.org/draft/2020-12/schema" });
        }

        /*******************************************/
    }
}
