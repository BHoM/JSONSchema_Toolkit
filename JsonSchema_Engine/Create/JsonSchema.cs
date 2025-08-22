using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using BH.oM.Base.Attributes;
using BH.oM.JsonSchema;

namespace BH.Engine.JsonSchema
{
    public static partial class Create 
    {
        /*******************************************/
        /**** Public Methods                    ****/
        /*******************************************/

        [Description("Creates a JSON Schema with a single keyword. This is typically used for simple schemas that only require one keyword, such as a type or item keyword.")]
        public static oM.JsonSchema.JsonSchema JsonSchemaSingleKeyword(ISchemaKeyWord keyWord)
        {
            return new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord> { keyWord } };
        }

        /*******************************************/

        [Description("Creates a JsonSchema with the type keyword set to the provided schema type")]
        public static oM.JsonSchema.JsonSchema JsonSchema(SchemaType schemaType, bool addNull = false)
        {
            return JsonSchemaSingleKeyword(Create.TypeKeyword(schemaType, addNull));
        }

        /*******************************************/

        [Description("Creates a JSON Schema with a reference to another schema by its type. If the type does not have a schema ID, returns null.")]
        public static oM.JsonSchema.JsonSchema JsonSchema(Type type)
        {
            return JsonSchemaSingleKeyword(Create.TypeKeyword(type));
        }

        /*******************************************/

        [Description("Creates a JSON Schema with a reference to another schema by its type and branch name. If the type does not have a schema ID, returns null.")]
        [Input("type", "The type for which the reference schema is being created. The type must implement IObject or be an enum in the BH.oM namespace.")]
        [Input("branchName", "The branch name to be used in the schema reference. This is typically the branch of the BHoM repository where the schema is located, e.g., 'develop' or 'v8.2'.")]
        [Input("description", "The description text to be added to the schema. If null or empty, only the type's DescriptionAttribute will be used if available.")]
        [Output("schema", "The JSON Schema with a reference to the specified type. If the type does not have a schema ID, returns null.")]
        public static oM.JsonSchema.JsonSchema RefJsonSchema(Type type, string branchName, string description)
        {
            var id = type.SchemaId(branchName);
            if (id != null)
            {
                var schema = new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord>() };
                schema.AddDescription(type, description);   //Add descriptions for refs so that they do show up on properties.
                schema.Keywords.Add(new RefKeyword() { Uri = id });
                return schema;
            }
            return null;
        }

        /*******************************************/
    }
}
