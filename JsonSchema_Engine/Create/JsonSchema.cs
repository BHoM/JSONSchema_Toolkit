using System;
using System.Collections.Generic;
using System.Text;
using BH.oM.JsonSchema;

namespace BH.Engine.JsonSchema
{
    public static partial class Create 
    {
        /*******************************************/
        /**** Public Methods                    ****/
        /*******************************************/

        public static oM.JsonSchema.JsonSchema JsonSchemaSingleKeyword(ISchemaKeyWord keyWord)
        {
            return new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord> { keyWord } };
        }

        /*******************************************/

        public static oM.JsonSchema.JsonSchema JsonSchema(SchemaType schemaType, bool addNullIfNullable = false)
        {
            return JsonSchemaSingleKeyword(Create.TypeKeyword(schemaType));
        }

        /*******************************************/

        public static oM.JsonSchema.JsonSchema JsonSchema(Type type)
        {
            return JsonSchemaSingleKeyword(Create.TypeKeyword(type));
        }

        /*******************************************/
    }
}
