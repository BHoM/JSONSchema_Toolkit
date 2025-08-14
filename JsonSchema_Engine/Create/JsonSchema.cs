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

        public static oM.JsonSchema.JsonSchema JsonSchemaSingelKeyword(ISchemaKeyWord keyWord)
        {
            return new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord> { keyWord } };
        }

        /*******************************************/

        public static oM.JsonSchema.JsonSchema JsonSchema(SchemaType schemaType, bool addNullIfNullable = false)
        {
            return JsonSchemaSingelKeyword(Create.TypeKeyword(schemaType));
        }

        /*******************************************/

        public static oM.JsonSchema.JsonSchema JsonSchema(Type type)
        {
            return JsonSchemaSingelKeyword(Create.TypeKeyword(type));
        }

        /*******************************************/
    }
}
